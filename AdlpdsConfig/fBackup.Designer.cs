namespace AdlpdsConfig;

partial class fBackup
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
        dgSections = new DataGridView();
        cSel = new DataGridViewCheckBoxColumn();
        cIndex = new DataGridViewTextBoxColumn();
        cType = new DataGridViewTextBoxColumn();
        cName = new DataGridViewTextBoxColumn();
        cBlocks = new DataGridViewTextBoxColumn();
        cBlockSize = new DataGridViewTextBoxColumn();
        cSize = new DataGridViewTextBoxColumn();
        groupAll = new GroupBox();
        lbBackups = new ListBox();
        bScan = new Button();
        bSelectAll = new Button();
        bSave = new Button();
        bLoad = new Button();
        bFromFile = new Button();
        bRefresh = new Button();
        lStatus = new Label();
        ((System.ComponentModel.ISupportInitialize)dgSections).BeginInit();
        groupAll.SuspendLayout();
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
        lInfo.Size = new Size(500, 20);
        lInfo.TabIndex = 0;
        // 
        // dgSections
        // 
        dgSections.AllowUserToAddRows = false;
        dgSections.AllowUserToDeleteRows = false;
        dgSections.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        dgSections.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgSections.Columns.AddRange(new DataGridViewColumn[] { cSel, cIndex, cType, cName, cBlocks, cBlockSize, cSize });
        dgSections.EditMode = DataGridViewEditMode.EditOnEnter;
        dgSections.Location = new Point(12, 40);
        dgSections.MultiSelect = false;
        dgSections.Name = "dgSections";
        dgSections.RowHeadersVisible = false;
        dgSections.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgSections.Size = new Size(725, 240);
        dgSections.TabIndex = 3;
        dgSections.CellValueChanged += dgSections_CellValueChanged;
        dgSections.CurrentCellDirtyStateChanged += dgSections_CurrentCellDirtyStateChanged;
        // 
        // cSel
        // 
        cSel.HeaderText = "";
        cSel.Name = "cSel";
        cSel.Width = 36;
        // 
        // cIndex
        // 
        cIndex.HeaderText = "№";
        cIndex.Name = "cIndex";
        cIndex.ReadOnly = true;
        cIndex.Width = 40;
        // 
        // cType
        // 
        cType.HeaderText = "Тип";
        cType.Name = "cType";
        cType.ReadOnly = true;
        cType.Width = 120;
        // 
        // cName
        // 
        cName.HeaderText = "Имя";
        cName.Name = "cName";
        cName.ReadOnly = true;
        cName.Width = 150;
        // 
        // cBlocks
        // 
        cBlocks.HeaderText = "Блоков";
        cBlocks.Name = "cBlocks";
        cBlocks.ReadOnly = true;
        cBlocks.Width = 60;
        // 
        // cBlockSize
        // 
        cBlockSize.HeaderText = "Размер блока";
        cBlockSize.Name = "cBlockSize";
        cBlockSize.ReadOnly = true;
        cBlockSize.Width = 90;
        // 
        // cSize
        // 
        cSize.HeaderText = "Размер, байт";
        cSize.Name = "cSize";
        cSize.ReadOnly = true;
        cSize.Width = 90;
        // 
        // groupAll
        // 
        groupAll.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupAll.Controls.Add(lbBackups);
        groupAll.Location = new Point(12, 290);
        groupAll.Name = "groupAll";
        groupAll.Size = new Size(857, 190);
        groupAll.TabIndex = 6;
        groupAll.TabStop = false;
        groupAll.Text = "Доступные бэкапы";
        // 
        // lbBackups
        // 
        lbBackups.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lbBackups.FormattingEnabled = true;
        lbBackups.Location = new Point(20, 22);
        lbBackups.Name = "lbBackups";
        lbBackups.Size = new Size(601, 139);
        lbBackups.TabIndex = 7;
        lbBackups.SelectedIndexChanged += lbBackups_SelectedIndexChanged;
        // 
        // bScan
        // 
        bScan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bScan.Location = new Point(753, 9);
        bScan.Name = "bScan";
        bScan.Size = new Size(116, 25);
        bScan.TabIndex = 8;
        bScan.Text = "Скан";
        bScan.UseVisualStyleBackColor = true;
        bScan.Click += bScan_Click;
        // 
        // bSelectAll
        // 
        bSelectAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bSelectAll.Location = new Point(753, 40);
        bSelectAll.Name = "bSelectAll";
        bSelectAll.Size = new Size(116, 25);
        bSelectAll.TabIndex = 9;
        bSelectAll.Text = "Выбрать все";
        bSelectAll.UseVisualStyleBackColor = true;
        bSelectAll.Click += bSelectAll_Click;
        // 
        // bSave
        // 
        bSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bSave.Location = new Point(753, 71);
        bSave.Name = "bSave";
        bSave.Size = new Size(116, 25);
        bSave.TabIndex = 10;
        bSave.Text = "Сохранить…";
        bSave.UseVisualStyleBackColor = true;
        bSave.Click += bSave_Click;
        // 
        // bLoad
        // 
        bLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bLoad.Location = new Point(753, 102);
        bLoad.Name = "bLoad";
        bLoad.Size = new Size(116, 25);
        bLoad.TabIndex = 11;
        bLoad.Text = "Загрузить";
        bLoad.UseVisualStyleBackColor = true;
        bLoad.Click += bLoad_Click;
        // 
        // bFromFile
        // 
        bFromFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bFromFile.Location = new Point(753, 133);
        bFromFile.Name = "bFromFile";
        bFromFile.Size = new Size(116, 25);
        bFromFile.TabIndex = 12;
        bFromFile.Text = "Из файла…";
        bFromFile.UseVisualStyleBackColor = true;
        bFromFile.Click += bFromFile_Click;
        // 
        // bRefresh
        // 
        bRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bRefresh.Location = new Point(753, 164);
        bRefresh.Name = "bRefresh";
        bRefresh.Size = new Size(116, 25);
        bRefresh.TabIndex = 13;
        bRefresh.Text = "Обновить";
        bRefresh.UseVisualStyleBackColor = true;
        bRefresh.Click += bRefresh_Click;
        // 
        // lStatus
        // 
        lStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lStatus.AutoEllipsis = true;
        lStatus.Location = new Point(12, 500);
        lStatus.Name = "lStatus";
        lStatus.Size = new Size(857, 20);
        lStatus.TabIndex = 14;
        // 
        // fBackup
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(881, 530);
        Controls.Add(lStatus);
        Controls.Add(bRefresh);
        Controls.Add(bFromFile);
        Controls.Add(bLoad);
        Controls.Add(bSave);
        Controls.Add(bSelectAll);
        Controls.Add(bScan);
        Controls.Add(groupAll);
        Controls.Add(dgSections);
        Controls.Add(lInfo);
        Name = "fBackup";
        Text = "Бэкап настроек";
        Load += fBackup_Load;
        ((System.ComponentModel.ISupportInitialize)dgSections).EndInit();
        groupAll.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Timer tPoll;
    private Label lInfo;
    private DataGridView dgSections;
    private DataGridViewCheckBoxColumn cSel;
    private DataGridViewTextBoxColumn cIndex;
    private DataGridViewTextBoxColumn cType;
    private DataGridViewTextBoxColumn cName;
    private DataGridViewTextBoxColumn cBlocks;
    private DataGridViewTextBoxColumn cBlockSize;
    private DataGridViewTextBoxColumn cSize;
    private GroupBox groupAll;
    private ListBox lbBackups;
    private Button bScan;
    private Button bSelectAll;
    private Button bSave;
    private Button bLoad;
    private Button bFromFile;
    private Button bRefresh;
    private Label lStatus;
}