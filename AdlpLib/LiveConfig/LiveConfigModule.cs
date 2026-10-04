using AdlpLib.Core;
using AdlpLib.Core.Packet;
using AdlpLib.Core.Packet.Config;
using AdlpLib.Core.Packet.Status;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// Незавершённый под-запрос чтения/записи конфигурации
    /// </summary>
    internal class LiveConfigRequest
    {
        public int Tid;
        public IPEndPoint EP;
        public DateTime Sended;
        public int Timeout;
        public Action<ParamDataField[], bool> Callback;
    }

    /// <summary>
    /// Модуль чтения/записи конфигурации устройств наживую (COMMAND_GET_PARAM / COMMAND_SET_PARAM).
    /// Запросы сопоставляются по Tid, завершаются колбэком (значения, успешно).
    /// Чтение разбивается на под-запросы так, чтобы ожидаемый размер ответа не превышал
    /// буфер прошивки (1500 байт, аналог get_param_data_max_size / split_get_requests в C++).
    /// Результаты всех под-запросов объединяются и передаются одним колбэком.
    /// </summary>
    public class LiveConfigModule : Module
    {
        private readonly List<LiveConfigRequest> Requests = new List<LiveConfigRequest>();

        public override bool IsDeviceInteresting(SenderInfo Info, Status Status)
        {
            return false;
        }

        /// <summary>
        /// Разбить список элементов на пакеты запросов с учётом размера ответа.
        /// Аналог split_get_requests в C++ (configurator_base_module.cpp).
        /// </summary>
        public static List<GetParamItem[]> SplitGetRequests(GetParamItem[] Items)
        {
            var Result = new List<GetParamItem[]>();
            if (Items == null || Items.Length == 0)
                return Result;

            int maxPayload = 1500 - BasePacket.HeaderSize - ParamDataList.HeaderSize;

            // Помимо размера ответа в байтах, прошивка имеет жёсткие ограничения на КОЛИЧЕСТВО
            // полей одного типа в одном запросе (эмпирически на живом цветном ПУ):
            //   U16 — максимум 144 поля/запрос, U8 — 500, STR — 228.
            // Берём с запасом ~15%, чтобы устройство гарантированно обработало запрос.
            int maxFields = 120;
            int maxFieldsU8 = 400;
            int maxFieldsStr = 180;

            var Batch = new List<GetParamItem>();
            int current = 0;
            int countU8 = 0;
            int countStr = 0;
            foreach (var Item in Items)
            {
                int fieldSize = LiveConfigCodec.GetResponseFieldSize(Item.ExpectedType);
                bool isU8 = Item.ExpectedType == ParamDataType.PARAM_DATA_TYPE_U8 ||
                            Item.ExpectedType == ParamDataType.PARAM_DATA_TYPE_S8;
                bool isStr = Item.ExpectedType == ParamDataType.PARAM_DATA_TYPE_STR ||
                             Item.ExpectedType == ParamDataType.PARAM_DATA_TYPE_RAW;

                bool overflow = (Batch.Count + 1 > maxFields) ||
                                (isU8 && countU8 + 1 > maxFieldsU8) ||
                                (isStr && countStr + 1 > maxFieldsStr) ||
                                (current + fieldSize > maxPayload);
                if (overflow && Batch.Count > 0)
                {
                    Result.Add(Batch.ToArray());
                    Batch.Clear();
                    current = 0;
                    countU8 = 0;
                    countStr = 0;
                }
                Batch.Add(Item);
                current += fieldSize;
                if (isU8) countU8++;
                if (isStr) countStr++;
            }
            if (Batch.Count > 0)
                Result.Add(Batch.ToArray());

            return Result;
        }

        /// <summary>
        /// Прочитать параметры устройства. Запрос разбивается на под-запросы по размеру ответа.
        /// </summary>
        /// <param name="EP">Конечная точка устройства</param>
        /// <param name="Items">Параметры для чтения (идентификаторы + ожидаемые типы)</param>
        /// <param name="Callback">Колбэк (все поля всех под-запросов, успешно). null - при ошибке/таймауте.</param>
        /// <param name="Timeout">Таймаут, мс</param>
        /// <returns>true, если запросы отправлены</returns>
        public bool ReadParameters(IPEndPoint EP, GetParamItem[] Items, Action<ParamDataField[], bool> Callback, int Timeout = 4000)
        {
            if (Items == null || Items.Length == 0)
            {
                Callback?.Invoke(Array.Empty<ParamDataField>(), true);
                return true;
            }
            if (AdlpCore == null)
                return false;

            var SubRequests = SplitGetRequests(Items);
            if (SubRequests.Count == 0)
                return false;

            var Collected = new List<ParamDataField>();
            int Pending = SubRequests.Count;
            bool Failed = false;

            foreach (var Sub in SubRequests)
            {
                var Ids = Sub.Select(I => I.Id).ToArray();
                int Tid = AdlpCore.SendRequest(EP, PacketCode.COMMAND_GET_PARAM, new ParamDataRequestList(Ids).getPacket());
                if (Tid < 0)
                {
                    Failed = true;
                    continue;
                }

                lock (Requests)
                {
                    Requests.Add(new LiveConfigRequest
                    {
                        Tid = Tid,
                        EP = EP,
                        Sended = DateTime.Now,
                        Timeout = Timeout,
                        Callback = (fields, ok) =>
                        {
                            lock (Collected)
                            {
                                Pending--;
                                if (!ok)
                                    Failed = true;
                                else if (fields != null)
                                    Collected.AddRange(fields);
                            }
                            if (Pending == 0)
                                Callback?.Invoke(Collected.ToArray(), !Failed);
                        }
                    });
                }
            }

            return true;
        }

        /// <summary>
        /// Прочитать параметры устройства последовательными под-запросами
        /// (следующий — только после ответа на текущий). Для больших списков параметров
        /// (например, конфигурация ПУ с группами и кнопками), чтобы не заваливать
        /// устройство параллельными запросами.
        /// </summary>
        /// <param name="EP">Конечная точка устройства</param>
        /// <param name="Items">Параметры для чтения (идентификаторы + ожидаемые типы)</param>
        /// <param name="Callback">Колбэк (все поля всех под-запросов, успешно). null - при ошибке/таймауте.</param>
        /// <param name="Timeout">Таймаут, мс</param>
        /// <returns>true, если запросы отправлены</returns>
        public bool ReadParametersSequential(IPEndPoint EP, GetParamItem[] Items, Action<ParamDataField[], bool> Callback, int Timeout = 4000)
        {
            if (Items == null || Items.Length == 0)
            {
                Callback?.Invoke(Array.Empty<ParamDataField>(), true);
                return true;
            }
            if (AdlpCore == null)
                return false;

            var SubRequests = SplitGetRequests(Items);
            if (SubRequests.Count == 0)
                return false;

            var Collected = new List<ParamDataField>();

            void SendNext(int Index)
            {
                var Sub = SubRequests[Index];
                var Ids = Sub.Select(I => I.Id).ToArray();

                int Tid = AdlpCore.SendRequest(EP, PacketCode.COMMAND_GET_PARAM, new ParamDataRequestList(Ids).getPacket());
                if (Tid < 0)
                {
                    Callback?.Invoke(Array.Empty<ParamDataField>(), false);
                    return;
                }

                lock (Requests)
                {
                    Requests.Add(new LiveConfigRequest
                    {
                        Tid = Tid,
                        EP = EP,
                        Sended = DateTime.Now,
                        Timeout = Timeout,
                        Callback = (fields, ok) =>
                        {
                            if (!ok)
                            {
                                Callback?.Invoke(Array.Empty<ParamDataField>(), false);
                                return;
                            }
                            if (fields != null)
                            {
                                lock (Collected)
                                    Collected.AddRange(fields);
                            }
                            if (Index + 1 >= SubRequests.Count)
                            {
                                Callback?.Invoke(Collected.ToArray(), true);
                                return;
                            }
                            Thread.Sleep(10);
                            SendNext(Index + 1);
                        }
                    });
                }
            }

            SendNext(0);
            return true;
        }

        /// <summary>
        /// Записать параметры устройства. Список разбивается на пакеты по размеру полезной нагрузки.
        /// Под-запросы отправляются последовательно: следующий — только после ответа на текущий
        /// (прошивка обрабатывает COMMAND_SET_PARAM по одному, как и эталонный конфигуратор).
        /// </summary>
        /// <param name="EP">Конечная точка устройства</param>
        /// <param name="Fields">Параметры для записи</param>
        /// <param name="Callback">Колбэк (значения-эхо, успешно). null - при ошибке/таймауте.</param>
        /// <param name="Timeout">Таймаут, мс</param>
        /// <returns>true, если запросы отправлены</returns>
        public bool WriteParameters(IPEndPoint EP, ParamDataField[] Fields, Action<ParamDataField[], bool> Callback, int Timeout = 4000)
        {
            if (Fields == null || Fields.Length == 0)
            {
                Callback?.Invoke(Array.Empty<ParamDataField>(), true);
                return true;
            }
            if (AdlpCore == null)
                return false;

            var SubRequests = SplitWriteFields(Fields);
            if (SubRequests.Count == 0)
                return false;

            void SendNext(int Index)
            {
                var Sub = SubRequests[Index];

                int Tid = AdlpCore.SendRequest(EP, PacketCode.COMMAND_SET_PARAM, new ParamDataList(Sub).getPacket());
                if (Tid < 0)
                {
                    Callback?.Invoke(Array.Empty<ParamDataField>(), false);
                    return;
                }

                lock (Requests)
                {
                    Requests.Add(new LiveConfigRequest
                    {
                        Tid = Tid,
                        EP = EP,
                        Sended = DateTime.Now,
                        Timeout = Timeout,
                        Callback = (fields, ok) =>
                        {
                            if (!ok || Index + 1 >= SubRequests.Count)
                            {
                                Callback?.Invoke(Array.Empty<ParamDataField>(), ok);
                                return;
                            }
                            SendNext(Index + 1);
                        }
                    });
                }
            }

            SendNext(0);
            return true;
        }

        /// <summary>
        /// Разбить список полей записи на пакеты по максимальному размеру полезной нагрузки.
        /// Лимит для записи ниже, чем для чтения: это исходящий UDP-датаграмма. Чтобы не было
        /// IP-фрагментации (MTU 1500), полезная нагрузка SET_PARAM не должна превышать
        /// 1472 байт (1500 - IP20 - UDP8); берём 1400 с запасом на VLAN/PPPoE.
        /// </summary>
        public static List<ParamDataField[]> SplitWriteFields(ParamDataField[] Fields)
        {
            var Result = new List<ParamDataField[]>();
            int maxPayload = 1400 - BasePacket.HeaderSize - ParamDataList.HeaderSize;

            var Batch = new List<ParamDataField>();
            int current = 0;
            foreach (var F in Fields)
            {
                int fieldSize = F.getLength();
                if ((current + fieldSize > maxPayload) && (Batch.Count > 0))
                {
                    Result.Add(Batch.ToArray());
                    Batch.Clear();
                    current = 0;
                }
                Batch.Add(F);
                current += fieldSize;
            }
            if (Batch.Count > 0)
                Result.Add(Batch.ToArray());

            return Result;
        }

        /// <summary>
        /// Обработка ответа от устройства
        /// </summary>
        public override bool OnPacket(SenderInfo Info, BasePacket Packet)
        {
            bool Response = Packet.Flags.HasFlag(HeaderFlags.ADLPDS_HEADER_FLAG_RESPONSE);
            if (!Response)
                return false;

            if (Packet.Command != PacketCode.COMMAND_GET_PARAM && Packet.Command != PacketCode.COMMAND_SET_PARAM)
                return false;

            LiveConfigRequest Req = null;
            lock (Requests)
            {
                Req = Requests.FirstOrDefault(R => R.Tid == Info.Tid && R.EP.Equals(Info.Endpoint));
                if (Req != null)
                    Requests.Remove(Req);
            }

            if (Req == null)
                return false;

            try
            {
                var List = new ParamDataList(Packet.Payload);
                Req.Callback?.Invoke(List.Fields, true);
            }
            catch (Exception ex)
            {
                // Ответ не является списком параметров (например, устройства подтверждает
                // запись пустым/иным форматом) — не блокируем завершение запроса.
                Debug.WriteLine($"LiveConfigModule.OnPacket: не удалось разобрать ответ {Packet.Command}: {ex.Message}");
                Req.Callback?.Invoke(Array.Empty<ParamDataField>(), true);
            }

            return true;
        }

        /// <summary>
        /// Обработка таймаутов
        /// </summary>
        public override void Process()
        {
            LiveConfigRequest[] Expired;
            lock (Requests)
            {
                Expired = Requests.Where(R => (DateTime.Now - R.Sended).TotalMilliseconds > R.Timeout).ToArray();
                foreach (var R in Expired)
                    Requests.Remove(R);
            }

            foreach (var R in Expired)
            {
                try
                {
                    R.Callback?.Invoke(null, false);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Отменить все незавершённые запросы
        /// </summary>
        public void CancelAll()
        {
            LiveConfigRequest[] All;
            lock (Requests)
            {
                All = Requests.ToArray();
                Requests.Clear();
            }

            foreach (var R in All)
            {
                try
                {
                    R.Callback?.Invoke(null, false);
                }
                catch
                {
                }
            }
        }
    }
}
