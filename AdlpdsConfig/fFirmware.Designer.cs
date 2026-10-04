namespace AdlpdsConfig;

partial class fFirmware
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
        components = new System.ComponentModel.Container();
        tPoll = new System.Windows.Forms.Timer(components);
        lInfo = new Label();
        lType = new Label();
        groupFile = new GroupBox();
        lbFiles = new ListBox();
        bOpen = new Button();
        bRefresh = new Button();
        lFwInfo = new Label();
        pbProgress = new ProgressBar();
        lState = new Label();
        bFirmware = new Button();
        bClose = new Button();
        lStatus = new Label();
        groupFile.SuspendLayout();
        SuspendLayout();
        // 
        // tPoll
        // 
        tPoll.Interval = 500;
        tPoll.Tick += tPoll_Tick;
        // 
        // lInfo
        // 
        lInfo.AutoEllipsis = true;
        lInfo.Location = new Point(12, 12);
        lInfo.Name = "lInfo";
        lInfo.Size = new Size(390, 24);
        lInfo.TabIndex = 0;
        // 
        // lType
        // 
        lType.AutoEllipsis = true;
        lType.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lType.Location = new Point(410, 12);
        lType.Name = "lType";
        lType.Size = new Size(290, 24);
        lType.TabIndex = 11;
        lType.TextAlign = ContentAlignment.MiddleRight;
        lType.Text = "";
        // 
        // groupFile
        // 
        groupFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupFile.Controls.Add(lbFiles);
        groupFile.Controls.Add(bOpen);
        groupFile.Controls.Add(bRefresh);
        groupFile.Location = new Point(12, 40);
        groupFile.Name = "groupFile";
        groupFile.Size = new Size(560, 160);
        groupFile.TabIndex = 1;
        groupFile.TabStop = false;
        groupFile.Text = "Файл прошивки (.adlfw) или спека (.yaml/.yml)";
        // 
        // lbFiles
        // 
        lbFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lbFiles.FormattingEnabled = true;
        lbFiles.Location = new Point(20, 24);
        lbFiles.Name = "lbFiles";
        lbFiles.Size = new Size(408, 125);
        lbFiles.TabIndex = 2;
        lbFiles.SelectedIndexChanged += lbFiles_SelectedIndexChanged;
        // 
        // bOpen
        // 
        bOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bOpen.Location = new Point(436, 24);
        bOpen.Name = "bOpen";
        bOpen.Size = new Size(116, 25);
        bOpen.TabIndex = 3;
        bOpen.Text = "Открыть…";
        bOpen.UseVisualStyleBackColor = true;
        bOpen.Click += bOpen_Click;
        // 
        // bRefresh
        // 
        bRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bRefresh.Location = new Point(436, 55);
        bRefresh.Name = "bRefresh";
        bRefresh.Size = new Size(116, 25);
        bRefresh.TabIndex = 4;
        bRefresh.Text = "Обновить";
        bRefresh.UseVisualStyleBackColor = true;
        bRefresh.Click += bRefresh_Click;
        // 
        // lFwInfo
        // 
        lFwInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lFwInfo.Location = new Point(12, 210);
        lFwInfo.Name = "lFwInfo";
        lFwInfo.Size = new Size(560, 120);
        lFwInfo.TabIndex = 5;
        lFwInfo.Text = "";
        // 
        // pbProgress
        // 
        pbProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pbProgress.Location = new Point(12, 340);
        pbProgress.Name = "pbProgress";
        pbProgress.Size = new Size(560, 20);
        pbProgress.TabIndex = 6;
        // 
        // lState
        // 
        lState.AutoEllipsis = true;
        lState.Location = new Point(12, 366);
        lState.Name = "lState";
        lState.Size = new Size(560, 20);
        lState.TabIndex = 7;
        lState.Text = "";
        // 
        // bFirmware
        // 
        bFirmware.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bFirmware.Location = new Point(580, 335);
        bFirmware.Name = "bFirmware";
        bFirmware.Size = new Size(120, 30);
        bFirmware.TabIndex = 8;
        bFirmware.Text = "Прошить";
        bFirmware.UseVisualStyleBackColor = true;
        bFirmware.Click += bFirmware_Click;
        // 
        // bClose
        // 
        bClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bClose.Location = new Point(580, 370);
        bClose.Name = "bClose";
        bClose.Size = new Size(120, 25);
        bClose.TabIndex = 9;
        bClose.Text = "Закрыть";
        bClose.UseVisualStyleBackColor = true;
        bClose.Click += bClose_Click;
        // 
        // lStatus
        // 
        lStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lStatus.AutoEllipsis = true;
        lStatus.Location = new Point(12, 410);
        lStatus.Name = "lStatus";
        lStatus.Size = new Size(688, 20);
        lStatus.TabIndex = 10;
        // 
        // fFirmware
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(712, 440);
        Controls.Add(lStatus);
        Controls.Add(bClose);
        Controls.Add(bFirmware);
        Controls.Add(lState);
        Controls.Add(pbProgress);
        Controls.Add(lFwInfo);
        Controls.Add(groupFile);
        Controls.Add(lInfo);
        Controls.Add(lType);
        Name = "fFirmware";
        Text = "Прошивка ПО";
        Load += fFirmware_Load;
        groupFile.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Timer tPoll;
    private Label lInfo;
    private Label lType;
    private GroupBox groupFile;
    private ListBox lbFiles;
    private Button bOpen;
    private Button bRefresh;
    private Label lFwInfo;
    private ProgressBar pbProgress;
    private Label lState;
    private Button bFirmware;
    private Button bClose;
    private Label lStatus;
}