using AdlpLib.Core.Modules.Types;

namespace AdlpLib.Fw
{
    /// <summary>
    /// Прошивка, упакованная в файл формата ADLFW
    /// </summary>
    public class AdlFirmware : Firmware
    {
        private readonly AdlFirmwareFile _file;

        public AdlFirmware(AdlFirmwareFile file)
        {
            _file = file ?? throw new System.ArgumentNullException(nameof(file));
        }

        public override string DeviceType => _file.DeviceType;

        public override string DeviceVersion => _file.SoftwareVersion;

        public override byte[] Data => _file.Data;
    }
}