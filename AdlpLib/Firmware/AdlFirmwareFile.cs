using CrcSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AdlpLib.Fw
{
    /// <summary>
    /// Файл прошивки в формате ADLFW (расширение .adlfw).
    ///
    /// Структура файла (v1, все целые — little-endian):
    ///   [0..5)    Магическая строка "ADLFW"
    ///   [5]       Версия формата (1)
    ///   [6..8)    Размер заголовка (u16) — смещение начала тела файла
    ///   [8..]     Поля заголовка:
    ///              u8   длина + байты  тип устройства
    ///              u8   длина + байты  версия ядра
    ///              u8   длина + байты  версия ПО
    ///              u8   длина + байты  описание (UTF-8)
    ///              u8   длина + байты  комментарий (UTF-8)
    ///              u32  дата сборки (unix-секунды UTC)
    ///              u32  размер прошивки
    ///              u32  контрольная сумма прошивки (CRC32)
    ///   [..]      Тело:
    ///              байты прошивки (firmwareSize)
    ///              u8   количество изображений
    ///              для каждого: u32 длина + байты (JPEG)
    ///              u32  контрольная сумма файла (CRC32 всех предыдущих байт)
    /// </summary>
    public class AdlFirmwareFile
    {
        /// <summary>
        /// Магическая строка формата
        /// </summary>
        public const string Magic = "ADLFW";

        /// <summary>
        /// Версия формата
        /// </summary>
        public const byte FormatVersion = 1;

        private const long MaxFirmwareSize = 32L * 1024 * 1024;
        private const long MaxImageSize = 32L * 1024 * 1024;
        private const int MaxImageCount = 64;
        private const int MaxStringLen = 4096;

        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes(Magic);
        private static readonly Crc Crc32 = new Crc(new CrcParameters(32, 0x04C11DB7, 0xFFFFFFFF, 0xFFFFFFFF, true, true));

        /// <summary>Тип устройства, под которое собрана прошивка (например, ADL006BW)</summary>
        public string DeviceType { get; set; } = "";

        /// <summary>Версия ядра ADLP</summary>
        public string KernelVersion { get; set; } = "";

        /// <summary>Версия ПО устройства</summary>
        public string SoftwareVersion { get; set; } = "";

        /// <summary>Описание прошивки</summary>
        public string Description { get; set; } = "";

        /// <summary>Комментарий к прошивке</summary>
        public string Comment { get; set; } = "";

        /// <summary>Дата и время сборки (локальное время)</summary>
        public DateTime BuildDate { get; set; } = DateTime.Now;

        /// <summary>Данные прошивки (бинарь)</summary>
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        /// <summary>Контрольная сумма (CRC32) данных прошивки</summary>
        public uint FirmwareChecksum { get; private set; }

        /// <summary>Изображения (JPEG) — фото устройства и т.п.</summary>
        public List<byte[]> Images { get; } = new List<byte[]>();

        /// <summary>
        /// Создать пустой пакет прошивки
        /// </summary>
        public static AdlFirmwareFile Create() => new AdlFirmwareFile();

        /// <summary>
        /// Задать данные прошивки (бинарь)
        /// </summary>
        public AdlFirmwareFile SetFirmwareData(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) throw new ArgumentException("Данные прошивки пусты", nameof(data));
            if (data.Length > MaxFirmwareSize) throw new ArgumentException($"Прошивка слишком большая ({data.Length} байт)", nameof(data));

            Data = data;
            FirmwareChecksum = ComputeCrc32(data);
            return this;
        }

        /// <summary>
        /// Установить служебные поля пакета
        /// </summary>
        public AdlFirmwareFile SetInfo(string deviceType, string kernelVersion, string softwareVersion, string description = "", string comment = "", DateTime? buildDate = null)
        {
            DeviceType = deviceType ?? "";
            KernelVersion = kernelVersion ?? "";
            SoftwareVersion = softwareVersion ?? "";
            Description = description ?? "";
            Comment = comment ?? "";
            if (buildDate.HasValue)
                BuildDate = buildDate.Value;
            return this;
        }

        /// <summary>
        /// Добавить изображение (JPEG)
        /// </summary>
        public AdlFirmwareFile AddImage(byte[] image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (image.Length == 0) throw new ArgumentException("Изображение пустое", nameof(image));
            if (image.Length > MaxImageSize) throw new ArgumentException("Изображение слишком большое", nameof(image));
            if (Images.Count >= MaxImageCount) throw new InvalidOperationException("Слишком много изображений");

            Images.Add(image);
            return this;
        }

        /// <summary>
        /// Добавить изображение из файла (JPEG)
        /// </summary>
        public AdlFirmwareFile AddImageFromFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            return AddImage(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Очистить список изображений
        /// </summary>
        public AdlFirmwareFile ClearImages()
        {
            Images.Clear();
            return this;
        }

        /// <summary>
        /// Собрать файл формата ADLFW
        /// </summary>
        public byte[] Build()
        {
            if (Data.Length == 0) throw new InvalidOperationException("Не заданы данные прошивки");
            if (string.IsNullOrEmpty(DeviceType)) throw new InvalidOperationException("Не задан тип устройства");

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, true);

            bw.Write(MagicBytes);
            bw.Write(FormatVersion);
            long headerSizeOffset = ms.Position;
            bw.Write((ushort)0); // placeholder

            WriteString(bw, DeviceType);
            WriteString(bw, KernelVersion);
            WriteString(bw, SoftwareVersion);
            WriteString(bw, Description);
            WriteString(bw, Comment);
            bw.Write(ToUnixSeconds(BuildDate));
            bw.Write((uint)Data.Length);
            bw.Write(FirmwareChecksum);

            // Зафиксируем размер заголовка
            long headerSize = ms.Position;
            ms.Position = headerSizeOffset;
            bw.Write((ushort)headerSize);
            ms.Position = headerSize;

            bw.Write(Data);

            bw.Write((byte)Images.Count);
            foreach (var image in Images)
            {
                bw.Write((uint)image.Length);
                bw.Write(image);
            }

            // Контрольная сумма файла (по всем предшествующим байтам)
            long fileCrcOffset = ms.Position;
            bw.Write((uint)0); // placeholder
            var allBytes = ms.ToArray();
            uint fileCrc = ComputeCrc32(allBytes, 0, (int)fileCrcOffset);
            ms.Position = fileCrcOffset;
            bw.Write(fileCrc);
            bw.Flush();

            return ms.ToArray();
        }

        /// <summary>
        /// Сохранить в файл
        /// </summary>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Build());
        }

        /// <summary>
        /// Разобрать файл ADLFW из байтов. Проверяет структуру и контрольные суммы.
        /// </summary>
        public static AdlFirmwareFile Parse(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var (file, error) = TryParse(data, true);
            if (file == null)
                throw new FormatException(error);

            return file;
        }

        /// <summary>
        /// Прочитать файл ADLFW из файла
        /// </summary>
        public static AdlFirmwareFile Load(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            return Parse(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Проверить файл ADLFW (структура + контрольные суммы)
        /// </summary>
        public static bool Validate(byte[] data, out string error)
        {
            var (file, err) = TryParse(data, true);
            error = err;
            return file != null;
        }

        /// <summary>
        /// Проверить файл ADLFW на диске
        /// </summary>
        public static bool ValidateFile(string path, out string error)
        {
            error = null;
            try
            {
                return Validate(File.ReadAllBytes(path), out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// CRC32 данных
        /// </summary>
        public static uint ComputeCrc32(byte[] data, int offset = 0, int? length = null)
        {
            int len = length ?? data.Length;
            if (offset == 0 && len == data.Length)
                return BitConverter.ToUInt32(Crc32.CalculateCheckValue(data), 0);

            var segment = new byte[len];
            Array.Copy(data, offset, segment, 0, len);
            return BitConverter.ToUInt32(Crc32.CalculateCheckValue(segment), 0);
        }

        /// <summary>
        /// Внутренний разбор без бросания исключений
        /// </summary>
        private static (AdlFirmwareFile, string) TryParse(byte[] data, bool verifyChecksums)
        {
            try
            {
                int pos = 0;

                // Магия
                for (int i = 0; i < MagicBytes.Length; i++)
                {
                    if (pos >= data.Length || data[pos++] != MagicBytes[i])
                        return (null, $"Неверная сигнатура файла: ожидалось '{Magic}'");
                }

                // Версия
                if (pos >= data.Length) return (null, "Файл обрезан (версия)");
                byte version = data[pos++];
                if (version != FormatVersion)
                    return (null, $"Неподдерживаемая версия формата: {version}");

                // Размер заголовка
                if (pos + 2 > data.Length) return (null, "Файл обрезан (размер заголовка)");
                int headerSize = (int)data[pos] | ((int)data[pos + 1] << 8);
                pos += 2;
                if (headerSize < 8 || headerSize > data.Length)
                    return (null, $"Некорректный размер заголовка: {headerSize}");

                var file = new AdlFirmwareFile();
                var endHeader = headerSize;
                var raw = data;

                pos = ReadString(raw, pos, endHeader, out string deviceType);
                pos = ReadString(raw, pos, endHeader, out string kernelVersion);
                pos = ReadString(raw, pos, endHeader, out string softwareVersion);
                pos = ReadString(raw, pos, endHeader, out string description);
                pos = ReadString(raw, pos, endHeader, out string comment);

                if (pos + 12 > endHeader) return (null, "Файл обрезан (поля даты/размеров)");
                uint buildUnix = BitConverter.ToUInt32(raw, pos); pos += 4;
                uint firmwareSize = BitConverter.ToUInt32(raw, pos); pos += 4;
                uint fwCrc = BitConverter.ToUInt32(raw, pos); pos += 4;

                file.DeviceType = deviceType;
                file.KernelVersion = kernelVersion;
                file.SoftwareVersion = softwareVersion;
                file.Description = description;
                file.Comment = comment;
                file.BuildDate = FromUnixSeconds(buildUnix);

                if (firmwareSize > MaxFirmwareSize)
                    return (null, $"Размер прошивки превышает лимит ({firmwareSize})");
                if (pos + (long)firmwareSize > data.Length)
                    return (null, "Файл обрезан (данные прошивки)");

                file.SetFirmwareDataRaw(raw, pos, firmwareSize);
                pos += (int)firmwareSize;
                if (fwCrc != file.FirmwareChecksum)
                {
                    if (verifyChecksums)
                        return (null, $"Контрольная сумма прошивки не совпадает (файл 0x{fwCrc:X8}, вычислено 0x{file.FirmwareChecksum:X8})");
                    file.FirmwareChecksum = fwCrc;
                }

                if (pos >= data.Length) return (null, "Файл обрезан (изображения)");
                int imageCount = data[pos++];
                if (imageCount < 0 || imageCount > MaxImageCount)
                    return (null, $"Некорректное количество изображений: {imageCount}");

                for (int i = 0; i < imageCount; i++)
                {
                    if (pos + 4 > data.Length) return (null, "Файл обрезан (изображение)");
                    uint imageLen = BitConverter.ToUInt32(raw, pos); pos += 4;
                    if (imageLen > MaxImageSize) return (null, $"Слишком большое изображение ({imageLen})");
                    if (pos + (long)imageLen > data.Length) return (null, "Файл обрезан (данные изображения)");
                    var image = new byte[imageLen];
                    Array.Copy(raw, pos, image, 0, imageLen);
                    pos += (int)imageLen;
                    file.Images.Add(image);
                }

                if (pos + 4 > data.Length) return (null, "Файл обрезан (контрольная сумма файла)");
                uint fileCrcStored = BitConverter.ToUInt32(raw, pos);

                if (verifyChecksums)
                {
                    uint fileCrcComputed = ComputeCrc32(raw, 0, pos);
                    if (fileCrcComputed != fileCrcStored)
                        return (null, $"Контрольная сумма файла не совпадает (файл 0x{fileCrcStored:X8}, вычислено 0x{fileCrcComputed:X8})");
                }

                return (file, null);
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка разбора: {ex.Message}");
            }
        }

        private static void WriteString(BinaryWriter bw, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? "");
            if (bytes.Length > byte.MaxValue)
                throw new InvalidOperationException("Поле слишком длинное");
            bw.Write((byte)bytes.Length);
            bw.Write(bytes);
        }

        private static int ReadString(byte[] data, int pos, int end, out string value)
        {
            if (pos >= end) throw new FormatException("Файл обрезан (строка)");
            byte len = data[pos++];
            if (pos + len > end) throw new FormatException("Файл обрезан (строка)");
            value = Encoding.UTF8.GetString(data, pos, len);
            return pos + len;
        }

        private void SetFirmwareDataRaw(byte[] raw, int offset, uint size)
        {
            var data = new byte[size];
            Array.Copy(raw, offset, data, 0, size);
            Data = data;
            FirmwareChecksum = ComputeCrc32(data);
        }

        private static uint ToUnixSeconds(DateTime dt) => (uint)new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeSeconds();
        private static DateTime FromUnixSeconds(uint sec) => DateTimeOffset.FromUnixTimeSeconds(sec).ToLocalTime().DateTime;
    }
}