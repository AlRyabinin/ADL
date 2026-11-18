namespace AdlpdsConfig.Controls;

partial class fDeviceIdentityDialog
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
        lblHeader = new Label();
        lblNote = new Label();
        lblSid = new Label();
        tbSid = new TextBox();
        lblSidName = new Label();
        tbSidName = new TextBox();
        lblUuid = new Label();
        tbUuid = new TextBox();
        lblSn = new Label();
        tbSn = new TextBox();
        btnAdd = new Button();
        btnCancel = new Button();
        tbSidComment = new TextBox();
        label1 = new Label();
        generateButton = new Button();
        button1 = new Button();
        SuspendLayout();
        // 
        // lblHeader
        // 
        lblHeader.Location = new Point(12, 12);
        lblHeader.Name = "lblHeader";
        lblHeader.Size = new Size(496, 32);
        lblHeader.TabIndex = 0;
        lblHeader.Text = "Этих данных устройства нет в БД. Добавить?";
        // 
        // lblNote
        // 
        lblNote.ForeColor = SystemColors.GrayText;
        lblNote.Location = new Point(12, 48);
        lblNote.Name = "lblNote";
        lblNote.Size = new Size(496, 44);
        lblNote.TabIndex = 1;
        // 
        // lblSid
        // 
        lblSid.Location = new Point(12, 103);
        lblSid.Name = "lblSid";
        lblSid.Size = new Size(122, 23);
        lblSid.TabIndex = 2;
        lblSid.Text = "SID:";
        lblSid.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tbSid
        // 
        tbSid.CharacterCasing = CharacterCasing.Upper;
        tbSid.Location = new Point(140, 103);
        tbSid.MaxLength = 8;
        tbSid.Name = "tbSid";
        tbSid.Size = new Size(259, 23);
        tbSid.TabIndex = 0;
        // 
        // lblSidName
        // 
        lblSidName.Location = new Point(12, 135);
        lblSidName.Name = "lblSidName";
        lblSidName.Size = new Size(122, 23);
        lblSidName.TabIndex = 3;
        lblSidName.Text = "Название SID:";
        lblSidName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tbSidName
        // 
        tbSidName.Location = new Point(140, 135);
        tbSidName.MaxLength = 64;
        tbSidName.Name = "tbSidName";
        tbSidName.Size = new Size(368, 23);
        tbSidName.TabIndex = 1;
        // 
        // lblUuid
        // 
        lblUuid.Location = new Point(12, 222);
        lblUuid.Name = "lblUuid";
        lblUuid.Size = new Size(122, 23);
        lblUuid.TabIndex = 4;
        lblUuid.Text = "UUID:";
        lblUuid.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tbUuid
        // 
        tbUuid.CharacterCasing = CharacterCasing.Upper;
        tbUuid.Location = new Point(140, 222);
        tbUuid.MaxLength = 36;
        tbUuid.Name = "tbUuid";
        tbUuid.Size = new Size(259, 23);
        tbUuid.TabIndex = 2;
        // 
        // lblSn
        // 
        lblSn.Location = new Point(12, 193);
        lblSn.Name = "lblSn";
        lblSn.Size = new Size(122, 23);
        lblSn.TabIndex = 5;
        lblSn.Text = "SN:";
        lblSn.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tbSn
        // 
        tbSn.Location = new Point(140, 193);
        tbSn.MaxLength = 32;
        tbSn.Name = "tbSn";
        tbSn.Size = new Size(368, 23);
        tbSn.TabIndex = 3;
        // 
        // btnAdd
        // 
        btnAdd.Location = new Point(282, 251);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(140, 27);
        btnAdd.TabIndex = 4;
        btnAdd.Text = "Добавить в БД";
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += btnAdd_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(428, 251);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 27);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Отмена";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // tbSidComment
        // 
        tbSidComment.Location = new Point(140, 164);
        tbSidComment.MaxLength = 64;
        tbSidComment.Name = "tbSidComment";
        tbSidComment.Size = new Size(368, 23);
        tbSidComment.TabIndex = 6;
        // 
        // label1
        // 
        label1.Location = new Point(12, 163);
        label1.Name = "label1";
        label1.Size = new Size(122, 23);
        label1.TabIndex = 7;
        label1.Text = "Комментарий:";
        label1.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // generateButton
        // 
        generateButton.Location = new Point(405, 103);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(103, 23);
        generateButton.TabIndex = 8;
        generateButton.Text = "Сгенерировать";
        generateButton.UseVisualStyleBackColor = true;
        generateButton.Click += button1_Click;
        // 
        // button1
        // 
        button1.Location = new Point(405, 222);
        button1.Name = "button1";
        button1.Size = new Size(103, 23);
        button1.TabIndex = 9;
        button1.Text = "Сгенерировать";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click_1;
        // 
        // fDeviceIdentityDialog
        // 
        AcceptButton = btnAdd;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(520, 287);
        Controls.Add(button1);
        Controls.Add(generateButton);
        Controls.Add(label1);
        Controls.Add(tbSidComment);
        Controls.Add(lblHeader);
        Controls.Add(lblNote);
        Controls.Add(lblSid);
        Controls.Add(tbSid);
        Controls.Add(lblSidName);
        Controls.Add(tbSidName);
        Controls.Add(lblUuid);
        Controls.Add(tbUuid);
        Controls.Add(lblSn);
        Controls.Add(tbSn);
        Controls.Add(btnAdd);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "fDeviceIdentityDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "SID и UUID устройства";
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblHeader;
    private Label lblNote;
    private Label lblSid;
    private TextBox tbSid;
    private Label lblSidName;
    private TextBox tbSidName;
    private Label lblUuid;
    private TextBox tbUuid;
    private Label lblSn;
    private TextBox tbSn;
    private Button btnAdd;
    private Button btnCancel;
    private TextBox tbSidComment;
    private Label label1;
    private Button generateButton;
    private Button button1;
}
