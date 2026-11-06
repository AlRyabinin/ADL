using AdlpLib.Configuration.Application;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices.NetGate
{
    /// <summary>
    /// Форма редактирования параметров устройства NetGate.
    /// </summary>
    public partial class EditNetGateParametersForm : Form
    {
        private readonly DeviceConfigRepository _repository;

        /// <summary>Редактируемое устройство.</summary>
        public NetGateConfigDto CurrentDevice { get; set; }

        private BindingList<NetGateConveyorCfg> _conveyors = new();
        private BindingList<NetGateChannelCfg> _channels = new();
        private BindingList<NetGatePictureCfg> _pictures = new();

        public EditNetGateParametersForm()
        {
            InitializeComponent();
        }

        public EditNetGateParametersForm(NetGateConfigDto device, IReadOnlyList<ListSidEntity> sidRecords, DeviceConfigRepository repository)
        {
            InitializeComponent();

            _repository = repository;
            CurrentDevice = device ?? throw new ArgumentNullException(nameof(device));

            NameTextBox.Text = device.Name ?? string.Empty;
            MacTextBox.Text = device.Mac ?? string.Empty;
            IpTextBox.Text = device.Ip ?? string.Empty;
            MaskTextBox.Text = device.Mask ?? string.Empty;
            textBox1.Text = device.Gateway ?? string.Empty;

            LoadSidRecords(sidRecords ?? Array.Empty<ListSidEntity>(), device.PkListSid);
            LoadAllTabs();
        }

        private sealed class SidComboItem
        {
            public string? Key { get; set; }
            public string Caption { get; set; } = string.Empty;
        }

        private void LoadSidRecords(
            IReadOnlyList<ListSidEntity> records,
            string? selectedKey)
        {
            var items = new List<SidComboItem>
            {
                new() { Key = null, Caption = "— Не назначен —" }
            };

            items.AddRange(records.Select(x => new SidComboItem
            {
                Key = x.PkListSid,
                Caption = $"{x.NameObjSid} ({x.PkListSid})"
            }));

            // Не теряем ранее сохранённый ключ, даже если вызывающий код
            // пока не передал полный список listSID.
            if(!string.IsNullOrWhiteSpace(selectedKey) &&
               !items.Any(x => string.Equals(
                   x.Key, selectedKey, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(new SidComboItem
                {
                    Key = selectedKey,
                    Caption = $"Не найден в listSID ({selectedKey})"
                });
            }

            _cbSid.DisplayMember = nameof(SidComboItem.Caption);
            _cbSid.ValueMember = nameof(SidComboItem.Key);
            _cbSid.DataSource = items;

            int selectedIndex = items.FindIndex(x =>
                string.Equals(x.Key, selectedKey, StringComparison.OrdinalIgnoreCase));

            _cbSid.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        // =====================================================================
        //  Загрузка вкладок
        // =====================================================================

        private void LoadAllTabs()
        {
            // --- Конвейеры (первая вкладка в дизайнере) ---
            tabPageConveyors.Text = "Конвейеры";

            _conveyors = new BindingList<NetGateConveyorCfg>(
                (CurrentDevice.Conveyors ?? new List<NetGateConveyorCfg>())
                    .OrderBy(c => c.Local)
                    .ToList());

            ConveyorsGrid.AutoGenerateColumns = true;
            ConveyorsGrid.DataSource = _conveyors;
            SetupConveyorColumns();

            ConveyorsGrid.CellFormatting += ConveyorsGrid_CellFormatting;

            // --- Каналы ---
            _channels = new BindingList<NetGateChannelCfg>(
                (CurrentDevice.Channels ?? new List<NetGateChannelCfg>())
                    .OrderBy(c => c.Local)
                    .ToList());

            ChannelsGrid.AutoGenerateColumns = true;
            ChannelsGrid.DataSource = _channels;
            SetupChannelColumns();

            // --- Картинки ---
            _pictures = new BindingList<NetGatePictureCfg>(
                (CurrentDevice.Pictures ?? new List<NetGatePictureCfg>())
                    .OrderBy(p => p.Local)
                    .ToList());

            PicturesGrid.AutoGenerateColumns = true;
            PicturesGrid.DataSource = _pictures;
            SetupPictureColumns();

            // Устанавливаем выбранное значение из текущего количества конвейеров
            int current = _conveyors.Count;
            // Если значение не 4 и не 8 (например, из JSON пришло 0 или 6) — берём ближайшее допустимое
            if(current != 4 && current != 8)
            {
                current = current <= 4 ? 4 : 8;

                // Синхронизируем сам список под допустимое число
                while(_conveyors.Count < current)
                {
                    int local = _conveyors.Count + 1;
                    _conveyors.Add(new NetGateConveyorCfg
                    {
                        Local = local,
                        SourceA = new NetGateSourceCfg { Protocol = "sACN", Universe = local },
                        SourceB = new NetGateSourceCfg { Protocol = "artnet", Universe = local - 1 },
                        SourceC = new NetGateSourceCfg { Protocol = "artnet", Universe = local - 1 },
                        Destination = new NetGateSourceCfg { Protocol = "dmx", Universe = local - 1 },
                        SumBTo = 255,
                        SumCFrom = 256,
                        SumCTo = 511
                    });
                }
                while(_conveyors.Count > current)
                {
                    _conveyors.RemoveAt(_conveyors.Count - 1);
                }
                CurrentDevice.ConveyorCount = current;
            }

            _cbConveyorCount.SelectedItem = current.ToString();
        }

        // =====================================================================
        //  Настройка колонок
        // =====================================================================

        private static void SetupChannelColumns()
        {
            // Настраивается в дизайнере, здесь оставлено для симметрии
        }

        private void SetupPictureColumns()
        {
            var g = PicturesGrid;

            void Header(string name, string text) { if(g.Columns[name] != null) g.Columns[name].HeaderText = text; }

            Header("Local", "№");
            Header("Picture", "Картина");
            Header("FadeIn", "Fade In, мс");
            Header("FadeOut", "Fade Out, мс");
            Header("Mux1", "Mux1");
            Header("Mux2", "Mux2");
            Header("Mux3", "Mux3");
            Header("Mux4", "Mux4");

            if(g.Columns["Local"] != null) g.Columns["Local"].ReadOnly = true;
        }

        private void SetupConveyorColumns()
        {
            var g = ConveyorsGrid;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();

            void AddText(string prop, string header, int width = 70)
            {
                g.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = prop,
                    DataPropertyName = prop,
                    HeaderText = header,
                    Width = width
                });
            }

            AddText("Local", "№", 40);

            // Псевдо-колонки для источников — заполняются в CellFormatting
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourceAView", HeaderText = "Источник A", Width = 90 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourceBView", HeaderText = "Источник B", Width = 90 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourceCView", HeaderText = "Источник C", Width = 90 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "DestView", HeaderText = "Приёмник", Width = 90 });

            AddText("SourceSelect", "Выбор", 60);
            AddText("SumType", "Смешив.", 70);
            AddText("Frequency", "Частота", 60);
            AddText("Recordable", "Запись", 55);
            AddText("Timeouted", "Таймаут", 60);
            AddText("RdmEnabled", "RDM", 50);
            AddText("SumBFrom", "B от", 55);
            AddText("SumBTo", "B до", 55);
            AddText("SumCFrom", "C от", 55);
            AddText("SumCTo", "C до", 55);
        }

        private void ConveyorsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if(e.RowIndex < 0 || e.RowIndex >= _conveyors.Count) return;
            var cv = _conveyors[e.RowIndex];
            string col = ConveyorsGrid.Columns[e.ColumnIndex].Name;

            switch(col)
            {
                case "SourceAView": e.Value = Fmt(cv.SourceA); break;
                case "SourceBView": e.Value = Fmt(cv.SourceB); break;
                case "SourceCView": e.Value = Fmt(cv.SourceC); break;
                case "DestView": e.Value = Fmt(cv.Destination); break;
            }

            static string Fmt(NetGateSourceCfg s)
                => s == null ? "нет 0" : $"{s.Protocol} {s.Universe}";
        }

        private void BtnConveyorProps_Click(object sender, EventArgs e)
        {
            if(ConveyorsGrid.CurrentRow == null) return;

            var cv = _conveyors[ConveyorsGrid.CurrentRow.Index];
            using var dlg = new ConveyorPropertiesForm(cv);
            if(dlg.ShowDialog(this) == DialogResult.OK)
            {
                ConveyorsGrid.Refresh();
            }
        }

        private void ConveyorsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex < 0) return;
            BtnConveyorProps_Click(sender, EventArgs.Empty);
        }

        private void CbConveyorCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(_cbConveyorCount.SelectedItem == null) return;

            int newCount = int.Parse(_cbConveyorCount.SelectedItem.ToString());

            while(_conveyors.Count < newCount)
            {
                int local = _conveyors.Count + 1;
                _conveyors.Add(new NetGateConveyorCfg
                {
                    Local = local,
                    SourceA = new NetGateSourceCfg { Protocol = "sACN", Universe = local },
                    SourceB = new NetGateSourceCfg { Protocol = "artnet", Universe = local - 1 },
                    SourceC = new NetGateSourceCfg { Protocol = "artnet", Universe = local - 1 },
                    Destination = new NetGateSourceCfg { Protocol = "dmx", Universe = local - 1 },
                    SumBTo = 255,
                    SumCFrom = 256,
                    SumCTo = 511
                });
            }
            while(_conveyors.Count > newCount)
            {
                _conveyors.RemoveAt(_conveyors.Count - 1);
            }

            CurrentDevice.ConveyorCount = newCount;
        }
        private static void SetNumericValue(NumericUpDown n, decimal value)
        {
            if(n != null) n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        // =====================================================================
        //  Валидация
        // =====================================================================

        private void MacTextBox_Validating(object sender, CancelEventArgs e)
        {
            // MAC необязателен. Проверяем формат, только если поле заполнено.
            if(!string.IsNullOrWhiteSpace(MacTextBox.Text) &&
               !IsValidMac(MacTextBox.Text))
            {
                MessageBox.Show("Некорректный MAC-адрес!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        public static bool IsValidMac(string mac)
        {
            if(string.IsNullOrWhiteSpace(mac)) return false;
            const string pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$";
            return Regex.IsMatch(mac, pattern);
        }

        private void IpTextBox_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidIp(IpTextBox.Text))
            {
                MessageBox.Show("Некорректный IP-адрес!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        public static bool IsValidIp(string ip)
        {
            if(string.IsNullOrWhiteSpace(ip)) return false;
            const string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, pattern);
        }

        private void MaskTextBox_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidMask(MaskTextBox.Text))
            {
                MessageBox.Show("Некорректная маска подсети! Пример: 255.255.255.0", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        public static bool IsValidMask(string mask)
        {
            if(!IsValidIp(mask)) return false;
            string[] parts = mask.Split('.');
            uint maskValue = 0;
            foreach(var part in parts) maskValue = (maskValue << 8) | byte.Parse(part);
            uint inverted = ~maskValue;
            return (inverted & (inverted + 1)) == 0;
        }

        // =====================================================================
        //  Сохранение / отмена
        // =====================================================================

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // 1. Имя
            if(string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Укажите название устройства.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                NameTextBox.Focus();
                return;
            }

            // 2. MAC необязателен, но если указан — должен иметь корректный формат.
            if(!string.IsNullOrWhiteSpace(MacTextBox.Text) &&
               !IsValidMac(MacTextBox.Text))
            {
                MessageBox.Show("Укажите корректный MAC-адрес или оставьте поле пустым.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MacTextBox.Focus();
                return;
            }

            // 3. IP
            if(string.IsNullOrWhiteSpace(IpTextBox.Text) || !IsValidIp(IpTextBox.Text))
            {
                MessageBox.Show("Укажите корректный IP-адрес.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IpTextBox.Focus();
                return;
            }

            // 4. Маска
            if(string.IsNullOrWhiteSpace(MaskTextBox.Text) || !IsValidMask(MaskTextBox.Text))
            {
                MessageBox.Show("Укажите корректную маску подсети.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MaskTextBox.Focus();
                return;
            }

            // 5. SID необязателен. Пустой выбор означает NULL в Devices.PKlistSID.

            // 6. Конвейеры: ровно 4 или 8, каждый полностью заполнен
            if(_conveyors.Count != 4 && _conveyors.Count != 8)
            {
                MessageBox.Show("Количество конвейеров должно быть 4 или 8.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for(int i = 0; i < _conveyors.Count; i++)
            {
                var cv = _conveyors[i];
                if(cv.SourceA == null || cv.SourceB == null ||
                   cv.SourceC == null || cv.Destination == null)
                {
                    MessageBox.Show($"Конвейер {cv.Local}: не все источники заполнены.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if(string.IsNullOrWhiteSpace(cv.SourceA.Protocol) ||
                   string.IsNullOrWhiteSpace(cv.SourceB.Protocol) ||
                   string.IsNullOrWhiteSpace(cv.SourceC.Protocol) ||
                   string.IsNullOrWhiteSpace(cv.Destination.Protocol))
                {
                    MessageBox.Show($"Конвейер {cv.Local}: укажите протоколы для всех источников.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if(cv.SumBFrom > cv.SumBTo || cv.SumCFrom > cv.SumCTo)
                {
                    MessageBox.Show($"Конвейер {cv.Local}: диапазон «от/до» задан неверно.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // --- Всё ок, сохраняем ---
            CurrentDevice.Name = NameTextBox.Text.Trim();
            CurrentDevice.Mac = MacTextBox.Text.Trim();
            CurrentDevice.Ip = IpTextBox.Text.Trim();
            CurrentDevice.Mask = MaskTextBox.Text.Trim();
            CurrentDevice.Gateway = textBox1.Text.Trim();
            CurrentDevice.PkListSid = _cbSid.SelectedValue as string;

            CurrentDevice.ConveyorCount = _conveyors.Count;
            CurrentDevice.Conveyors = _conveyors.ToList();
            CurrentDevice.Channels = _channels.ToList();
            CurrentDevice.Pictures = _pictures.ToList();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private async void _btnManageSid_Click(object sender, EventArgs e)
        {
            using var form = new ListSidManagerForm(_repository);
            form.ShowDialog(this);

            var records = await _repository.GetListSidsAsync();
            LoadSidRecords(
                records,
                form.CreatedPkListSid ?? CurrentDevice.PkListSid);
        }
    }
}