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
    /// Редактор конфигурации гейта (INDIGATE — пиксельгейт).
    /// Каналы отображения состояния линий управления (PARAM_OBJECT_PIXELGATE_CHANNEL).
    /// Источники: устройство (живой GET_PARAM), дефолт, файл, пресет из базы.
    /// </summary>
    internal partial class fGateEditor : Form
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

        private BindingList<LivePixelgateChannelCfg> Channels = new();

        private bool Busy;

        public fGateEditor()
        {
            InitializeComponent();

            dgvChannels.DataSource = Channels;
        }

        public fGateEditor(AdlpdsService svc, Device device, Guid dbKey, IDeviceConfigStore store) : this()
        {
            Svc = svc;
            Device = device;
            DbKey = dbKey;
            Store = store;
            HasDevice = device != null;

            Text = HasDevice
                ? $"Редактор гейта — {Device.Name} ({Device.Endpoint})"
                : "Редактор гейта";
        }

        private void fGateEditor_Load(object? sender, EventArgs e)
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
            if (preset?.Gate != null)
            {
                LoadConfig(preset.Gate);
                lStatus.Text = "Загружен текущий пресет «" + preset.PresetName + "»";
            }
            else
            {
                LoadConfig(LiveConfigDefaults.CreateGateDefault(block));
            }
            UpdatePresetButton();
        }

        private void udBlock_ValueChanged(object? sender, EventArgs e) => UpdateBaseChannel();

        private void dgvChannels_DataError(object? sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;

        private void bDefault_Click(object? sender, EventArgs e) => LoadConfig(LiveConfigDefaults.CreateGateDefault((int)udBlock.Value));

        private void bClose_Click(object? sender, EventArgs e) => Close();

        private void UpdateBaseChannel()
        {
            if (udBlock == null || lBaseChannel == null)
                return;
            int baseCh = 1 + ((int)udBlock.Value - 1) * LiveConfigDefaults.ChannelsPerBlock;
            lBaseChannel.Text = $"Блок {udBlock.Value}: базовый канал {baseCh}";
        }

        private static LivePixelgateChannelCfg CloneChannel(LivePixelgateChannelCfg c) => new()
        {
            Local = c.Local,
            Name = c.Name,
            Index = c.Index
        };

        private void LoadConfig(LiveGateConfig cfg)
        {
            Channels = new BindingList<LivePixelgateChannelCfg>(
                cfg.Channels.OrderBy(c => c.Local).Select(CloneChannel).ToList());

            dgvChannels.DataSource = Channels;

            udBlock.Value = Math.Max(udBlock.Minimum, Math.Min(udBlock.Maximum, cfg.Block));

            UpdateBaseChannel();
            lStatus.Text = $"Загружено: {Channels.Count} каналов отображения";
        }

        private LiveGateConfig BuildConfig()
        {
            return new LiveGateConfig
            {
                Block = (int)udBlock.Value,
                Channels = Channels.Select(CloneChannel).ToList()
            };
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

            SetBusy(true, $"Чтение с устройства {Device.Endpoint}…");
            bool sent = Svc.ReadGateConfig(Device, (dev, cfg, ok) =>
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
            bool sent = Svc.WriteGateConfig(Device, cfg, (dev, ok) =>
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
                Title = "Открыть конфигурацию гейта",
                Filter = "Config files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var cfg = JsonSerializer.Deserialize<LiveGateConfig>(File.ReadAllText(dlg.FileName));
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
                Title = "Сохранить конфигурацию гейта",
                Filter = "Config files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "gate-config.json"
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
            if (preset?.Gate == null)
            {
                MessageBox.Show("Нет текущего пресета для этого устройства.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadConfig(preset.Gate);
            lStatus.Text = "Загружен текущий пресет «" + preset.PresetName + "»";
        }

        private void bSavePreset_Click(object? sender, EventArgs e)
        {
            string? name = ShowPrompt("Имя пресета", Store?.GetCurrentPreset(DbKey)?.PresetName ?? $"Конфигурация {Device?.Name ?? "гейт"} блок {(int)udBlock.Value}");
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
                DeviceType = LiveDeviceTypes.Gate,
                DeviceName = Device?.Name ?? "INDIGATE",
                PresetName = name,
                Block = (int)udBlock.Value,
                IsCurrent = true,
                Gate = BuildConfig()
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
