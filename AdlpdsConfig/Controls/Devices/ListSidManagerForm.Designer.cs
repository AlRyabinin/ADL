namespace AdlpdsConfig.Controls.Devices
{
    partial class ListSidManagerForm
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
            _gridSids = new DataGridView();
            _groupCreate = new GroupBox();
            _lblNameObjSid = new Label();
            _txtNameObjSid = new TextBox();
            _lblComment = new Label();
            _txtComment = new TextBox();
            _btnCreate = new Button();
            _btnRefresh = new Button();
            _btnClose = new Button();
            _lblCount = new Label();
            _lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)_gridSids).BeginInit();
            _groupCreate.SuspendLayout();
            SuspendLayout();
            // 
            // _gridSids
            // 
            _gridSids.AllowUserToAddRows = false;
            _gridSids.AllowUserToDeleteRows = false;
            _gridSids.AllowUserToResizeRows = false;
            _gridSids.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _gridSids.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridSids.BackgroundColor = SystemColors.Window;
            _gridSids.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridSids.Location = new Point(12, 12);
            _gridSids.MultiSelect = false;
            _gridSids.Name = "_gridSids";
            _gridSids.ReadOnly = true;
            _gridSids.RowHeadersVisible = false;
            _gridSids.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridSids.Size = new Size(956, 282);
            _gridSids.TabIndex = 0;
            // 
            // _groupCreate
            // 
            _groupCreate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _groupCreate.Controls.Add(_lblNameObjSid);
            _groupCreate.Controls.Add(_txtNameObjSid);
            _groupCreate.Controls.Add(_lblComment);
            _groupCreate.Controls.Add(_txtComment);
            _groupCreate.Controls.Add(_btnCreate);
            _groupCreate.Location = new Point(12, 316);
            _groupCreate.Name = "_groupCreate";
            _groupCreate.Size = new Size(956, 126);
            _groupCreate.TabIndex = 1;
            _groupCreate.TabStop = false;
            _groupCreate.Text = "Создать собственный SID";
            // 
            // _lblNameObjSid
            // 
            _lblNameObjSid.AutoSize = true;
            _lblNameObjSid.Location = new Point(14, 27);
            _lblNameObjSid.Name = "_lblNameObjSid";
            _lblNameObjSid.Size = new Size(106, 15);
            _lblNameObjSid.TabIndex = 0;
            _lblNameObjSid.Text = "Название объекта";
            // 
            // _txtNameObjSid
            // 
            _txtNameObjSid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtNameObjSid.Location = new Point(17, 47);
            _txtNameObjSid.MaxLength = 64;
            _txtNameObjSid.Name = "_txtNameObjSid";
            _txtNameObjSid.Size = new Size(425, 23);
            _txtNameObjSid.TabIndex = 1;
            // 
            // _lblComment
            // 
            _lblComment.AutoSize = true;
            _lblComment.Location = new Point(460, 27);
            _lblComment.Name = "_lblComment";
            _lblComment.Size = new Size(84, 15);
            _lblComment.TabIndex = 2;
            _lblComment.Text = "Комментарий";
            // 
            // _txtComment
            // 
            _txtComment.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtComment.Location = new Point(463, 47);
            _txtComment.MaxLength = 255;
            _txtComment.Name = "_txtComment";
            _txtComment.Size = new Size(474, 23);
            _txtComment.TabIndex = 3;
            // 
            // _btnCreate
            // 
            _btnCreate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCreate.Location = new Point(823, 86);
            _btnCreate.Name = "_btnCreate";
            _btnCreate.Size = new Size(114, 27);
            _btnCreate.TabIndex = 4;
            _btnCreate.Text = "Создать SID";
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
            // ListSidManagerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 493);
            Controls.Add(_lblStatus);
            Controls.Add(_lblCount);
            Controls.Add(_btnClose);
            Controls.Add(_btnRefresh);
            Controls.Add(_groupCreate);
            Controls.Add(_gridSids);
            MinimumSize = new Size(800, 520);
            Name = "ListSidManagerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Справочник SID";
            Load += ListSidManagerForm_Load;
            ((System.ComponentModel.ISupportInitialize)_gridSids).EndInit();
            _groupCreate.ResumeLayout(false);
            _groupCreate.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView _gridSids;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colPkListSid;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colNameObjSid;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colIdTxtDCon;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colComment;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colRegWebUser;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDateRec;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDateRecOn;
        private System.Windows.Forms.GroupBox _groupCreate;
        private System.Windows.Forms.Label _lblNameObjSid;
        private System.Windows.Forms.TextBox _txtNameObjSid;
        private System.Windows.Forms.Label _lblComment;
        private System.Windows.Forms.TextBox _txtComment;
        private System.Windows.Forms.Button _btnCreate;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.Label _lblCount;
        private System.Windows.Forms.Label _lblStatus;
    }
}
