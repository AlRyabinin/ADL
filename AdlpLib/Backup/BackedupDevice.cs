using AdlpLib.Core;
using AdlpLib.Core.Packet.Backup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace AdlpLib.Backup
{
    /// <summary>
    /// Объект бэкапа настроек устройства.
    /// Управляет конечным автоматом опроса секций и загрузки блоков настроек.
    /// </summary>
    public class BackedupDevice
    {
        /// <summary>
        /// Информация об устройстве
        /// </summary>
        public readonly Device Device;

        /// <summary>
        /// Куда отправлять запросы
        /// </summary>
        public IPEndPoint Endpoint => Device.Endpoint;

        /// <summary>
        /// UUID устройства
        /// </summary>
        public Guid Uuid => Device.Uuid;

        /// <summary>
        /// Тип устройства
        /// </summary>
        public string DeviceType => Device.Name;

        /// <summary>
        /// SystemId устройства
        /// </summary>
        public uint SystemId { get; private set; } = 0;

        /// <summary>
        /// Номер блока
        /// </summary>
        public int Block => Device.Block;

        /// <summary>
        /// Режим "проба" - только запрос поддержки бэкапа (без опроса секций)
        /// </summary>
        public readonly bool ProbeOnly;

        /// <summary>
        /// Загружать ли данные блоков (false - только скан секций)
        /// </summary>
        public readonly bool DownloadData;

        /// <summary>
        /// Текущий шаг конечного автомата
        /// </summary>
        private BackupStep Step = BackupStep.STEP_QUERY_SUPPORT;

        /// <summary>
        /// Поддерживается ли бэкап
        /// </summary>
        public bool BackupSupported { get; private set; } = false;

        /// <summary>
        /// Число секций на устройстве
        /// </summary>
        public int SectionCount { get; private set; } = 0;

        /// <summary>
        /// Данные секций
        /// </summary>
        private List<BackupSectionData> Sections = new List<BackupSectionData>();

        /// <summary>
        /// Текущий индекс опрашиваемой секции (1..SectionCount, 0 невалиден)
        /// </summary>
        private int CurrentSectionIndex = 0;

        /// <summary>
        /// Данные текущей секции (внутренний список 0-базовый)
        /// </summary>
        private BackupSectionData CurrentSection => Sections[CurrentSectionIndex - 1];

        /// <summary>
        /// Текущий индекс опрашиваемого блока (1..BlockCount, 0 невалиден)
        /// </summary>
        private int CurrentBlockIndex = 0;

        /// <summary>
        /// Актуальный запрос
        /// </summary>
        private BackupRequest ActiveRequest = null;

        /// <summary>
        /// Повторный запрос
        /// </summary>
        private BackupRequest RepeatRequest = null;

        /// <summary>
        /// Обработано
        /// </summary>
        private bool Processed = false;

        /// <summary>
        /// Время обработки
        /// </summary>
        private long ProcessedTime = 0;

        /// <summary>
        /// Ошибка (описание)
        /// </summary>
        public string ErrorMessage { get; private set; } = null;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <param name="SystemId">Номер системы</param>
        /// <param name="ProbeOnly">Только проверить поддержку бэкапа</param>
        /// <param name="DownloadData">Загружать данные блоков</param>
        public BackedupDevice(Device Device, uint SystemId, bool ProbeOnly = false, bool DownloadData = true)
        {
            this.Device = Device;
            this.SystemId = SystemId;
            this.ProbeOnly = ProbeOnly;
            this.DownloadData = DownloadData;

            // Начинаем с запроса поддержки
            EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SUPPORT, 0, 0);
        }

        /// <summary>
        /// Проверка устройства по адресу
        /// </summary>
        public bool Check(IPEndPoint Endpoint) => Device.CheckAddress(Endpoint);

        /// <summary>
        /// Проверка устройства по UUID
        /// </summary>
        public bool Check(Guid UUID) => Uuid.Equals(UUID);

        /// <summary>
        /// Добавить запрос в очередь
        /// </summary>
        private void EnqueueCommand(BackupCommandCode Code, ushort Section, ushort Block)
        {
            var Cmd = new BackupCommand(Code, Section, Block);
            EnqueueRequest(new BackupRequest(Cmd, BACKUP_REQUEST_TIMEOUT));
        }

        private const int BACKUP_REQUEST_TIMEOUT = 4000;

        /// <summary>
        /// Максимальное число повторов одного запроса (при отсутствии ответа)
        /// </summary>
        public const int RequestMaxRetryCount = 5;

        /// <summary>
        /// Устройство не ответило (превышен лимит повторов запроса)
        /// </summary>
        public bool TimedOut { get; private set; } = false;

        private Queue<BackupRequest> RequestQueue = new Queue<BackupRequest>();

        private void EnqueueRequest(BackupRequest Req)
        {
            RequestQueue.Enqueue(Req);
        }

        /// <summary>
        /// Есть ли запросы для отправки
        /// </summary>
        public bool HasRequestsToSend()
        {
            return RepeatRequest != null || (ActiveRequest == null && RequestQueue.Count > 0);
        }

        /// <summary>
        /// Получить следующий запрос
        /// </summary>
        public BackupRequest GetNextRequest()
        {
            if (RepeatRequest != null)
            {
                RepeatRequest.Start();
                ActiveRequest = RepeatRequest;
                RepeatRequest = null;
                return ActiveRequest;
            }
            else
            {
                var Req = RequestQueue.Dequeue();
                ActiveRequest = Req;
                Req.Start();
                return Req;
            }
        }

        /// <summary>
        /// Обработать ответ от устройства
        /// </summary>
        public void ProcessResponse(BackupData Response, int Tid)
        {
            if (ActiveRequest == null)
                return;

            if (ActiveRequest.Tid != Tid)
                return;

            try
            {
                if (Response.Error != BackupErrorCode.BACKUP_DATA_ERROR_CODE_OK)
                {
                    Debug.WriteLine($"Backup: device returned error {Response.Error} on code {Response.Code}");
                    ActiveRequest.ResultError($"Error: {Response.Error}");
                    Step = BackupStep.STEP_COMPLETED;
                    RequestQueue.Clear();
                    ErrorMessage = Response.Error.ToString();
                    return;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Backup: exception while processing response: {ex}");
                ActiveRequest.ResultError($"Exception: {ex.Message}");
                Step = BackupStep.STEP_COMPLETED;
                RequestQueue.Clear();
                ErrorMessage = ex.Message;
                return;
            }

            switch (Step)
            {
                case BackupStep.STEP_QUERY_SUPPORT:
                    ProcessSupportResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_SECTION_COUNT:
                    ProcessSectionCountResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_SECTION_BLOCK_COUNT:
                    ProcessSectionBlockCountResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_SECTION_TYPE:
                    ProcessSectionTypeResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_SECTION_NAME:
                    ProcessSectionNameResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_BLOCK_SIZE:
                    ProcessBlockSizeResponse(Response);
                    break;
                case BackupStep.STEP_QUERY_BLOCK_DATA:
                    ProcessBlockDataResponse(Response);
                    break;
                case BackupStep.STEP_RESET:
                    ProcessResetResponse(Response);
                    break;
            }

            ActiveRequest.ResultOk();
        }

        /// <summary>
        /// Обработка ответа на поддержку бэкапа
        /// </summary>
        private void ProcessSupportResponse(BackupData Response)
        {
            BackupSupported = (Response.DataValue != 0);

            if (ProbeOnly)
            {
                // Проба - выясняем только поддержку бэкапа
                Step = BackupStep.STEP_COMPLETED;
                return;
            }

            if (BackupSupported)
            {
                Step = BackupStep.STEP_QUERY_SECTION_COUNT;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_COUNT, 0, 0);
            }
            else
            {
                Step = BackupStep.STEP_COMPLETED;
                ErrorMessage = "Backup not supported by device";
            }
        }

        /// <summary>
        /// Обработка ответа на число секций
        /// </summary>
        private void ProcessSectionCountResponse(BackupData Response)
        {
            SectionCount = Response.DataValue;

            Sections.Clear();
            for (int i = 0; i < SectionCount; i++)
            {
                Sections.Add(new BackupSectionData());
            }

            // Номера секций идут с 1
            CurrentSectionIndex = 1;

            if (SectionCount > 0)
            {
                Step = BackupStep.STEP_QUERY_SECTION_BLOCK_COUNT;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_COUNT, (ushort)CurrentSectionIndex, 0);
            }
            else
            {
                Step = BackupStep.STEP_RESET;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_RESET, 0, 0);
            }
        }

        /// <summary>
        /// Обработка ответа на число блоков секции
        /// </summary>
        private void ProcessSectionBlockCountResponse(BackupData Response)
        {
            if (CurrentSectionIndex >= 1 && CurrentSectionIndex <= SectionCount)
            {
                int BlockCount = Response.DataValue;
                CurrentSection.BlockCount = BlockCount;
                CurrentSection.Index = (ushort)CurrentSectionIndex;
            }

            // После числа блоков запрашиваем тип секции
            Step = BackupStep.STEP_QUERY_SECTION_TYPE;
            EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_TYPE, (ushort)CurrentSectionIndex, 0);
        }

        /// <summary>
        /// Обработка ответа на тип секции (BACKUP_DATA_TYPE_SECTION_BLOCK_TYPE)
        /// </summary>
        private void ProcessSectionTypeResponse(BackupData Response)
        {
            if (CurrentSectionIndex >= 1 && CurrentSectionIndex <= SectionCount)
            {
                CurrentSection.Type = Response.SectionType;
            }

            // Запрашиваем имя секции
            Step = BackupStep.STEP_QUERY_SECTION_NAME;
            EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_NAME, (ushort)CurrentSectionIndex, 0);
        }

        /// <summary>
        /// Обработка ответа на имя секции
        /// </summary>
        private void ProcessSectionNameResponse(BackupData Response)
        {
            if (CurrentSectionIndex >= 1 && CurrentSectionIndex <= SectionCount)
            {
                string SectionName = DecodeSectionName(Response.Data);

                // Имя может отсутствовать (например, для типа DEVICE) - подставляем имя по типу секции
                if (string.IsNullOrWhiteSpace(SectionName) || SectionName.Trim('?').Length == 0)
                    SectionName = DefaultSectionName(CurrentSection.Type);

                if (SectionName.Length > BackupFile.SectionNameSize)
                    SectionName = SectionName.Substring(0, BackupFile.SectionNameSize);
                CurrentSection.Name = SectionName;
            }

            // Запрашиваем размер блока секции
            Step = BackupStep.STEP_QUERY_BLOCK_SIZE;
            EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_SIZE, (ushort)CurrentSectionIndex, 0);
        }

        /// <summary>
        /// Имя секции по умолчанию (по её типу)
        /// </summary>
        private static string DefaultSectionName(BackupSectionType Type)
        {
            switch (Type)
            {
                case BackupSectionType.BACKUP_DATA_SECTION_DEVICE: return "device";
                case BackupSectionType.BACKUP_DATA_SECTION_CHANNELS: return "channels";
                case BackupSectionType.BACKUP_DATA_SECTION_GROUPS: return "groups";
                case BackupSectionType.BACKUP_DATA_SECTION_STREAMS: return "streams";
                case BackupSectionType.BACKUP_DATA_SECTION_CONTROL: return "control";
                case BackupSectionType.BACKUP_DATA_SECTION_EXEC: return "exec";
                case BackupSectionType.BACKUP_DATA_SECTION_RAW: return "raw";
                default: return "";
            }
        }

        /// <summary>
        /// Обработка ответа на размер блока
        /// </summary>
        private void ProcessBlockSizeResponse(BackupData Response)
        {
            if (CurrentSectionIndex >= 1 && CurrentSectionIndex <= SectionCount)
            {
                CurrentSection.BlockSize = Response.DataValue;
            }

            // Скан секций без загрузки данных, либо начало загрузки блоков.
            // Номера блоков идут с 1, 0 невалиден.
            CurrentBlockIndex = 1;

            if (DownloadData && CurrentSection.BlockCount > 0)
            {
                Step = BackupStep.STEP_QUERY_BLOCK_DATA;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_DATA, (ushort)CurrentSectionIndex, (ushort)CurrentBlockIndex);
            }
            else
            {
                // Нет блоков или скан без выгрузки данных, переходим к следующей секции
                GoToNextSection();
            }
        }

        /// <summary>
        /// Обработка ответа на данные блока
        /// </summary>
        private void ProcessBlockDataResponse(BackupData Response)
        {
            if (CurrentSectionIndex >= 1 && CurrentSectionIndex <= SectionCount)
            {
                var Section = CurrentSection;

                // Записываем данные блока
                if (Response.Data != null && Response.Size > 0)
                {
                    Section.AddBlockData(CurrentBlockIndex, Response.Data);
                }

                CurrentBlockIndex++;

                if (CurrentBlockIndex <= Section.BlockCount)
                {
                    // Следующий блок в той же секции
                    EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_DATA, (ushort)CurrentSectionIndex, (ushort)CurrentBlockIndex);
                }
                else
                {
                    // Все блоки загружены, переходим к следующей секции
                    GoToNextSection();
                }
            }
        }

        /// <summary>
        /// Декодирование имени секции: сначала строгий UTF-8, иначе Windows-1251
        /// (ASCII-имена декодируются одинаково в обеих кодировках)
        /// </summary>
        private static string DecodeSectionName(byte[] Data)
        {
            if (Data == null || Data.Length == 0)
                return "";

            int Length = Data.Length;
            while (Length > 0 && Data[Length - 1] == 0)
                Length--;

            if (Length == 0)
                return "";

            try
            {
                var Utf8 = new UTF8Encoding(false, true);
                return Utf8.GetString(Data, 0, Length);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1251).GetString(Data, 0, Length);
            }
        }

        /// <summary>
        /// Переход к следующей секции
        /// </summary>
        private void GoToNextSection()
        {
            CurrentSectionIndex++;
            if (CurrentSectionIndex <= SectionCount)
            {
                // Запрашиваем число блоков следующей секции
                Step = BackupStep.STEP_QUERY_SECTION_BLOCK_COUNT;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_COUNT, (ushort)CurrentSectionIndex, 0);
            }
            else
            {
                // Все секции загружены, делаем reset
                Step = BackupStep.STEP_RESET;
                EnqueueCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_RESET, 0, 0);
            }
        }

        /// <summary>
        /// Обработка ответа на reset
        /// </summary>
        private void ProcessResetResponse(BackupData Response)
        {
            Step = BackupStep.STEP_COMPLETED;
        }

        /// <summary>
        /// Функция обработки таймаутов
        /// </summary>
        public void Process()
        {
            if (ActiveRequest != null)
            {
                if (ActiveRequest.Completed)
                {
                    if (ActiveRequest.Ok)
                    {
                        ActiveRequest = null;
                    }
                }
                else if (ActiveRequest.IsTimeouted())
                {
                    ActiveRequest.RepeatCount++;
                    Debug.WriteLine($"Backup: response timeout (attempt {ActiveRequest.RepeatCount}/{RequestMaxRetryCount})");

                    if (ActiveRequest.RepeatCount >= RequestMaxRetryCount)
                    {
                        Debug.WriteLine("Backup: no response, aborting");
                        TimedOut = true;
                        ErrorMessage = "No response from device";
                        Step = BackupStep.STEP_COMPLETED;
                        RequestQueue.Clear();
                        ActiveRequest = null;
                        return;
                    }

                    RepeatRequest = ActiveRequest;
                    ActiveRequest = null;
                }
            }
        }

        /// <summary>
        /// Процесс завершён
        /// </summary>
        public bool IsCompleted()
        {
            return Step == BackupStep.STEP_COMPLETED || Step == BackupStep.STEP_COMPLETED_OK;
        }

        /// <summary>
        /// Процесс завершён успешно
        /// </summary>
        public bool IsCompletedOk()
        {
            return Step == BackupStep.STEP_COMPLETED && ErrorMessage == null;
        }

        /// <summary>
        /// Запросы закончились и ожидается ответ
        /// </summary>
        public bool IsStepCompleted()
        {
            return ActiveRequest == null && RequestQueue.Count == 0 && RepeatRequest == null;
        }

        /// <summary>
        /// Сбросить счётчик повторов запросов (после перепрошивки/перезапуска устройства)
        /// </summary>
        public void ResetRetry()
        {
            if (ActiveRequest != null)
                ActiveRequest.RepeatCount = 0;
            if (RepeatRequest != null)
                RepeatRequest.RepeatCount = 0;
            foreach (var Req in RequestQueue)
                Req.RepeatCount = 0;
        }

        /// <summary>
        /// Отметить как обработанное
        /// </summary>
        public void MarkProcessed(long Time)
        {
            Processed = true;
            ProcessedTime = Time;
        }

        /// <summary>
        /// Помечено ли как обработанное
        /// </summary>
        public bool IsProcessed() => Processed;

        /// <summary>
        /// Таймаут на удаление из списка
        /// </summary>
        public bool IsCompletedWithTimeout(long Time)
        {
            if (Processed)
                return Time - ProcessedTime > 10000;

            return false;
        }

        /// <summary>
        /// Получить данные секции по индексу
        /// </summary>
        public BackupSectionData GetSection(int Index)
        {
            if (Index < 0 || Index >= Sections.Count)
                return null;
            return Sections[Index];
        }

        /// <summary>
        /// Получить все секции
        /// </summary>
        public BackupSectionData[] GetSections()
        {
            return Sections.ToArray();
        }

        /// <summary>
        /// Шаги конечного автомата
        /// </summary>
        private enum BackupStep
        {
            STEP_QUERY_SUPPORT,
            STEP_QUERY_SECTION_COUNT,
            STEP_QUERY_SECTION_BLOCK_COUNT,
            STEP_QUERY_SECTION_TYPE,
            STEP_QUERY_SECTION_NAME,
            STEP_QUERY_BLOCK_SIZE,
            STEP_QUERY_BLOCK_DATA,
            STEP_RESET,
            STEP_COMPLETED,
            STEP_COMPLETED_OK
        }
    }

    /// <summary>
    /// Данные одной секции бэкапа
    /// </summary>
    public class BackupSectionData
    {
        /// <summary>
        /// Индекс секции
        /// </summary>
        public ushort Index { get; set; } = 0;

        /// <summary>
        /// Тип секции
        /// </summary>
        public BackupSectionType Type { get; set; } = BackupSectionType.BACKUP_DATA_SECTION_UNKNOWN;

        /// <summary>
        /// Имя секции
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Число блоков
        /// </summary>
        public int BlockCount { get; set; } = 0;

        /// <summary>
        /// Размер блока
        /// </summary>
        public int BlockSize { get; set; } = 0;

        /// <summary>
        /// Данные блоков (индекс -> данные)
        /// </summary>
        private SortedDictionary<int, byte[]> BlockData = new SortedDictionary<int, byte[]>();

        /// <summary>
        /// Записать данные блока
        /// </summary>
        public void AddBlockData(int BlockIndex, byte[] Data)
        {
            BlockData[BlockIndex] = Data;
        }

        /// <summary>
        /// Получить данные блока
        /// </summary>
        public byte[] GetBlock(int BlockIndex)
        {
            if (BlockData.TryGetValue(BlockIndex, out byte[] Data))
                return Data;
            return null;
        }

        /// <summary>
        /// Получить все данные блоков последовательно
        /// </summary>
        public byte[] GetAllData()
        {
            var Result = new List<byte>();
            foreach (var KV in BlockData)
            {
                Result.AddRange(KV.Value);
            }
            return Result.ToArray();
        }

        public override string ToString()
        {
            return $"Section[{Index}] \"{Name}\" Blocks={BlockCount} BlockSize={BlockSize}";
        }
    }

    /// <summary>
    /// Запрос бэкапа
    /// </summary>
    public class BackupRequest
    {
        /// <summary>
        /// Пакет команды
        /// </summary>
        public readonly BackupCommand Command;

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
        /// Завершён ли запрос
        /// </summary>
        public bool Completed { get; private set; } = false;

        /// <summary>
        /// Успешно ли выполнен
        /// </summary>
        public bool Ok { get; private set; } = false;

        /// <summary>
        /// Счётчик повторов запроса (при таймаутах) - сохраняется между попытками
        /// </summary>
        public int RepeatCount { get; set; } = 0;

        /// <summary>
        /// Конструктор
        /// </summary>
        public BackupRequest(BackupCommand Command, int Timeout)
        {
            this.Command = Command;
            this.Timeout = Timeout;
        }

        /// <summary>
        /// Сообщить об ошибке
        /// </summary>
        public void ResultError(string Text)
        {
            Completed = true;
        }

        /// <summary>
        /// Сообщить об успешном ответе
        /// </summary>
        public void ResultOk()
        {
            Ok = true;
            Completed = true;
        }

        /// <summary>
        /// Сброс перед отправкой
        /// </summary>
        public void Start()
        {
            Sended = DateTime.Now;
            Tid = -1;
            Completed = false;
            Ok = false;
        }

        /// <summary>
        /// Проверка таймаута
        /// </summary>
        public bool IsTimeouted()
        {
            return (DateTime.Now - Sended).TotalMilliseconds > Timeout;
        }
    }
}
