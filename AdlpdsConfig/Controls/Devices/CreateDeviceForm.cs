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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    public partial class CreateDeviceForm : Form
    {
        public DeviceConfigDtoBase CreatedDevice { get; private set; }

        public CreateDeviceForm()
        {
            InitializeComponent();

            TypeComboBox.SelectedIndex = 0;
        }

        private void TypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(TypeComboBox.SelectedItem == null)
                return;

            CountComboBox.Items.Clear();
            CountComboBox.SelectedIndex = -1;
            CountComboBox.Text = string.Empty;

            string selectedType = TypeComboBox.SelectedItem.ToString();

            switch(selectedType)
            {
                case "IKB":
                    CountComboBox.Items.AddRange(new object[] { "24" });
                    break;

                case "DIM220":
                    CountComboBox.Items.AddRange(new object[] { "6", "12", "24" });
                    break;

                case "Dimmer cabinet":
                    CountComboBox.Items.AddRange(new object[] { "48", "96" });
                    break;

                case "NetGate":
                    CountComboBox.Items.AddRange(new object[] { "12", "24", "48" });
                    break;

                default:
                    CountComboBox.Items.AddRange(new object[] { "Нет данных" });
                    break;
            }

            CountComboBox.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if(TypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип устройства.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(CountComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите количество каналов.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedType = TypeComboBox.SelectedItem.ToString();

            if(!int.TryParse(CountComboBox.SelectedItem.ToString(), out int channelCount))
            {
                MessageBox.Show("Некорректное количество каналов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switch(selectedType)
            {
                case "IKB":
                case "NetGate":
                    CreatedDevice = new PuConfigDto()
                    {
                        Channels = new List<LiveControlChannelCfg>(channelCount)
                    };
                    break;

                case "DIM220":
                case "Dimmer cabinet":
                    CreatedDevice = new ExecConfigDto()
                    {
                        Channels = new List<LiveChannelCfg>(channelCount)
                    };
                    break;

                default:
                    MessageBox.Show("Неизвестный тип устройства.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            DialogResult = DialogResult.OK;
            return;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            return;
        }
    }
}
