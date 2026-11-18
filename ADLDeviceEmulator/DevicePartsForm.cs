using ADLDeviceEmulator.Models;
using System.ComponentModel;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class DevicePartsForm : Form
{
    private VirtualDeviceConfig? _config;
    private VirtualDeviceConfig? _targetConfig;

    // Пустой конструктор оставлен для Visual Studio Designer.
    public DevicePartsForm()
    {
        InitializeComponent();
        if(LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;
    }

    public DevicePartsForm(VirtualDeviceConfig config)
    {
        InitializeComponent();
        _targetConfig = config ?? throw new ArgumentNullException(nameof(config));
        _config = config.Clone();
        _config.EnsurePartsInitialized();
        Text = $"Состав устройства — {_config.Display}";
        _deviceLabel.Text = $"{_config.Type} / {_config.DeviceName} / UUID {_config.Uuid}";
        ConfigureVisibleSections();
        RefreshCounts();
    }

    private void ConfigureVisibleSections()
    {
        if(_config == null) return;
        string type = VirtualDeviceConfig.NormalizeType(_config.Type);
        bool exec = VirtualDeviceConfig.IsExecType(type);
        bool control = VirtualDeviceConfig.IsControlType(type);
        bool gate = VirtualDeviceConfig.IsGateType(type);

        _execChannelsButton.Visible = exec;
        _streamsButton.Visible = exec;
        _controlChannelsButton.Visible = control;
        _groupsButton.Visible = control;
        _picturesButton.Visible = control;
        _buttonsButton.Visible = type == "PU";
        _pixelgateChannelsButton.Visible = gate;
        _gateConveyorsButton.Visible = gate;
    }

    private void OpenPart(DevicePartKind kind)
    {
        if(_config == null) return;
        using var editor = new DevicePartEditorForm(_config, kind);
        if(editor.ShowDialog(this) == DialogResult.OK)
        {
            _config.SyncLegacyChannelIndexes();
            RefreshCounts();
        }
    }

    private void deviceParametersButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.DeviceParameters);
    private void execChannelsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.ExecChannels);
    private void streamsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.Streams);
    private void controlChannelsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.ControlChannels);
    private void groupsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.Groups);
    private void picturesButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.Pictures);
    private void buttonsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.Buttons);
    private void pixelgateChannelsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.PixelgateChannels);
    private void gateConveyorsButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.GateConveyors);
    private void igmpButton_Click(object? sender, EventArgs e) => OpenPart(DevicePartKind.Igmp);

    private void RefreshCounts()
    {
        if(_config == null) return;
        _execChannelsButton.Text = $"Каналы EXEC ({_config.ExecChannels.Count})…";
        _streamsButton.Text = $"Потоки ({_config.Streams.Count})…";
        _controlChannelsButton.Text = $"Каналы ПУ ({_config.ControlChannels.Count})…";
        _groupsButton.Text = $"Группы ({_config.Groups.Count})…";
        _picturesButton.Text = $"Картины ({_config.Pictures.Count})…";
        _buttonsButton.Text = $"Кнопки ({_config.Buttons.Count})…";
        _pixelgateChannelsButton.Text = $"Каналы PixelGate ({_config.PixelgateChannels.Count})…";
        _gateConveyorsButton.Text = $"Конвейеры Gate ({_config.GateConveyors.Count})…";
        _igmpButton.Text = $"IGMP-подписки ({_config.GetSubscriptionList().Length}/32)…";
    }

    private void closeButton_Click(object? sender, EventArgs e)
    {
        if(_config == null || _targetConfig == null)
            return;

        _config.SyncLegacyChannelIndexes();
        _targetConfig.CopyFrom(_config);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
