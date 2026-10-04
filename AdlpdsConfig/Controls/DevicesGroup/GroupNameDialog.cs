using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.DevicesGroup
{
    public partial class GroupNameDialog : Form
    {
        public string GroupName { get; private set; }

        public GroupNameDialog(string initial = "")
        {
            InitializeComponent();
            _groupName.Text = initial ?? "";
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var v = _groupName.Text?.Trim();
            if(string.IsNullOrEmpty(v))
            {
                MessageBox.Show(this,
                    "Название не может быть пустым.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            GroupName = v;

            DialogResult = DialogResult.OK;
            return;
        }

        private void binCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            return;
        }
    }
}
