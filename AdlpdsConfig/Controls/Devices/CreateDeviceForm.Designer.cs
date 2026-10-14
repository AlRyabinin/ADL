namespace AdlpdsConfig.Controls.Devices
{
    partial class CreateDeviceForm
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
            TypeComboBox = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            ChannelCountComboBox = new ComboBox();
            label3 = new Label();
            ConveyorCountComboBox = new ComboBox();
            CancelButton = new Button();
            SaveButton = new Button();
            SuspendLayout();
            // 
            // TypeComboBox
            // 
            TypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TypeComboBox.FormattingEnabled = true;
            TypeComboBox.Items.AddRange(new object[] { "IKB", "DIM220", "Dimmer cabinet", "NetGate" });
            TypeComboBox.Location = new Point(138, 6);
            TypeComboBox.Name = "TypeComboBox";
            TypeComboBox.Size = new Size(112, 23);
            TypeComboBox.TabIndex = 0;
            TypeComboBox.SelectedIndexChanged += TypeComboBox_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 1;
            label1.Text = "Тип:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 37);
            label2.Name = "label2";
            label2.Size = new Size(123, 15);
            label2.TabIndex = 2;
            label2.Text = "Количество каналов:";
            // 
            // ChannelCountComboBox
            // 
            ChannelCountComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            ChannelCountComboBox.FormattingEnabled = true;
            ChannelCountComboBox.Location = new Point(138, 34);
            ChannelCountComboBox.Name = "ChannelCountComboBox";
            ChannelCountComboBox.Size = new Size(112, 23);
            ChannelCountComboBox.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 66);
            label3.Name = "label3";
            label3.Size = new Size(143, 15);
            label3.TabIndex = 4;
            label3.Text = "Количество конвейеров:";
            label3.Visible = false;
            // 
            // ConveyorCountComboBox
            // 
            ConveyorCountComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            ConveyorCountComboBox.FormattingEnabled = true;
            ConveyorCountComboBox.Location = new Point(160, 63);
            ConveyorCountComboBox.Name = "ConveyorCountComboBox";
            ConveyorCountComboBox.Size = new Size(90, 23);
            ConveyorCountComboBox.TabIndex = 5;
            ConveyorCountComboBox.Visible = false;
            // 
            // CancelButton
            // 
            CancelButton.Location = new Point(12, 100);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new Size(75, 23);
            CancelButton.TabIndex = 6;
            CancelButton.Text = "Отмена";
            CancelButton.UseVisualStyleBackColor = true;
            CancelButton.Click += CancelButton_Click;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(175, 100);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(75, 23);
            SaveButton.TabIndex = 7;
            SaveButton.Text = "Сохранить";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // CreateDeviceForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(261, 135);
            ControlBox = false;
            Controls.Add(SaveButton);
            Controls.Add(CancelButton);
            Controls.Add(ConveyorCountComboBox);
            Controls.Add(label3);
            Controls.Add(ChannelCountComboBox);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(TypeComboBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "CreateDeviceForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Новое устройство";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox TypeComboBox;
        private Label label1;
        private Label label2;
        private ComboBox ChannelCountComboBox;
        private Label label3;
        private ComboBox ConveyorCountComboBox;
        private Button CancelButton;
        private Button SaveButton;
    }
}