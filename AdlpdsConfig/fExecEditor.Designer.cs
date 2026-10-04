namespace AdlpdsConfig;

partial class fExecEditor
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
        lStatus = new Label();
        bRead = new Button();
        gSource = new GroupBox();
        bDefault = new Button();
        bOpenFile = new Button();
        bLoadPreset = new Button();
        bWrite = new Button();
        gWrite = new GroupBox();
        bSavePreset = new Button();
        bSaveFile = new Button();
        udBlock = new NumericUpDown();
        lBaseChannel = new Label();
        tabs = new TabControl();
        tabStreams = new TabPage();
        dgvStreams = new DataGridView();
        tabChannels = new TabPage();
        dgvChannels = new DataGridView();
        gSource.SuspendLayout();
        gWrite.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)udBlock).BeginInit();
        tabs.SuspendLayout();
        tabStreams.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvStreams).BeginInit();
        tabChannels.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvChannels).BeginInit();
        SuspendLayout();
        // 
        // lStatus
        // 
        lStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lStatus.AutoEllipsis = true;
        lStatus.Location = new Point(12, 6);
        lStatus.Name = "lStatus";
        lStatus.Size = new Size(816, 18);
        lStatus.TabIndex = 0;
        // 
        // bRead
        // 
        bRead.Location = new Point(12, 22);
        bRead.Name = "bRead";
        bRead.Size = new Size(145, 28);
        bRead.TabIndex = 0;
        bRead.Text = "Загрузить с устройства";
        bRead.UseVisualStyleBackColor = true;
        bRead.Click += bRead_Click;
        // 
        // gSource
        // 
        gSource.Controls.Add(bRead);
        gSource.Controls.Add(bDefault);
        gSource.Controls.Add(bOpenFile);
        gSource.Controls.Add(bLoadPreset);
        gSource.Location = new Point(12, 26);
        gSource.Name = "gSource";
        gSource.Size = new Size(500, 95);
        gSource.TabIndex = 1;
        gSource.TabStop = false;
        gSource.Text = "Источник";
        // 
        // bDefault
        // 
        bDefault.Location = new Point(12, 52);
        bDefault.Name = "bDefault";
        bDefault.Size = new Size(145, 28);
        bDefault.TabIndex = 1;
        bDefault.Text = "По умолчанию";
        bDefault.UseVisualStyleBackColor = true;
        bDefault.Click += bDefault_Click;
        // 
        // bOpenFile
        // 
        bOpenFile.Location = new Point(163, 22);
        bOpenFile.Name = "bOpenFile";
        bOpenFile.Size = new Size(331, 28);
        bOpenFile.TabIndex = 2;
        bOpenFile.Text = "Открыть файл…";
        bOpenFile.UseVisualStyleBackColor = true;
        bOpenFile.Click += bOpenFile_Click;
        // 
        // bLoadPreset
        // 
        bLoadPreset.Location = new Point(163, 52);
        bLoadPreset.Name = "bLoadPreset";
        bLoadPreset.Size = new Size(331, 28);
        bLoadPreset.TabIndex = 3;
        bLoadPreset.Text = "Текущий пресет";
        bLoadPreset.UseVisualStyleBackColor = true;
        bLoadPreset.Click += bLoadPreset_Click;
        // 
        // bWrite
        // 
        bWrite.Location = new Point(12, 20);
        bWrite.Name = "bWrite";
        bWrite.Size = new Size(284, 28);
        bWrite.TabIndex = 0;
        bWrite.Text = "Записать на устройство";
        bWrite.UseVisualStyleBackColor = true;
        bWrite.Click += bWrite_Click;
        // 
        // gWrite
        // 
        gWrite.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        gWrite.Controls.Add(bWrite);
        gWrite.Controls.Add(bSavePreset);
        gWrite.Controls.Add(bSaveFile);
        gWrite.Location = new Point(520, 26);
        gWrite.Name = "gWrite";
        gWrite.Size = new Size(308, 88);
        gWrite.TabIndex = 2;
        gWrite.TabStop = false;
        gWrite.Text = "Запись";
        // 
        // bSavePreset
        // 
        bSavePreset.Location = new Point(12, 52);
        bSavePreset.Name = "bSavePreset";
        bSavePreset.Size = new Size(136, 28);
        bSavePreset.TabIndex = 1;
        bSavePreset.Text = "Сохранить пресет…";
        bSavePreset.UseVisualStyleBackColor = true;
        bSavePreset.Click += bSavePreset_Click;
        // 
        // bSaveFile
        // 
        bSaveFile.Location = new Point(156, 52);
        bSaveFile.Name = "bSaveFile";
        bSaveFile.Size = new Size(140, 28);
        bSaveFile.TabIndex = 2;
        bSaveFile.Text = "Сохранить в файл…";
        bSaveFile.UseVisualStyleBackColor = true;
        bSaveFile.Click += bSaveFile_Click;
        // 
        // udBlock
        // 
        udBlock.Location = new Point(12, 127);
        udBlock.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
        udBlock.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        udBlock.Name = "udBlock";
        udBlock.Size = new Size(70, 23);
        udBlock.TabIndex = 3;
        udBlock.Value = new decimal(new int[] { 1, 0, 0, 0 });
        udBlock.ValueChanged += udBlock_ValueChanged;
        // 
        // lBaseChannel
        // 
        lBaseChannel.AutoSize = true;
        lBaseChannel.Location = new Point(90, 131);
        lBaseChannel.Name = "lBaseChannel";
        lBaseChannel.Size = new Size(140, 15);
        lBaseChannel.TabIndex = 4;
        lBaseChannel.Text = "Блок 1: базовый канал 1";
        // 
        // tabs
        // 
        tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabs.Controls.Add(tabChannels);
        tabs.Controls.Add(tabStreams);
        tabs.Location = new Point(12, 158);
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(816, 390);
        tabs.TabIndex = 5;
        // 
        // tabStreams
        // 
        tabStreams.Controls.Add(dgvStreams);
        tabStreams.Location = new Point(4, 24);
        tabStreams.Name = "tabStreams";
        tabStreams.Padding = new Padding(3);
        tabStreams.Size = new Size(808, 362);
        tabStreams.TabIndex = 1;
        tabStreams.Text = "Потоки";
        tabStreams.UseVisualStyleBackColor = true;
        // 
        // dgvStreams
        // 
        dgvStreams.AllowUserToAddRows = false;
        dgvStreams.AllowUserToDeleteRows = false;
        dgvStreams.BackgroundColor = SystemColors.Window;
        dgvStreams.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvStreams.Dock = DockStyle.Fill;
        dgvStreams.Location = new Point(3, 3);
        dgvStreams.Name = "dgvStreams";
        dgvStreams.RowHeadersVisible = false;
        dgvStreams.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvStreams.Size = new Size(802, 356);
        dgvStreams.TabIndex = 0;
        dgvStreams.DataError += dgvStreams_DataError;
        // 
        // tabChannels
        // 
        tabChannels.Controls.Add(dgvChannels);
        tabChannels.Location = new Point(4, 24);
        tabChannels.Name = "tabChannels";
        tabChannels.Padding = new Padding(3);
        tabChannels.Size = new Size(808, 362);
        tabChannels.TabIndex = 0;
        tabChannels.Text = "Каналы";
        tabChannels.UseVisualStyleBackColor = true;
        // 
        // dgvChannels
        // 
        dgvChannels.AllowUserToAddRows = false;
        dgvChannels.AllowUserToDeleteRows = false;
        dgvChannels.BackgroundColor = SystemColors.Window;
        dgvChannels.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvChannels.Dock = DockStyle.Fill;
        dgvChannels.Location = new Point(3, 3);
        dgvChannels.Name = "dgvChannels";
        dgvChannels.RowHeadersVisible = false;
        dgvChannels.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvChannels.Size = new Size(802, 356);
        dgvChannels.TabIndex = 0;
        dgvChannels.DataError += dgvChannels_DataError;
        // 
        // fExecEditor
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(840, 560);
        Controls.Add(tabs);
        Controls.Add(lBaseChannel);
        Controls.Add(udBlock);
        Controls.Add(gWrite);
        Controls.Add(gSource);
        Controls.Add(lStatus);
        MinimumSize = new Size(736, 518);
        Name = "fExecEditor";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Редактор исполнительного устройства";
        Load += fExecEditor_Load;
        gSource.ResumeLayout(false);
        gWrite.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)udBlock).EndInit();
        tabs.ResumeLayout(false);
        tabStreams.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvStreams).EndInit();
        tabChannels.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvChannels).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lStatus;
    private Button bRead;
    private GroupBox gSource;
    private Button bDefault;
    private Button bOpenFile;
    private Button bLoadPreset;
    private Button bWrite;
    private GroupBox gWrite;
    private Button bSavePreset;
    private Button bSaveFile;
    private NumericUpDown udBlock;
    private Label lBaseChannel;
    private TabControl tabs;
    private TabPage tabStreams;
    private DataGridView dgvStreams;
    private DataGridViewTextBoxColumn sIndex;
    private DataGridViewComboBoxColumn sProtocol;
    private DataGridViewTextBoxColumn sUniverse;
    private DataGridViewTextBoxColumn sAddress;
    private TabPage tabChannels;
    private DataGridView dgvChannels;
    private DataGridViewTextBoxColumn cLocal;
    private DataGridViewTextBoxColumn cGlobal;
    private DataGridViewTextBoxColumn cName;
    private DataGridViewComboBoxColumn cControl;
    private DataGridViewComboBoxColumn cProtocol;
    private DataGridViewTextBoxColumn cUniverse;
    private DataGridViewTextBoxColumn cPatch;
    private DataGridViewCheckBoxColumn cWide;
    private DataGridViewTextBoxColumn cPreheat;
    private DataGridViewTextBoxColumn cLimit;
    private DataGridViewTextBoxColumn cCurve;
    private DataGridViewTextBoxColumn cSwitchDelay;
}