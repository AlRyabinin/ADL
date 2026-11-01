
using AdlpdsConfig.Service;
using AdlpLib.Configuration.Application;
using AdlpLib.Core;
using AdlpLib.Models.Devices;
using System.Net;
using System.Net.Sockets;

namespace AdlpdsConfig.Controls
{
    public partial class fMulticastEditor : Form
    {
        private readonly AdlpdsService _svc;
        private readonly Device _device;

        private readonly ComboBox _transport = new();
        private readonly TextBox _ownAddress = new();
        private readonly TextBox _subscriptions = new();
        private readonly Button _read = new();
        private readonly Button _write = new();
        private readonly Label _status = new();

        private bool _busy;
        private bool _available = true;
        private bool _errorShown;
        private readonly DeviceConfigRepository _repository;
        private readonly long _deviceId;

        internal fMulticastEditor(AdlpdsService svc, Device device, DeviceConfigRepository repository, long deviceId)
        {
            _svc = svc;
            _device = device;
            _repository = repository;
            _deviceId = deviceId;

            Text = $"Мультикаст — {device.Name}";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(520, 420);
            Size = new Size(650, 520);

            BuildInterface();

            Shown += (_, _) => ReadMulticast();
        }

        private void BuildInterface()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 2,
                RowCount = 4
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            _transport.DropDownStyle = ComboBoxStyle.DropDownList;
            _transport.Items.AddRange(new object[]
            {
                "Broadcast (0)",
                "Multicast (1)"
            });
            _transport.SelectedIndex = 0;
            _transport.Dock = DockStyle.Fill;

            _ownAddress.Dock = DockStyle.Fill;
            _ownAddress.Font = new Font("Consolas", 10F);

            _subscriptions.Multiline = true;
            _subscriptions.AcceptsReturn = true;
            _subscriptions.ScrollBars = ScrollBars.Vertical;
            _subscriptions.WordWrap = false;
            _subscriptions.Dock = DockStyle.Fill;
            _subscriptions.Font = new Font("Consolas", 10F);

            _read.Text = "Прочитать";
            _read.AutoSize = true;
            _read.Click += (_, _) => ReadMulticast();

            _write.Text = "Записать";
            _write.AutoSize = true;
            _write.Click += (_, _) => WriteMulticast();

            _status.AutoSize = true;
            _status.Margin = new Padding(8, 7, 3, 3);
            _status.Text = "Ожидание чтения";

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Margin = new Padding(0)
            };

            buttons.Controls.Add(_read);
            buttons.Controls.Add(_write);
            buttons.Controls.Add(_status);

            layout.Controls.Add(
                new Label { Text = "Режим статусов:", AutoSize = true }, 0, 0);
            layout.Controls.Add(_transport, 1, 0);

            layout.Controls.Add(
                new Label { Text = "Свой адрес группы:", AutoSize = true }, 0, 1);
            layout.Controls.Add(_ownAddress, 1, 1);

            layout.Controls.Add(
                new Label
                {
                    Text = "IGMP-подписки — до 32 IPv4-адресов, по одному на строку:",
                    AutoSize = true
                }, 0, 2);

            layout.Controls.RemoveAt(layout.Controls.Count - 1);
            layout.Controls.Add(_subscriptions, 0, 2);
            layout.SetColumnSpan(_subscriptions, 2);

