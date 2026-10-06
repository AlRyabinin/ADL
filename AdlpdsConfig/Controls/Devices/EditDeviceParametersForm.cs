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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AdlpdsConfig.Controls.Devices
{
    public partial class EditDeviceParametersForm : Form
    {
        public DeviceConfigDtoBase CurrentDevice { get; set; }
        private BindingList<LiveChannelCfg> ExecChannels = new();
        private BindingList<LiveControlChannelCfg> PuChannels = new();

        public EditDeviceParametersForm()
        {
            InitializeComponent();
        }

        public EditDeviceParametersForm(DeviceConfigDtoBase newDevice)
        {
            InitializeComponent();

            CurrentDevice = newDevice;

            NameTextBox.Text = string.IsNullOrEmpty(newDevice.Name)
                ? string.Empty
                : newDevice.Name;

            MacTextBox.Text = newDevice.Mac ?? "";
            IpTextBox.Text = newDevice.Ip ?? "";
            MaskTextBox.Text = newDevice.Mask ?? "";

            LoadChannels();
        }

        /// <summary>
        /// Заполняет dgvChannels в зависимости от типа устройства.
        /// </summary>
        private void LoadChannels()
        {
            switch(CurrentDevice)
            {
                case PuConfigDto pu:
                    PuChannels = new BindingList<LiveControlChannelCfg>(
                        (pu.Channels ?? new List<LiveControlChannelCfg>())
                            .OrderBy(c => c.Local)
                            .ToList());

                    MainDataGridView.AutoGenerateColumns = true;
                    MainDataGridView.DataSource = PuChannels;

                    SetupPuColumns();
                    break;

                case ExecConfigDto exec:
                    ExecChannels = new BindingList<LiveChannelCfg>(
                        (exec.Channels ?? new List<LiveChannelCfg>())
                            .OrderBy(c => c.Local)
                            .ToList());

                    MainDataGridView.AutoGenerateColumns = true;
                    MainDataGridView.DataSource = ExecChannels;

                    SetupExecColumns();
                    break;
            }
        }

        /// <summary>
        /// Настройка колонок для каналов управления (PU):
        /// скрываем числовой Type, оставляем TypeName;
        /// Type заменяем на выпадающий список для редактирования.
        /// </summary>
        private void SetupPuColumns()
        {
            // Скрываем "сырой" Type
            if(MainDataGridView.Columns["Type"] != null)
                MainDataGridView.Columns["Type"].Visible = false;

            if(MainDataGridView.Columns["Local"] != null)
                MainDataGridView.Columns["Local"].HeaderText = "№";

            if(MainDataGridView.Columns["Name"] != null)
                MainDataGridView.Columns["Name"].HeaderText = "Имя";

            if(MainDataGridView.Columns["Index"] != null)
                MainDataGridView.Columns["Index"].HeaderText = "ADLDC-номер";

            if(MainDataGridView.Columns["TypeName"] != null)
                MainDataGridView.Columns["TypeName"].Visible = false;

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
                new((int)ChannelType.CHTYPE_NONE,    "Нет"),
                new((int)ChannelType.CHTYPE_CHANNEL, "Канал"),
                new((int)ChannelType.CHTYPE_GROUP,   "Группа"),
                new((int)ChannelType.CHTYPE_PICTURE, "Картина")
            },
                    DisplayMember = nameof(ChannelTypeItem.Name),
                    ValueMember = nameof(ChannelTypeItem.Value),
                    FlatStyle = FlatStyle.Flat
                };

                MainDataGridView.Columns.RemoveAt(idx);
                MainDataGridView.Columns.Insert(idx, combo);
            }
        }

        /// <summary>
        /// Настройка колонок для каналов исполнительных устройств (EXEC).
        /// </summary>
        private void SetupExecColumns()
        {
            if(MainDataGridView.Columns["Local"] != null)
                MainDataGridView.Columns["Local"].HeaderText = "№";

            if(MainDataGridView.Columns["GlobalIndex"] != null)
                MainDataGridView.Columns["GlobalIndex"].HeaderText = "Глобальный №";

            if(MainDataGridView.Columns["Name"] != null)
                MainDataGridView.Columns["Name"].HeaderText = "Имя";

            if(MainDataGridView.Columns["Control"] != null)
                MainDataGridView.Columns["Control"].HeaderText = "Управление";

            if(MainDataGridView.Columns["Wide"] != null)
                MainDataGridView.Columns["Wide"].HeaderText = "16 бит";

            if(MainDataGridView.Columns["StreamId"] != null)
                MainDataGridView.Columns["StreamId"].Visible = false;

            if(MainDataGridView.Columns["Protocol"] != null)
                MainDataGridView.Columns["Protocol"].HeaderText = "Протокол";

            if(MainDataGridView.Columns["Universe"] != null)
                MainDataGridView.Columns["Universe"].HeaderText = "Вселенная";

            if(MainDataGridView.Columns["Patch"] != null)
                MainDataGridView.Columns["Patch"].HeaderText = "Патч";

            if(MainDataGridView.Columns["Preheat"] != null)
                MainDataGridView.Columns["Preheat"].HeaderText = "Преднакал";

            if(MainDataGridView.Columns["Limit"] != null)
                MainDataGridView.Columns["Limit"].HeaderText = "Лимит";

            if(MainDataGridView.Columns["Curve"] != null)
                MainDataGridView.Columns["Curve"].HeaderText = "Кривая";

            if(MainDataGridView.Columns["SwitchDelay"] != null)
                MainDataGridView.Columns["SwitchDelay"].HeaderText = "Задержка, мс";
        }

        private void MacTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!IsValidMac(MacTextBox.Text))
            {
                MessageBox.Show("Некорректный MAC-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Валидация MAC-адреса (поддерживает форматы: 00:1A:2B:3C:4D:5E, 00-1A-2B-3C-4D-5E, 001A2B3C4D5E)
        /// </summary>
        /// <param name="mac"></param>
        /// <returns></returns>
        public static bool IsValidMac(string mac)
        {
            if(string.IsNullOrWhiteSpace(mac)) return false;
            string pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$";
            return Regex.IsMatch(mac, pattern);
        }

        private void IpTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!IsValidIp(IpTextBox.Text))
            {
                MessageBox.Show("Некорректный IP-адрес!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Валидация IPv4 (значения от 0 до 255)
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsValidIp(string ip)
        {
            if(string.IsNullOrWhiteSpace(ip)) return false;
            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, pattern);
        }

        private void MaskTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!IsValidMask(MaskTextBox.Text))
            {
                MessageBox.Show("Некорректная маска подсети! Пример: 255.255.255.0", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Валидация маски подсети (проверка на непрерывность единиц)
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static bool IsValidMask(string mask)
        {
            if(!IsValidIp(mask)) return false;

            string[] parts = mask.Split('.');
            uint maskValue = 0;
            foreach(var part in parts)
            {
                maskValue = (maskValue << 8) | byte.Parse(part);
            }

            uint inverted = ~maskValue;
            return (inverted & (inverted + 1)) == 0;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            CurrentDevice.Name = NameTextBox.Text;

            CurrentDevice.Mac = MacTextBox.Text;

            CurrentDevice.Ip = IpTextBox.Text;

            CurrentDevice.Mask = MaskTextBox.Text;

            DialogResult = DialogResult.OK;

            return;
        }

        /// <summary>Служебная запись для ComboBox типов каналов.</summary>
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
    }
}
