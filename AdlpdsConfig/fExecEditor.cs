using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpdsConfig.Service;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AdlpdsConfig
{
    /// <summary>
    /// Редактор конфигурации исполнительного устройства (IKB).
    /// Источники: устройство (живой GET_PARAM), дефолт, файл, пресет из базы.
    /// </summary>
    internal partial class fExecEditor : Form
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };

        private readonly AdlpdsService Svc;
        private readonly Device Device;
        private readonly Guid DbKey;
        private readonly IDeviceConfigStore Store;
        private readonly bool HasDevice;

        private BindingList<LiveChannelCfg> Channels = new();
        private BindingList<LiveStreamCfg> Streams = new();

        private bool Busy;

        public fExecEditor()
        {
            InitializeComponent();

            dgvChannels.DataSource = Channels;
            dgvStreams.DataSource = Streams;
        }

        public fExecEditor(AdlpdsService svc, Device device, Guid dbKey, IDeviceConfigStore store) : this()
        {
            Svc = svc;
            Device = device;
            DbKey = dbKey;
            Store = store;
            HasDevice = device != null;

            Text = HasDevice
                ? $"Редактор исполнительного устройства — {Device.Name} ({Device.Endpoint})"
                : "Редактор исполнительного устройства";

            UpdateBaseChannel();
        }

        private void fExecEditor_Load(object? sender, EventArgs e)
        {
            if (DesignMode)
                return;

            var wa = Screen.FromControl(this).WorkingArea;
            if (Width > wa.Width)
                Width = wa.Width;
            if (Height > wa.Height)
                Height = wa.Height;

            int block = HasDevice ? Device.Block : 1;
            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, block));

            var preset = Store?.GetCurrentPreset(DbKey);
            if (preset?.Exec != null)
            {
                LoadConfig(preset.Exec);
                lStatus.Text = "Загружен текущий пресет «" + preset.PresetName + "»";
            }
            else
            {
                LoadConfig(LiveConfigDefaults.CreateExecDefault(HasDevice ? Device.Block : 1));
            }
            UpdatePresetButton();
        }

        private void udBlock_ValueChanged(object? sender, EventArgs e) => UpdateBaseChannel();

        private void dgvChannels_DataError(object? sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;

        private void dgvStreams_DataError(object? sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;

        private void bDefault_Click(object? sender, EventArgs e) => LoadConfig(LiveConfigDefaults.CreateExecDefault((int)udBlock.Value));

        private void bClose_Click(object? sender, EventArgs e) => Close();

        private void UpdateBaseChannel()
        {
            if (udBlock == null || lBaseChannel == null)
                return;
            int baseCh = 1 + ((int)udBlock.Value - 1) * LiveConfigDefaults.ChannelsPerBlock;
            lBaseChannel.Text = $"Блок {udBlock.Value}: базовый канал {baseCh}";
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

        private void LoadConfig(LiveExecConfig cfg)
        {
            Channels = new BindingList<LiveChannelCfg>(cfg.Channels.OrderBy(c => c.Local).Select(CloneChannel).ToList());
            Streams = new BindingList<LiveStreamCfg>(cfg.Streams.OrderBy(s => s.Index).Select(CloneStream).ToList());
            foreach (var s in Streams)
            {
                var Name = LiveConfigDefaults.ProtocolName(LiveConfigDefaults.StreamTypeFor(s.Protocol));
                if (Name == "device")
                    Name = "нет";
                s.Protocol = Name;
            }
            dgvChannels.DataSource = Channels;
            dgvStreams.DataSource = Streams;
            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, cfg.Block));
            UpdateBaseChannel();
            lStatus.Text = $"Загружено: {Channels.Count} каналов, {Streams.Count} потоков";
        }

        private LiveExecConfig BuildConfig()
        {
            var cfg = new LiveExecConfig { Block = (int)udBlock.Value };
            cfg.Channels.AddRange(Channels.Select(CloneChannel));
            cfg.Streams.AddRange(Streams.Select(CloneStream));
            return cfg;
        }

        private void SetBusy(bool busy, string text)
        {
            Busy = busy;
            lStatus.Text = text;
            bRead.Enabled = !busy;
            bDefault.Enabled = !busy;
            bOpenFile.Enabled = !busy;
            bWrite.Enabled = !busy;
            bSavePreset.Enabled = !busy;
            bLoadPreset.Enabled = !busy;
            bSaveFile.Enabled = !busy;
        }

        private void bRead_Click(object? sender, EventArgs e)
        {
            if (!HasDevice)
            {
                MessageBox.Show("Нет устройства.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int count = Device.Channels > 0 ? Device.Channels : LiveConfigDefaults.ChannelsPerBlock;
            SetBusy(true, $"Чтение с устройства {Device.Endpoint}…");
            bool sent = Svc.ReadExecConfig(Device, count, 2, (dev, cfg, ok) =>
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed)
                        return;
                    SetBusy(false, ok
                        ? "Конфигурация прочитана с устройства"
                        : "Не удалось прочитать конфигурацию с устройства (таймаут).");
                    if (ok)
                        LoadConfig(cfg);
                })));
            if (!sent)
            {
                SetBusy(false, "Не удалось отправить запрос.");
                MessageBox.Show("Не удалось отправить запрос.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void bWrite_Click(object? sender, EventArgs e)
        {
            if (!HasDevice)
            {
                MessageBox.Show("Нет устройства.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cfg = BuildConfig();
            SetBusy(true, $"Запись на устройство {Device.Endpoint}…");
            bool sent = Svc.WriteExecConfig(Device, cfg, (dev, ok) =>
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed)
                        return;
                    SetBusy(false, ok
                        ? "Конфигурация записана на устройство"
                        : "Не удалось записать конфигурацию (таймаут).");
                })));
            if (!sent)
            {
                SetBusy(false, "Не удалось отправить запрос.");
                MessageBox.Show("Не удалось отправить запрос.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void bOpenFile_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Открыть конфигурацию",
                Filter = "Config files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var cfg = JsonSerializer.Deserialize<LiveExecConfig>(File.ReadAllText(dlg.FileName));
                if (cfg == null)
                    throw new Exception("Пустой файл.");
                LoadConfig(cfg);
                lStatus.Text = "Загружено из файла " + dlg.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть файл:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void bSaveFile_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Сохранить конфигурацию",
                Filter = "Config files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "exec-config.json"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(BuildConfig(), JsonOpts));
                lStatus.Text = "Сохранено в файл " + dlg.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить файл:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdatePresetButton()
        {
            string? current = Store?.GetCurrentPreset(DbKey)?.PresetName;
            bLoadPreset.Enabled = !string.IsNullOrEmpty(current);
            bLoadPreset.Text = current != null ? $"Текущий пресет: {current}" : "Текущий пресет";
        }

        private void bLoadPreset_Click(object? sender, EventArgs e)
        {
            var preset = Store.GetCurrentPreset(DbKey);
            if (preset?.Exec == null)
            {
                MessageBox.Show("Нет текущего пресета для этого устройства.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadConfig(preset.Exec);
            lStatus.Text = "Загружен текущий пресет «" + preset.PresetName + "»";
        }

        private void bSavePreset_Click(object? sender, EventArgs e)
        {
            string? name = ShowPrompt("Имя пресета", Store?.GetCurrentPreset(DbKey)?.PresetName ?? $"Конфигурация {Device.Name} блок {(int)udBlock.Value}");
            if (name == null)
                return;
            name = name.Trim();
            if (name.Length == 0)
                return;

            var existing = Store.GetCurrentPreset(DbKey);
            if (existing == null)
            {
                existing = Store.GetPresets(DbKey).FirstOrDefault(p => p.PresetName == name);
            }

            var preset = new DeviceConfigPreset
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                DeviceUuid = DbKey,
                DeviceType = LiveDeviceTypes.Exec,
                DeviceName = Device.Name ?? "IKB",
                PresetName = name,
                Block = (int)udBlock.Value,
                IsCurrent = true,
                Exec = BuildConfig()
            };
            Store.AddOrUpdatePreset(preset);
            Store.SetCurrent(DbKey, preset.Id);
            Store.Save();

            UpdatePresetButton();
            lStatus.Text = "Пресет «" + name + "» сохранён в базе " + Store.Location;
        }

        /// <summary>
        /// Простое окно ввода строки; возвращает null при отмене
        /// </summary>
        private string? ShowPrompt(string caption, string value)
        {
            using var form = new Form
            {
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(360, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var tb = new TextBox { Location = new Point(12, 12), Width = 336, Text = value };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(160, 60), Size = new Size(90, 28) };
            var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(256, 60), Size = new Size(90, 28) };
            form.Controls.Add(tb);
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            return form.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
        }
    }
}