            layout.Controls.Add(buttons, 0, 3);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
        }

        private void ReadMulticast()
        {
            if(!_available || _busy)
                return;

            if(_device.Endpoint == null)
            {
                MarkUnavailable("Устройство не подключено.");
                return;
            }

            SetBusy(true, "Чтение...");

            try
            {
                bool sent = _svc.ReadMulticast(
                    _device,
                    (ignoredDevice, cfg, ok) =>
    {
                        if(IsDisposed || !IsHandleCreated)
                            return;

                        try
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if(IsDisposed)
                                    return;

                                if(!ok)
                                {
                                    MarkUnavailable();
                                    return;
                                }

                                _transport.SelectedIndex =
                                    cfg.Transport == 1 ? 1 : 0;

                                _ownAddress.Text = cfg.OwnAddress ?? "";
                                _subscriptions.Lines =
                                    cfg.Subscriptions.ToArray();

                                _ = SaveReadSettingsAsync(cfg);

                                SetBusy(false, "Параметры прочитаны");
                            }));
                        }
                        catch(InvalidOperationException)
                        {

                        }
                    });

                if(!sent)
                    MarkUnavailable("Не удалось отправить запрос чтения.");
            }
            catch(Exception ex)
            {
                MarkUnavailable(ex.Message);
            }
        }

        private async Task SaveReadSettingsAsync(
    AdlpdsService.MulticastSettings cfg)
        {
            try
            {
                await _repository.SaveMulticastSettingsAsync(
                    _deviceId,
                    cfg.OwnAddress,
                    cfg.Subscriptions);

                if(!IsDisposed)
                    SetBusy(false, "Параметры прочитаны и сохранены в БД");
            }
            catch(Exception ex)
            {
                if(IsDisposed)
                    return;

                SetBusy(false, "Прочитано, но БД не обновлена");

                MessageBox.Show(
                    $"Не удалось сохранить настройки в БД:\n{ex.Message}",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void WriteMulticast()
        {
            if(!_available || _busy)
                return;

            if(_device.Endpoint == null)
            {
                MarkUnavailable("Устройство не подключено.");
                return;
            }

            var cfg = CollectSettings();

            if(cfg == null)
                return;

            SetBusy(true, "Запись...");

            try
            {
                _svc.WriteMulticast(_device, cfg);

                await _repository.SaveMulticastSettingsAsync(
                    _deviceId,
                    cfg.OwnAddress,
                    cfg.Subscriptions);

                SetBusy(false, "Настройки записаны и сохранены в БД");
            }
            catch(Exception ex)
            {
                SetBusy(false, "Ошибка записи или сохранения");

                MessageBox.Show(
                    $"Ошибка при записи мультикаст-настроек:\n{ex.Message}",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private AdlpdsService.MulticastSettings? CollectSettings()
        {
            var cfg = new AdlpdsService.MulticastSettings
            {
                Transport = (byte)(_transport.SelectedIndex == 1 ? 1 : 0)
            };

            string own = _ownAddress.Text.Trim();

            if(own.Length > 0)
            {
                if(!IsMulticastAddress(own))
                {
                    MessageBox.Show(
                        "Свой адрес должен быть IPv4-мультикастом " +
                        "(224.0.0.0–239.255.255.255).",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return null;
                }

                cfg.OwnAddress = own;
            }

            foreach(string line in _subscriptions.Lines)
            {
                string address = line.Trim();

                if(address.Length == 0)
                    continue;

                if(!IsMulticastAddress(address))
                {
                    MessageBox.Show(
                        $"Некорректный адрес подписки: {address}",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return null;
                }

                if(!cfg.Subscriptions.Contains(address))
                    cfg.Subscriptions.Add(address);

                if(cfg.Subscriptions.Count > 32)
                {
                    MessageBox.Show(
                        "Максимальное количество подписок — 32.",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return null;
                }
            }

            return cfg;
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;

            _read.Enabled = !busy && _available;
            _write.Enabled = !busy && _available;
            _transport.Enabled = !busy && _available;
            _ownAddress.Enabled = !busy && _available;
            _subscriptions.Enabled = !busy && _available;

            _status.Text = status;
        }

        private void MarkUnavailable(string? details = null)
        {
            _available = false;
            SetBusy(false, "Мультикаст недоступен");

            if(_errorShown)
                return;

            _errorShown = true;

            string message =
                "Мультикаст недоступен. Не удалось прочитать параметры устройства.";

            if(!string.IsNullOrWhiteSpace(details))
                message += Environment.NewLine + Environment.NewLine + details;

            MessageBox.Show(
                this,
                message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static bool IsMulticastAddress(string address)
        {
            if(!IPAddress.TryParse(address, out var ip) ||
                ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] bytes = ip.GetAddressBytes();
            return bytes[0] >= 224 && bytes[0] <= 239;
        }
    }
}
