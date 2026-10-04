    using AdlpLib.Core.Modules.Types;
    using AdlpLib.Core.Packet.Config;
    using AdlpLib.Core.Packet.Firmware;
    using AdlpLib.Core.Packet.Status;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    namespace AdlpLib.Core.Modules
    {
        public class ConfiguratorBaseModule : Module
        {
            private List<ConfiguredDevice> CfgDevices = new List<ConfiguredDevice>();
            private List<FirmwaredDevice> FwDevices = new List<FirmwaredDevice>();

            /// <summary>Ожидающие ответа на GET_PARAM запросы (Tid → контекст)</summary>
            private readonly Dictionary<int, PendingGet> mPending = new();

            /// <summary>Таймаут ожидания ответа на GET_PARAM, мс</summary>
            private const long GET_PARAM_TIMEOUT_MS = 1000;

            private sealed class PendingGet
            {
                public IPEndPoint Endpoint;
                public Action<ParamDataField[], bool> Callback;
                public long SentAt;
            }

            private string ForceName = "";
            private int ForceIndex = 0;

            /// <summary>
            /// Получить возможности модуля
            /// </summary>
            /// <returns></returns>
            public override BlockCapability GetCapabilities()
            {
                return BlockCapability.BLOCKCAP_CFG;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            public ConfiguratorBaseModule()
            {

            }

            /// <summary>
            /// Является ли устройство интересным для нас
            /// </summary>
            /// <param name="Info">Информация об отправителе</param>
            /// <param name="Status">Статус</param>
            /// <returns></returns>
            public override bool IsDeviceInteresting(SenderInfo Info, Status Status)
            {
                return true;
            }

            /// <summary>
            /// Получить метку времени
            /// </summary>
            /// <returns></returns>
            private StatusField GetTimesyncStatus()
            {
                return new StatusField(StatusFieldType.STATUS_FIELD_TIMESYNC, new TimeSyncField().getPacket());
            }

            /// <summary>
            /// Получить поля статуса
            /// </summary>
            /// <returns></returns>
            public override List<StatusField> GetStatusFields()
            {
                var Fields = base.GetStatusFields();
                Fields.Add(GetTimesyncStatus());
                return Fields;
            }

            /// <summary>
            /// Есть ли прошивки
            /// </summary>
            /// <param name="EP"></param>
            /// <returns></returns>
            protected virtual bool HasFirmwares()
            {
                return false;
            }

            /// <summary>
            /// Надо ли его обновить по IP
            /// </summary>
            /// <param name="EP"></param>
            /// <returns></returns>
            protected virtual bool IsForcedDevice(IPEndPoint EP)
            {
                return false;
            }

            /// <summary>
            /// Обработать основное сообщение статуса
            /// </summary>
            /// <param name="Info">Информация об отправителе</param>
            /// <param name="Status">Данные поля статуса</param>
            /// <returns></returns>
            public override bool ProcessDeviceStatus(SenderInfo Info, Status Status)
            {
                // Проверим, нуждается ли устройство в конфигурации
                if(Status.Flags.HasFlag(Status.StatusFlags.STATUS_FLAG_CONFIGURATION_REQUEST) || IsForcedDevice(Info.Endpoint))
                {
                    // Получим объект для этого устройства
                    var Dev = GetConfiguredDevice(Info, Status.Uuid);

                    // Объект создан и может теперь по очереди настраиваться, а статус обновления хранится в объекте класса...
                }

                if (HasFirmwares())
                {
                    if (Status.Flags.HasFlag(Status.StatusFlags.STATUS_FLAG_FIRMWARE_REQUEST))
                    {
                        // Получим объект для этого устройства
                        var Dev = GetFirmwaredDevice(Info, Status.Uuid);

                        // Объект создан и может теперь по очереди настраиваться, а статус обновления хранится в объекте класса...
                    }
                }
                return true;
            }

            /// <summary>
            /// Создание объекта для слежением за прошивкой устройства
            /// </summary>
            /// <param name="Device"></param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <returns></returns>
            virtual protected FirmwaredDevice CreateFirmwaredDevice(Device Device, Guid Uuid)
            {
                return CreateFirmwaredDevice(Device, Uuid, true);
            }

            /// <summary>
            /// Создание объекта для слежением за прошивкой устройства
            /// </summary>
            /// <param name="Device">Информация об устройстве</param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <param name="sendDetect">Выполнить запросы определения устройства (тип/версия/производитель).
            /// Для активной прошивки определение не нужно: тип известен по сканированию.</param>
            /// <returns></returns>
            virtual protected FirmwaredDevice CreateFirmwaredDevice(Device Device, Guid Uuid, bool sendDetect)
            {
                return new FirmwaredDevice(Device, Uuid, sendDetect);
            }

            /// <summary>
            /// Определить параметры устройства (тип/версия ПО/производитель) командой FIRMWARE_COMMAND_DETECT.
            /// Результат приходит асинхронно: тип — индекс 0, версия ПО — индекс 1, производитель — индекс 2.
            /// После определения объект автоматически удаляется из списка отслеживания.
            /// </summary>
            /// <param name="Device">Информация об устройстве</param>
            /// <returns>Объект слежения или null, если для устройства уже выполняется прошивка или определение</returns>
            public FirmwaredDevice ProbeFirmwareDevice(Device Device)
            {
                lock (FwDevices)
                {
                    foreach (var Existing in FwDevices)
                    {
                        if (Existing.Check(Device.Uuid))
                            return null;
                    }

                    var NewDev = CreateFirmwaredDevice(Device, Device.Uuid, true);
                    if (NewDev == null)
                        return null;

                    NewDev.IsProbe = true;
                    FwDevices.Add(NewDev);

                    Console.WriteLine($"Probe firmware device {Device.Name} {Device.Block} UUID={Device.Uuid} ({Device.Endpoint})");
                    return NewDev;
                }
            }

            /// <summary>
            /// Создание объекта для слежением за конфигурированием устройства
            /// </summary>
            /// <param name="Device"></param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <returns></returns>
            virtual protected ConfiguredDevice CreateDevice(Device Device, Guid Uuid)
            {
                var Req = GetRequestsByUuid(Device.Endpoint, Uuid);

                if(Req != null)
                    return new ConfiguredDevice(Device, Uuid, Req);
                else
                    return null;
            }

            /// <summary>
            /// Функция для получения конфигурации устройства по его UUID
            /// </summary>
            /// <param name="Uuid"></param>
            /// <returns></returns>
            protected virtual ConfigurationRequest[] GetRequestsByUuid(IPEndPoint EP, Guid Uuid)
            {
                return null;
            }

            /// <summary>
            /// Получить объект конфигурируемого устройства из списка имеющихся
            /// </summary>
            /// <param name="Info"></param>
            /// <returns></returns>
            private ConfiguredDevice GetConfiguredDeviceByInfo(SenderInfo Info)
            {
                lock (CfgDevices)
                {
                    // Проверим текущий список...
                    foreach (var Device in CfgDevices)
                    {
                        if (Device.Check(Info.Endpoint))
                            return Device;
                    }
                }

                return null;
            }

            /// <summary>
            /// Получить объект конфигурируемого устройства
            /// </summary>
            /// <param name="Status">Данные поля статуса</param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <returns></returns>
            private ConfiguredDevice GetConfiguredDevice(SenderInfo Info, Guid Uuid)
            {
                lock (CfgDevices)
                {
                    // Проверим текущий список...
                    foreach (var Device in CfgDevices)
                    {
                        if (Device.Check(Info.Endpoint, Uuid))
                            return Device;
                    }

                    // Создадим новое устройство слежения
                    var Dev = AdlpCore.GetDeviceByInfo(Info);
                    if (Dev != null)
                    {
                        var NewDev = CreateDevice(Dev, Uuid);

                        if (NewDev != null)
                        {
                            Console.WriteLine($"Added device {Dev.Name} {Dev.Block} UUID={Dev.Uuid} ({Dev.Endpoint})" );
                            CfgDevices.Add(NewDev);
                            return NewDev;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Получить объект прошиваемого устройства из списка имеющихся
            /// </summary>
            /// <param name="Info"></param>
            /// <returns></returns>
            private FirmwaredDevice GetFirmwaredDeviceByInfo(SenderInfo Info)
            {
                lock (FwDevices)
                {
                    // Проверим текущий список...
                    foreach (var Device in FwDevices)
                    {
                        if (Device.Check(Info.Endpoint))
                            return Device;
                    }
                }

                return null;
            }

            /// <summary>
            /// Получить объект прошиваемого устройства
            /// </summary>
            /// <param name="Status">Данные поля статуса</param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <returns></returns>
            private FirmwaredDevice GetFirmwaredDevice(SenderInfo Info, Guid Uuid)
            {
                lock (FwDevices)
                {
                    // Проверим текущий список...
                    foreach (var Device in FwDevices)
                    {
                        if (Device.Check(Uuid))
                            return Device;
                    }

                    // Создадим новое устройство слежения
                    var Dev = AdlpCore.GetDeviceByInfo(Info);
                    if (Dev != null)
                    {
                        var NewDev = CreateFirmwaredDevice(Dev, Uuid);

                        if (NewDev != null)
                        {
                            Console.WriteLine($"Added fw device {Dev.Name} {Dev.Block} UUID={Dev.Uuid} ({Dev.Endpoint})");
                            FwDevices.Add(NewDev);
                            return NewDev;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Задать параметры устройства
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="Fileds"></param>
            /// <returns>Номер запроса</returns>
            public int SetParameters(IPEndPoint EP, ParamDataField[] Fields)
            {
                var Req = new ParamDataList(Fields);
                return AdlpCore.SendRequest(EP, PacketCode.COMMAND_SET_PARAM, Req.getPacket());
            }

            public int GetParameters(IPEndPoint EP, ParamDataId[] Fields,
                         Action<ParamDataField[], bool> onResult)
            {
                var Req = new ParamDataRequestList(Fields);          

                int tid = AdlpCore.SendRequest(EP, PacketCode.COMMAND_GET_PARAM, Req.getPacket());
                if(tid < 0)
                {
                    onResult?.Invoke(Array.Empty<ParamDataField>(), false);
                    return -1;
                }

                lock(mPending)
                {
                    mPending[tid] = new PendingGet
                    {
                        Endpoint = EP,
                        Callback = onResult,
                        SentAt = Milliseconds
                    };
                }
                return tid;
            }

        /// <summary>
        /// Пакет обновления ПО
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Fileds"></param>
        /// <returns>Номер запроса</returns>
        public int SendFirmwareRequest(IPEndPoint EP, Packet.Firmware.FirmwareRequest Request)
            {
                return AdlpCore.SendRequest(EP, PacketCode.COMMAND_FIRMWARE, Request.getPacket());
            }

            /// <summary>
            /// Активно прошить устройство (без ожидания запроса прошивки со стороны устройства).
            /// Детект устройства и запись прошивки выполняются автоматически в фоновом процессе.
            /// </summary>
            /// <param name="EP">Адрес устройства</param>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <param name="Fw">Прошивка</param>
            /// <returns>Объект слежения за прошивкой или null, если устройство уже прошивается</returns>
            public FirmwaredDevice FirmwareDevice(IPEndPoint EP, Guid Uuid, Firmware Fw)
            {
                if (Fw == null)
                    throw new ArgumentNullException(nameof(Fw));

                lock (FwDevices)
                {
                    // Уже прошивается — не начинаем повторно
                    foreach (var Existing in FwDevices)
                    {
                        if (Existing.Check(Uuid))
                        {
                            if (Existing.IsProbe)
                            {
                                // Определение ещё не закончилось — оно больше не нужно, начинаем активную прошивку
                                FwDevices.Remove(Existing);
                                break;
                            }
                            if (!Existing.IsCompleted())
                                return null;
                            FwDevices.Remove(Existing);
                            break;
                        }
                    }

                    var Dev = AdlpCore.GetDeviceByInfo(new SenderInfo(EP, 0));
                    if (Dev == null)
                        return null;

                    var NewDev = CreateFirmwaredDevice(Dev, Uuid, false);
                    if (NewDev == null)
                        return null;

                    FwDevices.Add(NewDev);
                    NewDev.SetFirmware(Fw);

                    Console.WriteLine($"Force firmware {Fw.DeviceVersion} for {Dev.Name} {Dev.Block} UUID={Dev.Uuid} ({Dev.Endpoint})");
                    return NewDev;
                }
            }

            /// <summary>
            /// Получить объект слежения за прошивкой устройства по UUID
            /// </summary>
            /// <param name="Uuid">Идентификатор устройства</param>
            /// <returns>Объект или null, если прошивка не запущена</returns>
            public FirmwaredDevice GetFirmwaredDevice(Guid Uuid)
            {
                lock (FwDevices)
                {
                    foreach (var Dev in FwDevices)
                    {
                        if (Dev.Check(Uuid))
                            return Dev;
                    }
                }
                return null;
            }

            /// <summary>
            /// Обработать ответ от устройства...
            /// </summary>
            /// <param name="Info"></param>
            /// <param name="Params"></param>
            /// <returns></returns>
            private bool ProcessSetParamResponse(SenderInfo Info, ParamDataList Params)
            {
                var Device = GetConfiguredDeviceByInfo(Info);
                if (Device != null)
                    Device.ProcessResponse(Params);

                // True только если ответ адресован отслеживаемому устройству.
                // Иначе не перехватываем чужой ответ (например, LiveConfigModule).
                return Device != null;
            }

            /// <summary>
            /// Обработать ответ от устройства...
            /// </summary>
            /// <param name="Info"></param>
            /// <param name="Params"></param>
            /// <returns></returns>
            private bool ProcessFirmwareResponse(SenderInfo Info, FirmwareResponse Resp)
            {
                var Device = GetFirmwaredDeviceByInfo(Info);
                if (Device != null)
                    Device.ProcessResponse(Resp, Info.Tid);

                // True только если ответ адресован отслеживаемому устройству.
                // Иначе не перехватываем чужой ответ (например, LiveConfigModule).
                return Device != null;
            }

            /// <summary>
            /// Обработка пакета [событие ядра]
            /// </summary>
            /// <param name="Info">Информация об отправителе</param>
            /// <param name="Packet">Пакет</param>
            /// <returns>true, если обработано</returns>
            public override bool OnPacket(SenderInfo Info, Packet.BasePacket Packet)
            {
                if (base.OnPacket(Info, Packet))
                    return true;

                bool Response = Packet.Flags.HasFlag(AdlpLib.Core.Packet.HeaderFlags.ADLPDS_HEADER_FLAG_RESPONSE);

                if (Response)
                {
                    switch (Packet.Command)
                    {
                    case PacketCode.COMMAND_GET_PARAM:
                        System.Diagnostics.Debug.WriteLine(
                            $"[GET_PARAM RESP] len={Packet.Payload.Length}, " +
                            $"hex={BitConverter.ToString(Packet.Payload)}");
                        return ProcessGetParamResponse(Info, Packet.Tid, new ParamDataList(Packet.Payload));
                    case PacketCode.COMMAND_SET_PARAM: return ProcessSetParamResponse(Info, new ParamDataList(Packet.Payload));
                        case PacketCode.COMMAND_FIRMWARE: return ProcessFirmwareResponse(Info, new FirmwareResponse(Packet.Payload));
                        default: return false;
                    }
                }
                else
                    return false;
            }

            /// <summary>
            /// Доставка ответа на GET_PARAM конкретному ожидающему запросу по Tid.
            /// </summary>
            private bool ProcessGetParamResponse(SenderInfo Info, int Tid, ParamDataList Params)
            {
                PendingGet Pending;
                lock(mPending)
                {
                    if(!mPending.TryGetValue(Tid, out Pending))
                        return false;               // не наш ответ — не перехватываем
                    mPending.Remove(Tid);
                }

                Pending.Callback?.Invoke(Params.Fields ?? Array.Empty<ParamDataField>(), true);
                return true;
            }

            /// <summary>
            /// Получить список конфигурируемых устройств
            /// </summary>
            /// <returns></returns>
            private ConfiguredDevice[] GetConfiguredDevices()
            {
                lock(CfgDevices)
                {
                    return CfgDevices.ToArray();
                }
            }

            /// <summary>
            /// Изменить состояние таблицы, уберём оттуда всё лишнее...
            /// </summary>
            private void UpdateConfiguredDevicesTable()
            {
                lock (CfgDevices)
                {
                    foreach(var Dev in CfgDevices)
                    {
                        if(Dev.IsCompleted())
                            Console.WriteLine($"Removed device {Dev.Device.Name} {Dev.Device.Block} UUID={Dev.Device.Uuid} ({Dev.Device.Endpoint})");
                    }
                    CfgDevices.RemoveAll(Dev => Dev.IsCompleted());
                }
            }

            /// <summary>
            /// Получить список прошиваемых устройств
            /// </summary>
            /// <returns></returns>
            private FirmwaredDevice[] GetFirmwaredDevices()
            {
                lock (FwDevices)
                {
                    return FwDevices.ToArray();
                }
            }

            /// <summary>
            /// Изменить состояние таблицы, уберём оттуда всё лишнее...
            /// </summary>
            private void UpdateFirmwaredDevicesTable()
            {
                lock (FwDevices)
                {
                    foreach (var Dev in FwDevices)
                    {
                        if (!Dev.IsProcessed())
                        {
                            if (Dev.IsCompletedOk())
                            {
                                Console.WriteLine($"Firmware is upgraded on  {Dev.Device.Name} {Dev.Device.Block} UUID={Dev.Device.Uuid} ({Dev.Device.Endpoint})");
                                RemoveFirmwareRequest(Dev);

                                Dev.MarkProcessed(Milliseconds);
                            }
                            else if (Dev.IsCompleted())
                            {
                                Console.WriteLine($"Firmware is not upgraded on {Dev.Device.Name} {Dev.Device.Block} UUID={Dev.Device.Uuid} ({Dev.Device.Endpoint})");
                                RemoveFirmwareRequest(Dev);

                                Dev.MarkProcessed(Milliseconds);
                            }
                        }
                    }

                    FwDevices.RemoveAll(Dev => Dev.IsCompletedWithTimeout(Milliseconds));
                }
            }

            /// <summary>
            /// Раз в тик проверяем, не истекли ли ожидания GET_PARAM.
            /// </summary>
            private void ProcessGetParamTimeouts()
            {
                List<PendingGet> Expired = null;

                lock(mPending)
                {
                    if(mPending.Count == 0) return;

                    long Now = Milliseconds;
                    List<int> DeadTids = null;

                    foreach(var kv in mPending)
                    {
                        if(Now - kv.Value.SentAt > GET_PARAM_TIMEOUT_MS)
                            (DeadTids ??= new List<int>()).Add(kv.Key);
                    }

                    if(DeadTids == null) return;

                    Expired = new List<PendingGet>(DeadTids.Count);
                    foreach(var tid in DeadTids)
                    {
                        Expired.Add(mPending[tid]);
                        mPending.Remove(tid);
                    }
                }

                // Колбэки — уже вне lock, чтобы не держать мьютекс на чужом коде
                foreach(var P in Expired)
                    P.Callback?.Invoke(Array.Empty<ParamDataField>(), false);
            }


            /// <summary>
            /// Обработать таблицу конфигурируемых устройств
            /// </summary>
            private void ProcessConfiguredDevices()
            {
                var Devs = GetConfiguredDevices();
                foreach (var Dev in Devs)
                {
                    Dev.Process();

                    if (Dev.HasParamsToSend())
                    {
                        var Request = Dev.GetNextRequest();

                        int Tid = SetParameters(Dev.Endpoint, Request.Fields);
                        if (Tid >= 0)
                        {
                            // Запрос успешно отправлен, следим за идентификатором пакета в ответах...
                            Request.Tid = Tid;
                        }
                        else
                        {
                            Request.ResultError($"Cannot send request to {Dev.Device.Endpoint}!");
                        }
                    }
                }
            }

            /// <summary>
            /// Отключить запрос прошивки
            /// </summary>
            /// <param name="Dev"></param>
            private void RemoveFirmwareRequest(FirmwaredDevice Dev)
            {
                // Отключим запрос прошивки
                SetParameters(Dev.Endpoint, new ParamDataField[] {
                                    new ParamDataField(new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceFirmware),
                                    new ParamDataValue((byte)0)) });
            }

            /// <summary>
            /// Обработать таблицу прошиваемых устройств
            /// </summary>
            private void ProcessFirmwaredDevices()
            {
                var Devs = GetFirmwaredDevices();
                foreach (var Dev in Devs)
                {
                    Dev.Process();

                    if (Dev.HasRequestsToSend())
                    {
                        var Request = Dev.GetNextRequest();

                        int Tid = SendFirmwareRequest(Dev.Endpoint, Request.Request);
                        if (Tid >= 0)
                        {
                            // Запрос успешно отправлен, следим за идентификатором пакета в ответах...
                            Request.Tid = Tid;
                            Thread.Sleep(10);
                        }
                        else
                        {
                            Request.ResultError("Cannot send request!");
                        }
                    }
                    else if(Dev.IsStepCompleted())
                    {
                        Dev.NextStep();
                        if (Dev.IsProbe)
                        {
                            // Определение завершено (или не удалось) — больше не следим
                            lock (FwDevices) FwDevices.Remove(Dev);
                            continue;
                        }
                        if(Dev.IsFirmwareRequested())
                        {
                            Console.WriteLine($"Detected device {Dev.Device.Name} {Dev.Device.Block} TYPE={Dev.DeviceName} VERSION={Dev.DeviceVersion} MAN={Dev.DeviceManufacturer}");

                            bool Ok = false;
                            var Fw = GetFirmware(Dev.DeviceName, Dev.DeviceVersion);
                            if(Fw != null)
                            {
                                if (Fw.DeviceVersion != Dev.DeviceVersion)
                                {
                                    Console.WriteLine($"Found firmware for {Dev.Device.Name} {Dev.Device.Block}: {Dev.DeviceVersion} ==> {Fw.DeviceVersion}");
                                    Dev.SetFirmware(Fw);
                                    Ok = true;
                                }
                                else
                                    Console.WriteLine($"Firmware is same version for {Dev.Device.Name} {Dev.Device.Block}");
                            }
                            else
                                Console.WriteLine($"Firmware is not found for {Dev.Device.Name} {Dev.Device.Block}");

                            /*if(!Ok)
                            {
                                RemoveFirmwareRequest(Dev);
                            }*/
                        }
                    }
                }
            }

            /// <summary>
            /// Получить ПО по названию платы
            /// </summary>
            /// <param name="Type"></param>
            /// <param name="Version"></param>
            /// <returns></returns>
            virtual protected Firmware GetFirmware(string Type, string Version)
            {
                return null;
            }

            /// <summary>
            /// Обработаем таблицу конфинурируемых устройств
            /// </summary>
            public override void Process()
            {
                base.Process();

                ProcessGetParamTimeouts();

                ProcessConfiguredDevices();
                ProcessFirmwaredDevices();

                UpdateConfiguredDevicesTable();
                UpdateFirmwaredDevicesTable();
            }
        }
    }
