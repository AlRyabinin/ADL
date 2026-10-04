namespace AdlpdsConfig.Controls;

partial class fNameDialog
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
        lblName = new Label();
        tbName = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();

        lblName.Text = "Device Name:";
        lblName.Location = new Point(12, 15);
        lblName.Size = new Size(80, 23);

        tbName.Location = new Point(98, 12);
        tbName.Size = new Size(180, 23);
        tbName.MaxLength = 32;

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
        Controls.Add(lblName);
        Controls.Add(tbName);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Set Device Name";
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblName;
    private TextBox tbName;
    private Button btnOk;
    private Button btnCancel;
}
