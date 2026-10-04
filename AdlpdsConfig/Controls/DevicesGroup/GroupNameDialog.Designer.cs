namespace AdlpdsConfig.Controls.DevicesGroup
{
    partial class GroupNameDialog
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
            _groupName = new TextBox();
            binCancel = new Button();
            btnOk = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // _groupName
            // 
            _groupName.Location = new Point(12, 28);
            _groupName.Name = "_groupName";
            _groupName.Size = new Size(243, 23);
            _groupName.TabIndex = 0;
            // 
            // binCancel
            // 
            binCancel.Location = new Point(12, 57);
            binCancel.Name = "binCancel";
            binCancel.Size = new Size(75, 23);
            binCancel.TabIndex = 1;
            binCancel.Text = "Отмена";
            binCancel.UseVisualStyleBackColor = true;
            binCancel.Click += binCancel_Click;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(180, 57);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 2;
            btnOk.Text = "Ок";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(62, 15);
            label1.TabIndex = 3;
            label1.Text = "Название:";
            // 
            // GroupNameDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(267, 89);
            ControlBox = false;
            Controls.Add(label1);
            Controls.Add(btnOk);
            Controls.Add(binCancel);
            Controls.Add(_groupName);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "GroupNameDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Название группы";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox _groupName;
        private Button binCancel;
        private Button btnOk;
        private Label label1;
    }
}