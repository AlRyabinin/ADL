using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AdlpLib.Fw
{
    /// <summary>
    /// Спека прошивки в YAML-подобном формате (ключ: значение).
    /// Комментарии — '#', текстовые значения можно заключать в кавычки.
    ///
    /// Пример:
    ///   file: adl006_bw.bin          # путь к бинарю (относительно спеки)
    ///   type: ADL006BW               # тип устройства
    ///   kernel: 7.0.0                # версия ядра
    ///   version: 0.1.1               # версия ПО
    ///   description: "Прошивка ПУ"
    ///   comment: "Первый релиз"
    ///   date: 2026-09-07             # необязательно; по умолчанию теперь
    ///   image: photo1.jpg            # изображения (JPEG), можно несколько
    ///   image: photo2.jpg
    /// </summary>
    public class AdlFirmwareSpec
    {
        /// <summary>Путь к файлу прошивки (.bin) — относительно каталога спеки</summary>
        public string FirmwareFile { get; set; } = "";

        /// <summary>Тип устройства</summary>
        public string DeviceType { get; set; } = "";

        /// <summary>Версия ядра</summary>
        public string KernelVersion { get; set; } = "";

        /// <summary>Версия ПО</summary>
        public string SoftwareVersion { get; set; } = "";

        /// <summary>Описание</summary>
        public string Description { get; set; } = "";

        /// <summary>Комментарий</summary>
        public string Comment { get; set; } = "";

        /// <summary>Дата сборки (необязательно)</summary>
        public DateTime? BuildDate { get; set; }

        /// <summary>Пути к изображениям (JPEG)</summary>
        public List<string> Images { get; } = new List<string>();

        /// <summary>
        /// Загрузить спеку из YAML-файла
        /// </summary>
        public static AdlFirmwareSpec Load(string path, out string baseDir)
        {
            baseDir = Path.GetDirectoryName(Path.GetFullPath(path));
            var spec = new AdlFirmwareSpec();
            bool hasDate = false;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine;
                var comment = line.IndexOf('#');
                if (comment >= 0)
                    line = line.Substring(0, comment);

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                var colon = line.IndexOf(':');
                int sep = colon > 0 ? colon : line.IndexOf(' ');
                if (sep <= 0)
                    continue;

                var key = line.Substring(0, sep).Trim().ToLowerInvariant();
                var value = Unquote(line.Substring(sep + 1).Trim());

                switch (key)
                {
                    case "file":
                    case "firmware":
                    case "bin":
                        spec.FirmwareFile = value;
                        break;
                    case "type":
                    case "device":
                    case "devicetype":
                        spec.DeviceType = value;
                        break;
                    case "kernel":
                    case "kernelversion":
                    case "kernel_version":
                        spec.KernelVersion = value;
                        break;
                    case "version":
                    case "software":
                    case "softwareversion":
                    case "software_version":
                    case "fwversion":
                        spec.SoftwareVersion = value;
                        break;
                    case "description":
                    case "desc":
                        spec.Description = value;
                        break;
                    case "comment":
                    case "comments":
                        spec.Comment = value;
                        break;
                    case "date":
                    case "build":
                    case "builddate":
                    case "build_date":
                        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                        {
                            spec.BuildDate = dt;
                            hasDate = true;
                        }
                        break;
                    case "image":
                    case "photo":
                    case "images":
                    case "photos":
                        foreach (var image in SplitList(value))
                        {
                            if (image.Length > 0)
                                spec.Images.Add(image);
                        }
                        break;
                }
            }

            if (!hasDate)
                spec.BuildDate = DateTime.Now;

            return spec;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                    return value.Substring(1, value.Length - 2);
            }
            return value;
        }

        private static IEnumerable<string> SplitList(string value)
        {
            foreach (var part in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                yield return part.Trim();
        }

        /// <summary>
        /// Сохранить спеку в YAML-файл.
        /// Пути к изображениям и прошивке делаются относительными baseDir.
        /// Дата сборки не сохраняется.
        /// </summary>
        public void Save(string path, string baseDir)
        {
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(FirmwareFile))
            {
                var rel = MakeRelative(baseDir, FirmwareFile);
                lines.Add($"file: {rel}");
            }

            if (!string.IsNullOrEmpty(DeviceType))
                lines.Add($"type: {DeviceType}");

            if (!string.IsNullOrEmpty(KernelVersion))
                lines.Add($"kernel: {KernelVersion}");

            if (!string.IsNullOrEmpty(SoftwareVersion))
                lines.Add($"version: {SoftwareVersion}");

            if (!string.IsNullOrEmpty(Description))
                lines.Add($"description: \"{Description}\"");

            if (!string.IsNullOrEmpty(Comment))
                lines.Add($"comment: \"{Comment}\"");

            foreach (var img in Images)
            {
                var rel = MakeRelative(baseDir, img);
                lines.Add($"image: {rel}");
            }

            File.WriteAllText(path, string.Join("\n", lines) + "\n");
        }

        private static string MakeRelative(string baseDir, string fullPath)
        {
            if (string.IsNullOrEmpty(baseDir))
                return fullPath;

            var absFull = Path.GetFullPath(fullPath);
            var absBase = Path.GetFullPath(baseDir);

            if (absFull.StartsWith(absBase, StringComparison.OrdinalIgnoreCase))
            {
                var rel = absFull.Substring(absBase.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return rel;
            }

            return fullPath;
        }

        /// <summary>
        /// Построить пакет .adlfw из спеки (файл прошивки и изображения читаются с диска)
        /// </summary>
        public AdlFirmwareFile Build(string baseDir = null)
        {
            if (string.IsNullOrEmpty(FirmwareFile))
                throw new InvalidOperationException("В спеке не указан файл прошивки (file)");

            var dir = baseDir ?? Directory.GetCurrentDirectory();
            var fwPath = Path.IsPathRooted(FirmwareFile) ? FirmwareFile : Path.Combine(dir, FirmwareFile);

            var file = AdlFirmwareFile.Create();
            file.SetFirmwareData(File.ReadAllBytes(fwPath))
                .SetInfo(DeviceType, KernelVersion, SoftwareVersion, Description, Comment, BuildDate);

            foreach (var image in Images)
            {
                var imagePath = Path.IsPathRooted(image) ? image : Path.Combine(dir, image);
                file.AddImageFromFile(imagePath);
            }

            return file;
        }
    }
}