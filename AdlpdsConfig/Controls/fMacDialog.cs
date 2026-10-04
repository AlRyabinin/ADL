using System.ComponentModel;

namespace AdlpdsConfig.Controls;

public partial class fMacDialog : Form
{
    public string MacAddress => tbMac.Text.Trim();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentMac { get; set; } = "";

    public fMacDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        tbMac.Text = CurrentMac;
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        var mac = MacAddress;
        if (mac.Length != 17 || mac.Split(':').Length != 6 ||
            !mac.Replace(":", "").All("0123456789ABCDEFabcdef".Contains))
        {
            MessageBox.Show("Invalid MAC address. Use format AA:BB:CC:DD:EE:FF", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }
}
