namespace AdlpdsConfig;

partial class fPuEditor
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
        tabChannels = new TabPage();
        dgvChannels = new DataGridView();
        tabGroups = new TabPage();
        dgvGroups = new DataGridView();
        tabPictures = new TabPage();
        dgvPictures = new DataGridView();
        tabButtons = new TabPage();
        dgvButtons = new DataGridView();
        tabSettings = new TabPage();
        pSettings = new Panel();
        tblSettings = new TableLayoutPanel();
        lPriority = new Label();
        udPriority = new NumericUpDown();
        lPicSwitch = new Label();
        udPicSwitch = new NumericUpDown();
        lPageCount = new Label();
        udPageCount = new NumericUpDown();
        lSwapX = new Label();
        udSwapX = new NumericUpDown();
        lSwapY = new Label();
        udSwapY = new NumericUpDown();
        lBrightness = new Label();
        udBrightness = new NumericUpDown();
        lBrightnessSleep = new Label();
        udBrightnessSleep = new NumericUpDown();
        lSleepTimeout = new Label();
        udSleepTimeout = new NumericUpDown();
        gSource.SuspendLayout();
        gWrite.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)udBlock).BeginInit();
        tabs.SuspendLayout();
        tabChannels.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvChannels).BeginInit();
        tabGroups.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvGroups).BeginInit();
        tabPictures.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvPictures).BeginInit();
        tabButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvButtons).BeginInit();
        tabSettings.SuspendLayout();
        pSettings.SuspendLayout();
        tblSettings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)udPriority).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udPicSwitch).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udPageCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udSwapX).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udSwapY).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udBrightness).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udBrightnessSleep).BeginInit();
        ((System.ComponentModel.ISupportInitialize)udSleepTimeout).BeginInit();
        SuspendLayout();
        // 
        // lStatus
        // 
        lStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lStatus.AutoEllipsis = true;
        lStatus.Location = new Point(12, 6);
        lStatus.Name = "lStatus";
        lStatus.Size = new Size(936, 18);
        lStatus.TabIndex = 0;
        // 
        // bRead
        // 
        bRead.Location = new Point(12, 22);
        bRead.Name = "bRead";
        bRead.Size = new Size(150, 28);
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
        gSource.Location = new Point(12, 27);
        gSource.Name = "gSource";
        gSource.Size = new Size(520, 88);
        gSource.TabIndex = 1;
        gSource.TabStop = false;
        gSource.Text = "Источник";
        // 
        // bDefault
        // 
        bDefault.Location = new Point(12, 52);
        bDefault.Name = "bDefault";
        bDefault.Size = new Size(150, 28);
        bDefault.TabIndex = 1;
        bDefault.Text = "По умолчанию";
        bDefault.UseVisualStyleBackColor = true;
        bDefault.Click += bDefault_Click;
        // 
        // bOpenFile
        // 
        bOpenFile.Location = new Point(168, 22);
        bOpenFile.Name = "bOpenFile";
        bOpenFile.Size = new Size(346, 28);
        bOpenFile.TabIndex = 2;
        bOpenFile.Text = "Открыть файл…";
        bOpenFile.UseVisualStyleBackColor = true;
        bOpenFile.Click += bOpenFile_Click;
        // 
        // bLoadPreset
        // 
        bLoadPreset.Location = new Point(168, 52);
        bLoadPreset.Name = "bLoadPreset";
        bLoadPreset.Size = new Size(346, 28);
        bLoadPreset.TabIndex = 3;
        bLoadPreset.Text = "Текущий пресет";
        bLoadPreset.UseVisualStyleBackColor = true;
        bLoadPreset.Click += bLoadPreset_Click;
        // 
        // bWrite
        // 
        bWrite.Location = new Point(12, 20);
        bWrite.Name = "bWrite";
        bWrite.Size = new Size(376, 28);
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
        gWrite.Location = new Point(548, 27);
        gWrite.Name = "gWrite";
        gWrite.Size = new Size(400, 88);
        gWrite.TabIndex = 2;
        gWrite.TabStop = false;
        gWrite.Text = "Запись";
        // 
        // bSavePreset
        // 
        bSavePreset.Location = new Point(12, 52);
        bSavePreset.Name = "bSavePreset";
        bSavePreset.Size = new Size(180, 28);
        bSavePreset.TabIndex = 1;
        bSavePreset.Text = "Сохранить пресет…";
        bSavePreset.UseVisualStyleBackColor = true;
        bSavePreset.Click += bSavePreset_Click;
        // 
        // bSaveFile
        // 
        bSaveFile.Location = new Point(200, 52);
        bSaveFile.Name = "bSaveFile";
        bSaveFile.Size = new Size(188, 28);
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
        tabs.Controls.Add(tabChannels);
        tabs.Controls.Add(tabGroups);
        tabs.Controls.Add(tabPictures);
        tabs.Controls.Add(tabButtons);
        tabs.Controls.Add(tabSettings);
        tabs.Location = new Point(12, 158);
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(936, 450);
        tabs.TabIndex = 5;
        // 
        // tabChannels
        // 
        tabChannels.Controls.Add(dgvChannels);
        tabChannels.Location = new Point(4, 24);
        tabChannels.Name = "tabChannels";
        tabChannels.Padding = new Padding(3);
        tabChannels.Size = new Size(928, 422);
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
        dgvChannels.Size = new Size(922, 416);
        dgvChannels.TabIndex = 0;
        dgvChannels.DataError += dgvChannels_DataError;
        // 
        // tabGroups
        // 
        tabGroups.Controls.Add(dgvGroups);
        tabGroups.Location = new Point(4, 24);
        tabGroups.Name = "tabGroups";
        tabGroups.Padding = new Padding(3);
        tabGroups.Size = new Size(928, 422);
        tabGroups.TabIndex = 1;
        tabGroups.Text = "Группы";
        tabGroups.UseVisualStyleBackColor = true;
        // 
        // dgvGroups
        // 
        dgvGroups.AllowUserToAddRows = false;
        dgvGroups.AllowUserToDeleteRows = false;
        dgvGroups.BackgroundColor = SystemColors.Window;
        dgvGroups.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvGroups.Dock = DockStyle.Fill;
        dgvGroups.Location = new Point(3, 3);
        dgvGroups.Name = "dgvGroups";
        dgvGroups.RowHeadersVisible = false;
        dgvGroups.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvGroups.Size = new Size(922, 416);
        dgvGroups.TabIndex = 0;
        dgvGroups.DataError += dgvGroups_DataError;
        // 
        // tabPictures
        // 
        tabPictures.Controls.Add(dgvPictures);
        tabPictures.Location = new Point(4, 24);
        tabPictures.Name = "tabPictures";
        tabPictures.Padding = new Padding(3);
        tabPictures.Size = new Size(928, 422);
        tabPictures.TabIndex = 2;
        tabPictures.Text = "Картины";
        tabPictures.UseVisualStyleBackColor = true;
        // 
        // dgvPictures
        // 
        dgvPictures.AllowUserToAddRows = false;
        dgvPictures.AllowUserToDeleteRows = false;
        dgvPictures.BackgroundColor = SystemColors.Window;
        dgvPictures.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvPictures.Dock = DockStyle.Fill;
        dgvPictures.Location = new Point(3, 3);
        dgvPictures.Name = "dgvPictures";
        dgvPictures.RowHeadersVisible = false;
        dgvPictures.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPictures.Size = new Size(922, 416);
        dgvPictures.TabIndex = 0;
        dgvPictures.DataError += dgvPictures_DataError;
        // 
        // tabButtons
        // 
        tabButtons.Controls.Add(dgvButtons);
        tabButtons.Location = new Point(4, 24);
        tabButtons.Name = "tabButtons";
        tabButtons.Padding = new Padding(3);
        tabButtons.Size = new Size(928, 422);
        tabButtons.TabIndex = 3;
        tabButtons.Text = "Кнопки";
        tabButtons.UseVisualStyleBackColor = true;
        // 
        // dgvButtons
        // 
        dgvButtons.AllowUserToAddRows = false;
        dgvButtons.AllowUserToDeleteRows = false;
        dgvButtons.BackgroundColor = SystemColors.Window;
        dgvButtons.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvButtons.Dock = DockStyle.Fill;
        dgvButtons.Location = new Point(3, 3);
        dgvButtons.Name = "dgvButtons";
        dgvButtons.RowHeadersVisible = false;
        dgvButtons.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvButtons.Size = new Size(922, 416);
        dgvButtons.TabIndex = 0;
        dgvButtons.DataError += dgvButtons_DataError;
        // 
        // tabSettings
        // 
        tabSettings.Controls.Add(pSettings);
        tabSettings.Location = new Point(4, 24);
        tabSettings.Name = "tabSettings";
        tabSettings.Padding = new Padding(3);
        tabSettings.Size = new Size(928, 422);
        tabSettings.TabIndex = 4;
        tabSettings.Text = "Параметры";
        tabSettings.UseVisualStyleBackColor = true;
        // 
        // pSettings
        // 
        pSettings.Controls.Add(tblSettings);
        pSettings.Dock = DockStyle.Fill;
        pSettings.Location = new Point(3, 3);
        pSettings.Name = "pSettings";
        pSettings.Size = new Size(922, 416);
        pSettings.TabIndex = 0;
        // 
        // tblSettings
        // 
        tblSettings.ColumnCount = 2;
        tblSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        tblSettings.ColumnStyles.Add(new ColumnStyle());
        tblSettings.Controls.Add(lPriority, 0, 0);
        tblSettings.Controls.Add(udPriority, 1, 0);
        tblSettings.Controls.Add(lPicSwitch, 0, 1);
        tblSettings.Controls.Add(udPicSwitch, 1, 1);
        tblSettings.Controls.Add(lPageCount, 0, 2);
        tblSettings.Controls.Add(udPageCount, 1, 2);
        tblSettings.Controls.Add(lSwapX, 0, 3);
        tblSettings.Controls.Add(udSwapX, 1, 3);
        tblSettings.Controls.Add(lSwapY, 0, 4);
        tblSettings.Controls.Add(udSwapY, 1, 4);
        tblSettings.Controls.Add(lBrightness, 0, 5);
        tblSettings.Controls.Add(udBrightness, 1, 5);
        tblSettings.Controls.Add(lBrightnessSleep, 0, 6);
        tblSettings.Controls.Add(udBrightnessSleep, 1, 6);
        tblSettings.Controls.Add(lSleepTimeout, 0, 7);
        tblSettings.Controls.Add(udSleepTimeout, 1, 7);
        tblSettings.Dock = DockStyle.Fill;
        tblSettings.Location = new Point(0, 0);
        tblSettings.Name = "tblSettings";
        tblSettings.RowCount = 8;
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.RowStyles.Add(new RowStyle());
        tblSettings.Size = new Size(922, 416);
        tblSettings.TabIndex = 0;
        // 
        // lPriority
        // 
        lPriority.Anchor = AnchorStyles.Left;
        lPriority.AutoSize = true;
        lPriority.Location = new Point(3, 7);
        lPriority.Name = "lPriority";
        lPriority.Size = new Size(67, 15);
        lPriority.TabIndex = 0;
        lPriority.Text = "Приоритет";
        // 
        // udPriority
        // 
        udPriority.Anchor = AnchorStyles.Left;
        udPriority.Location = new Point(263, 3);
        udPriority.Maximum = new decimal(new int[] { 254, 0, 0, 0 });
        udPriority.Name = "udPriority";
        udPriority.Size = new Size(120, 23);
        udPriority.TabIndex = 1;
        udPriority.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // lPicSwitch
        // 
        lPicSwitch.Anchor = AnchorStyles.Left;
        lPicSwitch.AutoSize = true;
        lPicSwitch.Location = new Point(3, 36);
        lPicSwitch.Name = "lPicSwitch";
        lPicSwitch.Size = new Size(177, 15);
        lPicSwitch.TabIndex = 2;
        lPicSwitch.Text = "Переход между картинами, мс";
        // 
        // udPicSwitch
        // 
        udPicSwitch.Anchor = AnchorStyles.Left;
        udPicSwitch.Location = new Point(263, 32);
        udPicSwitch.Maximum = new decimal(new int[] { 20000, 0, 0, 0 });
        udPicSwitch.Name = "udPicSwitch";
        udPicSwitch.Size = new Size(120, 23);
        udPicSwitch.TabIndex = 3;
        // 
        // lPageCount
        // 
        lPageCount.Anchor = AnchorStyles.Left;
        lPageCount.AutoSize = true;
        lPageCount.Location = new Point(3, 65);
        lPageCount.Name = "lPageCount";
        lPageCount.Size = new Size(90, 15);
        lPageCount.TabIndex = 4;
        lPageCount.Text = "Число страниц";
        // 
        // udPageCount
        // 
        udPageCount.Anchor = AnchorStyles.Left;
        udPageCount.Location = new Point(263, 61);
        udPageCount.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
        udPageCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        udPageCount.Name = "udPageCount";
        udPageCount.Size = new Size(120, 23);
        udPageCount.TabIndex = 5;
        udPageCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // lSwapX
        // 
        lSwapX.Anchor = AnchorStyles.Left;
        lSwapX.AutoSize = true;
        lSwapX.Location = new Point(3, 94);
        lSwapX.Name = "lSwapX";
        lSwapX.Size = new Size(170, 15);
        lSwapX.TabIndex = 6;
        lSwapX.Text = "Поменять ось X сенсора (0-1)";
        // 
        // udSwapX
        // 
        udSwapX.Anchor = AnchorStyles.Left;
        udSwapX.Location = new Point(263, 90);
        udSwapX.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        udSwapX.Name = "udSwapX";
        udSwapX.Size = new Size(120, 23);
        udSwapX.TabIndex = 7;
        // 
        // lSwapY
        // 
        lSwapY.Anchor = AnchorStyles.Left;
        lSwapY.AutoSize = true;
        lSwapY.Location = new Point(3, 123);
        lSwapY.Name = "lSwapY";
        lSwapY.Size = new Size(170, 15);
        lSwapY.TabIndex = 8;
        lSwapY.Text = "Поменять ось Y сенсора (0-1)";
        // 
        // udSwapY
        // 
        udSwapY.Anchor = AnchorStyles.Left;
        udSwapY.Location = new Point(263, 119);
        udSwapY.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
        udSwapY.Name = "udSwapY";
        udSwapY.Size = new Size(120, 23);
        udSwapY.TabIndex = 9;
        // 
        // lBrightness
        // 
        lBrightness.Anchor = AnchorStyles.Left;
        lBrightness.AutoSize = true;
        lBrightness.Location = new Point(3, 152);
        lBrightness.Name = "lBrightness";
        lBrightness.Size = new Size(67, 15);
        lBrightness.TabIndex = 10;
        lBrightness.Text = "Яркость, %";
        // 
        // udBrightness
        // 
        udBrightness.Anchor = AnchorStyles.Left;
        udBrightness.Location = new Point(263, 148);
        udBrightness.Name = "udBrightness";
        udBrightness.Size = new Size(120, 23);
        udBrightness.TabIndex = 11;
        udBrightness.Value = new decimal(new int[] { 100, 0, 0, 0 });
        // 
        // lBrightnessSleep
        // 
        lBrightnessSleep.Anchor = AnchorStyles.Left;
        lBrightnessSleep.AutoSize = true;
        lBrightnessSleep.Location = new Point(3, 181);
        lBrightnessSleep.Name = "lBrightnessSleep";
        lBrightnessSleep.Size = new Size(113, 15);
        lBrightnessSleep.TabIndex = 12;
        lBrightnessSleep.Text = "Яркость при сне, %";
        // 
        // udBrightnessSleep
        // 
        udBrightnessSleep.Anchor = AnchorStyles.Left;
        udBrightnessSleep.Location = new Point(263, 177);
        udBrightnessSleep.Name = "udBrightnessSleep";
        udBrightnessSleep.Size = new Size(120, 23);
        udBrightnessSleep.TabIndex = 13;
        udBrightnessSleep.Value = new decimal(new int[] { 100, 0, 0, 0 });
        // 
        // lSleepTimeout
        // 
        lSleepTimeout.Anchor = AnchorStyles.Left;
        lSleepTimeout.AutoSize = true;
        lSleepTimeout.Location = new Point(3, 302);
        lSleepTimeout.Name = "lSleepTimeout";
        lSleepTimeout.Size = new Size(92, 15);
        lSleepTimeout.TabIndex = 14;
        lSleepTimeout.Text = "Время до сна, с";
        // 
        // udSleepTimeout
        // 
        udSleepTimeout.Anchor = AnchorStyles.Left;
        udSleepTimeout.Location = new Point(263, 298);
        udSleepTimeout.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
        udSleepTimeout.Name = "udSleepTimeout";
        udSleepTimeout.Size = new Size(120, 23);
        udSleepTimeout.TabIndex = 15;
        // 
        // fPuEditor
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 620);
        Controls.Add(tabs);
        Controls.Add(lBaseChannel);
        Controls.Add(udBlock);
        Controls.Add(gWrite);
        Controls.Add(gSource);
        Controls.Add(lStatus);
        MinimumSize = new Size(836, 558);
        Name = "fPuEditor";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Редактор управляющего устройства";
        Load += fPuEditor_Load;
        gSource.ResumeLayout(false);
        gWrite.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)udBlock).EndInit();
        tabs.ResumeLayout(false);
        tabChannels.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvChannels).EndInit();
        tabGroups.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvGroups).EndInit();
        tabPictures.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvPictures).EndInit();
        tabButtons.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvButtons).EndInit();
        tabSettings.ResumeLayout(false);
        pSettings.ResumeLayout(false);
        tblSettings.ResumeLayout(false);
        tblSettings.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)udPriority).EndInit();
        ((System.ComponentModel.ISupportInitialize)udPicSwitch).EndInit();
        ((System.ComponentModel.ISupportInitialize)udPageCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)udSwapX).EndInit();
        ((System.ComponentModel.ISupportInitialize)udSwapY).EndInit();
        ((System.ComponentModel.ISupportInitialize)udBrightness).EndInit();
        ((System.ComponentModel.ISupportInitialize)udBrightnessSleep).EndInit();
        ((System.ComponentModel.ISupportInitialize)udSleepTimeout).EndInit();
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
    private TabPage tabChannels;
    private DataGridView dgvChannels;
    private DataGridViewTextBoxColumn cLocal;
    private DataGridViewTextBoxColumn cName;
    private DataGridViewTextBoxColumn cIndex;
    private DataGridViewComboBoxColumn cType;
    private TabPage tabGroups;
    private DataGridView dgvGroups;
    private DataGridViewTextBoxColumn gLocal;
    private DataGridViewTextBoxColumn gIndex;
    private DataGridViewTextBoxColumn gChannels;
    private TabPage tabPictures;
    private DataGridView dgvPictures;
    private DataGridViewTextBoxColumn pLocal;
    private DataGridViewTextBoxColumn pName;
    private TabPage tabButtons;
    private DataGridView dgvButtons;
    private DataGridViewTextBoxColumn bLocal;
    private DataGridViewTextBoxColumn bPage;
    private DataGridViewTextBoxColumn bLocalChannel;
    private DataGridViewTextBoxColumn bPosX;
    private DataGridViewTextBoxColumn bPosY;
    private DataGridViewTextBoxColumn bIcon;
    private DataGridViewTextBoxColumn bColor;
    private TabPage tabSettings;
    private Panel pSettings;
    private TableLayoutPanel tblSettings;
    private Label lPriority;
    private NumericUpDown udPriority;
    private Label lPicSwitch;
    private NumericUpDown udPicSwitch;
    private Label lPageCount;
    private NumericUpDown udPageCount;
    private Label lSwapX;
    private NumericUpDown udSwapX;
    private Label lSwapY;
    private NumericUpDown udSwapY;
    private Label lBrightness;
    private NumericUpDown udBrightness;
    private Label lBrightnessSleep;
    private NumericUpDown udBrightnessSleep;
    private Label lSleepTimeout;
    private NumericUpDown udSleepTimeout;
}