using AdlpdsConfig.Service;
using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace AdlpdsConfig.Controls
{
    public partial class fConfSaver : Form
    {
        private bool _loadedFromConfig;

        private readonly AdlpdsService Svc;

        public Device CurrentDevice { get; set; }

        public string Mac { get; set; }

        private BindingList<LiveChannelCfg> ExecChannels = new();

        private BindingList<LiveControlChannelCfg> PuChannels = new();

        private BindingList<LiveStreamCfg> Streams = new();
        private BindingList<LiveControlGroupCfg> Groups = new();
        private BindingList<LiveControlPictureCfg> Pictures = new();
        private BindingList<LiveControlButtonCfg> Buttons = new();

        public DeviceConfigDtoBase? ResultConfig { get; private set; }

        private TabPage? _multicastTab;

        private readonly ComboBox cbMulticastTransport = new();
        private readonly TextBox tbMulticastOwnAddress = new();
        private readonly TextBox tbMulticastSubscriptions = new();
        private readonly Button btnMulticastRead = new();
        private readonly Button btnMulticastWrite = new();
        private readonly Label lblMulticastStatus = new();

        private bool _multicastLoaded;
        private bool _multicastBusy;
        private bool _multicastAvailable = true;

        internal fConfSaver(AdlpdsService svc, Device device)
        {
            Svc = svc;
            CurrentDevice = device;

            InitializeComponent();
        }

        private void TablesTab_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if(tablesTab.SelectedTab == _multicastTab &&
                _multicastAvailable &&
                !_multicastLoaded &&
                !_multicastBusy)
            {
                ReadMulticast();
            }
        }

        private void TablesTab_Selecting(object? sender, TabControlCancelEventArgs e)
        {
            if(e.TabPage == _multicastTab && !_multicastAvailable)
                e.Cancel = true;
        }

        private void MarkMulticastUnavailable(string? details = null)
        {
            if(!_multicastAvailable)
                return;

            _multicastAvailable = false;
            _multicastLoaded = false;
            _multicastBusy = false;

            if(_multicastTab != null)
                _multicastTab.Text = "Мультикаст (недоступен)";

            btnMulticastRead.Enabled = false;
            btnMulticastWrite.Enabled = false;
            cbMulticastTransport.Enabled = false;
            tbMulticastOwnAddress.Enabled = false;
            tbMulticastSubscriptions.Enabled = false;

            lblMulticastStatus.Text = "Мультикаст недоступен";

            if(tablesTab.SelectedTab == _multicastTab)
            {
                foreach(TabPage page in tablesTab.TabPages)
                {
                    if(page != _multicastTab)
                    {
                        tablesTab.SelectedTab = page;
                        break;
                    }
                }
            }

            string message =
                "Мультикаст недоступен. Не удалось прочитать параметры устройства.";

            if(!string.IsNullOrWhiteSpace(details))
                message += Environment.NewLine + Environment.NewLine + details;

            MessageBox.Show(
                message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private TabPage BuildMulticastTab()
        {
            var page = new TabPage("Мультикаст")
            {
                Padding = new Padding(0),
                UseVisualStyleBackColor = true
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 2,
                RowCount = 5
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            var lblTransport = new Label
            {
                Text = "Режим статусов:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 3)
            };

            cbMulticastTransport.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMulticastTransport.Dock = DockStyle.Fill;
            cbMulticastTransport.Items.AddRange(new object[]
            {
                "Broadcast (0)",
                "Multicast (1)"
            });
            cbMulticastTransport.SelectedIndex = 0;

            var lblOwnAddress = new Label
            {
                Text = "Свой адрес группы:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 3)
            };

            tbMulticastOwnAddress.Dock = DockStyle.Fill;
            tbMulticastOwnAddress.Font = new Font("Consolas", 10F);

            var lblSubscriptions = new Label
            {
                Text = "Подписки — до 32 IPv4-адресов, по одному на строку:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            tbMulticastSubscriptions.Multiline = true;
            tbMulticastSubscriptions.AcceptsReturn = true;
            tbMulticastSubscriptions.ScrollBars = ScrollBars.Vertical;
            tbMulticastSubscriptions.WordWrap = false;
            tbMulticastSubscriptions.Dock = DockStyle.Fill;
            tbMulticastSubscriptions.Font = new Font("Consolas", 10F);

            btnMulticastRead.Text = "Прочитать";
            btnMulticastRead.Size = new Size(100, 28);
            btnMulticastRead.Click += (_, _) => ReadMulticast();

            btnMulticastWrite.Text = "Записать";
            btnMulticastWrite.Size = new Size(100, 28);
            btnMulticastWrite.Click += (_, _) => WriteMulticast();

            lblMulticastStatus.AutoSize = true;
            lblMulticastStatus.Margin = new Padding(8, 7, 3, 3);
            lblMulticastStatus.Text = "Не прочитано";

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            buttons.Controls.Add(btnMulticastRead);
            buttons.Controls.Add(btnMulticastWrite);
            buttons.Controls.Add(lblMulticastStatus);

            layout.Controls.Add(lblTransport, 0, 0);
            layout.Controls.Add(cbMulticastTransport, 1, 0);

            layout.Controls.Add(lblOwnAddress, 0, 1);
            layout.Controls.Add(tbMulticastOwnAddress, 1, 1);

            layout.Controls.Add(lblSubscriptions, 0, 2);
            layout.SetColumnSpan(lblSubscriptions, 2);

            layout.Controls.Add(tbMulticastSubscriptions, 0, 3);
            layout.SetColumnSpan(tbMulticastSubscriptions, 2);

            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);

            page.Controls.Add(layout);

            return page;
        }

        private void ReadMulticast()
        {

            SetMulticastBusy(true, "Чтение...");

            try
            {
                bool sent = Svc.ReadMulticast(
                    CurrentDevice,
                    (device, cfg, ok) =>
                    {
                        if(IsDisposed || !IsHandleCreated)
                            return;

                        BeginInvoke(new Action(() =>
                        {
                            if(IsDisposed)
                                return;

                            if(!ok)
                            {
                                MarkMulticastUnavailable();
                                return;
                            }

                            SetMulticastBusy(false, "Параметры прочитаны");

                            cbMulticastTransport.SelectedIndex =
                                cfg.Transport == 1 ? 1 : 0;

                            tbMulticastOwnAddress.Text =
                                cfg.OwnAddress ?? "";

                            tbMulticastSubscriptions.Lines =
                                cfg.Subscriptions.ToArray();

                            _multicastLoaded = true;
                        }));
                    });

                if(!sent)
                    SetMulticastBusy(false, "Не удалось отправить запрос");
            }
            catch(Exception ex)
            {
                SetMulticastBusy(false, "Ошибка чтения");

                MessageBox.Show(
                    $"Не удалось прочитать настройки мультикаста:\n{ex.Message}",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void WriteMulticast()
        {
            if(CurrentDevice?.Endpoint == null)
            {
                SetMulticastBusy(false, "Устройство недоступно");
                return;
            }

            var cfg = CollectMulticastSettings();

            if(cfg == null)
                return;

            SetMulticastBusy(true, "Отправка команд...");

            try
            {
                Svc.WriteMulticast(CurrentDevice, cfg);

                _multicastLoaded = true;

                SetMulticastBusy(
                    false,
                    "Команды отправлены");
            }
            catch(Exception ex)
            {
                SetMulticastBusy(false, "Ошибка записи");

                MessageBox.Show(
                    $"Не удалось отправить настройки мультикаста:\n{ex.Message}",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetMulticastBusy(bool busy, string status)
        {
            _multicastBusy = busy;

            btnMulticastRead.Enabled = !busy;
            btnMulticastWrite.Enabled = !busy;

            cbMulticastTransport.Enabled = !busy;
            tbMulticastOwnAddress.Enabled = !busy;
            tbMulticastSubscriptions.Enabled = !busy;

            lblMulticastStatus.Text = status;
        }

        private AdlpdsService.MulticastSettings? CollectMulticastSettings()
        {
            var result = new AdlpdsService.MulticastSettings
            {
                Transport = (byte)(
                    cbMulticastTransport.SelectedIndex == 1 ? 1 : 0)
            };

            string ownAddress = tbMulticastOwnAddress.Text.Trim();

            if(ownAddress.Length > 0)
            {
                if(!IsMulticastAddress(ownAddress))
                {
                    MessageBox.Show(
                        "Свой адрес должен быть IPv4-мультикастом " +
                        "(224.0.0.0–239.255.255.255).",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return null;
                }

                result.OwnAddress = ownAddress;
            }

            foreach(string line in tbMulticastSubscriptions.Lines)
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

                if(!result.Subscriptions.Contains(address))
                    result.Subscriptions.Add(address);

                if(result.Subscriptions.Count > 32)
                {
                    MessageBox.Show(
                        "Максимальное количество подписок — 32.",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return null;
                }
            }

            return result;
        }

        private static bool IsMulticastAddress(string address)
        {
            if(!IPAddress.TryParse(address, out var ip) ||
                ip.AddressFamily !=
                    System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] bytes = ip.GetAddressBytes();

            return bytes[0] >= 224 && bytes[0] <= 239;
        }

        public void LoadFromConfig(PuColorConfigDto cfg)
        {
            _loadedFromConfig = true;

            FillCommon(cfg);

            if(cfg.Channels != null)
                PuChannels = new BindingList<LiveControlChannelCfg>(
                    cfg.Channels.OrderBy(c => c.Local).ToList());

            if(cfg.Groups != null)
                Groups = new BindingList<LiveControlGroupCfg>(
                    cfg.Groups.OrderBy(g => g.Index).ToList());

            if(cfg.Pictures != null)
                Pictures = new BindingList<LiveControlPictureCfg>(
                    cfg.Pictures.ToList());

            if(cfg.Buttons != null)
                Buttons = new BindingList<LiveControlButtonCfg>(
                    cfg.Buttons.ToList());

            dgvChannels.DataSource = PuChannels;

            if(udPriority == null)
            {
                var paramTab = BuildParametersTab(
                    out udPriority, out udPicSwitch, out udPageCount,
                    out udSwapX, out udSwapY,
                    out udBrightness, out udBrightnessSleep, out udSleepTimeout);
                tablesTab.TabPages.Add(paramTab);
            }

            Clamp(udPriority, cfg.Priority);
            Clamp(udPicSwitch, cfg.PictureSwitchTime);
            Clamp(udPageCount, cfg.PageCount);
            Clamp(udSwapX, cfg.SwapX);
            Clamp(udSwapY, cfg.SwapY);
            Clamp(udBrightness, cfg.Brightness);
            Clamp(udBrightnessSleep, cfg.BrightnessSleep);
            Clamp(udSleepTimeout, cfg.SleepTimeout);

            if(gridGroups != null) gridGroups.DataSource = new BindingSource { DataSource = Groups };
            if(gridPictures != null) gridPictures.DataSource = new BindingSource { DataSource = Pictures };
            if(gridButtons != null) gridButtons.DataSource = new BindingSource { DataSource = Buttons };

            UpdateBaseChannel();
        }

        private static void Clamp(NumericUpDown n, decimal value)
            => n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, value));

        public void LoadFromConfig(ExecConfigDto cfg)
        {
            _loadedFromConfig = true;
            FillCommon(cfg);

            if(cfg.Channels != null)
            {
                int block = cfg.Block > 0 ? cfg.Block : (int)udBlock.Value;
                int baseCh = 1 + (block - 1) * LiveConfigDefaults.ChannelsPerBlock;

                var channels = cfg.Channels.OrderBy(c => c.Local).ToList();
                foreach(var ch in channels)
                {
                    ch.GlobalIndex = baseCh + (ch.Local - 1);
                }

                ExecChannels = new BindingList<LiveChannelCfg>(channels);
                dgvChannels.DataSource = ExecChannels;
            }
            UpdateBaseChannel();
        }

        private void FillCommon(IDeviceCommon cfg)
        {
            MacValue.Text = cfg.Mac ?? "";
            tbIp.Text = cfg.Ip ?? "";
            tbMask.Text = cfg.Mask ?? "";
            tbgw.Text = cfg.Gateway ?? "";
            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, cfg.Block));
        }

        private void fConfSaver_Load(object sender, EventArgs e)
        {
            if(DesignMode || CurrentDevice == null) return;

            if(_loadedFromConfig) return;

            LoadMac();

            LoadIP();

            LoadMask();

            LoadGateway();

            if(CurrentDevice.Name == "PU")
                LoadConfig(LiveConfigDefaults.CreateControlDefault(CurrentDevice != null ? CurrentDevice.Block : 1, LiveDeviceTypes.IsColorPanel(CurrentDevice?.Name ?? "")));
            else
            {
                int count = CurrentDevice.Channels > 0 ? CurrentDevice.Channels : LiveConfigDefaults.ChannelsPerBlock;

                bool sent = Svc.ReadExecConfig(CurrentDevice, count, 2, (dev, cfg, ok) =>
                 BeginInvoke(new Action(() =>
                 {
                     if(IsDisposed)
                         return;
                     if(ok)
                         LoadConfig(cfg);
                 })));
            }

            
        }

        private void udBlock_ValueChanged(object? sender, EventArgs e)
        {
            UpdateBaseChannel();

            if(ExecChannels != null && ExecChannels.Count > 0)
            {
                int block = (int)udBlock.Value;
                int baseCh = 1 + (block - 1) * LiveConfigDefaults.ChannelsPerBlock;

                foreach(var ch in ExecChannels)
                {
                    ch.GlobalIndex = baseCh + (ch.Local - 1);
                }

                dgvChannels.DataSource = null;
                dgvChannels.DataSource = ExecChannels;
            }

            if(PuChannels != null && PuChannels.Count > 0)
            {
                int block = (int)udBlock.Value;
                int baseCh = 1 + (block - 1) * LiveConfigDefaults.ChannelsPerBlock;

                foreach(var ch in PuChannels)
                {
                    ch.Index = baseCh + (ch.Local - 1);
                }

                dgvChannels.DataSource = null;
                dgvChannels.DataSource = PuChannels;
            }
        }

        private void LoadConfig(LiveExecConfig cfg)
        {
            int baseCh = 1 + (cfg.Block - 1) * LiveConfigDefaults.ChannelsPerBlock;

            var channels = cfg.Channels
                .OrderBy(c => c.Local)
                .Select(c =>
                {
                    var clone = CloneChannel(c);
                    clone.GlobalIndex = baseCh + (clone.Local - 1);
                    return clone;
                })
                .ToList();

            ExecChannels = new BindingList<LiveChannelCfg>(channels);
            Streams = new BindingList<LiveStreamCfg>(cfg.Streams.OrderBy(s => s.Index).Select(CloneStream).ToList());

            foreach(var s in Streams)
            {
                var Name = LiveConfigDefaults.ProtocolName(LiveConfigDefaults.StreamTypeFor(s.Protocol));
                if(Name == "device")
                    Name = "нет";
                s.Protocol = Name;
            }
            dgvChannels.DataSource = ExecChannels;

            TabPage page = new TabPage($"Потоки")
            {
                Padding = new Padding(0)
            };

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,               
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                DataSource = new BindingSource { DataSource = Streams }
            };

            page.Controls.Add(grid);

            tablesTab.TabPages.Add(page);

            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, CurrentDevice.Block));
            UpdateBaseChannel();

            _multicastTab = BuildMulticastTab();
            tablesTab.TabPages.Add(_multicastTab);

            tablesTab.SelectedIndexChanged += TablesTab_SelectedIndexChanged;
            tablesTab.Selecting += TablesTab_Selecting;
        }

        private void LoadConfig(LiveControlConfig cfg)
        {
            PuChannels = new BindingList<LiveControlChannelCfg>(cfg.Channels.OrderBy(c => c.Local).Select(CloneChannel).ToList());

            dgvChannels.DataSource = PuChannels;

            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, cfg.Block));

            Groups = new BindingList<LiveControlGroupCfg>(cfg.Groups.OrderBy(g => g.Index).Select(CloneGroup).ToList());

            Pictures = new BindingList<LiveControlPictureCfg>(cfg.Pictures.Select(ClonePicture).ToList());

            Buttons = new BindingList<LiveControlButtonCfg>(cfg.Buttons.Select(CloneButton).ToList());

            var gridGroups   = CreateGrid(new BindingSource { DataSource = Groups});
            var gridPictures = CreateGrid(new BindingSource { DataSource = Pictures });
            var gridButtons  = CreateGrid(new BindingSource { DataSource = Buttons });

            tablesTab.TabPages.AddRange(new[]
            {
                MakeTab("Группы",    gridGroups),
                MakeTab("Картины",   gridPictures),
                MakeTab("Кнопки",    gridButtons),
                BuildParametersTab(
                    out udPriority, out udPicSwitch, out udPageCount,
                    out udSwapX,    out udSwapY,
                    out udBrightness, out udBrightnessSleep, out udSleepTimeout)
            });

            UpdateBaseChannel();

            _multicastTab = BuildMulticastTab();
            tablesTab.TabPages.Add(_multicastTab);

            tablesTab.SelectedIndexChanged += TablesTab_SelectedIndexChanged;
            tablesTab.Selecting += TablesTab_Selecting;
        }

        private DataGridView gridGroups, gridPictures, gridButtons;

        private NumericUpDown udPriority;
        private NumericUpDown udPicSwitch;
        private NumericUpDown udPageCount;
        private NumericUpDown udSwapX;
        private NumericUpDown udSwapY;
        private NumericUpDown udBrightness;
        private NumericUpDown udBrightnessSleep;
        private NumericUpDown udSleepTimeout;

        private static TabPage MakeTab(string title, DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;

            var page = new TabPage(title)
            {
                Padding = new Padding(0),
                UseVisualStyleBackColor = true
            };
            page.Controls.Add(grid);

            return page;
        }

        DataGridView CreateGrid(object? dataSource = null) => new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            DataSource = dataSource
        };

        private static TabPage BuildParametersTab(
                                    out NumericUpDown udPriority,
                                    out NumericUpDown udPicSwitch,
                                    out NumericUpDown udPageCount,
                                    out NumericUpDown udSwapX,
                                    out NumericUpDown udSwapY,
                                    out NumericUpDown udBrightness,
                                    out NumericUpDown udBrightnessSleep,
                                    out NumericUpDown udSleepTimeout)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(8),
                ColumnStyles =
        {
            new ColumnStyle(SizeType.Absolute, 220),
            new ColumnStyle(SizeType.AutoSize)
        }
            };

            NumericUpDown AddRow(string label, decimal value, decimal min, decimal max, decimal step = 1)
            {
                var lbl = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 12, 6)
                };

                var num = new NumericUpDown
                {
                    Minimum = min,
                    Maximum = max,
                    Value = Math.Max(min, Math.Min(max, value)),
                    Increment = step,
                    Width = 80,
                    Anchor = AnchorStyles.Left
                };

                table.RowCount++;
                table.Controls.Add(lbl, 0, table.RowCount - 1);
                table.Controls.Add(num, 1, table.RowCount - 1);
                return num;
            }

            udPriority = AddRow("Приоритет", 5, 0, 100);
            udPicSwitch = AddRow("Переход между картинами, мс", 0, 0, 60000, 10);
            udPageCount = AddRow("Число страниц", 1, 1, 255);
            udSwapX = AddRow("Поменять ось X сенсора (0-1)", 0, 0, 1);
            udSwapY = AddRow("Поменять ось Y сенсора (0-1)", 0, 0, 1);
            udBrightness = AddRow("Яркость, %", 100, 0, 100);
            udBrightnessSleep = AddRow("Яркость при сне, %", 100, 0, 100);

            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            udSleepTimeout = AddRow("Время до сна, с", 0, 0, 60000, 10);

            var page = new TabPage("Параметры") { Padding = new Padding(0) };
            page.Controls.Add(table);
            return page;
        }

        private void UpdateBaseChannel()
        {
            if(udBlock == null || BaseChannelValue == null)
                return;

            int channelInBlock = LiveConfigDefaults.ChannelsPerBlock;

            int baseCh = 1 + ((int)udBlock.Value - 1) * channelInBlock;

            BaseChannelValue.Text = baseCh.ToString();
        }

        private static LiveChannelCfg CloneChannel(LiveChannelCfg c) => new()
        {
            Local = c.Local,
            GlobalIndex = c.GlobalIndex,
            Name = c.Name,
            Control = c.Control,
            Wide = c.Wide,
            StreamId = c.StreamId,
            Patch = c.Patch,
            Preheat = c.Preheat,
            Limit = c.Limit,
            Curve = c.Curve,
            SwitchDelay = c.SwitchDelay
        };

        private static LiveStreamCfg CloneStream(LiveStreamCfg s) => new()
        {
            Index = s.Index,
            Protocol = s.Protocol,
            Universe = s.Universe,
            Address = s.Address
        };

        private static LiveControlChannelCfg CloneChannel(LiveControlChannelCfg c) => new()
        {
            Local = c.Local,
            Name = c.Name,
            Index = c.Index,
            Type = c.Type
        };

        private static LiveControlGroupCfg CloneGroup(LiveControlGroupCfg g) => new()
        {
            Local = g.Local,
            Index = g.Index,
            Channels = g.Channels?.ToList() ?? new List<int>()
        };

        private static LiveControlPictureCfg ClonePicture(LiveControlPictureCfg p) => new()
        {
            Local = p.Local,
            Name = p.Name
        };

        private static LiveControlButtonCfg CloneButton(LiveControlButtonCfg b) => new()
        {
            Local = b.Local,
            Page = b.Page,
            LocalChannel = b.LocalChannel,
            PosX = b.PosX,
            PosY = b.PosY,
            Icon = b.Icon,
            Color = b.Color
        };

        private void LoadMac()
        {
            MacValue.Text = "";
            Svc.GetMac(CurrentDevice, (mac, ok) => BeginInvoke(new Action(() =>
            {
                if(IsDisposed) return;
                MacValue.Text = ok ? mac : "";
            })));
        }

        private void LoadIP()
        {
            tbIp.Text = "";
            Svc.GetIP(CurrentDevice, (ip, ok) => BeginInvoke(new Action(() =>
            {
                if(IsDisposed) return;
                tbIp.Text = ok ? (ip ?? "") : "";
            })));
        }

        private void LoadMask()
        {
            tbMask.Text = "";
            Svc.GetMask(CurrentDevice, (mask, ok) => BeginInvoke(new Action(() =>
            {
                if(IsDisposed) return;
                tbMask.Text = ok ? (mask ?? "") : "";
            })));
        }

        private void LoadGateway()
        {
            tbgw.Text = "";

            Svc.GetGateway(CurrentDevice, (gw, ok) => BeginInvoke(new Action(() =>
            {
                if(IsDisposed) return;
                tbgw.Text = ok ? (gw ?? "") : "";
            })));
        }

        private static bool IsValidIp(string s)
        {
            if(!IPAddress.TryParse(s, out var ip))
                return false;

            return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        private static bool IsValidNetmask(string s)
        {
            if(!IPAddress.TryParse(s, out var ip) ||
                ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            var b = ip.GetAddressBytes();
            uint m = (uint)b[0] << 24 | (uint)b[1] << 16 | (uint)b[2] << 8 | b[3];

            uint inv = ~m;
            return (inv & (inv + 1)) == 0;
        }

        private void tbIp_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            string s = tb.Text;

            var parts = s.Split('.')
                         .Select(x => new string(x.Where(char.IsDigit).ToArray()))
                         .Take(4)
                         .ToList();

            for(int i = 0; i < parts.Count; i++)
            {
                if(parts[i].Length > 3) parts[i] = parts[i].Substring(0, 3);
                if(int.TryParse(parts[i], out int v) && v > 255)
                    parts[i] = "255";
            }

            string formatted = string.Join(".", parts);
            if(formatted != s)
            {
                int caret = tb.SelectionStart;
                tb.Text = formatted;
                tb.SelectionStart = Math.Min(caret, tb.Text.Length);
            }
        }

        private void tbMask_TextChanged(object sender, EventArgs e)
        {
            string s = tbMask.Text;

            var parts = s.Split('.')
                         .Select(x => new string(x.Where(char.IsDigit).ToArray()))
                         .Take(4)
                         .ToList();

            for(int i = 0; i < parts.Count; i++)
            {
                if(parts[i].Length > 3) parts[i] = parts[i].Substring(0, 3);
                if(int.TryParse(parts[i], out int v) && v > 255)
                    parts[i] = "255";
            }

            string formatted = string.Join(".", parts);
            if(formatted != s)
            {
                int caret = tbMask.SelectionStart;
                tbMask.Text = formatted;
                tbMask.SelectionStart = Math.Min(caret, tbMask.Text.Length);
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            if(!ValidateInputs())
            {
                DialogResult = DialogResult.None;
                return;
            }

            ResultConfig = BuildResult();

            DialogResult = DialogResult.OK;
            Close();
        }

        private DeviceConfigDtoBase BuildResult() => CurrentDevice.Name switch
        {
            "PU" => new PuColorConfigDto
            {
                Mac = MacValue.Text,
                Ip = tbIp.Text,
                Mask = tbMask.Text,
                Gateway = tbgw.Text,
                Block = (int)udBlock.Value,
                Brightness = (int)udBrightness.Value,
                BrightnessSleep = (int)udBrightnessSleep.Value,
                PageCount = (int)udPageCount.Value,
                PictureSwitchTime = (int)udPicSwitch.Value,
                Priority = (int)udPriority.Value,
                SleepTimeout = (int)udSleepTimeout.Value,
                SwapX = (int)udSwapX.Value,
                SwapY = (int)udSwapY.Value,
                Buttons = Buttons.ToList(),
                Groups = Groups.ToList(),
                Pictures = Pictures.ToList(),
                Channels = PuChannels.ToList(),
            },
            _ => new ExecConfigDto
            {
                Mac = MacValue.Text,
                Ip = tbIp.Text,
                Mask = tbMask.Text,
                Gateway = tbgw.Text,
                Block = (int)udBlock.Value,
                BaseChannel = BaseChannelValue.Text,
                Channels = ExecChannels.ToList(),
                Streams = Streams.ToList()
            }
        };

        private bool ValidateInputs()
        {
            if(!IsValidIp(tbIp.Text))
            {
                MessageBox.Show("Некорректный IP-адрес.\nОжидается: A.B.C.D",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbIp.Focus();
                tbIp.SelectAll();
                return false;
            }

            if(!IsValidNetmask(tbMask.Text))
            {
                MessageBox.Show("Некорректная маска подсети.\nЕдиницы должны идти подряд: 255.255.255.0",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbMask.Focus();
                tbMask.SelectAll();
                return false;
            }

            return true;
        }
    }

    public static class ConfigFile
    {
        public const string TypePropertyName = "configType";

        /// <summary>
        /// Читает значение "configType" из JSON без полной десериализации.
        /// Возвращает null, если поля нет или JSON битый.
        /// </summary>
        public static string? ReadConfigType(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty(TypePropertyName, out var p)
                    ? p.GetString()
                    : null;
            }
            catch(JsonException)
            {
                return null;
            }
        }
    }
}