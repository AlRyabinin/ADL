namespace AdlpdsConfig.Controls.Devices.NetGate
{
    partial class EditNetGateParametersForm
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
            NameTextBox = new TextBox();
            label1 = new Label();
            SaveButton = new Button();
            CancelButton = new Button();
            MacTextBox = new TextBox();
            IpTextBox = new TextBox();
            label2 = new Label();
            MaskTextBox = new TextBox();
            label3 = new Label();
            label4 = new Label();
            tabPagePictures = new TabPage();
            PicturesGrid = new DataGridView();
            tabPageChannels = new TabPage();
            ChannelsGrid = new DataGridView();
            tabPageConveyors = new TabPage();
            ConveyorsGrid = new DataGridView();
            btnConveyorProps = new Button();
            _cbConveyorCount = new ComboBox();
            lblConveyorCount = new Label();
            tablesTab = new TabControl();
            label5 = new Label();
            _cbSid = new ComboBox();
            label6 = new Label();
            textBox1 = new TextBox();
            _btnManageSid = new Button();
            tabPagePictures.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PicturesGrid).BeginInit();
            tabPageChannels.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ChannelsGrid).BeginInit();
            tabPageConveyors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ConveyorsGrid).BeginInit();
            tablesTab.SuspendLayout();
            SuspendLayout();
            // 
            // NameTextBox
            // 
            NameTextBox.Location = new Point(12, 27);
            NameTextBox.Name = "NameTextBox";
            NameTextBox.Size = new Size(259, 23);
            NameTextBox.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(62, 15);
            label1.TabIndex = 1;
            label1.Text = "Название:";
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(465, 472);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(75, 23);
            SaveButton.TabIndex = 2;
            SaveButton.Text = "Сохранить";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // CancelButton
            // 
            CancelButton.Location = new Point(12, 472);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new Size(75, 23);
            CancelButton.TabIndex = 3;
            CancelButton.Text = "Отмена";
            CancelButton.UseVisualStyleBackColor = true;
            CancelButton.Click += CancelButton_Click;
            // 
            // MacTextBox
            // 
            MacTextBox.Location = new Point(297, 27);
            MacTextBox.Name = "MacTextBox";
            MacTextBox.Size = new Size(236, 23);
            MacTextBox.TabIndex = 4;
            MacTextBox.Validating += MacTextBox_Validating;
            // 
            // IpTextBox
            // 
            IpTextBox.Location = new Point(297, 71);
            IpTextBox.Name = "IpTextBox";
            IpTextBox.Size = new Size(236, 23);
            IpTextBox.TabIndex = 5;
            IpTextBox.Validating += IpTextBox_Validating;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(297, 9);
            label2.Name = "label2";
            label2.Size = new Size(33, 15);
            label2.TabIndex = 6;
            label2.Text = "Mac:";
            // 
            // MaskTextBox
            // 
            MaskTextBox.Location = new Point(297, 115);
            MaskTextBox.Name = "MaskTextBox";
            MaskTextBox.Size = new Size(236, 23);
            MaskTextBox.TabIndex = 7;
            MaskTextBox.Validating += MaskTextBox_Validating;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(297, 53);
            label3.Name = "label3";
            label3.Size = new Size(20, 15);
            label3.TabIndex = 8;
            label3.Text = "Ip:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(297, 97);
            label4.Name = "label4";
            label4.Size = new Size(38, 15);
            label4.TabIndex = 9;
            label4.Text = "Mask:";
            // 
            // tabPagePictures
            // 
            tabPagePictures.Controls.Add(PicturesGrid);
            tabPagePictures.Location = new Point(4, 24);
            tabPagePictures.Name = "tabPagePictures";
            tabPagePictures.Padding = new Padding(3);
            tabPagePictures.Size = new Size(520, 250);
            tabPagePictures.TabIndex = 2;
            tabPagePictures.Text = "Картинки";
            tabPagePictures.UseVisualStyleBackColor = true;
            // 
            // PicturesGrid
            // 
            PicturesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            PicturesGrid.Dock = DockStyle.Fill;
            PicturesGrid.Location = new Point(3, 3);
            PicturesGrid.Name = "PicturesGrid";
            PicturesGrid.Size = new Size(514, 244);
            PicturesGrid.TabIndex = 0;
            // 
            // tabPageChannels
            // 
            tabPageChannels.Controls.Add(ChannelsGrid);
            tabPageChannels.Location = new Point(4, 24);
            tabPageChannels.Name = "tabPageChannels";
            tabPageChannels.Padding = new Padding(3);
            tabPageChannels.Size = new Size(520, 250);
            tabPageChannels.TabIndex = 1;
            tabPageChannels.Text = "Каналы";
            tabPageChannels.UseVisualStyleBackColor = true;
            // 
            // ChannelsGrid
            // 
            ChannelsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ChannelsGrid.Dock = DockStyle.Fill;
            ChannelsGrid.Location = new Point(3, 3);
            ChannelsGrid.Name = "ChannelsGrid";
            ChannelsGrid.Size = new Size(514, 244);
            ChannelsGrid.TabIndex = 0;
            // 
            // tabPageConveyors
            // 
            tabPageConveyors.Controls.Add(ConveyorsGrid);
            tabPageConveyors.Controls.Add(btnConveyorProps);
            tabPageConveyors.Controls.Add(_cbConveyorCount);
            tabPageConveyors.Controls.Add(lblConveyorCount);
            tabPageConveyors.Location = new Point(4, 24);
            tabPageConveyors.Name = "tabPageConveyors";
            tabPageConveyors.Padding = new Padding(3);
            tabPageConveyors.Size = new Size(520, 250);
            tabPageConveyors.TabIndex = 0;
            tabPageConveyors.Text = "Конвейеры";
            tabPageConveyors.UseVisualStyleBackColor = true;
            // 
            // ConveyorsGrid
            // 
            ConveyorsGrid.AllowUserToAddRows = false;
            ConveyorsGrid.AllowUserToDeleteRows = false;
            ConveyorsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ConveyorsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            ConveyorsGrid.Location = new Point(3, 40);
            ConveyorsGrid.MultiSelect = false;
            ConveyorsGrid.Name = "ConveyorsGrid";
            ConveyorsGrid.ReadOnly = true;
            ConveyorsGrid.RowHeadersVisible = false;
            ConveyorsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ConveyorsGrid.Size = new Size(514, 207);
            ConveyorsGrid.TabIndex = 0;
            ConveyorsGrid.CellDoubleClick += ConveyorsGrid_CellDoubleClick;
            // 
            // btnConveyorProps
            // 
            btnConveyorProps.Location = new Point(180, 6);
            btnConveyorProps.Name = "btnConveyorProps";
            btnConveyorProps.Size = new Size(160, 27);
            btnConveyorProps.TabIndex = 1;
            btnConveyorProps.Text = "Свойства конвейера…";
            btnConveyorProps.UseVisualStyleBackColor = true;
            btnConveyorProps.Click += BtnConveyorProps_Click;
            // 
            // _cbConveyorCount
            // 
            _cbConveyorCount.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbConveyorCount.Items.AddRange(new object[] { "4", "8" });
            _cbConveyorCount.Location = new Point(100, 8);
            _cbConveyorCount.Name = "_cbConveyorCount";
            _cbConveyorCount.Size = new Size(70, 23);
            _cbConveyorCount.TabIndex = 2;
            _cbConveyorCount.SelectedIndexChanged += CbConveyorCount_SelectedIndexChanged;
            // 
            // lblConveyorCount
            // 
            lblConveyorCount.AutoSize = true;
            lblConveyorCount.Location = new Point(10, 12);
            lblConveyorCount.Name = "lblConveyorCount";
            lblConveyorCount.Size = new Size(76, 15);
            lblConveyorCount.TabIndex = 3;
            lblConveyorCount.Text = "Конвейеров:";
            // 
            // tablesTab
            // 
            tablesTab.Controls.Add(tabPageConveyors);
            tablesTab.Controls.Add(tabPageChannels);
            tablesTab.Controls.Add(tabPagePictures);
            tablesTab.Location = new Point(12, 188);
            tablesTab.Name = "tablesTab";
            tablesTab.SelectedIndex = 0;
            tablesTab.Size = new Size(528, 278);
            tablesTab.TabIndex = 10;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 53);
            label5.Name = "label5";
            label5.Size = new Size(27, 15);
            label5.TabIndex = 12;
            label5.Text = "SID:";
            // 
            // _cbSid
            // 
            _cbSid.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbSid.FormattingEnabled = true;
            _cbSid.Location = new Point(12, 71);
            _cbSid.Name = "_cbSid";
            _cbSid.Size = new Size(152, 23);
            _cbSid.TabIndex = 13;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(297, 141);
            label6.Name = "label6";
            label6.Size = new Size(43, 15);
            label6.TabIndex = 15;
            label6.Text = "Шлюз:";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(297, 159);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(236, 23);
            textBox1.TabIndex = 14;
            // 
            // _btnManageSid
            // 
            _btnManageSid.Location = new Point(170, 70);
            _btnManageSid.Name = "_btnManageSid";
            _btnManageSid.Size = new Size(101, 23);
            _btnManageSid.TabIndex = 16;
            _btnManageSid.Text = "Управление SID";
            _btnManageSid.UseVisualStyleBackColor = true;
            _btnManageSid.Click += _btnManageSid_Click;
            // 
            // EditNetGateParametersForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(552, 507);
            Controls.Add(_btnManageSid);
            Controls.Add(label6);
            Controls.Add(textBox1);
            Controls.Add(_cbSid);
            Controls.Add(label5);
            Controls.Add(tablesTab);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(MaskTextBox);
            Controls.Add(label2);
            Controls.Add(IpTextBox);
            Controls.Add(MacTextBox);
            Controls.Add(CancelButton);
            Controls.Add(SaveButton);
            Controls.Add(label1);
            Controls.Add(NameTextBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "EditNetGateParametersForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Параметры устройства NetGate";
            tabPagePictures.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PicturesGrid).EndInit();
            tabPageChannels.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ChannelsGrid).EndInit();
            tabPageConveyors.ResumeLayout(false);
            tabPageConveyors.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)ConveyorsGrid).EndInit();
            tablesTab.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private TextBox NameTextBox;
        private Label label1;
        private Button SaveButton;
        private Button CancelButton;
        private TextBox MacTextBox;
        private TextBox IpTextBox;
        private Label label2;
        private TextBox MaskTextBox;
        private Label label3;
        private Label label4;
        private TabPage tabPagePictures;
        private DataGridView PicturesGrid;
        private TabPage tabPageChannels;
        private DataGridView ChannelsGrid;
        private TabPage tabPageConveyors;
        private DataGridView ConveyorsGrid;
        private TabControl tablesTab;
        private Label label5;
        private ComboBox _cbSid;
        private Label lblConveyorCount;
        private ComboBox _cbConveyorCount;
        private Button btnConveyorProps;
        private Label label6;
        private TextBox textBox1;
        private Button _btnManageSid;
    }
}