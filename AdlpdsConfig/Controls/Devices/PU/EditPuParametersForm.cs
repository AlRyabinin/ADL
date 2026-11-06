using AdlpLib.Configuration.Application;
using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices.PU;
using AdlpLib.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    public partial class EditPuParametersForm : Form
    {
        private readonly DeviceConfigRepository _repository;

        public PuConfigDtoBase CurrentDevice { get; set; }

        private BindingList<LiveControlChannelCfg> _puChannels;
        private BindingList<LiveControlGroupCfg> _groups;
        private BindingList<LiveControlPictureCfg> _pictures;
        private BindingList<LiveControlButtonCfg> _buttons;

        private DataGridView _mainDataGridView;

        private NumericUpDown _udPriority, _udPicSwitch, _udPageCount, _udSwapX, _udSwapY, _udBrightness, _udBrightnessSleep, _udSleepTimeout;

        public EditPuParametersForm(PuConfigDtoBase device, IReadOnlyList<ListSidEntity> sidRecords, DeviceConfigRepository repository)
        {
            InitializeComponent();

            _repository = repository;

            CurrentDevice = device;

            Text = device switch
            {
                Pu6ConfigDto => "Параметры ПУ 6",
                PuBWConfigDto => "Параметры ПУ Ч/Б",
                PuColorConfigDto => "Параметры Цветного ПУ",
                _ => "Параметры устройства"
            };

            _txtName.Text = device.Name ?? "";
            _txtMac.Text = device.Mac ?? "";
            _txtIp.Text = device.Ip ?? "";
            _txtMask.Text = device.Mask ?? "";
            _txtGateway.Text = device.Gateway ?? "";
            LoadSidRecords(sidRecords, device.PkListSid);

            LoadTabs();
            LoadData();
        }

        private sealed class SidComboItem
        {
            public string? Key { get; set; }
            public string Caption { get; set; } = "";
        }

        private void LoadSidRecords(IReadOnlyList<ListSidEntity> records, string? selectedKey)
        {
            var items = new List<SidComboItem>
            {
                new()
                {
                    Key = null,
                    Caption = "— Не назначен —"
                }
            };

            if(records != null)
            {
                items.AddRange(records.Select(x => new SidComboItem
                {
                    Key = x.PkListSid?.Trim(),
                    Caption = $"{x.NameObjSid} ({x.PkListSid})"
                }));
            }

            _cbSid.DisplayMember = nameof(SidComboItem.Caption);
            _cbSid.ValueMember = nameof(SidComboItem.Key);
            _cbSid.DataSource = items;

            int selectedIndex = items.FindIndex(x =>
                string.Equals(
                    x.Key?.Trim(),
                    selectedKey?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            _cbSid.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private void LoadTabs()
        {
            _puChannels = new BindingList<LiveControlChannelCfg>((CurrentDevice.Channels ?? new List<LiveControlChannelCfg>()).OrderBy(c => c.Local).ToList());

            _mainDataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = false,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            _mainDataGridView.DataBindingComplete += (s, e) => SetupPuColumns();
            _tablesTab.TabPages.Add(CreateTab("Каналы", _mainDataGridView));
            _mainDataGridView.DataSource = _puChannels;

            _groups = new BindingList<LiveControlGroupCfg>((CurrentDevice.Groups ?? new List<LiveControlGroupCfg>()).OrderBy(g => g.Local).ToList());
            _tablesTab.TabPages.Add(CreateTab("Группы", CreateGrid(_groups)));

            _pictures = new BindingList<LiveControlPictureCfg>((CurrentDevice.Pictures ?? new List<LiveControlPictureCfg>()).ToList());
            _tablesTab.TabPages.Add(CreateTab("Картины", CreateGrid(_pictures)));

            _buttons = new BindingList<LiveControlButtonCfg>((CurrentDevice.Buttons ?? new List<LiveControlButtonCfg>()).ToList());
            _tablesTab.TabPages.Add(CreateTab("Кнопки", CreateGrid(_buttons)));

            _tablesTab.TabPages.Add(BuildParametersTab());
        }

        private void LoadData()
        {
            SetNumericValue(_udPriority, CurrentDevice.Priority);
            SetNumericValue(_udPicSwitch, CurrentDevice.PictureSwitchTime);
            SetNumericValue(_udPageCount, CurrentDevice.PageCount);
            SetNumericValue(_udSwapX, CurrentDevice.SwapX);
            SetNumericValue(_udSwapY, CurrentDevice.SwapY);
            SetNumericValue(_udBrightness, CurrentDevice.Brightness);
            SetNumericValue(_udBrightnessSleep, CurrentDevice.BrightnessSleep);
            SetNumericValue(_udSleepTimeout, CurrentDevice.SleepTimeout);
        }

        private static void SetNumericValue(NumericUpDown n, decimal value)
        {
            if(n != null) n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        private DataGridView CreateGrid(object dataSource) => new DataGridView
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

        private TabPage CreateTab(string title, DataGridView grid)
        {
            var page = new TabPage(title) { Padding = new Padding(0) };
            page.Controls.Add(grid);
            return page;
        }

        private TabPage BuildParametersTab()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                ColumnStyles = {
                    new ColumnStyle(SizeType.Absolute, 250),
                    new ColumnStyle(SizeType.AutoSize)
                }
            };

            NumericUpDown AddRow(string label, decimal min, decimal max, decimal step = 1)
            {
                var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) };
                var num = new NumericUpDown
                {
                    Minimum = min,
                    Maximum = max,
                    Increment = step,
                    Width = 100,
                    Anchor = AnchorStyles.Left
                };
                table.RowCount++;
                table.Controls.Add(lbl, 0, table.RowCount - 1);
                table.Controls.Add(num, 1, table.RowCount - 1);
                return num;
            }

            _udPriority = AddRow("Приоритет", 0, 100);
            _udPicSwitch = AddRow("Переход между картинами, мс", 0, 60000, 10);
            _udPageCount = AddRow("Число страниц", 1, 255);
            _udSwapX = AddRow("Поменять ось X сенсора (0-1)", 0, 1);
            _udSwapY = AddRow("Поменять ось Y сенсора (0-1)", 0, 1);
            _udBrightness = AddRow("Яркость, %", 0, 100);
            _udBrightnessSleep = AddRow("Яркость при сне, %", 0, 100);
            _udSleepTimeout = AddRow("Время до сна, с", 0, 60000, 10);

            var page = new TabPage("Параметры") { Padding = new Padding(0), AutoScroll = true };
            page.Controls.Add(table);
            return page;
        }

        private void SetupPuColumns()
        {
            if(_mainDataGridView.Columns["Type"] != null) _mainDataGridView.Columns["Type"].Visible = false;
            if(_mainDataGridView.Columns["TypeName"] != null) _mainDataGridView.Columns["TypeName"].Visible = false;

            if(_mainDataGridView.Columns["Local"] != null)
            {
                _mainDataGridView.Columns["Local"].HeaderText = "№";
                _mainDataGridView.Columns["Local"].ReadOnly = true;
            }
            if(_mainDataGridView.Columns["Name"] != null) _mainDataGridView.Columns["Name"].HeaderText = "Имя";
            if(_mainDataGridView.Columns["Index"] != null) _mainDataGridView.Columns["Index"].HeaderText = "ADLDC-номер";

            if(_mainDataGridView.Columns["Type"] is DataGridViewColumn oldCol)
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
                _mainDataGridView.Columns.RemoveAt(idx);
                _mainDataGridView.Columns.Insert(idx, combo);
            }
        }

        private void _txtMac_Validating(object sender, CancelEventArgs e)
        {
            // MAC не обязателен. Проверяем формат только если поле заполнено.
            if(string.IsNullOrWhiteSpace(_txtMac.Text))
                return;

            if(!IsValidMac(_txtMac.Text))
            {
                MessageBox.Show("Некорректный MAC-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void _txtIp_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidIp(_txtIp.Text))
            {
                MessageBox.Show("Некорректный IP-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void _txtMask_Validating(object sender, CancelEventArgs e)
        {
            if(!IsValidMask(_txtMask.Text))
            {
                MessageBox.Show("Некорректная маска!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            CurrentDevice.Name = _txtName.Text;
            CurrentDevice.Mac = _txtMac.Text;
            CurrentDevice.Ip = _txtIp.Text;
            CurrentDevice.Mask = _txtMask.Text;
            CurrentDevice.Gateway = _txtGateway.Text;
            CurrentDevice.PkListSid = _cbSid.SelectedValue as string;

            CurrentDevice.Priority = (int)_udPriority.Value;
            CurrentDevice.PictureSwitchTime = (int)_udPicSwitch.Value;
            CurrentDevice.PageCount = (int)_udPageCount.Value;
            CurrentDevice.SwapX = (int)_udSwapX.Value;
            CurrentDevice.SwapY = (int)_udSwapY.Value;
            CurrentDevice.Brightness = (int)_udBrightness.Value;
            CurrentDevice.BrightnessSleep = (int)_udBrightnessSleep.Value;
            CurrentDevice.SleepTimeout = (int)_udSleepTimeout.Value;

            CurrentDevice.Groups = _groups.ToList();
            CurrentDevice.Pictures = _pictures.ToList();
            CurrentDevice.Buttons = _buttons.ToList();
            CurrentDevice.Channels = _puChannels.ToList();

            DialogResult = DialogResult.OK;
            Close();
        }

        public static bool IsValidMac(string mac) => !string.IsNullOrWhiteSpace(mac) && Regex.IsMatch(mac, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$");
        public static bool IsValidIp(string ip) => !string.IsNullOrWhiteSpace(ip) && Regex.IsMatch(ip, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        public static bool IsValidMask(string mask)
        {
            if(!IsValidIp(mask)) return false;
            uint maskValue = 0;
            foreach(var part in mask.Split('.')) maskValue = (maskValue << 8) | byte.Parse(part);
            uint inverted = ~maskValue;
            return (inverted & (inverted + 1)) == 0;
        }

        public class ChannelTypeItem
        {
            public int Value { get; }
            public string Name { get; }
            public ChannelTypeItem(int value, string name) { Value = value; Name = name; }
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