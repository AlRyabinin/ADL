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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    public partial class CreateDeviceForm : Form
    {
        public DeviceConfigDtoBase CreatedDevice { get; private set; }

        public string SelectedType { get; set; }

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

            SelectedType = TypeComboBox.SelectedItem.ToString();

            switch(SelectedType)
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

            SelectedType = TypeComboBox.SelectedItem.ToString();

            if(!int.TryParse(CountComboBox.SelectedItem.ToString(), out int channelCount))
            {
                MessageBox.Show("Некорректное количество каналов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switch(SelectedType)
            {
                case "IKB":
                case "NetGate":
                {
                    var pu = new PuConfigDto
                    {
                        DeviceType = SelectedType,
                        Channels = new List<LiveControlChannelCfg>(channelCount)
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        pu.Channels.Add(new LiveControlChannelCfg
                        {
                            Local = i,
                            Name = $"Канал {i}",   
                            Index = 0,             
                            Type = (int)ChannelType.CHTYPE_CHANNEL
                        });
                    }

                    CreatedDevice = pu;
                    break;
                }
                case "DIM220":
                case "Dimmer cabinet":
                {
                    var exec = new ExecConfigDto
                    {
                        DeviceType = SelectedType,
                        Channels = new List<LiveChannelCfg>(channelCount)
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        exec.Channels.Add(new LiveChannelCfg
                        {
                            Local = i,
                            GlobalIndex = i,       // по умолчанию = локальному, потом можно править
                            Name = $"Канал {i}",
                            Control = "adlp",
                            Wide = false,
                            StreamId = 0,
                            Patch = 0,
                            Preheat = 0,
                            Limit = 100,
                            Curve = 0,
                            SwitchDelay = 0
                        });
                    }

                    CreatedDevice = exec;
                    break;
                }

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
