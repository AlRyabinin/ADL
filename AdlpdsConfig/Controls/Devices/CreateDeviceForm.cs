using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
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

            ChannelCountComboBox.Items.Clear();
            ChannelCountComboBox.SelectedIndex = -1;
            ChannelCountComboBox.Text = string.Empty;

            ConveyorCountComboBox.Items.Clear();
            ConveyorCountComboBox.SelectedIndex = -1;
            ConveyorCountComboBox.Text = string.Empty;

            label3.Visible = false;
            ConveyorCountComboBox.Visible = false;

            SelectedType = TypeComboBox.SelectedItem.ToString();

            switch(SelectedType)
            {
                case "IKB":
                    ChannelCountComboBox.Items.AddRange(new object[] { "24" });
                    break;

                case "DIM220":
                    ChannelCountComboBox.Items.AddRange(new object[] { "6", "12", "24" });
                    break;

                case "Dimmer cabinet":
                    ChannelCountComboBox.Items.AddRange(new object[] { "48", "96" });
                    break;

                case "NetGate":
                    ChannelCountComboBox.Items.AddRange(new object[] { "12", "24", "48" });

                    label3.Visible = true;
                    ConveyorCountComboBox.Visible = true;
                    ConveyorCountComboBox.Items.AddRange(new object[] { "4", "8" });
                    break;

                case "PU 6":
                    ChannelCountComboBox.Items.AddRange(new object[] { "6" });
                    break;

                case "PU BW":
                    ChannelCountComboBox.Items.AddRange(new object[] { "6", "12", "24" });
                    break;

                case "PU color":
                    ChannelCountComboBox.Items.AddRange(new object[] { "12", "24", "48", "96" });
                    break;

                default:
                    ChannelCountComboBox.Items.AddRange(new object[] { "Нет данных" });
                    break;
            }

            ChannelCountComboBox.SelectedIndex = 0;

            if(ConveyorCountComboBox.Visible)
                ConveyorCountComboBox.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if(TypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип устройства.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedType = TypeComboBox.SelectedItem.ToString();

            if(ChannelCountComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите количество каналов.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(!int.TryParse(ChannelCountComboBox.SelectedItem.ToString(), out int channelCount))
            {
                MessageBox.Show("Некорректное количество каналов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int conveyorCount = 0;
            if(ConveyorCountComboBox.Visible)
            {
                if(!int.TryParse(ConveyorCountComboBox.SelectedItem.ToString(), out int cbConveyorCount))
                {
                    MessageBox.Show("Некорректное количество конвейеров.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if(ConveyorCountComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите количество конвейеров.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                conveyorCount = cbConveyorCount;
            }
            
            switch(SelectedType)
            {
                case "IKB":
                case "DIM220":
                case "Dimmer cabinet":
                {
                    var exec = new ExecConfigDto
                    {
                        Channels = new List<LiveChannelCfg>(channelCount)
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        exec.Channels.Add(new LiveChannelCfg
                        {
                            Local = i,
                            GlobalIndex = i,      
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
                case "NetGate":
                {
                    var netGate = new NetGateConfigDto
                    {
                        ConveyorCount = conveyorCount,
                        Conveyors = new List<NetGateConveyorCfg>(conveyorCount),
                        Channels = new List<NetGateChannelCfg>(channelCount),
                        Pictures = new List<NetGatePictureCfg>()
                    };

                    for(int i = 1; i <= conveyorCount; i++)
                    {
                        netGate.Conveyors.Add(new NetGateConveyorCfg
                        {
                            Local = i,
                            SourceA = new NetGateSourceCfg { Protocol = "sACN", Universe = i },
                            SourceB = new NetGateSourceCfg { Protocol = "artnet", Universe = i - 1 },
                            SourceC = new NetGateSourceCfg { Protocol = "artnet", Universe = i - 1 },
                            Destination = new NetGateSourceCfg { Protocol = "dmx", Universe = i - 1 },
                            SourceSelect = 0,
                            SumType = 0,
                            Frequency = 0,
                            Recordable = false,
                            Timeouted = false,
                            RdmEnabled = false,
                            SumBFrom = 0,
                            SumBTo = 255,
                            SumCFrom = 256,
                            SumCTo = 511
                        });
                    }

                    for(int i = 1; i <= channelCount; i++)
                    {
                        netGate.Channels.Add(new NetGateChannelCfg
                        {
                            Local = i,
                            Name = $"Канал {i}",
                            Index = i
                        });
                    }

                    CreatedDevice = netGate;
                    break;
                }

                case "PU 6":

                    var pu6 = new Pu6ConfigDto
                    {
                        Groups = [],
                        Channels = new List<LiveControlChannelCfg>(channelCount),
                        Pictures = [],
                        Buttons  = []
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        pu6.Channels.Add(new LiveControlChannelCfg
                        {
                            Local = i,
                            Name = $"Канал {i}",
                            Index = i,
                            Type = 1 
                        });
                    }

                    CreatedDevice = pu6;

                    break;

                case "PU BW":

                    var puBW = new PuBWConfigDto
                    {
                        Groups = [],
                        Channels = new List<LiveControlChannelCfg>(channelCount),
                        Pictures = [],
                        Buttons = []
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        puBW.Channels.Add(new LiveControlChannelCfg
                        {
                            Local = i,
                            Name = $"Канал {i}",
                            Index = i,
                            Type = 1 
                        });
                    }

                    CreatedDevice = puBW;

                    break;

                case "PU color":

                    var puColor = new PuColorConfigDto
                    {
                        Groups = [],
                        Channels = new List<LiveControlChannelCfg>(channelCount),
                        Pictures = [],
                        Buttons = []
                    };

                    for(int i = 1; i <= channelCount; i++)
                    {
                        puColor.Channels.Add(new LiveControlChannelCfg
                        {
                            Local = i,
                            Name = $"Канал {i}",
                            Index = i,
                            Type = 1 
                        });
                    }

                    CreatedDevice = puColor;

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
