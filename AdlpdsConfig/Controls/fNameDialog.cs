using System.ComponentModel;

namespace AdlpdsConfig.Controls;

public partial class fNameDialog : Form
{
    public string DeviceName => tbName.Text.Trim();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentName { get; set; } = "";

    public fNameDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        tbName.Text = CurrentName;
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        if (DeviceName.Length == 0)
        {
            MessageBox.Show("Name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }
}
