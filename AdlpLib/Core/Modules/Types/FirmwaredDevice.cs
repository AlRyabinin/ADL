using AdlpLib.Core.Packet.Config;
using AdlpLib.Core.Packet.Firmware;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Modules.Types
{
    public class FirmwaredDevice
    {
        /// <summary>
        /// Информация об устройстве
        /// </summary>
        public readonly Device Device;

        /// <summary>
        /// Уникальный идентификатор устройства
        /// </summary>
        protected Guid Uuid;

        /// <summary>
        /// Обработано
        /// </summary>
        private bool Processed = false;

        /// <summary>
        /// Время обработки
        /// </summary>
        private long ProcessedTime = 0;

        /// <summary>
        /// 
        /// </summary>
        private FirmwareStep Step = FirmwareStep.STEP_DETECT;

        /// <summary>
        /// Куда отправлять запросы
        /// </summary>
        public IPEndPoint Endpoint => Device.Endpoint;

        /// <summary>
        /// ПО
        /// </summary>
        protected Firmware Firmware = null;

        /// <summary>
        /// Список запросов
        /// </summary>
        private Queue<FirmwareRequest> UpdateRequests = new Queue<FirmwareRequest>();

        /// <summary>
        /// Данные ПО
        /// </summary>
        protected byte[] Data;

        /// <summary>
        /// Актуальнй запрос
        /// </summary>
        private FirmwareRequest Request;

        /// <summary>
        /// Повторный запрос
        /// </summary>
        private FirmwareRequest RepeatRequest;


        /// <summary>
        /// Счётчик повторов
        /// </summary>
        private int RepeatRequestCount = 10;

        /// <summary>
        /// Тип текущего устройства
        /// </summary>
        public string DeviceName { get; private set; } = null;

        /// <summary>
        /// Версия текущего устройства
        /// </summary>
        public string DeviceVersion { get; private set; } = null;

        /// <summary>
        /// Производитель текущего устройства
        /// </summary>
        public string DeviceManufacturer { get; private set; } = null;

        /// <summary>
        /// Устройство только для определения параметров (тип/версия/производитель), прошивка не выполняется
        /// </summary>
        public bool IsProbe { get; set; } = false;

        /// <summary>
        /// Сколько байт прошивки уже записано
        /// </summary>
        public int CompleteBytes { get; private set; } = 0;

        /// <summary>
        /// Размер прошивки в байтах
        /// </summary>
        public int DataLength => Data?.Length ?? 0;

        /// <summary>
        /// Прогресс записи прошивки (0..100)
        /// </summary>
        public int ProgressPercent
        {
            get
            {
                if (Data == null || Data.Length == 0)
                    return 0;
                return Math.Max(0, Math.Min(100, CompleteBytes * 100 / Data.Length));
            }
        }

        /// <summary>
        /// Идёт ли процесс прошивки (детект/запись)
        /// </summary>
        public bool IsBusy => !IsCompleted() && !IsCompletedOk();

        /// <summary>
        /// Текстовое описание текущего шага
        /// </summary>
        public string StateText
        {
            get
            {
                switch (Step)
                {
                    case FirmwareStep.STEP_DETECT: return IsBusy ? "Определение устройства..." : "Идёт определение";
                    case FirmwareStep.STEP_UPGRADE: return IsBusy ? "Идёт обновление..." : "Подготовка записи";
                    case FirmwareStep.STEP_WORK: return "Запись прошивки...";
                    case FirmwareStep.STEP_COMPLETED_OK: return "Прошивка успешно завершена";
                    case FirmwareStep.STEP_COMPLETED: return "Прошивка завершена с ошибкой";
                    default: return "Неизвестное состояние";
                }
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <param name="Uuid">Уникальный идентификатор</param>
        public FirmwaredDevice(Device Device, Guid Uuid) : this(Device, Uuid, true)
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <param name="Uuid">Уникальный идентификатор</param>
        /// <param name="sendDetect">Отправить запросы определения устройства (тип/версия/производитель).
        /// При активной прошивке тип уже известен по сканированию, поэтому определение можно пропустить.</param>
        public FirmwaredDevice(Device Device, Guid Uuid, bool sendDetect)
        {
            this.Device = Device;
            this.Uuid = Uuid;

            Step = sendDetect ? FirmwareStep.STEP_DETECT : FirmwareStep.STEP_UPGRADE;

            if (sendDetect)
            {
                UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_DETECT, 0, new byte[] { }, 1000));
                UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_DETECT, 1, new byte[] { }, 1000));
                UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_DETECT, 2, new byte[] { }, 1000));
            }
        }

        /// <summary>
        /// Проверка устройства
        /// </summary>
        /// <param name="EP"></param>
        /// <returns></returns>
        public bool Check(IPEndPoint Endpoint) => Device.CheckAddress(Endpoint);

        /// <summary>
        /// Проверка устройства
        /// </summary>
        /// <param name="EP"></param>
        /// <returns></returns>
        public bool Check(Guid UUID) => Uuid.Equals(UUID);

        /// <summary>
        /// Обработать ответ от устройства
        /// </summary>
        /// <param name="Params"></param>
        virtual internal void ProcessResponse(FirmwareResponse Response, int Tid)
        {
            if (Request != null)
            {
                //Console.WriteLine($"Response: {Response.Code} on command {Request.Request.Command} {Tid}");

                if (Request.Tid != Tid)
                    return;

                if (Response.Code == FirmwareResponseCode.FIRMWARE_RESPONSE_OK)
                {
                    if (Request.Request.Command == FirmwareCommand.FIRMWARE_COMMAND_DETECT)
                    {
                        switch (Request.Request.Param)
                        {
                            case 0: DeviceName = Response.Result; break;
                            case 1: DeviceVersion = Response.Result; break;
                            case 2: DeviceManufacturer = Response.Result; break;
                        }
                    }
                    else if (Request.Request.Command == FirmwareCommand.FIRMWARE_COMMAND_WRITE)
                    {
                        CompleteBytes += Request.Request.Data.Length;
                    }
                    else if (Request.Request.Command == FirmwareCommand.FIRMWARE_COMMAND_FINISH)
                    {
                        Step = FirmwareStep.STEP_COMPLETED_OK;
                    }

                    Request.ResultOk();
                }
                else
                {
                    Step = FirmwareStep.STEP_COMPLETED;
                    UpdateRequests.Clear();
                    UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_DETECT, 2, new byte[] { }, 1000));
                    Console.WriteLine($"Device return error code {Response.Code} on command {Request.Request.Command}");
                    Request.ResultError($"Device return error code {Response.Code} on command {Request.Request.Command}");
                }
            }
        }

        /// <summary>
        /// Функция обработки статуса, проверки таймаутов и т.д.
        /// </summary>
        virtual public void Process()
        {
            // Идёт передача конфигурации...
            if (Request != null)
            {
                if (Request.Completed)
                {
                    // Хорошо, запрос закончен...
                    if (Request.Ok)
                    {
                        // Освобождаем очередь
                        Request = null;
                    }
                    else
                    {
                        // Можно выдать его ещё через некоторое время...
                        // TODO...
                        // Для устройства-определения не блокируем очередь, чтобы
                        // не отвечающее устройство не повисало в списке навсегда.
                        if (IsProbe)
                            Request = null;
                    }
                }
                else if (Request.IsTimeouted())
                {
                    if (RepeatRequestCount > 0)
                    {
                        RepeatRequestCount--;
                        Console.WriteLine($"Response timeout, retry ({RepeatRequestCount})...");
                        SetRepeatRequest(Request);
                        Request = null;
                    }
                    else
                    {
                        Console.WriteLine("Response timeout.");
                        Request.ResultError("Response timeout");
                        Request = null;
                    }
                }
            }
        }

        /// <summary>
        /// Закончен ли процесс обновления ПО
        /// </summary>
        /// <returns></returns>
        virtual public bool IsCompletedOk()
        {
            return Step == FirmwareStep.STEP_COMPLETED_OK;
        }

        /// <summary>
        /// Закончен ли процесс обновления ПО
        /// </summary>
        /// <returns></returns>
        virtual public bool IsCompleted()
        {
            return Step == FirmwareStep.STEP_COMPLETED || Step == FirmwareStep.STEP_COMPLETED_OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsProcessed()
        {
            return Processed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsCompletedWithTimeout(long Time)
        {
            if(Processed)
            {
                return Time - ProcessedTime > 10000;
            }

            return false;
        }

        /// <summary>
        /// Отметить как обработанную
        /// </summary>
        /// <param name="Time"></param>
        public void MarkProcessed(long Time)
        {
            Processed = true;

            ProcessedTime = Time;
        }

        void SetRepeatRequest(FirmwareRequest Req)
        {
            RepeatRequest = Req;
        }

        /// <summary>
        /// Закончен ли процесс конфигурации
        /// </summary>
        /// <returns></returns>
        virtual public bool IsFirmwareRequested()
        {
            return Step == FirmwareStep.STEP_UPGRADE && Firmware == null;
        }

        /// <summary>
        /// Установить ПО
        /// </summary>
        public void SetFirmware(Firmware Fw)
        {
            Firmware = Fw;
            Data = Fw.Data;

            // Подготовим пакеты обновления...
            uint BlockSize = 512;
            uint Offset = 0;

            UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_PREPARE, Convert.ToUInt32(Data.Length), new byte[] { }, 8000));
            while (Offset < Data.Length)
            {
                uint RemainSize = Convert.ToUInt32(Math.Min(BlockSize, Data.Length - Offset));
                var Block = Data.ReadArray(Offset, RemainSize);

                UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_WRITE, Offset, Block, 1000));

                Offset += RemainSize;
            }

            uint CRC = new CRC16().ComputeChecksum(Data);
            UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_CHECK, CRC, new byte[] { }, 8000));
            UpdateRequests.Enqueue(new FirmwareRequest(FirmwareCommand.FIRMWARE_COMMAND_FINISH, 0, new byte[] { }, 2000));
        }

        /// <summary>
        /// Закончен ли процесс конфигурации
        /// </summary>
        /// <returns></returns>
        virtual public bool IsStepCompleted()
        {
            return Request == null && UpdateRequests.Count == 0 && RepeatRequest == null;
        }

        /// <summary>
        /// Следующий шаг прошивки...
        /// </summary>
        public void NextStep()
        {
            switch (Step)
            {
                case FirmwareStep.STEP_DETECT:
                    Step = FirmwareStep.STEP_UPGRADE;
                    break;
                case FirmwareStep.STEP_UPGRADE:
                    Step = FirmwareStep.STEP_COMPLETED;
                    break;
                case FirmwareStep.STEP_COMPLETED:
                    break;
                case FirmwareStep.STEP_COMPLETED_OK:
                    break;
            }
        }

        /// <summary>
        /// Есть ли параметры для отправки
        /// </summary>
        /// <returns></returns>
        virtual public bool HasRequestsToSend()
        {
            return RepeatRequest != null || Request == null && UpdateRequests.Count > 0;
        }

        /// <summary>
        /// Получить список параметров для отправки
        /// </summary>
        /// <returns></returns>
        virtual public FirmwareRequest GetNextRequest()
        {
            if (RepeatRequest != null)
            {
                RepeatRequest.Start();
                Request = RepeatRequest;
                RepeatRequest = null;

                //Console.WriteLine($"Send repeated request  {Request.Request.Command}.");
                return Request;
            }
            else
            {
                var Req = UpdateRequests.Dequeue();
                Request = Req;
                Request.Start();

                //Console.WriteLine($"Send request {Request.Request.Command}");
                return Req;
            }
        }

        private enum FirmwareStep
        {
            STEP_DETECT,
            STEP_UPGRADE,
            STEP_WORK,
            STEP_COMPLETED,
            STEP_COMPLETED_OK
        }

        public class FirmwareRequest
        {

            /// <summary>
            /// Поля запроса
            /// </summary>
            public readonly Packet.Firmware.FirmwareRequest Request;

            /// <summary>
            /// Время отправки
            /// </summary>
            public DateTime Sended;

            /// <summary>
            /// Таймаут
            /// </summary>
            private int Timeout;

            /// <summary>
            /// Номер транзакции
            /// </summary>
            public int Tid = -1;

            /// <summary>
            /// Был ли завершён запрос
            /// </summary>
            public bool Completed { get; private set; } = false;

            /// <summary>
            /// Успешно ли выполнено
            /// </summary>
            public bool Ok { get; private set; } = false;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="Fields"></param>
            public FirmwareRequest(Packet.Firmware.FirmwareRequest Request, int Timeout)
            {
                this.Request = Request;
                this.Timeout = Timeout;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="Fields"></param>
            public FirmwareRequest(FirmwareCommand Command, uint Param, byte[] Data, int Timeout)
            {
                Request = new Packet.Firmware.FirmwareRequest(Command, Param, Data);
                this.Timeout = Timeout;
            }

            /// <summary>
            /// Сообщить об ошибке
            /// </summary>
            /// <param name="Text"></param>
            public void ResultError(string Text)
            {
                Completed = true;
            }

            /// <summary>
            /// Сообщить об успешном ответе
            /// </summary>
            /// <param name="Text"></param>
            public void ResultOk()
            {
                Ok = true;
                Completed = true;
            }

            /// <summary>
            /// Сброс
            /// </summary>
            public void Start()
            {
                Sended = DateTime.Now;
                Tid = -1;
                Completed = false;
                Ok = false;
            }

            /// <summary>
            /// Сколько время прошло с момента отправки
            /// </summary>
            /// <param name="Milliseconds"></param>
            /// <returns></returns>
            public bool IsTimeouted()
            {
                return (DateTime.Now - Sended).TotalMilliseconds > Timeout;
            }
        }
    }
}
