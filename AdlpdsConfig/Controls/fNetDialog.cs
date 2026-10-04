using System.ComponentModel;
using System.Net;

namespace AdlpdsConfig.Controls;

public partial class fNetDialog : Form
{
    public string IP => tbIP.Text.Trim();
    public string Mask => tbMask.Text.Trim();
    public string Gateway => tbGateway.Text.Trim();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentIP { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentMask { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentGateway { get; set; } = "";

    public fNetDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        tbIP.Text = CurrentIP;
        tbMask.Text = CurrentMask;
        tbGateway.Text = CurrentGateway;
    }

    private bool IsValidIP(string s) =>
        IPAddress.TryParse(s, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

    private void btnOk_Click(object? sender, EventArgs e)
    {
        if (!IsValidIP(IP))
        {
            MessageBox.Show("Invalid IP address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (!IsValidIP(Mask))
        {
            MessageBox.Show("Invalid subnet mask.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (Gateway.Length > 0 && !IsValidIP(Gateway))
        {
            MessageBox.Show("Invalid gateway address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
    }
}
