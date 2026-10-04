using AdlpLib.Models.Devices;
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
    public partial class EditDeviceParametersForm : Form
    {
        public DeviceConfigDtoBase CurrentDevice { get; set; }
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
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            CurrentDevice.Name = NameTextBox.Text;

            DialogResult = DialogResult.OK;

            return;
        }
    }
}
