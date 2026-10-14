using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    public partial class EditDeviceParametersForm : Form
    {
        public DeviceConfigDtoBase CurrentDevice { get; set; }

        private BindingList<LiveChannelCfg> ExecChannels = new();
        private BindingList<LiveControlChannelCfg> PuChannels = new();
        private BindingList<LiveControlGroupCfg> Groups = new();
        private BindingList<LiveControlPictureCfg> Pictures = new();
        private BindingList<LiveControlButtonCfg> Buttons = new();

        private NumericUpDown udPriority;
        private NumericUpDown udPicSwitch;
        private NumericUpDown udPageCount;
        private NumericUpDown udSwapX;
        private NumericUpDown udSwapY;
        private NumericUpDown udBrightness;
        private NumericUpDown udBrightnessSleep;
        private NumericUpDown udSleepTimeout;

        public EditDeviceParametersForm()
        {
            InitializeComponent();
        }

        public EditDeviceParametersForm(DeviceConfigDtoBase newDevice)
        {
            InitializeComponent();
            CurrentDevice = newDevice;

            NameTextBox.Text = string.IsNullOrEmpty(newDevice.Name) ? string.Empty : newDevice.Name;
            MacTextBox.Text = newDevice.Mac ?? "";
            IpTextBox.Text = newDevice.Ip ?? "";
            MaskTextBox.Text = newDevice.Mask ?? "";

            LoadAllTabs();
        }

        /// <summary>
        /// Загружает данные и создает вкладки в зависимости от типа устройства.
        /// </summary>
        private void LoadAllTabs()
        {
            // Очищаем вкладки, оставляя только "Каналы" (первую)
            if(tablesTab.TabPages.Count > 1)
            {
                for(int i = tablesTab.TabPages.Count - 1; i > 0; i--)
                {
                    tablesTab.TabPages.RemoveAt(i);
                }
            }

            switch(CurrentDevice)
            {
                case PuConfigDto pu:
                    // 1. Заполняем каналы
                    PuChannels = new BindingList<LiveControlChannelCfg>(
                        (pu.Channels ?? new List<LiveControlChannelCfg>()).OrderBy(c => c.Local).ToList());
                    MainDataGridView.AutoGenerateColumns = true;
                    MainDataGridView.DataSource = PuChannels;
                    SetupPuColumns();

                    // 2. Загружаем данные для остальных вкладок
                    Groups = new BindingList<LiveControlGroupCfg>((pu.Groups ?? new List<LiveControlGroupCfg>()).OrderBy(g => g.Index).ToList());
                    Pictures = new BindingList<LiveControlPictureCfg>((pu.Pictures ?? new List<LiveControlPictureCfg>()).ToList());
                    Buttons = new BindingList<LiveControlButtonCfg>((pu.Buttons ?? new List<LiveControlButtonCfg>()).ToList());

                    // 3. Создаем гриды для вкладок
                    var gridGroups = CreateGrid(new BindingSource { DataSource = Groups });
                    var gridPictures = CreateGrid(new BindingSource { DataSource = Pictures });
                    var gridButtons = CreateGrid(new BindingSource { DataSource = Buttons });

                    // 4. Добавляем вкладки
                    tablesTab.TabPages.AddRange(new[]
                    {
                        MakeTab("Группы", gridGroups),
                        MakeTab("Картины", gridPictures),
                        MakeTab("Кнопки", gridButtons),
                        BuildParametersTab(
                            out udPriority, out udPicSwitch, out udPageCount,
                            out udSwapX, out udSwapY,
                            out udBrightness, out udBrightnessSleep, out udSleepTimeout)
                    });

                    // 5. Устанавливаем значения параметров
                    SetNumericValue(udPriority, pu.Priority);
                    SetNumericValue(udPicSwitch, pu.PictureSwitchTime);
                    SetNumericValue(udPageCount, pu.PageCount);
                    SetNumericValue(udSwapX, pu.SwapX);
                    SetNumericValue(udSwapY, pu.SwapY);
                    SetNumericValue(udBrightness, pu.Brightness);
                    SetNumericValue(udBrightnessSleep, pu.BrightnessSleep);
                    SetNumericValue(udSleepTimeout, pu.SleepTimeout);
                    break;

                case ExecConfigDto exec:
                    ExecChannels = new BindingList<LiveChannelCfg>(
                        (exec.Channels ?? new List<LiveChannelCfg>()).OrderBy(c => c.Local).ToList());
                    MainDataGridView.AutoGenerateColumns = true;
                    MainDataGridView.DataSource = ExecChannels;
                    SetupExecColumns();
                    break;
            }
        }

        private static void SetNumericValue(NumericUpDown n, decimal value)
        {
            if(n != null) n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        private DataGridView CreateGrid(object dataSource = null) => new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = false,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            DataSource = dataSource
        };

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

        private static TabPage BuildParametersTab(
            out NumericUpDown udPriority, out NumericUpDown udPicSwitch, out NumericUpDown udPageCount,
            out NumericUpDown udSwapX, out NumericUpDown udSwapY,
            out NumericUpDown udBrightness, out NumericUpDown udBrightnessSleep, out NumericUpDown udSleepTimeout)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(8),
                ColumnStyles = {
                    new ColumnStyle(SizeType.Absolute, 220),
                    new ColumnStyle(SizeType.AutoSize)
                }
            };

            NumericUpDown AddRow(string label, decimal value, decimal min, decimal max, decimal step = 1)
            {
                var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) };
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
            udSleepTimeout = AddRow("Время до сна, с", 0, 0, 60000, 10);

            var page = new TabPage("Параметры") { Padding = new Padding(0) };
            page.Controls.Add(table);
            return page;
        }

        private void SetupPuColumns()
        {
            if(MainDataGridView.Columns["Type"] != null) MainDataGridView.Columns["Type"].Visible = false;
            if(MainDataGridView.Columns["Local"] != null)
            {
                MainDataGridView.Columns["Local"].HeaderText = "№";
                MainDataGridView.Columns["Local"].ReadOnly = true;
            }
            if(MainDataGridView.Columns["Name"] != null) MainDataGridView.Columns["Name"].HeaderText = "Имя";
            if(MainDataGridView.Columns["Index"] != null) MainDataGridView.Columns["Index"].HeaderText = "ADLDC-номер";
            if(MainDataGridView.Columns["TypeName"] != null) MainDataGridView.Columns["TypeName"].Visible = false;

            if(MainDataGridView.Columns["Type"] is DataGridViewColumn oldCol)
            {
                int idx = oldCol.Index;
                var combo = new DataGridViewComboBoxColumn
                {
                    Name = "TypeEdit",
                    HeaderText = "Тип",
                    DataPropertyName = "Type",
                    DataSource = new List<ChannelTypeItem>
                    {
                        new((int)ChannelType.CHTYPE_NONE, "Нет"),
                        new((int)ChannelType.CHTYPE_CHANNEL, "Канал"),
                        new((int)ChannelType.CHTYPE_GROUP, "Группа"),
                        new((int)ChannelType.CHTYPE_PICTURE, "Картина")
                    },
                    DisplayMember = "Name",
                    ValueMember = "Value",
                    FlatStyle = FlatStyle.Flat,
                    ReadOnly = false
                };
                MainDataGridView.Columns.RemoveAt(idx);
                MainDataGridView.Columns.Insert(idx, combo);
            }
        }

        private void SetupExecColumns()
        {
            // Настраиваем заголовки и доступность
            if(MainDataGridView.Columns["Local"] != null)
            {
                MainDataGridView.Columns["Local"].HeaderText = "№";
                MainDataGridView.Columns["Local"].ReadOnly = true; // <--- Запрещаем менять Local
            }
            if(MainDataGridView.Columns["GlobalIndex"] != null)
            {
                MainDataGridView.Columns["GlobalIndex"].HeaderText = "Глобальный №";
                MainDataGridView.Columns["GlobalIndex"].ReadOnly = true; // <--- Запрещаем менять GlobalIndex
            }

            if(MainDataGridView.Columns["Name"] != null) MainDataGridView.Columns["Name"].HeaderText = "Имя";
            if(MainDataGridView.Columns["Control"] != null) MainDataGridView.Columns["Control"].HeaderText = "Управление";
            if(MainDataGridView.Columns["Wide"] != null) MainDataGridView.Columns["Wide"].HeaderText = "16 бит";
            if(MainDataGridView.Columns["StreamId"] != null) MainDataGridView.Columns["StreamId"].Visible = false;
            if(MainDataGridView.Columns["Protocol"] != null) MainDataGridView.Columns["Protocol"].HeaderText = "Протокол";
            if(MainDataGridView.Columns["Universe"] != null) MainDataGridView.Columns["Universe"].HeaderText = "Вселенная";
            if(MainDataGridView.Columns["Patch"] != null) MainDataGridView.Columns["Patch"].HeaderText = "Патч";
            if(MainDataGridView.Columns["Preheat"] != null) MainDataGridView.Columns["Preheat"].HeaderText = "Преднакал";
            if(MainDataGridView.Columns["Limit"] != null) MainDataGridView.Columns["Limit"].HeaderText = "Лимит";
            if(MainDataGridView.Columns["Curve"] != null) MainDataGridView.Columns["Curve"].HeaderText = "Кривая";
            if(MainDataGridView.Columns["SwitchDelay"] != null) MainDataGridView.Columns["SwitchDelay"].HeaderText = "Задержка, мс";
        }

        private void MacTextBox_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidMac(MacTextBox.Text))
            {
                MessageBox.Show("Некорректный MAC-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        public static bool IsValidMac(string mac)
        {
            if(string.IsNullOrWhiteSpace(mac)) return false;
            string pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$";
            return Regex.IsMatch(mac, pattern);
        }

        private void IpTextBox_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidIp(IpTextBox.Text))
            {
                MessageBox.Show("Некорректный IP-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        public static bool IsValidIp(string ip)
        {
            if(string.IsNullOrWhiteSpace(ip)) return false;
            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, pattern);
        }

        private void MaskTextBox_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidMask(MaskTextBox.Text))
            {
                MessageBox.Show("Некорректная маска подсети! Пример: 255.255.255.0", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // --- Сохранение ---
        private void SaveButton_Click(object sender, EventArgs e)
        {
            CurrentDevice.Name = NameTextBox.Text;
            CurrentDevice.Mac = MacTextBox.Text;
            CurrentDevice.Ip = IpTextBox.Text;
            CurrentDevice.Mask = MaskTextBox.Text;

            if(CurrentDevice is PuConfigDto pu)
            {
                if(udPriority != null) pu.Priority = (int)udPriority.Value;
                if(udPicSwitch != null) pu.PictureSwitchTime = (int)udPicSwitch.Value;
                if(udPageCount != null) pu.PageCount = (int)udPageCount.Value;
                if(udSwapX != null) pu.SwapX = (int)udSwapX.Value;
                if(udSwapY != null) pu.SwapY = (int)udSwapY.Value;
                if(udBrightness != null) pu.Brightness = (int)udBrightness.Value;
                if(udBrightnessSleep != null) pu.BrightnessSleep = (int)udBrightnessSleep.Value;
                if(udSleepTimeout != null) pu.SleepTimeout = (int)udSleepTimeout.Value;

                pu.Groups = Groups.ToList();
                pu.Pictures = Pictures.ToList();
                pu.Buttons = Buttons.ToList();
                pu.Channels = PuChannels.ToList();
            }
            else if(CurrentDevice is ExecConfigDto exec)
            {
                exec.Channels = ExecChannels.ToList();
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        public class ChannelTypeItem
        {
            public int Value { get; }
            public string Name { get; }
            public ChannelTypeItem(int value, string name)
            {
                Value = value;
                Name = name;
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            return;
        }
    }
}