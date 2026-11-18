namespace AdlpdsConfig.Controls
{
    partial class fConfSaver
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            MacLabel = new Label();
            IPLabel = new Label();
            MaskLabel = new Label();
            tbIp = new TextBox();
            tbMask = new TextBox();
            Cancel = new Button();
            Ok = new Button();
            MacValue = new Label();
            udBlock = new NumericUpDown();
            BlockLabel = new Label();
            BaseChannel = new Label();
            BaseChannelValue = new Label();
            tabPage1 = new TabPage();
            dgvChannels = new DataGridView();
            tablesTab = new TabControl();
            tbgw = new TextBox();
            GateWayLabel = new Label();
            SidLabel = new Label();
            cbSid = new ComboBox();
            btnManageSid = new Button();
            UuidLabel = new Label();
            cbUuid = new ComboBox();
            btnManageUuid = new Button();
            ((System.ComponentModel.ISupportInitialize)udBlock).BeginInit();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvChannels).BeginInit();
            tablesTab.SuspendLayout();
            SuspendLayout();
            // 
            // MacLabel
            // 
            MacLabel.AutoSize = true;
            MacLabel.Font = new Font("Segoe UI", 14F);
            MacLabel.Location = new Point(12, 9);
            MacLabel.Name = "MacLabel";
            MacLabel.Size = new Size(52, 25);
            MacLabel.TabIndex = 1;
            MacLabel.Text = "Mac:";
            // 
            // IPLabel
            // 
            IPLabel.AutoSize = true;
            IPLabel.Font = new Font("Segoe UI", 14F);
            IPLabel.Location = new Point(37, 46);
            IPLabel.Name = "IPLabel";
            IPLabel.Size = new Size(32, 25);
            IPLabel.TabIndex = 4;
            IPLabel.Text = "IP:";
            // 
            // MaskLabel
            // 
            MaskLabel.AutoSize = true;
            MaskLabel.Font = new Font("Segoe UI", 14F);
            MaskLabel.Location = new Point(9, 79);
            MaskLabel.Name = "MaskLabel";
            MaskLabel.Size = new Size(60, 25);
            MaskLabel.TabIndex = 5;
            MaskLabel.Text = "Mask:";
            // 
            // tbIp
            // 
            tbIp.Font = new Font("Consolas", 14F);
            tbIp.Location = new Point(81, 46);
            tbIp.Name = "tbIp";
            tbIp.Size = new Size(158, 29);
            tbIp.TabIndex = 6;
            tbIp.TextChanged += tbIp_TextChanged;
            // 
            // tbMask
            // 
            tbMask.Font = new Font("Consolas", 14F);
            tbMask.Location = new Point(81, 79);
            tbMask.Name = "tbMask";
            tbMask.Size = new Size(158, 29);
            tbMask.TabIndex = 7;
            tbMask.TextChanged += tbMask_TextChanged;
            // 
            // Cancel
            // 
            Cancel.Font = new Font("Segoe UI", 14F);
            Cancel.Location = new Point(12, 514);
            Cancel.Name = "Cancel";
            Cancel.Size = new Size(137, 60);
            Cancel.TabIndex = 8;
            Cancel.Text = "Отмена";
            Cancel.UseVisualStyleBackColor = true;
            Cancel.Click += Cancel_Click;
            // 
            // Ok
            // 
            Ok.Font = new Font("Segoe UI", 14F);
            Ok.Location = new Point(452, 514);
            Ok.Name = "Ok";
            Ok.Size = new Size(126, 60);
            Ok.TabIndex = 9;
            Ok.Text = "Сохранить";
            Ok.UseVisualStyleBackColor = true;
            Ok.Click += Ok_Click;
            // 
            // MacValue
            // 
            MacValue.AutoSize = true;
            MacValue.Font = new Font("Consolas", 14F);
            MacValue.Location = new Point(85, 12);
            MacValue.Name = "MacValue";
            MacValue.Size = new Size(130, 22);
            MacValue.TabIndex = 10;
            MacValue.Text = "{CurrentMac}";
            // 
            // udBlock
            // 
            udBlock.Font = new Font("Consolas", 14F);
            udBlock.Location = new Point(81, 184);
            udBlock.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            udBlock.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            udBlock.Name = "udBlock";
            udBlock.Size = new Size(58, 29);
            udBlock.TabIndex = 12;
            udBlock.Value = new decimal(new int[] { 1, 0, 0, 0 });
            udBlock.ValueChanged += udBlock_ValueChanged;
            // 
            // BlockLabel
            // 
            BlockLabel.AutoSize = true;
            BlockLabel.Font = new Font("Segoe UI", 14F);
            BlockLabel.Location = new Point(12, 183);
            BlockLabel.Name = "BlockLabel";
            BlockLabel.Size = new Size(57, 25);
            BlockLabel.TabIndex = 13;
            BlockLabel.Text = "Блок:";
            // 
            // BaseChannel
            // 
            BaseChannel.AutoSize = true;
            BaseChannel.Font = new Font("Segoe UI", 14F);
            BaseChannel.Location = new Point(145, 184);
            BaseChannel.Name = "BaseChannel";
            BaseChannel.Size = new Size(145, 25);
            BaseChannel.TabIndex = 16;
            BaseChannel.Text = "Базовый канал:";
            // 
            // BaseChannelValue
            // 
            BaseChannelValue.AutoSize = true;
            BaseChannelValue.Font = new Font("Segoe UI", 14F);
            BaseChannelValue.Location = new Point(296, 184);
            BaseChannelValue.Name = "BaseChannelValue";
            BaseChannelValue.Size = new Size(135, 25);
            BaseChannelValue.TabIndex = 17;
            BaseChannelValue.Text = "{Base channel}";
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dgvChannels);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(559, 265);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Каналы";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // dgvChannels
            // 
            dgvChannels.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvChannels.Location = new Point(0, 0);
            dgvChannels.Name = "dgvChannels";
            dgvChannels.Size = new Size(559, 265);
            dgvChannels.TabIndex = 14;
            // 
            // tablesTab
            // 
            tablesTab.Controls.Add(tabPage1);
            tablesTab.Location = new Point(11, 219);
            tablesTab.Name = "tablesTab";
            tablesTab.SelectedIndex = 0;
            tablesTab.Size = new Size(567, 293);
            tablesTab.TabIndex = 18;
            // 
            // tbgw
            // 
            tbgw.Font = new Font("Consolas", 14F);
            tbgw.Location = new Point(81, 114);
            tbgw.Name = "tbgw";
            tbgw.Size = new Size(158, 29);
            tbgw.TabIndex = 19;
            // 
            // GateWayLabel
            // 
            GateWayLabel.AutoSize = true;
            GateWayLabel.Font = new Font("Segoe UI", 14F);
            GateWayLabel.Location = new Point(8, 114);
            GateWayLabel.Name = "GateWayLabel";
            GateWayLabel.Size = new Size(67, 25);
            GateWayLabel.TabIndex = 20;
            GateWayLabel.Text = "Шлюз:";
            // 
            // SidLabel
            // 
            SidLabel.AutoSize = true;
            SidLabel.Font = new Font("Segoe UI", 14F);
            SidLabel.Location = new Point(245, 46);
            SidLabel.Name = "SidLabel";
            SidLabel.Size = new Size(44, 25);
            SidLabel.TabIndex = 21;
            SidLabel.Text = "SID:";
            // 
            // cbSid
            // 
            cbSid.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSid.Font = new Font("Segoe UI", 12F);
            cbSid.FormattingEnabled = true;
            cbSid.Location = new Point(295, 46);
            cbSid.Name = "cbSid";
            cbSid.Size = new Size(225, 29);
            cbSid.TabIndex = 22;
            cbSid.SelectedValueChanged += cbSid_SelectedValueChanged;
            // 
            // btnManageSid
            // 
            btnManageSid.Font = new Font("Segoe UI", 12F);
            btnManageSid.Location = new Point(526, 46);
            btnManageSid.Name = "btnManageSid";
            btnManageSid.Size = new Size(52, 30);
            btnManageSid.TabIndex = 23;
            btnManageSid.Text = "...";
            btnManageSid.UseVisualStyleBackColor = true;
            btnManageSid.Click += btnManageSid_Click;
            // 
            // UuidLabel
            // 
            UuidLabel.AutoSize = true;
            UuidLabel.Font = new Font("Segoe UI", 14F);
            UuidLabel.Location = new Point(15, 149);
            UuidLabel.Name = "UuidLabel";
            UuidLabel.Size = new Size(60, 25);
            UuidLabel.TabIndex = 24;
            UuidLabel.Text = "UUID:";
            // 
            // cbUuid
            // 
            cbUuid.DropDownStyle = ComboBoxStyle.DropDownList;
            cbUuid.Font = new Font("Segoe UI", 12F);
            cbUuid.FormattingEnabled = true;
            cbUuid.Location = new Point(81, 149);
            cbUuid.Name = "cbUuid";
            cbUuid.Size = new Size(439, 29);
            cbUuid.TabIndex = 25;
            // 
            // btnManageUuid
            // 
            btnManageUuid.Font = new Font("Segoe UI", 12F);
            btnManageUuid.Location = new Point(526, 149);
            btnManageUuid.Name = "btnManageUuid";
            btnManageUuid.Size = new Size(52, 30);
            btnManageUuid.TabIndex = 26;
            btnManageUuid.Text = "...";
            btnManageUuid.UseVisualStyleBackColor = true;
            btnManageUuid.Click += btnManageUuid_Click;
            // 
            // fConfSaver
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(589, 579);
            Controls.Add(btnManageUuid);
            Controls.Add(cbUuid);
            Controls.Add(UuidLabel);
            Controls.Add(btnManageSid);
            Controls.Add(cbSid);
            Controls.Add(SidLabel);
            Controls.Add(GateWayLabel);
            Controls.Add(tbgw);
            Controls.Add(tablesTab);
            Controls.Add(BaseChannelValue);
            Controls.Add(BaseChannel);
            Controls.Add(BlockLabel);
            Controls.Add(udBlock);
            Controls.Add(MacValue);
            Controls.Add(Ok);
            Controls.Add(Cancel);
            Controls.Add(tbMask);
            Controls.Add(tbIp);
            Controls.Add(MaskLabel);
            Controls.Add(IPLabel);
            Controls.Add(MacLabel);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "fConfSaver";
            Text = "Configuration saver";
            Load += fConfSaver_Load;
            ((System.ComponentModel.ISupportInitialize)udBlock).EndInit();
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvChannels).EndInit();
            tablesTab.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label MacLabel;
        private Label IPLabel;
        private Label MaskLabel;
        private TextBox tbIp;
        private TextBox tbMask;
        private Button Cancel;
        private Button Ok;
        private Label MacValue;
        private NumericUpDown udBlock;
        private Label BlockLabel;
        private Label BaseChannel;
        private Label BaseChannelValue;
        private TabPage tabPage1;
        private DataGridView dgvChannels;
        private TabControl tablesTab;
        private TextBox tbgw;
        private Label GateWayLabel;
        private Label SidLabel;
        private ComboBox cbSid;
        private Button btnManageSid;
        private Label UuidLabel;
        private ComboBox cbUuid;
        private Button btnManageUuid;
    }
}