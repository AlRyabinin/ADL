using AdlpLib.Core;
using AdlpLib.Core.Packet;
using AdlpLib.Core.Packet.Backup;
using AdlpLib.Core.Packet.Status;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace AdlpLib.Backup
{
    /// <summary>
    /// Модуль бэкапа настроек устройств.
    /// Подключается к ядру конфигуратора и позволяет оперировать бэкапом
    /// настроек по протоколу BACKUP_CMD/BACKUP_DATA.
    ///
    /// Протокол загрузки (выгрузки с устройства):
    /// 1. Запрос BACKUP_DATA_TYPE_SUPPORT - поддерживается ли бэкап
    /// 2. Запрос BACKUP_DATA_TYPE_SECTION_COUNT - число секций
    /// 3. Для каждой секции: BLOCK_COUNT, SECTION_NAME, BLOCK_SIZE
    /// 4. Для каждого блока: BLOCK_DATA
    /// 5. BACKUP_DATA_TYPE_SECTION_BLOCK_RESET - сброс
    ///
    /// Пример использования:
    ///   var BackupModule = new BackupCfgModule();
    ///   Core.AddModule(BackupModule);
    ///   Core.Init();
    ///   BackupModule.RequestBackup(Device, SystemId);
    ///   // ... в Process() автоматически опрашивается устройство ...
    ///   if (BackupModule.IsDeviceBackupCompleted(Device))
    ///       BackupModule.SaveToFile(Device, "backup.bin");
    /// </summary>
    public class BackupCfgModule : Module
    {
        /// <summary>
        /// Список устройств, для которых выполняется или выполнялся бэкап
        /// </summary>
        private List<BackedupDevice> BackupDevices = new List<BackedupDevice>();

        /// <summary>
        /// Конструктор
        /// </summary>
        public BackupCfgModule()
        {
        }

        /// <summary>
        /// Получить возможности модуля
        /// </summary>
        public override BlockCapability GetCapabilities()
        {
            return BlockCapability.BLOCKCAP_CFG;
        }

        /// <summary>
        /// Является ли устройство интересным для нас
        /// </summary>
        public override bool IsDeviceInteresting(SenderInfo Info, Status Status)
        {
            return true;
        }

        /// <summary>
        /// Запросить бэкап настроек с устройства
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <param name="SystemId">Номер системы</param>
        /// <param name="DownloadData">Загружать данные блоков (true) или только сканировать секции (false)</param>
        /// <returns>true, если запрос создан</returns>
        public bool RequestBackup(Device Device, uint SystemId = 0, bool DownloadData = true)
        {
            lock (BackupDevices)
            {
                // Если запись уже есть - перезапустим её
                for (int i = BackupDevices.Count - 1; i >= 0; i--)
                {
                    if (!BackupDevices[i].Check(Device.Endpoint))
                        continue;

                    if (!BackupDevices[i].IsCompleted())
                    {
                        Debug.WriteLine($"Backup already in progress for {Device.Name} {Device.Block} ({Device.Endpoint})");
                        return false;
                    }

                    BackupDevices.RemoveAt(i);
                    break;
                }

                var BackedUp = new BackedupDevice(Device, SystemId, false, DownloadData);
                BackupDevices.Add(BackedUp);

                Console.WriteLine($"Backup requested for {Device.Name} {Device.Block} UUID={Device.Uuid} ({Device.Endpoint}) (download={(DownloadData ? "full" : "scan")})");
                return true;
            }
        }

        /// <summary>
        /// Проверить поддержку бэкапа устройством (без опроса секций)
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <returns>true, если запрос создан (результат читается через GetBackedupDevice)</returns>
        public bool RequestProbe(Device Device)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Device.Endpoint))
                        return false; // уже есть запись (проба/скан/бэкап) - читаем состояние
                }

                var BackedUp = new BackedupDevice(Device, 0, true, false);
                BackupDevices.Add(BackedUp);
                return true;
            }
        }

        /// <summary>
        /// Занято ли устройство процессом бэкапа/скана/пробы
        /// </summary>
        public bool IsDeviceBusy(Device Device)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Device.Endpoint) && !Dev.IsCompleted())
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Сбросить процесс бэкапа устройства (например, после перепрошивки/перезапуска,
        /// когда счётчик запросов против устройства уже неактуален)
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        public void ResetBackup(Device Device)
        {
            lock (BackupDevices)
            {
                for (int i = BackupDevices.Count - 1; i >= 0; i--)
                {
                    if (!BackupDevices[i].Check(Device.Endpoint))
                        continue;

                    BackupDevices[i].ResetRetry();
                    Console.WriteLine($"Backup reset for {Device.Name} {Device.Block} ({Device.Endpoint})");
                    return;
                }
            }
        }

        /// <summary>
        /// Запросить бэкап настроек по адресу устройства
        /// </summary>
        /// <param name="EP">Конечная точка устройства</param>
        /// <param name="SystemId">Номер системы</param>
        /// <returns>true, если запрос создан</returns>
        public bool RequestBackup(IPEndPoint EP, uint SystemId = 0)
        {
            var Dev = AdlpCore.GetDeviceByInfo(new SenderInfo(EP, 0));
            if (Dev != null)
                return RequestBackup(Dev, SystemId);

            Debug.WriteLine($"Device not found for {EP}");
            return false;
        }

        /// <summary>
        /// Проверить, завершён ли бэкап устройства (успешно)
        /// </summary>
        public bool IsDeviceBackupCompleted(Device Device)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Device.Endpoint))
                        return Dev.IsCompletedOk();
                }
            }
            return false;
        }

        /// <summary>
        /// Проверить, завершён ли бэкап устройства
        /// </summary>
        public bool IsDeviceBackupDone(Device Device)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Device.Endpoint))
                        return Dev.IsCompleted();
                }
            }
            return false;
        }

        /// <summary>
        /// Получить объект бэкапа устройства
        /// </summary>
        public BackedupDevice GetBackedupDevice(Device Device)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Device.Endpoint))
                        return Dev;
                }
            }
            return null;
        }

        /// <summary>
        /// Сохранить бэкап устройства в файл
        /// </summary>
        /// <param name="Device">Устройство</param>
        /// <param name="FileName">Имя файла</param>
        /// <param name="SectionIndices">Список индексов секций для сохранения (null - все)</param>
        /// <returns>true, если успешно</returns>
        public bool SaveToFile(Device Device, string FileName, IEnumerable<int> SectionIndices = null)
        {
            var BackedUp = GetBackedupDevice(Device);
            if (BackedUp == null || !BackedUp.IsCompletedOk())
            {
                Debug.WriteLine($"Backup not ready for {Device.Name} {Device.Block}");
                return false;
            }

            var File = new BackupFile();
            File.FromBackedupDevice(BackedUp, SectionIndices);

            try
            {
                File.Save(FileName);
                Console.WriteLine($"Backup saved: {File}");
                return true;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine($"Backup save error: {Ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Загрузить бэкап из файла
        /// </summary>
        /// <param name="FileName">Имя файла</param>
        /// <returns>Объект бэкапа или null</returns>
        public BackupFile LoadFromFile(string FileName)
        {
            var File = new BackupFile();

            try
            {
                if (File.Load(FileName))
                {
                    Console.WriteLine($"Backup loaded: {File}");
                    return File;
                }
                else
                {
                    Debug.WriteLine($"Failed to load backup from {FileName}");
                    return null;
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine($"Backup load error: {Ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Отправить команду выгрузки бэкапа на устройство (запись блоков настроек)
        /// </summary>
        /// <param name="Device">Устройство</param>
        /// <param name="File">Файл бэкапа</param>
        /// <param name="IgnoreUuid">Игнорировать проверку UUID (по умолчанию false)</param>
        /// <param name="SectionIndices">Список индексов секций для загрузки (null - все)</param>
        /// <returns>true, если запрос отправлен</returns>
        public bool UploadBackup(Device Device, BackupFile File, bool IgnoreUuid = false, IEnumerable<int> SectionIndices = null)
        {
            if (File == null)
            {
                Debug.WriteLine("Backup file not specified");
                return false;
            }

            // Проверяем тип устройства
            string DeviceType = (Device.Name ?? "").Trim();
            if (!string.Equals(File.DeviceType.Trim(), DeviceType, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Backup type mismatch: file=\"{File.DeviceType}\" device=\"{Device.Name}\"");
                return false;
            }

            // Проверяем UUID устройства (если обе стороны знают UUID и не запрещено игнорировать)
            if (!IgnoreUuid &&
                File.Uuid != Guid.Empty &&
                Device.Uuid != Guid.Empty &&
                !File.Uuid.Equals(Device.Uuid))
            {
                Debug.WriteLine($"Backup UUID mismatch: file={File.Uuid} device={Device.Uuid}");
                return false;
            }

            // Отправляем запросы на запись блоков
            int Tid = -1;
            foreach (var Section in File.Sections)
            {
                // Фильтр секций
                if (SectionIndices != null && !SectionIndices.Contains((int)Section.Index))
                    continue;

                // Проверка контрольной суммы секции
                if (Section.Data.Length != Section.TotalSize)
                {
                    Debug.WriteLine($"Backup section {Section.Index} size mismatch");
                    return false;
                }
                var Crc = new CRC16();
                if (Crc.ComputeChecksum(Section.Data) != Section.Crc)
                {
                    Debug.WriteLine($"Backup section {Section.Index} CRC mismatch");
                    return false;
                }

                int Offset = 0;
                int BlockIndex = 1;

                while (Offset < Section.Data.Length)
                {
                    int BlockLength = Math.Min(Section.BlockSize, Section.Data.Length - Offset);
                    var BlockData = new byte[BlockLength];
                    Array.Copy(Section.Data, Offset, BlockData, 0, BlockLength);

                    var DataPacket = new BackupData(
                        BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_DATA,
                        Section.Index,
                        (ushort)BlockIndex,
                        BackupErrorCode.BACKUP_DATA_ERROR_CODE_OK,
                        BlockData);

                    Tid = AdlpCore.SendRequest(Device.Endpoint, PacketCode.COMMAND_BACKUP_DATA, DataPacket.getPacket());

                    if (Tid < 0)
                    {
                        Debug.WriteLine($"Failed to send backup block to {Device.Endpoint}");
                        return false;
                    }

                    Offset += BlockLength;
                    BlockIndex++;
                }

                Console.WriteLine($"Uploaded section {Section.Index} ({Section.Name}): {Section.BlockCount} blocks, {Section.TotalSize} bytes");
            }

            // Отправляем reset
            var ResetCmd = new BackupCommand(BackupCommandCode.BACKUP_DATA_TYPE_SECTION_BLOCK_RESET, 0, 0);
            Tid = AdlpCore.SendRequest(Device.Endpoint, PacketCode.COMMAND_BACKUP_CMD, ResetCmd.getPacket());

            Console.WriteLine($"Backup upload completed for {Device.Name} {Device.Block} ({Device.Endpoint})");
            return true;
        }

        /// <summary>
        /// Отправить команду выгрузки бэкапа из файла
        /// </summary>
        /// <param name="EP">Конечная точка устройства</param>
        /// <param name="FileName">Имя файла бэкапа</param>
        /// <param name="IgnoreUuid">Игнорировать проверку UUID (по умолчанию false)</param>
        /// <returns>true, если успешно</returns>
        public bool UploadBackup(IPEndPoint EP, string FileName, bool IgnoreUuid = false)
        {
            var Dev = AdlpCore.GetDeviceByInfo(new SenderInfo(EP, 0));
            if (Dev == null)
            {
                Debug.WriteLine($"Device not found for {EP}");
                return false;
            }

            var File = LoadFromFile(FileName);
            if (File == null)
                return false;

            return UploadBackup(Dev, File, IgnoreUuid);
        }

        /// <summary>
        /// Обработка пакета
        /// </summary>
        public override bool OnPacket(SenderInfo Info, BasePacket InputPacket)
        {
            if (base.OnPacket(Info, InputPacket))
                return true;

            bool Response = InputPacket.Flags.HasFlag(HeaderFlags.ADLPDS_HEADER_FLAG_RESPONSE);

            if (Response)
            {
                switch (InputPacket.Command)
                {
                    case PacketCode.COMMAND_BACKUP_DATA:
                    case PacketCode.COMMAND_BACKUP_CMD:
                        return ProcessBackupDataResponse(Info, new BackupData(InputPacket.Payload));
                }
            }
            else
            {
                switch (InputPacket.Command)
                {
                    case PacketCode.COMMAND_BACKUP_CMD:
                        return ProcessBackupCommand(Info, new BackupCommand(InputPacket.Payload));
                }
            }

            return false;
        }

        /// <summary>
        /// Обработка входящей команды бэкапа (от устройства)
        /// </summary>
        private bool ProcessBackupCommand(SenderInfo Info, BackupCommand Command)
        {
            // Устройство запросило бэкап - обрабатываем как ответ
            return true;
        }

        /// <summary>
        /// Обработка ответа данных бэкапа
        /// </summary>
        private bool ProcessBackupDataResponse(SenderInfo Info, BackupData Response)
        {
            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (Dev.Check(Info.Endpoint))
                    {
                        Dev.ProcessResponse(Response, Info.Tid);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Обработка таблицы устройств бэкапа
        /// </summary>
        private void ProcessBackupDevices()
        {
            BackedupDevice[] Devs;

            lock (BackupDevices)
            {
                Devs = BackupDevices.ToArray();
            }

            foreach (var Dev in Devs)
            {
                Dev.Process();

                if (Dev.HasRequestsToSend())
                {
                    var Request = Dev.GetNextRequest();

                    int Tid = AdlpCore.SendRequest(Dev.Endpoint, PacketCode.COMMAND_BACKUP_CMD, Request.Command.getPacket());
                    if (Tid >= 0)
                    {
                        Request.Tid = Tid;
                    }
                    else
                    {
                        Request.ResultError($"Cannot send backup request to {Dev.Endpoint}!");
                    }
                }
            }

            lock (BackupDevices)
            {
                foreach (var Dev in BackupDevices)
                {
                    if (!Dev.IsProcessed())
                    {
                        if (Dev.IsCompletedOk())
                        {
                            Console.WriteLine($"Backup completed for {Dev.DeviceType} {Dev.Block} UUID={Dev.Uuid} ({Dev.Endpoint})");
                            Dev.MarkProcessed(Milliseconds);
                        }
                        else if (Dev.IsCompleted())
                        {
                            Console.WriteLine($"Backup failed for {Dev.DeviceType} {Dev.Block} UUID={Dev.Uuid} ({Dev.Endpoint}): {Dev.ErrorMessage}");
                            Dev.MarkProcessed(Milliseconds);
                        }
                    }
                }

                BackupDevices.RemoveAll(Dev => Dev.IsCompletedWithTimeout(Milliseconds));
            }
        }

        /// <summary>
        /// Тик обработки
        /// </summary>
        public override void Process()
        {
            base.Process();
            ProcessBackupDevices();
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public override void OnUninit()
        {
            base.OnUninit();

            lock (BackupDevices)
            {
                BackupDevices.Clear();
            }
        }
    }
}
