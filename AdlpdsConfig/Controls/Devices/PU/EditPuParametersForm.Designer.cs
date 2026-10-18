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
            this.lblName = new System.Windows.Forms.Label();
            this._txtName = new System.Windows.Forms.TextBox();
            this.lblSID = new System.Windows.Forms.Label();
            this._udSID = new System.Windows.Forms.NumericUpDown();
            this.lblMac = new System.Windows.Forms.Label();
            this._txtMac = new System.Windows.Forms.TextBox();
            this.lblIp = new System.Windows.Forms.Label();
            this._txtIp = new System.Windows.Forms.TextBox();
            this.lblMask = new System.Windows.Forms.Label();
            this._txtMask = new System.Windows.Forms.TextBox();
            this.lblGateway = new System.Windows.Forms.Label();
            this._txtGateway = new System.Windows.Forms.TextBox();
            this._tablesTab = new System.Windows.Forms.TabControl();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._udSID)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 9);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(62, 15);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Название:";
            // 
            // _txtName
            // 
            this._txtName.Location = new System.Drawing.Point(12, 27);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(236, 23);
            this._txtName.TabIndex = 0;
            // 
            // lblSID
            // 
            this.lblSID.AutoSize = true;
            this.lblSID.Location = new System.Drawing.Point(270, 9);
            this.lblSID.Name = "lblSID";
            this.lblSID.Size = new System.Drawing.Size(29, 15);
            this.lblSID.TabIndex = 15;
            this.lblSID.Text = "SID:";
            // 
            // _udSID
            // 
            this._udSID.Location = new System.Drawing.Point(270, 27);
            this._udSID.Maximum = new decimal(new int[] { 2147483647, 0, 0, 0 });
            this._udSID.Name = "_udSID";
            this._udSID.Size = new System.Drawing.Size(100, 23);
            this._udSID.TabIndex = 16;
            // 
            // lblMac
            // 
            this.lblMac.AutoSize = true;
            this.lblMac.Location = new System.Drawing.Point(12, 53);
            this.lblMac.Name = "lblMac";
            this.lblMac.Size = new System.Drawing.Size(33, 15);
            this.lblMac.TabIndex = 5;
            this.lblMac.Text = "Mac:";
            // 
            // _txtMac
            // 
            this._txtMac.Location = new System.Drawing.Point(12, 71);
            this._txtMac.Name = "_txtMac";
            this._txtMac.Size = new System.Drawing.Size(236, 23);
            this._txtMac.TabIndex = 3;
            this._txtMac.Validating += new System.ComponentModel.CancelEventHandler(this._txtMac_Validating);
            // 
            // lblIp
            // 
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(12, 97);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(20, 15);
            this.lblIp.TabIndex = 7;
            this.lblIp.Text = "Ip:";
            // 
            // _txtIp
            // 
            this._txtIp.Location = new System.Drawing.Point(12, 115);
            this._txtIp.Name = "_txtIp";
            this._txtIp.Size = new System.Drawing.Size(236, 23);
            this._txtIp.TabIndex = 4;
            this._txtIp.Validating += new System.ComponentModel.CancelEventHandler(this._txtIp_Validating);
            // 
            // lblMask
            // 
            this.lblMask.AutoSize = true;
            this.lblMask.Location = new System.Drawing.Point(12, 141);
            this.lblMask.Name = "lblMask";
            this.lblMask.Size = new System.Drawing.Size(38, 15);
            this.lblMask.TabIndex = 8;
            this.lblMask.Text = "Mask:";
            // 
            // _txtMask
            // 
            this._txtMask.Location = new System.Drawing.Point(12, 159);
            this._txtMask.Name = "_txtMask";
            this._txtMask.Size = new System.Drawing.Size(236, 23);
            this._txtMask.TabIndex = 6;
            this._txtMask.Validating += new System.ComponentModel.CancelEventHandler(this._txtMask_Validating);
            // 
            // lblGateway
            // 
            this.lblGateway.AutoSize = true;
            this.lblGateway.Location = new System.Drawing.Point(12, 185);
            this.lblGateway.Name = "lblGateway";
            this.lblGateway.Size = new System.Drawing.Size(44, 15);
            this.lblGateway.TabIndex = 11;
            this.lblGateway.Text = "Шлюз:";
            // 
            // _txtGateway
            // 
            this._txtGateway.Location = new System.Drawing.Point(12, 203);
            this._txtGateway.Name = "_txtGateway";
            this._txtGateway.Size = new System.Drawing.Size(236, 23);
            this._txtGateway.TabIndex = 10;
            // 
            // _tablesTab
            // 
            this._tablesTab.Location = new System.Drawing.Point(12, 232);
            this._tablesTab.Name = "_tablesTab";
            this._tablesTab.SelectedIndex = 0;
            this._tablesTab.Size = new System.Drawing.Size(528, 230);
            this._tablesTab.TabIndex = 12;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(465, 472);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(12, 472);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 14;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // EditPuParametersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(552, 507);
            this.Controls.Add(this._udSID);
            this.Controls.Add(this.lblSID);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this._tablesTab);
            this.Controls.Add(this._txtGateway);
            this.Controls.Add(this.lblGateway);
            this.Controls.Add(this._txtMask);
            this.Controls.Add(this.lblMask);
            this.Controls.Add(this._txtIp);
            this.Controls.Add(this.lblIp);
            this.Controls.Add(this._txtMac);
            this.Controls.Add(this.lblMac);
            this.Controls.Add(this._txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "EditPuParametersForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Параметры устройства";
            ((System.ComponentModel.ISupportInitialize)(this._udSID)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
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