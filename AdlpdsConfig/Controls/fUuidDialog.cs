using System.ComponentModel;

namespace AdlpdsConfig.Controls;

public partial class fUuidDialog : Form
{
    public string DeviceUuid => tbUuid.Text.Trim();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentUuid { get; set; } = "";

    public fUuidDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        tbUuid.Text = CurrentUuid;
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        if (!Guid.TryParse(tbUuid.Text.Trim(), out _))
        {
            MessageBox.Show("Invalid UUID format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }
}
