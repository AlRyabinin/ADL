namespace AdlpdsConfig.Controls.DevicesGroup
{
    partial class miDevicesGroup
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
            GroupsComboBox = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            groupNameTextBox = new TextBox();
            CreateButton = new Button();
            DeleteButton = new Button();
            DevicesComboBox = new ComboBox();
            label3 = new Label();
            DeleteDeviceButton = new Button();
            CreateDeviceButton = new Button();
            SuspendLayout();
            // 
            // GroupsComboBox
            // 
            GroupsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            GroupsComboBox.FormattingEnabled = true;
            GroupsComboBox.Location = new Point(12, 29);
            GroupsComboBox.Name = "GroupsComboBox";
            GroupsComboBox.Size = new Size(676, 23);
            GroupsComboBox.TabIndex = 0;
            GroupsComboBox.SelectedIndexChanged += GroupsComboBox_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(52, 15);
            label1.TabIndex = 1;
            label1.Text = "Группы:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 85);
            label2.Name = "label2";
            label2.Size = new Size(62, 15);
            label2.TabIndex = 2;
            label2.Text = "Название:";
            // 
            // groupNameTextBox
            // 
            groupNameTextBox.Location = new Point(12, 103);
            groupNameTextBox.Name = "groupNameTextBox";
            groupNameTextBox.Size = new Size(676, 23);
            groupNameTextBox.TabIndex = 3;
            // 
            // CreateButton
            // 
            CreateButton.Location = new Point(613, 58);
            CreateButton.Name = "CreateButton";
            CreateButton.Size = new Size(75, 23);
            CreateButton.TabIndex = 4;
            CreateButton.Text = "Создать";
            CreateButton.UseVisualStyleBackColor = true;
            CreateButton.Click += CreateButton_Click;
            // 
            // DeleteButton
            // 
            DeleteButton.Location = new Point(532, 58);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(75, 23);
            DeleteButton.TabIndex = 5;
            DeleteButton.Text = "Удалить";
            DeleteButton.UseVisualStyleBackColor = true;
            DeleteButton.Click += DeleteButton_Click;
            // 
            // DevicesComboBox
            // 
            DevicesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            DevicesComboBox.FormattingEnabled = true;
            DevicesComboBox.Location = new Point(12, 153);
            DevicesComboBox.Name = "DevicesComboBox";
            DevicesComboBox.Size = new Size(676, 23);
            DevicesComboBox.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 135);
            label3.Name = "label3";
            label3.Size = new Size(72, 15);
            label3.TabIndex = 7;
            label3.Text = "Устройства:";
            // 
            // DeleteDeviceButton
            // 
            DeleteDeviceButton.Location = new Point(532, 182);
            DeleteDeviceButton.Name = "DeleteDeviceButton";
            DeleteDeviceButton.Size = new Size(75, 23);
            DeleteDeviceButton.TabIndex = 10;
            DeleteDeviceButton.Text = "Удалить";
            DeleteDeviceButton.UseVisualStyleBackColor = true;
            DeleteDeviceButton.Click += DeleteDeviceButton_Click;
            // 
            // CreateDeviceButton
            // 
            CreateDeviceButton.Location = new Point(613, 182);
            CreateDeviceButton.Name = "CreateDeviceButton";
            CreateDeviceButton.Size = new Size(75, 23);
            CreateDeviceButton.TabIndex = 9;
            CreateDeviceButton.Text = "Создать";
            CreateDeviceButton.UseVisualStyleBackColor = true;
            CreateDeviceButton.Click += CreateDeviceButton_Click;
            // 
            // miDevicesGroup
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 692);
            Controls.Add(DeleteDeviceButton);
            Controls.Add(CreateDeviceButton);
            Controls.Add(label3);
            Controls.Add(DevicesComboBox);
            Controls.Add(DeleteButton);
            Controls.Add(CreateButton);
            Controls.Add(groupNameTextBox);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(GroupsComboBox);
            Name = "miDevicesGroup";
            Text = "Группы устройств";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox GroupsComboBox;
        private Label label1;
        private Label label2;
        private TextBox groupNameTextBox;
        private Button CreateButton;
        private Button DeleteButton;
        private ComboBox DevicesComboBox;
        private Label label3;
        private Button DeleteDeviceButton;
        private Button CreateDeviceButton;
    }
}