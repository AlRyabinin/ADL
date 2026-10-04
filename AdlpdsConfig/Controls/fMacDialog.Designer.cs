namespace AdlpdsConfig.Controls;

partial class fMacDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblMac = new Label();
        tbMac = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();

        lblMac.Text = "MAC Address:";
        lblMac.Location = new Point(12, 15);
        lblMac.Size = new Size(80, 23);

        tbMac.Location = new Point(98, 12);
        tbMac.Size = new Size(180, 23);
        tbMac.Text = "";

        btnOk.Text = "OK";
        btnOk.Location = new Point(122, 50);
        btnOk.Size = new Size(75, 23);
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Click += btnOk_Click;

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(203, 50);
        btnCancel.Size = new Size(75, 23);
        btnCancel.DialogResult = DialogResult.Cancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(290, 85);
        Controls.Add(lblMac);
        Controls.Add(tbMac);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Set MAC Address";
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblMac;
    private TextBox tbMac;
    private Button btnOk;
    private Button btnCancel;
}
