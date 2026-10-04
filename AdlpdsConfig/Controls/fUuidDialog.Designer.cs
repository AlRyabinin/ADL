namespace AdlpdsConfig.Controls;

partial class fUuidDialog
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
        lblUuid = new Label();
        tbUuid = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();

        lblUuid.Text = "UUID:";
        lblUuid.Location = new Point(12, 15);
        lblUuid.Size = new Size(40, 23);

        tbUuid.Location = new Point(55, 12);
        tbUuid.Size = new Size(270, 23);

        btnOk.Text = "OK";
        btnOk.Location = new Point(169, 50);
        btnOk.Size = new Size(75, 23);
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Click += btnOk_Click;

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(250, 50);
        btnCancel.Size = new Size(75, 23);
        btnCancel.DialogResult = DialogResult.Cancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(337, 85);
        Controls.Add(lblUuid);
        Controls.Add(tbUuid);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Set Device UUID";
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblUuid;
    private TextBox tbUuid;
    private Button btnOk;
    private Button btnCancel;
}
