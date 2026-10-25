namespace AdlpdsConfig.Controls.Devices
{
    partial class EditPuParametersForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblName = new Label();
            _txtName = new TextBox();
            lblSID = new Label();
            _udSID = new NumericUpDown();
            lblMac = new Label();
            _txtMac = new TextBox();
            lblIp = new Label();
            _txtIp = new TextBox();
            lblMask = new Label();
            _txtMask = new TextBox();
            lblGateway = new Label();
            _txtGateway = new TextBox();
            _tablesTab = new TabControl();
            btnSave = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)_udSID).BeginInit();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(12, 9);
            lblName.Name = "lblName";
            lblName.Size = new Size(62, 15);
            lblName.TabIndex = 1;
            lblName.Text = "Название:";
            // 
            // _txtName
            // 
            _txtName.Location = new Point(12, 27);
            _txtName.Name = "_txtName";
            _txtName.Size = new Size(236, 23);
            _txtName.TabIndex = 0;
            // 
            // lblSID
            // 
            lblSID.AutoSize = true;
            lblSID.Location = new Point(12, 53);
            lblSID.Name = "lblSID";
            lblSID.Size = new Size(27, 15);
            lblSID.TabIndex = 15;
            lblSID.Text = "SID:";
            // 
            // _udSID
            // 
            _udSID.Location = new Point(12, 71);
            _udSID.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            _udSID.Name = "_udSID";
            _udSID.Size = new Size(116, 23);
            _udSID.TabIndex = 16;
            // 
            // lblMac
            // 
            lblMac.AutoSize = true;
            lblMac.Location = new Point(286, 8);
            lblMac.Name = "lblMac";
            lblMac.Size = new Size(33, 15);
            lblMac.TabIndex = 5;
            lblMac.Text = "Mac:";
            // 
            // _txtMac
            // 
            _txtMac.Location = new Point(286, 26);
            _txtMac.Name = "_txtMac";
            _txtMac.Size = new Size(234, 23);
            _txtMac.TabIndex = 3;
            _txtMac.Validating += _txtMac_Validating;
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(286, 52);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(20, 15);
            lblIp.TabIndex = 7;
            lblIp.Text = "Ip:";
            // 
            // _txtIp
            // 
            _txtIp.Location = new Point(286, 70);
            _txtIp.Name = "_txtIp";
            _txtIp.Size = new Size(234, 23);
            _txtIp.TabIndex = 4;
            _txtIp.Validating += _txtIp_Validating;
            // 
            // lblMask
            // 
            lblMask.AutoSize = true;
            lblMask.Location = new Point(286, 96);
            lblMask.Name = "lblMask";
            lblMask.Size = new Size(38, 15);
            lblMask.TabIndex = 8;
            lblMask.Text = "Mask:";
            // 
            // _txtMask
            // 
            _txtMask.Location = new Point(286, 114);
            _txtMask.Name = "_txtMask";
            _txtMask.Size = new Size(234, 23);
            _txtMask.TabIndex = 6;
            _txtMask.Validating += _txtMask_Validating;
            // 
            // lblGateway
            // 
            lblGateway.AutoSize = true;
            lblGateway.Location = new Point(286, 140);
            lblGateway.Name = "lblGateway";
            lblGateway.Size = new Size(43, 15);
            lblGateway.TabIndex = 11;
            lblGateway.Text = "Шлюз:";
            // 
            // _txtGateway
            // 
            _txtGateway.Location = new Point(286, 158);
            _txtGateway.Name = "_txtGateway";
            _txtGateway.Size = new Size(234, 23);
            _txtGateway.TabIndex = 10;
            // 
            // _tablesTab
            // 
            _tablesTab.Location = new Point(12, 201);
            _tablesTab.Name = "_tablesTab";
            _tablesTab.SelectedIndex = 0;
            _tablesTab.Size = new Size(508, 230);
            _tablesTab.TabIndex = 12;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(445, 437);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 13;
            btnSave.Text = "Сохранить";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(12, 437);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // EditPuParametersForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(532, 472);
            Controls.Add(_udSID);
            Controls.Add(lblSID);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(_tablesTab);
            Controls.Add(_txtGateway);
            Controls.Add(lblGateway);
            Controls.Add(_txtMask);
            Controls.Add(lblMask);
            Controls.Add(_txtIp);
            Controls.Add(lblIp);
            Controls.Add(_txtMac);
            Controls.Add(lblMac);
            Controls.Add(_txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "EditPuParametersForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Параметры устройства";
            ((System.ComponentModel.ISupportInitialize)_udSID).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox _txtName;
        private System.Windows.Forms.Label lblSID;
        private System.Windows.Forms.NumericUpDown _udSID;
        private System.Windows.Forms.Label lblMac;
        private System.Windows.Forms.TextBox _txtMac;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox _txtIp;
        private System.Windows.Forms.Label lblMask;
        private System.Windows.Forms.TextBox _txtMask;
        private System.Windows.Forms.Label lblGateway;
        private System.Windows.Forms.TextBox _txtGateway;
        private System.Windows.Forms.TabControl _tablesTab;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}