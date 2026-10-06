
namespace AdlpdsConfig.Controls.Devices
{
    partial class EditDeviceParametersForm
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
            NameTextBox = new TextBox();
            label1 = new Label();
            SaveButton = new Button();
            MacTextBox = new TextBox();
            IpTextBox = new TextBox();
            label2 = new Label();
            MaskTextBox = new TextBox();
            label3 = new Label();
            label4 = new Label();
            MainDataGridView = new DataGridView();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            ((System.ComponentModel.ISupportInitialize)MainDataGridView).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // NameTextBox
            // 
            NameTextBox.Location = new Point(12, 27);
            NameTextBox.Name = "NameTextBox";
            NameTextBox.Size = new Size(236, 23);
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
            SaveButton.Location = new Point(465, 415);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(75, 23);
            SaveButton.TabIndex = 2;
            SaveButton.Text = "Сохранить";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // MacTextBox
            // 
            MacTextBox.Location = new Point(12, 71);
            MacTextBox.Name = "MacTextBox";
            MacTextBox.Size = new Size(236, 23);
            MacTextBox.TabIndex = 3;
            MacTextBox.Validating += MacTextBox_Validating;
            // 
            // IpTextBox
            // 
            IpTextBox.Location = new Point(12, 115);
            IpTextBox.Name = "IpTextBox";
            IpTextBox.Size = new Size(236, 23);
            IpTextBox.TabIndex = 4;
            IpTextBox.Validating += IpTextBox_Validating;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 53);
            label2.Name = "label2";
            label2.Size = new Size(33, 15);
            label2.TabIndex = 5;
            label2.Text = "Mac:";
            // 
            // MaskTextBox
            // 
            MaskTextBox.Location = new Point(12, 159);
            MaskTextBox.Name = "MaskTextBox";
            MaskTextBox.Size = new Size(236, 23);
            MaskTextBox.TabIndex = 6;
            MaskTextBox.Validating += MaskTextBox_Validating;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 97);
            label3.Name = "label3";
            label3.Size = new Size(20, 15);
            label3.TabIndex = 7;
            label3.Text = "Ip:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 141);
            label4.Name = "label4";
            label4.Size = new Size(38, 15);
            label4.TabIndex = 8;
            label4.Text = "Mask:";
            // 
            // MainDataGridView
            // 
            MainDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            MainDataGridView.Location = new Point(0, 0);
            MainDataGridView.Name = "MainDataGridView";
            MainDataGridView.Size = new Size(520, 193);
            MainDataGridView.TabIndex = 9;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Location = new Point(12, 188);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(528, 221);
            tabControl1.TabIndex = 10;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(MainDataGridView);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(520, 193);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Каналы";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // EditDeviceParametersForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(552, 450);
            Controls.Add(tabControl1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(MaskTextBox);
            Controls.Add(label2);
            Controls.Add(IpTextBox);
            Controls.Add(MacTextBox);
            Controls.Add(SaveButton);
            Controls.Add(label1);
            Controls.Add(NameTextBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "EditDeviceParametersForm";
            Text = "Параметры устройства";
            ((System.ComponentModel.ISupportInitialize)MainDataGridView).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox NameTextBox;
        private Label label1;
        private Button SaveButton;
        private TextBox MacTextBox;
        private TextBox IpTextBox;
        private Label label2;
        private TextBox MaskTextBox;
        private Label label3;
        private Label label4;
        private DataGridView MainDataGridView;
        private TabControl tabControl1;
        private TabPage tabPage1;
    }
}