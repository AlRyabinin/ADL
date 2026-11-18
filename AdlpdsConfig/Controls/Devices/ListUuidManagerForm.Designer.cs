namespace AdlpdsConfig.Controls.Devices
{
    partial class ListUuidManagerForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if(disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            _lblContext = new Label();
            _gridUuids = new DataGridView();
            _groupCreate = new GroupBox();
            _lblSn = new Label();
            _mtbSn = new MaskedTextBox();
            _btnCreate = new Button();
            _btnRefresh = new Button();
            _btnClose = new Button();
            _lblCount = new Label();
            _lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)_gridUuids).BeginInit();
            _groupCreate.SuspendLayout();
            SuspendLayout();
            // 
            // _lblContext
            // 
            _lblContext.AutoSize = true;
            _lblContext.Location = new Point(12, 12);
            _lblContext.Name = "_lblContext";
            _lblContext.Size = new Size(0, 15);
            _lblContext.TabIndex = 7;
            // 
            // _gridUuids
            // 
            _gridUuids.AllowUserToAddRows = false;
            _gridUuids.AllowUserToDeleteRows = false;
            _gridUuids.AllowUserToResizeRows = false;
            _gridUuids.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _gridUuids.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridUuids.BackgroundColor = SystemColors.Window;
            _gridUuids.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridUuids.Location = new Point(12, 36);
            _gridUuids.MultiSelect = false;
            _gridUuids.Name = "_gridUuids";
            _gridUuids.ReadOnly = true;
            _gridUuids.RowHeadersVisible = false;
            _gridUuids.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridUuids.Size = new Size(956, 258);
            _gridUuids.TabIndex = 0;
            _gridUuids.DataBindingComplete += GridUuids_DataBindingComplete;
            // 
            // _groupCreate
            // 
            _groupCreate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _groupCreate.Controls.Add(_lblSn);
            _groupCreate.Controls.Add(_mtbSn);
            _groupCreate.Controls.Add(_btnCreate);
            _groupCreate.Location = new Point(12, 316);
            _groupCreate.Name = "_groupCreate";
            _groupCreate.Size = new Size(956, 126);
            _groupCreate.TabIndex = 1;
            _groupCreate.TabStop = false;
            _groupCreate.Text = "Создать UUID";
            // 
            // _lblSn
            // 
            _lblSn.AutoSize = true;
            _lblSn.Location = new Point(14, 27);
            _lblSn.Name = "_lblSn";
            _lblSn.Size = new Size(185, 15);
            _lblSn.TabIndex = 0;
            _lblSn.Text = "Серийный номер (SN)";
            // 
            // _mtbSn
            // 
            _mtbSn.Font = new Font("Consolas", 14F);
            _mtbSn.Location = new Point(17, 47);
            _mtbSn.Name = "_mtbSn";
            _mtbSn.Size = new Size(320, 29);
            _mtbSn.TabIndex = 1;
            // 
            // _btnCreate
            // 
            _btnCreate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCreate.Location = new Point(823, 86);
            _btnCreate.Name = "_btnCreate";
            _btnCreate.Size = new Size(114, 27);
            _btnCreate.TabIndex = 2;
            _btnCreate.Text = "Создать UUID";
            _btnCreate.UseVisualStyleBackColor = true;
            _btnCreate.Click += BtnCreate_Click;
            // 
            // _btnRefresh
            // 
            _btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnRefresh.Location = new Point(12, 454);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.Size = new Size(96, 27);
            _btnRefresh.TabIndex = 2;
            _btnRefresh.Text = "Обновить";
            _btnRefresh.UseVisualStyleBackColor = true;
            _btnRefresh.Click += BtnRefresh_Click;
            // 
            // _btnClose
            // 
            _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnClose.DialogResult = DialogResult.OK;
            _btnClose.Location = new Point(893, 454);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(75, 27);
            _btnClose.TabIndex = 3;
            _btnClose.Text = "Закрыть";
            _btnClose.UseVisualStyleBackColor = true;
            _btnClose.Click += BtnClose_Click;
            // 
            // _lblCount
            // 
            _lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _lblCount.AutoSize = true;
            _lblCount.Location = new Point(12, 299);
            _lblCount.Name = "_lblCount";
            _lblCount.Size = new Size(65, 15);
            _lblCount.TabIndex = 5;
            _lblCount.Text = "Записей: 0";
            // 
            // _lblStatus
            // 
            _lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _lblStatus.AutoSize = true;
            _lblStatus.ForeColor = SystemColors.GrayText;
            _lblStatus.Location = new Point(660, 299);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(0, 15);
            _lblStatus.TabIndex = 6;
            // 
            // ListUuidManagerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 493);
            Controls.Add(_lblContext);
            Controls.Add(_lblStatus);
            Controls.Add(_lblCount);
            Controls.Add(_btnClose);
            Controls.Add(_btnRefresh);
            Controls.Add(_groupCreate);
            Controls.Add(_gridUuids);
            MinimumSize = new Size(800, 520);
            Name = "ListUuidManagerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Справочник UUID";
            Load += ListUuidManagerForm_Load;
            ((System.ComponentModel.ISupportInitialize)_gridUuids).EndInit();
            _groupCreate.ResumeLayout(false);
            _groupCreate.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblContext;
        private System.Windows.Forms.DataGridView _gridUuids;
        private System.Windows.Forms.GroupBox _groupCreate;
        private System.Windows.Forms.Label _lblSn;
        private System.Windows.Forms.MaskedTextBox _mtbSn;
        private System.Windows.Forms.Button _btnCreate;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.Label _lblCount;
        private System.Windows.Forms.Label _lblStatus;
    }
}
