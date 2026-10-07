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
            CreateButton = new Button();
            DeleteButton = new Button();
            DevicesComboBox = new ComboBox();
            label3 = new Label();
            DeleteDeviceButton = new Button();
            CreateDeviceButton = new Button();
            LoadDeviceButton = new Button();
            EditButton = new Button();
            RefreshButton = new Button();
            LoadGroupButton = new Button();
            SaveGroupButton = new Button();
            EditGroupButton = new Button();
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
            DeleteButton.Location = new Point(289, 58);
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
            DevicesComboBox.Location = new Point(12, 101);
            DevicesComboBox.Name = "DevicesComboBox";
            DevicesComboBox.Size = new Size(676, 23);
            DevicesComboBox.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 83);
            label3.Name = "label3";
            label3.Size = new Size(72, 15);
            label3.TabIndex = 7;
            label3.Text = "Устройства:";
            // 
            // DeleteDeviceButton
            // 
            DeleteDeviceButton.Location = new Point(370, 130);
            DeleteDeviceButton.Name = "DeleteDeviceButton";
            DeleteDeviceButton.Size = new Size(75, 23);
            DeleteDeviceButton.TabIndex = 10;
            DeleteDeviceButton.Text = "Удалить";
            DeleteDeviceButton.UseVisualStyleBackColor = true;
            DeleteDeviceButton.Click += DeleteDeviceButton_Click;
            // 
            // CreateDeviceButton
            // 
            CreateDeviceButton.Location = new Point(613, 130);
            CreateDeviceButton.Name = "CreateDeviceButton";
            CreateDeviceButton.Size = new Size(75, 23);
            CreateDeviceButton.TabIndex = 9;
            CreateDeviceButton.Text = "Создать";
            CreateDeviceButton.UseVisualStyleBackColor = true;
            CreateDeviceButton.Click += CreateDeviceButton_Click;
            // 
            // LoadDeviceButton
            // 
            LoadDeviceButton.Location = new Point(451, 130);
            LoadDeviceButton.Name = "LoadDeviceButton";
            LoadDeviceButton.Size = new Size(75, 23);
            LoadDeviceButton.TabIndex = 11;
            LoadDeviceButton.Text = "Загрузить";
            LoadDeviceButton.UseVisualStyleBackColor = true;
            LoadDeviceButton.Click += LoadButton_Click;
            // 
            // EditButton
            // 
            EditButton.Location = new Point(532, 130);
            EditButton.Name = "EditButton";
            EditButton.Size = new Size(75, 23);
            EditButton.TabIndex = 12;
            EditButton.Text = "Изменить";
            EditButton.UseVisualStyleBackColor = true;
            EditButton.Click += EditButton_Click;
            // 
            // RefreshButton
            // 
            RefreshButton.Location = new Point(12, 130);
            RefreshButton.Name = "RefreshButton";
            RefreshButton.Size = new Size(75, 23);
            RefreshButton.TabIndex = 13;
            RefreshButton.Text = "Обновить";
            RefreshButton.UseVisualStyleBackColor = true;
            RefreshButton.Click += RefreshButton_Click;
            // 
            // LoadGroupButton
            // 
            LoadGroupButton.Location = new Point(451, 58);
            LoadGroupButton.Name = "LoadGroupButton";
            LoadGroupButton.Size = new Size(75, 23);
            LoadGroupButton.TabIndex = 14;
            LoadGroupButton.Text = "Загрузить";
            LoadGroupButton.UseVisualStyleBackColor = true;
            LoadGroupButton.Click += LoadGroupButton_Click;
            // 
            // SaveGroupButton
            // 
            SaveGroupButton.Location = new Point(532, 58);
            SaveGroupButton.Name = "SaveGroupButton";
            SaveGroupButton.Size = new Size(75, 23);
            SaveGroupButton.TabIndex = 15;
            SaveGroupButton.Text = "Сохранить";
            SaveGroupButton.UseVisualStyleBackColor = true;
            SaveGroupButton.Click += SaveGroupButton_Click;
            // 
            // EditGroupButton
            // 
            EditGroupButton.Location = new Point(370, 58);
            EditGroupButton.Name = "EditGroupButton";
            EditGroupButton.Size = new Size(75, 23);
            EditGroupButton.TabIndex = 16;
            EditGroupButton.Text = "Изменить";
            EditGroupButton.UseVisualStyleBackColor = true;
            EditGroupButton.Click += EditGroupButton_Click;
            // 
            // miDevicesGroup
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 161);
            Controls.Add(EditGroupButton);
            Controls.Add(SaveGroupButton);
            Controls.Add(LoadGroupButton);
            Controls.Add(RefreshButton);
            Controls.Add(EditButton);
            Controls.Add(LoadDeviceButton);
            Controls.Add(DeleteDeviceButton);
            Controls.Add(CreateDeviceButton);
            Controls.Add(label3);
            Controls.Add(DevicesComboBox);
            Controls.Add(DeleteButton);
            Controls.Add(CreateButton);
            Controls.Add(label1);
            Controls.Add(GroupsComboBox);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "miDevicesGroup";
            Text = "Группы устройств";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox GroupsComboBox;
        private Label label1;
        private Button CreateButton;
        private Button DeleteButton;
        private ComboBox DevicesComboBox;
        private Label label3;
        private Button DeleteDeviceButton;
        private Button CreateDeviceButton;
        private Button LoadDeviceButton;
        private Button EditButton;
        private Button RefreshButton;
        private Button LoadGroupButton;
        private Button SaveGroupButton;
        private Button EditGroupButton;
    }
}