namespace AdlpdsConfig.Controls;

partial class fNetDialog
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
        lblIP = new Label();
        tbIP = new TextBox();
        lblMask = new Label();
        tbMask = new TextBox();
        lblGateway = new Label();
        tbGateway = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();

        lblIP.Text = "IP Address:";
        lblIP.Location = new Point(12, 15);
        lblIP.Size = new Size(70, 23);

        tbIP.Location = new Point(88, 12);
        tbIP.Size = new Size(150, 23);

        lblMask.Text = "Mask:";
        lblMask.Location = new Point(12, 44);
        lblMask.Size = new Size(70, 23);

        tbMask.Location = new Point(88, 41);
        tbMask.Size = new Size(150, 23);

        lblGateway.Text = "Gateway:";
        lblGateway.Location = new Point(12, 73);
        lblGateway.Size = new Size(70, 23);

        tbGateway.Location = new Point(88, 70);
        tbGateway.Size = new Size(150, 23);

        btnOk.Text = "OK";
        btnOk.Location = new Point(82, 108);
        btnOk.Size = new Size(75, 23);
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Click += btnOk_Click;

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(163, 108);
        btnCancel.Size = new Size(75, 23);
        btnCancel.DialogResult = DialogResult.Cancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(250, 143);
        Controls.Add(lblIP);
        Controls.Add(tbIP);
        Controls.Add(lblMask);
        Controls.Add(tbMask);
        Controls.Add(lblGateway);
        Controls.Add(tbGateway);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Set IP Configuration";
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblIP;
    private TextBox tbIP;
    private Label lblMask;
    private TextBox tbMask;
    private Label lblGateway;
    private TextBox tbGateway;
    private Button btnOk;
    private Button btnCancel;
}
