using ADLDeviceEmulator.Models;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class DeviceEditorForm : Form
{
    public VirtualDeviceConfig Result { get; private set; } = new();

    // Пустой конструктор нужен Visual Studio Windows Forms Designer.
    public DeviceEditorForm()
    {
        InitializeComponent();

        if(LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _transport.SelectedIndex = 0;
        _uuid.Text = Guid.NewGuid().ToString();
    }

    public DeviceEditorForm(VirtualDeviceConfig initial, bool isNew)
    {
        InitializeComponent();
        Result = initial.Clone();
        Text = isNew ? "Создание виртуального устройства" : "Изменение виртуального устройства";
        LoadConfig(initial);
    }

    private void generateUuidButton_Click(object? sender, EventArgs e) =>
        _uuid.Text = Guid.NewGuid().ToString();

    private void saveButton_Click(object? sender, EventArgs e) => SaveAndClose();

    private void LoadConfig(VirtualDeviceConfig config)
    {
        _enabled.Checked = config.Enabled;
        _type.Text = config.Type;
        _name.Text = config.DeviceName;
        _uuid.Text = config.Uuid.ToString();
        _block.Value = Math.Clamp(config.Block, (int)_block.Minimum, (int)_block.Maximum);
        _port.Value = Math.Clamp(config.Port, (int)_port.Minimum, (int)_port.Maximum);
        _ip.Text = config.IpAddress;
        _mask.Text = config.Mask;
        _gateway.Text = config.Gateway;
        _mac.Text = config.Mac;
        _channels.Text = config.ChannelIndexes;
        _transport.SelectedIndex = Math.Clamp(config.Transport, (byte)0, (byte)1);
        _ownAddress.Text = config.OwnAddress;
        _subscriptions.Text = string.Join(Environment.NewLine, config.GetSubscriptionList());
    }

    private void SaveAndClose()
    {
        string type = _type.Text.Trim().ToUpperInvariant();
        if(type.Length is < 1 or > 8)
        {
            ShowValidation("Тип устройства должен содержать от 1 до 8 символов.", _type);
            return;
        }
        if(string.IsNullOrWhiteSpace(_name.Text))
        {
            ShowValidation("Укажи название устройства.", _name);
            return;
        }
        if(!Guid.TryParse(_uuid.Text.Trim(), out Guid uuid) || uuid == Guid.Empty)
        {
            ShowValidation("Укажи корректный ненулевой UUID.", _uuid);
            return;
        }
        if(!IsIpv4(_ip.Text) || !IsIpv4(_mask.Text) || !IsIpv4(_gateway.Text))
        {
            MessageBox.Show(this, "IP, маска и шлюз должны быть корректными IPv4-адресами.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var channelTokens = _channels.Text.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if(channelTokens.Any(x => !ushort.TryParse(x, out ushort n) || n == 0))
        {
            ShowValidation("Индексы каналов должны быть целыми числами от 1 до 65535.", _channels);
            return;
        }

        string own = _ownAddress.Text.Trim();
        if(own.Length > 0 && !IsMulticastIpv4(own))
        {
            ShowValidation("Собственный адрес должен быть IPv4 multicast (224.0.0.0–239.255.255.255).", _ownAddress);
            return;
        }

        var subs = _subscriptions.Text
            .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if(subs.Count > 32)
        {
            ShowValidation("Подписок не может быть больше 32.", _subscriptions);
            return;
        }
        if(subs.Any(x => !IsMulticastIpv4(x)))
        {
            ShowValidation("Все подписки должны быть IPv4 multicast-адресами.", _subscriptions);
            return;
        }

        Result = new VirtualDeviceConfig
        {
            Enabled = _enabled.Checked,
            Type = type,
            DeviceName = _name.Text.Trim(),
            Uuid = uuid,
            Block = (int)_block.Value,
            Port = (int)_port.Value,
            IpAddress = _ip.Text.Trim(),
            Mask = _mask.Text.Trim(),
            Gateway = _gateway.Text.Trim(),
            Mac = _mac.Text.Trim(),
            ChannelIndexes = string.Join(",", channelTokens.Select(x => ushort.Parse(x)).Distinct()),
            Transport = (byte)Math.Max(0, _transport.SelectedIndex),
            OwnAddress = own,
            Subscriptions = string.Join(";", subs)
        };
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowValidation(string message, Control control)
    {
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        control.Focus();
    }

    private static bool IsIpv4(string value) =>
        IPAddress.TryParse(value.Trim(), out var ip) && ip.AddressFamily == AddressFamily.InterNetwork;

    private static bool IsMulticastIpv4(string value)
    {
        if(!IsIpv4(value)) return false;
        byte first = IPAddress.Parse(value.Trim()).GetAddressBytes()[0];
        return first >= 224 && first <= 239;
    }
}
