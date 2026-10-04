using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// JSON-хранилище конфигураций устройств (одна БД = один файл).
    /// </summary>
    public class JsonDeviceConfigStore : IDeviceConfigStore
    {
        private class FileContent
        {
            public List<DeviceConfigPreset> Presets { get; set; } = new List<DeviceConfigPreset>();
        }

        private readonly string FileName;
        private readonly FileContent Content = new FileContent();
        private bool Dirty = false;

        public string Location => FileName;

        public JsonDeviceConfigStore(string fileName)
        {
            FileName = fileName;
            if (File.Exists(fileName))
            {
                try
                {
                    var Loaded = JsonSerializer.Deserialize<FileContent>(File.ReadAllText(fileName, Encoding.UTF8));
                    if (Loaded != null)
                        Content.Presets = Loaded.Presets ?? new List<DeviceConfigPreset>();
                }
                catch
                {
                    Content.Presets = new List<DeviceConfigPreset>();
                }
            }
        }

        public IEnumerable<DeviceConfigPreset> GetPresets()
        {
            return Content.Presets.ToArray();
        }

        public IEnumerable<DeviceConfigPreset> GetPresets(Guid deviceUuid)
        {
            return Content.Presets.Where(P => P.DeviceUuid == deviceUuid).ToArray();
        }

        public DeviceConfigPreset GetCurrentPreset(Guid deviceUuid)
        {
            return Content.Presets.FirstOrDefault(P => P.DeviceUuid == deviceUuid && P.IsCurrent);
        }

        public void AddOrUpdatePreset(DeviceConfigPreset preset)
        {
            var Existing = Content.Presets.FirstOrDefault(P => P.Id == preset.Id);
            if (Existing != null)
                Content.Presets[Content.Presets.IndexOf(Existing)] = preset;
            else
                Content.Presets.Add(preset);
            Dirty = true;
        }

        public void SetCurrent(Guid deviceUuid, Guid presetId)
        {
            foreach (var P in Content.Presets.Where(P => P.DeviceUuid == deviceUuid))
                P.IsCurrent = (P.Id == presetId);
            Dirty = true;
        }

        /// <summary>
        /// Перенести все пресеты устройства на новый идентификатор (смена UUID устройства).
        /// </summary>
        /// <param name="oldUuid">Старый идентификатор устройства</param>
        /// <param name="newUuid">Новый идентификатор устройства</param>
        /// <returns>true, если перенесён хотя бы один пресет</returns>
        public bool MoveDevicePresets(Guid oldUuid, Guid newUuid)
        {
            var DevicePresets = Content.Presets.Where(P => P.DeviceUuid == oldUuid).ToArray();
            if (DevicePresets.Length == 0)
                return false;

            foreach (var P in DevicePresets)
                P.DeviceUuid = newUuid;
            Dirty = true;
            return true;
        }

        public void RemovePreset(Guid presetId)
        {
            var Existing = Content.Presets.FirstOrDefault(P => P.Id == presetId);
            if (Existing != null)
            {
                Content.Presets.Remove(Existing);
                Dirty = true;
            }
        }

        public void Save()
        {
            if (!Dirty && File.Exists(FileName))
                return;

            var Dir = Path.GetDirectoryName(FileName);
            if (!string.IsNullOrEmpty(Dir))
                Directory.CreateDirectory(Dir);

            var Options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            };

            File.WriteAllText(FileName, JsonSerializer.Serialize(Content, Options), Encoding.UTF8);
            Dirty = false;
        }

        /// <summary>
        /// Создать пустое JSON-хранилище
        /// </summary>
        public static JsonDeviceConfigStore Create(string fileName)
        {
            return new JsonDeviceConfigStore(fileName);
        }

        public void Dispose()
        {
            Save();
        }
    }
}