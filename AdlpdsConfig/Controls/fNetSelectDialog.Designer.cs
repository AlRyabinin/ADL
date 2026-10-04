namespace AdlpdsConfig.Controls;

partial class fNetSelectDialog
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
        lvInterfaces = new ListView();
        chIndex = new ColumnHeader();
        chIP = new ColumnHeader();
        chMask = new ColumnHeader();
        chName = new ColumnHeader();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();

        lvInterfaces.Location = new Point(12, 12);
        lvInterfaces.Size = new Size(460, 200);
        lvInterfaces.View = View.Details;
        lvInterfaces.FullRowSelect = true;
        lvInterfaces.MultiSelect = false;
        lvInterfaces.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        lvInterfaces.DoubleClick += lvInterfaces_DoubleClick;
        lvInterfaces.Columns.AddRange(new[] { chIndex, chIP, chMask, chName });
        chIndex.Text = "#";
        chIndex.Width = 30;
        chIP.Text = "IP";
        chIP.Width = 130;
        chMask.Text = "Mask";
        chMask.Width = 130;
        chName.Text = "Name";
        chName.Width = 160;

        btnOk.Text = "OK";
        btnOk.Location = new Point(316, 225);
        btnOk.Size = new Size(75, 23);
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Click += btnOk_Click;

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(397, 225);
        btnCancel.Size = new Size(75, 23);
        btnCancel.DialogResult = DialogResult.Cancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 260);
        Controls.Add(lvInterfaces);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Select Network Interface";
        ResumeLayout(false);
        PerformLayout();
    }

    private ListView lvInterfaces;
    private ColumnHeader chIndex;
    private ColumnHeader chIP;
    private ColumnHeader chMask;
    private ColumnHeader chName;
    private Button btnOk;
    private Button btnCancel;
}
