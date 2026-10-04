using System;
using System.Collections.Generic;
using System.Linq;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// Пресет конфигурации устройства (именованный набор настроек).
    /// Актуальная конфигурация = выбранные пресеты (IsCurrent) по каждому UUID устройства.
    /// </summary>
    public class DeviceConfigPreset
    {
        /// <summary>Идентификатор пресета</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>UUID устройства, к которому относится пресет</summary>
        public Guid DeviceUuid { get; set; }

        /// <summary>Тип устройства (IKB/PU24)</summary>
        public string DeviceType { get; set; } = "";

        /// <summary>Имя устройства</summary>
        public string DeviceName { get; set; } = "";

        /// <summary>Имя пресета (например "ИКБ 1")</summary>
        public string PresetName { get; set; } = "";

        /// <summary>Номер блока ADLD</summary>
        public int Block { get; set; } = 1;

        /// <summary>true - пресет входит в актуальную конфигурацию (один на устройство)</summary>
        public bool IsCurrent { get; set; }

        /// <summary>Конфигурация исполнительного устройства (для типа IKB)</summary>
        public LiveExecConfig Exec { get; set; }

        /// <summary>Конфигурация управляющего устройства (для типа PU24)</summary>
        public LiveControlConfig Control { get; set; }

        /// <summary>Конфигурация гейта (для типа INDIGATE)</summary>
        public LiveGateConfig Gate { get; set; }

        public override string ToString()
        {
            string Kind = (Exec != null) ? "exec" : (Control != null) ? "control" : (Gate != null) ? "gate" : "?";
            return $"{PresetName} [{DeviceType} block {Block}] {(IsCurrent ? "*" : "")} ({Kind})";
        }
    }

    /// <summary>
    /// Хранилище конфигураций устройств (пресеты + актуальная конфигурация).
    /// Промежуточный слой, чтобы позже заменить на сервер конфигураций.
    /// </summary>
    public interface IDeviceConfigStore : IDisposable
    {
        /// <summary>Расположение хранилища</summary>
        string Location { get; }

        /// <summary>Все пресеты</summary>
        IEnumerable<DeviceConfigPreset> GetPresets();

        /// <summary>Пресеты устройства</summary>
        IEnumerable<DeviceConfigPreset> GetPresets(Guid deviceUuid);

        /// <summary>Текущий (входящий в актуальную конфигурацию) пресет устройства</summary>
        DeviceConfigPreset GetCurrentPreset(Guid deviceUuid);

        /// <summary>Добавить или обновить пресет</summary>
        void AddOrUpdatePreset(DeviceConfigPreset preset);

        /// <summary>Сделать пресет актуальным для устройства (и единственным)</summary>
        void SetCurrent(Guid deviceUuid, Guid presetId);

        /// <summary>Удалить пресет</summary>
        void RemovePreset(Guid presetId);

        /// <summary>Перенести пресеты устройства на новый идентификатор (смена UUID устройства)</summary>
        bool MoveDevicePresets(Guid oldUuid, Guid newUuid);

        /// <summary>Сохранить изменения</summary>
        void Save();
    }
}