using AdlpLib.Core;
using AdlpLib.Core.Packet.Backup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Backup
{
    /// <summary>
    /// Формат бинарного файла бэкапа настроек устройства.
    ///
    /// Формат файла:
    /// =====================
    /// [FileHeader]       - Общий заголовок файла (96 байт)
    /// [SectionHeader*N]  - Заголовки секций (по 32 байта каждая)
    /// [BlockData]        - Данные блоков всех секций последовательно
    ///
    /// Заголовок файла (FileHeader):
    /// ----+------+---------------------------------------------------
    /// Off | Size | Описание
    /// ----+------+---------------------------------------------------
    ///  0  |  4   | Магические числа: 0x41444C42 ("ADLB")
    ///  4  |  2   | Версия формата файла (2)
    ///  6  |  2   | Зарезервировано
    ///  8  |  16  | UUID устройства (Guid, 16 байт)
    /// 24  |  8   | Тип устройства (строка, 8 символов, ANSI)
    /// 32  |  4   | SystemId устройства
    /// 36  |  4   | IP-адрес устройства (uint32, big-endian)
    /// 40  |  6   | MAC-адрес устройства (6 байт, 0 если неизвестен)
    /// 46  |  2   | Номер блока устройства
    /// 48  |  2   | Количество секций
    /// 50  |  2   | Зарезервировано
    /// 52  |  4   | Общий размер данных всех секций (байт)
    /// 56  |  8   | Временная метка создания (Ticks, версия 2)
    /// 64  | 32   | Зарезервировано (заполнено нулями)
    /// ----+------+---------------------------------------------------
    /// Итого: 96 байт
    ///
    /// Заголовок секции (SectionHeader):
    /// ----+------+---------------------------------------------------
    /// Off | Size | Описание
    /// ----+------+---------------------------------------------------
    ///  0  |  2   | Индекс секции (ushort)
    ///  2  |  2   | Тип секции (BackupSectionType, ushort)
    ///  4  |  32  | Имя секции (строка, ANSI, заполнена нулями)
    /// 36  |  2   | Число блоков в секции (ushort)
    /// 38  |  2   | Размер блока (ushort)
    /// 40  |  4   | Общий размер данных секции в байтах (uint)
    /// 44  |  4   | Контрольная сумма данных секции (CRC32, uint)
    /// 48  | 16   | Зарезервировано
    /// ----+------+---------------------------------------------------
    /// Итого: 64 байта
    ///
    /// Данные блоков:
    /// Блоки записываются последовательно для каждой секции в порядке
    /// индексов блоков (0, 1, 2, ...). Каждый блок содержит ровно
    /// SectionHeader.BlockSize байт. Исключение: последний блок секции
    /// может быть меньше, если общый размер секции не кратен размеру блока.
    /// =====================
    /// </summary>
    public class BackupFile
    {
        /// <summary>
        /// Магическое число файла
        /// </summary>
        public const uint MAGIC = 0x41444C42;

        /// <summary>
        /// Версия формата файла
        /// </summary>
        public const ushort FORMAT_VERSION = 2;

        /// <summary>
        /// Размер заголовка файла
        /// </summary>
        public const int FileHeaderSize = 96;

        /// <summary>
        /// Размер заголовка секции
        /// </summary>
        public const int SectionHeaderSize = 64;

        /// <summary>
        /// Длина имени устройства
        /// </summary>
        private const int DeviceTypeNameSize = 8;

        /// <summary>
        /// Длина имени секции
        /// </summary>
        public const int SectionNameSize = 32;

        /// <summary>
        /// Длина MAC-адреса
        /// </summary>
        private const int MacSize = 6;

        /// <summary>
        /// UUID устройства
        /// </summary>
        public Guid Uuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Тип устройства
        /// </summary>
        public string DeviceType { get; set; } = "";

        /// <summary>
        /// SystemId устройства
        /// </summary>
        public uint SystemId { get; set; } = 0;

        /// <summary>
        /// IP-адрес устройства
        /// </summary>
        public IPAddress IpAddress { get; set; } = IPAddress.None;

        /// <summary>
        /// MAC-адрес устройства
        /// </summary>
        public byte[] MacAddress { get; set; } = new byte[MacSize];

        /// <summary>
        /// Номер блока устройства
        /// </summary>
        public ushort DeviceBlock { get; set; } = 0;

        /// <summary>
        /// Временная метка создания бэкапа (версия файла 2)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Секции бэкапа
        /// </summary>
        public List<BackupFileSection> Sections { get; set; } = new List<BackupFileSection>();

        /// <summary>
        /// Создать бэкап из данных BackedupDevice
        /// </summary>
        /// <param name="Device">Объект бэкапированного устройства</param>
        /// <param name="SectionIndices">Список индексов секций для сохранения (null - все)</param>
        public void FromBackedupDevice(BackedupDevice Device, IEnumerable<int> SectionIndices = null)
        {
            Uuid = Device.Uuid;
            DeviceType = Device.DeviceType;
            SystemId = Device.SystemId;
            IpAddress = Device.Endpoint.Address;
            DeviceBlock = (ushort)Device.Block;
            Timestamp = DateTime.Now;

            Sections.Clear();

            foreach (var Section in Device.GetSections())
            {
                // Номера секций устройства идут с 1
                if (SectionIndices != null && !SectionIndices.Contains((int)Section.Index))
                    continue;

                var FileSection = new BackupFileSection();
                FileSection.Index = Section.Index;
                FileSection.Type = Section.Type;
                FileSection.Name = Section.Name;
                FileSection.BlockCount = (ushort)Section.BlockCount;
                FileSection.BlockSize = (ushort)Section.BlockSize;

                int TotalSize = 0;
                var AllData = new List<byte>();
                var Crc = new CRC16();

                for (int b = 1; b <= Section.BlockCount; b++)
                {
                    var BlockData = Section.GetBlock(b);
                    if (BlockData != null)
                    {
                        AllData.AddRange(BlockData);
                        TotalSize += BlockData.Length;
                    }
                }

                FileSection.Data = AllData.ToArray();
                FileSection.TotalSize = TotalSize;
                FileSection.Crc = Crc.ComputeChecksum(FileSection.Data);

                Sections.Add(FileSection);
            }
        }

        /// <summary>
        /// Сохранить бэкап в бинарный файл
        /// </summary>
        /// <param name="FileName">Имя файла</param>
        public void Save(string FileName)
        {
            using (var FS = new FileStream(FileName, FileMode.Create, FileAccess.Write))
            using (var W = new BinaryWriter(FS))
            {
                // Заголовок файла
                W.Write(MAGIC);
                W.Write(FORMAT_VERSION);
                W.Write((ushort)0);
                WriteGuid(W, Uuid);
                WriteAnsiString(W, DeviceType, DeviceTypeNameSize);
                W.Write(SystemId);
                WriteIpBE(W, IpAddress);
                WriteBytes(W, MacAddress, MacSize);
                W.Write(DeviceBlock);
                W.Write((ushort)Sections.Count);
                W.Write((ushort)0);

                int TotalDataSize = Sections.Sum(S => S.Data.Length);
                W.Write(TotalDataSize);

                // Временная метка (8 байт) + резерв (32 байта)
                W.Write(Timestamp.Ticks);
                W.Write(new byte[32]);

                // Заголовки секций
                foreach (var Section in Sections)
                {
                    W.Write(Section.Index);
                    W.Write((ushort)Section.Type);
                    WriteAnsiString(W, Section.Name, SectionNameSize);
                    W.Write(Section.BlockCount);
                    W.Write(Section.BlockSize);
                    W.Write(Section.TotalSize);
                    W.Write(Section.Crc);
                    W.Write(new byte[16]);
                }

                // Данные блоков
                foreach (var Section in Sections)
                {
                    W.Write(Section.Data);
                }
            }
        }

        /// <summary>
        /// Загрузить бэкап из бинарного файла
        /// </summary>
        /// <param name="FileName">Имя файла</param>
        /// <returns>true, если успешно загружен</returns>
        public bool Load(string FileName)
        {
            if (!File.Exists(FileName))
                return false;

            using (var FS = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            using (var R = new BinaryReader(FS))
            {
                if (FS.Length < FileHeaderSize)
                    return false;

                // Заголовок файла
                uint Magic = R.ReadUInt32();
                if (Magic != MAGIC)
                    return false;

                ushort Version = R.ReadUInt16();
                if (Version > FORMAT_VERSION)
                    return false;

                R.ReadUInt16(); // зарезервировано

                Uuid = ReadGuid(R);
                DeviceType = ReadAnsiString(R, DeviceTypeNameSize);
                SystemId = R.ReadUInt32();
                IpAddress = ReadIpBE(R);
                MacAddress = ReadBytes(R, MacSize);
                DeviceBlock = R.ReadUInt16();
                ushort SectionCount = R.ReadUInt16();
                R.ReadUInt16(); // зарезервировано
                int TotalDataSize = R.ReadInt32();

                if (Version >= 2)
                {
                    long Ticks = R.ReadInt64();
                    Timestamp = (Ticks != 0) ? new DateTime(Ticks) : DateTime.MinValue;
                    R.ReadBytes(32); // резерв
                }
                else
                {
                    Timestamp = DateTime.MinValue;
                    R.ReadBytes(40); // резерв
                }

                Sections.Clear();

                // Заголовки секций
                for (int i = 0; i < SectionCount; i++)
                {
                    if (FS.Position + SectionHeaderSize > FS.Length)
                        return false;

                    var Section = new BackupFileSection();
                    Section.Index = R.ReadUInt16();
                    Section.Type = (BackupSectionType)R.ReadUInt16();
                    Section.Name = ReadAnsiString(R, SectionNameSize);
                    Section.BlockCount = R.ReadUInt16();
                    Section.BlockSize = R.ReadUInt16();
                    Section.TotalSize = R.ReadInt32();
                    Section.Crc = R.ReadUInt16();
                    R.ReadBytes(16); // зарезервировано

                    Sections.Add(Section);
                }

                // Данные блоков
                foreach (var Section in Sections)
                {
                    if (FS.Position + Section.TotalSize > FS.Length)
                        return false;

                    Section.Data = R.ReadBytes(Section.TotalSize);
                }
            }

            return true;
        }

        /// <summary>
        /// Проверить контрольные суммы всех секций
        /// </summary>
        /// <returns>true, если все суммы корректны</returns>
        public bool Verify()
        {
            var Crc = new CRC16();
            foreach (var Section in Sections)
            {
                if (Section.Data.Length != Section.TotalSize)
                    return false;

                ushort Computed = Crc.ComputeChecksum(Section.Data);
                if (Computed != Section.Crc)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Подходит ли бэкап устройству (тип и номер блока, кроме ip и uuid)
        /// </summary>
        /// <param name="deviceType">Тип устройства</param>
        /// <param name="deviceBlock">Номер блока (0 - не проверять)</param>
        /// <returns>true, если бэкап подходит</returns>
        public bool Matches(string deviceType, int deviceBlock)
        {
            if (!string.Equals((DeviceType ?? "").Trim(), (deviceType ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            if ((DeviceBlock != 0) && (deviceBlock != 0) && (DeviceBlock != deviceBlock))
                return false;

            return true;
        }

        /// <summary>
        /// Получить краткую информацию о бэкапе
        /// </summary>
        /// <returns>Строковое описание</returns>
        public override string ToString()
        {
            return $"Backup: {DeviceType} UUID={Uuid} IP={IpAddress} Block={DeviceBlock} Sections={Sections.Count}";
        }

        private static void WriteGuid(BinaryWriter W, Guid Value)
        {
            W.Write(Value.ToByteArray());
        }

        private static Guid ReadGuid(BinaryReader R)
        {
            return new Guid(R.ReadBytes(16));
        }

        private static void WriteAnsiString(BinaryWriter W, string Value, int MaxLength)
        {
            var Bytes = Encoding.ASCII.GetBytes(Value ?? "");
            if (Bytes.Length > MaxLength)
            {
                var Truncated = new byte[MaxLength];
                Array.Copy(Bytes, Truncated, MaxLength);
                Bytes = Truncated;
            }
            else
            {
                var Padded = new byte[MaxLength];
                Array.Copy(Bytes, Padded, Bytes.Length);
                Bytes = Padded;
            }
            W.Write(Bytes);
        }

        private static string ReadAnsiString(BinaryReader R, int Length)
        {
            var Bytes = R.ReadBytes(Length);
            int End = Array.IndexOf(Bytes, (byte)0);
            if (End < 0) End = Length;
            return Encoding.ASCII.GetString(Bytes, 0, End);
        }

        private static void WriteIpBE(BinaryWriter W, IPAddress IP)
        {
            byte[] Bytes = IP.GetAddressBytes();
            W.Write(Bytes);
        }

        private static IPAddress ReadIpBE(BinaryReader R)
        {
            byte[] Bytes = R.ReadBytes(4);
            return new IPAddress(Bytes);
        }

        private static void WriteBytes(BinaryWriter W, byte[] Data, int Length)
        {
            if (Data != null && Data.Length >= Length)
                W.Write(Data, 0, Length);
            else
                W.Write(new byte[Length]);
        }

        private static byte[] ReadBytes(BinaryReader R, int Length)
        {
            return R.ReadBytes(Length);
        }
    }

    /// <summary>
    /// Заголовок секции в файле бэкапа
    /// </summary>
    public class BackupFileSection
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
        public ushort BlockCount { get; set; } = 0;

        /// <summary>
        /// Размер блока (максимальный)
        /// </summary>
        public ushort BlockSize { get; set; } = 0;

        /// <summary>
        /// Общий размер данных секции
        /// </summary>
        public int TotalSize { get; set; } = 0;

        /// <summary>
        /// Контрольная сумма данных
        /// </summary>
        public ushort Crc { get; set; } = 0;

        /// <summary>
        /// Данные блоков секции (последовательно)
        /// </summary>
        public byte[] Data { get; set; } = new byte[0];

        /// <summary>
        /// Получить данные конкретного блока по индексу
        /// </summary>
        /// <param name="BlockIndex">Индекс блока</param>
        /// <returns>Данные блока или null</returns>
        public byte[] GetBlockData(int BlockIndex)
        {
            if (BlockIndex < 0 || BlockIndex >= BlockCount)
                return null;

            int Offset = BlockIndex * BlockSize;
            if (Offset >= Data.Length)
                return null;

            int Length = Math.Min(BlockSize, Data.Length - Offset);
            var Result = new byte[Length];
            Array.Copy(Data, Offset, Result, 0, Length);
            return Result;
        }

        public override string ToString()
        {
            return $"Section[{Index}] {Type} \"{Name}\" Blocks={BlockCount} Size={TotalSize}";
        }
    }
}
