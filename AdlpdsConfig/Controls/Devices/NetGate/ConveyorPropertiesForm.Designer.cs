namespace AdlpdsConfig.Controls.Devices.NetGate
{
    partial class ConveyorPropertiesForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gbStreams = new GroupBox();
            this.gbProcessing = new GroupBox();
            this.okButton = new Button();
            this.cancelButton = new Button();

            // --- Потоки ---
            this.lblSourceA = new Label();
            this.cbSourceAProtocol = new ComboBox();
            this.udSourceAUniverse = new NumericUpDown();
            this.lblSourceAUni = new Label();

            this.lblSourceB = new Label();
            this.cbSourceBProtocol = new ComboBox();
            this.udSourceBUniverse = new NumericUpDown();
            this.lblSourceBUni = new Label();

            this.lblSourceC = new Label();
            this.cbSourceCProtocol = new ComboBox();
            this.udSourceCUniverse = new NumericUpDown();
            this.lblSourceCUni = new Label();

            this.lblDest = new Label();
            this.cbDestProtocol = new ComboBox();
            this.udDestUniverse = new NumericUpDown();
            this.lblDestUni = new Label();

            // --- Обработка и замена ---
            this.lblSourceSelect = new Label();
            this.cbSourceSelect = new ComboBox();

            this.lblSumType = new Label();
            this.cbSumType = new ComboBox();

            this.lblFrequency = new Label();
            this.udFrequency = new NumericUpDown();

            this.chkRecordable = new CheckBox();
            this.chkTimeouted = new CheckBox();
            this.chkRdm = new CheckBox();

            this.lblSumBFrom = new Label();
            this.udSumBFrom = new NumericUpDown();
            this.lblSumBTo = new Label();
            this.udSumBTo = new NumericUpDown();

            this.lblSumCFrom = new Label();
            this.udSumCFrom = new NumericUpDown();
            this.lblSumCTo = new Label();
            this.udSumCTo = new NumericUpDown();

            this.gbStreams.SuspendLayout();
            this.gbProcessing.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.udSourceAUniverse).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSourceBUniverse).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSourceCUniverse).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udDestUniverse).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udFrequency).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumBFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumBTo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumCFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumCTo).BeginInit();
            this.SuspendLayout();

            // =========================================================
            // gbStreams
            // =========================================================
            this.gbStreams.Text = "Потоки";
            this.gbStreams.Location = new System.Drawing.Point(8, 8);
            this.gbStreams.Size = new System.Drawing.Size(484, 140);
            this.gbStreams.Controls.Add(this.lblSourceA);
            this.gbStreams.Controls.Add(this.cbSourceAProtocol);
            this.gbStreams.Controls.Add(this.udSourceAUniverse);
            this.gbStreams.Controls.Add(this.lblSourceAUni);
            this.gbStreams.Controls.Add(this.lblSourceB);
            this.gbStreams.Controls.Add(this.cbSourceBProtocol);
            this.gbStreams.Controls.Add(this.udSourceBUniverse);
            this.gbStreams.Controls.Add(this.lblSourceBUni);
            this.gbStreams.Controls.Add(this.lblSourceC);
            this.gbStreams.Controls.Add(this.cbSourceCProtocol);
            this.gbStreams.Controls.Add(this.udSourceCUniverse);
            this.gbStreams.Controls.Add(this.lblSourceCUni);
            this.gbStreams.Controls.Add(this.lblDest);
            this.gbStreams.Controls.Add(this.cbDestProtocol);
            this.gbStreams.Controls.Add(this.udDestUniverse);
            this.gbStreams.Controls.Add(this.lblDestUni);

            // Источник A
            this.lblSourceA.Text = "Источник A";
            this.lblSourceA.Location = new System.Drawing.Point(10, 25);
            this.lblSourceA.AutoSize = true;

            this.cbSourceAProtocol.Location = new System.Drawing.Point(110, 22);
            this.cbSourceAProtocol.Width = 110;
            this.cbSourceAProtocol.DropDownStyle = ComboBoxStyle.DropDownList;

            this.udSourceAUniverse.Location = new System.Drawing.Point(230, 22);
            this.udSourceAUniverse.Width = 90;
            this.udSourceAUniverse.Maximum = 65535;

            this.lblSourceAUni.Text = "universe";
            this.lblSourceAUni.Location = new System.Drawing.Point(330, 25);
            this.lblSourceAUni.AutoSize = true;

            // Источник B
            this.lblSourceB.Text = "Источник B";
            this.lblSourceB.Location = new System.Drawing.Point(10, 55);
            this.lblSourceB.AutoSize = true;

            this.cbSourceBProtocol.Location = new System.Drawing.Point(110, 52);
            this.cbSourceBProtocol.Width = 110;
            this.cbSourceBProtocol.DropDownStyle = ComboBoxStyle.DropDownList;

            this.udSourceBUniverse.Location = new System.Drawing.Point(230, 52);
            this.udSourceBUniverse.Width = 90;
            this.udSourceBUniverse.Maximum = 65535;

            this.lblSourceBUni.Text = "universe";
            this.lblSourceBUni.Location = new System.Drawing.Point(330, 55);
            this.lblSourceBUni.AutoSize = true;

            // Источник C
            this.lblSourceC.Text = "Источник C";
            this.lblSourceC.Location = new System.Drawing.Point(10, 85);
            this.lblSourceC.AutoSize = true;

            this.cbSourceCProtocol.Location = new System.Drawing.Point(110, 82);
            this.cbSourceCProtocol.Width = 110;
            this.cbSourceCProtocol.DropDownStyle = ComboBoxStyle.DropDownList;

            this.udSourceCUniverse.Location = new System.Drawing.Point(230, 82);
            this.udSourceCUniverse.Width = 90;
            this.udSourceCUniverse.Maximum = 65535;

            this.lblSourceCUni.Text = "universe";
            this.lblSourceCUni.Location = new System.Drawing.Point(330, 85);
            this.lblSourceCUni.AutoSize = true;

            // Приёмник
            this.lblDest.Text = "Приёмник";
            this.lblDest.Location = new System.Drawing.Point(10, 115);
            this.lblDest.AutoSize = true;

            this.cbDestProtocol.Location = new System.Drawing.Point(110, 112);
            this.cbDestProtocol.Width = 110;
            this.cbDestProtocol.DropDownStyle = ComboBoxStyle.DropDownList;

            this.udDestUniverse.Location = new System.Drawing.Point(230, 112);
            this.udDestUniverse.Width = 90;
            this.udDestUniverse.Maximum = 65535;

            this.lblDestUni.Text = "universe";
            this.lblDestUni.Location = new System.Drawing.Point(330, 115);
            this.lblDestUni.AutoSize = true;

            // =========================================================
            // gbProcessing
            // =========================================================
            this.gbProcessing.Text = "Обработка и замена";
            this.gbProcessing.Location = new System.Drawing.Point(8, 154);
            this.gbProcessing.Size = new System.Drawing.Size(484, 176);
            this.gbProcessing.Controls.Add(this.lblSourceSelect);
            this.gbProcessing.Controls.Add(this.cbSourceSelect);
            this.gbProcessing.Controls.Add(this.lblSumType);
            this.gbProcessing.Controls.Add(this.cbSumType);
            this.gbProcessing.Controls.Add(this.lblFrequency);
            this.gbProcessing.Controls.Add(this.udFrequency);
            this.gbProcessing.Controls.Add(this.chkRecordable);
            this.gbProcessing.Controls.Add(this.chkTimeouted);
            this.gbProcessing.Controls.Add(this.chkRdm);
            this.gbProcessing.Controls.Add(this.lblSumBFrom);
            this.gbProcessing.Controls.Add(this.udSumBFrom);
            this.gbProcessing.Controls.Add(this.lblSumBTo);
            this.gbProcessing.Controls.Add(this.udSumBTo);
            this.gbProcessing.Controls.Add(this.lblSumCFrom);
            this.gbProcessing.Controls.Add(this.udSumCFrom);
            this.gbProcessing.Controls.Add(this.lblSumCTo);
            this.gbProcessing.Controls.Add(this.udSumCTo);

            // --- Строка 1: Выбор источника | Тип суммы ---

            // «Выбор источника» — подпись сверху
            this.lblSourceSelect.Text = "Выбор источника";
            this.lblSourceSelect.Location = new System.Drawing.Point(10, 22);
            this.lblSourceSelect.AutoSize = true;

            this.cbSourceSelect.Location = new System.Drawing.Point(10, 42);
            this.cbSourceSelect.Width = 190;
            this.cbSourceSelect.DropDownWidth = 210;
            this.cbSourceSelect.DropDownStyle = ComboBoxStyle.DropDownList;

            // «Тип суммы» — правая колонка
            this.lblSumType.Text = "Тип суммы";
            this.lblSumType.Location = new System.Drawing.Point(230, 22);
            this.lblSumType.AutoSize = true;

            this.cbSumType.Location = new System.Drawing.Point(230, 42);
            this.cbSumType.Width = 190;
            this.cbSumType.DropDownWidth = 210;
            this.cbSumType.DropDownStyle = ComboBoxStyle.DropDownList;

            // --- Строка 2: Частота + чекбоксы ---

            this.lblFrequency.Text = "Частота 0–255";
            this.lblFrequency.Location = new System.Drawing.Point(10, 84);
            this.lblFrequency.AutoSize = true;

            this.udFrequency.Location = new System.Drawing.Point(10, 104);
            this.udFrequency.Width = 70;
            this.udFrequency.Maximum = 255;

            this.chkRecordable.Text = "Запись";
            this.chkRecordable.Location = new System.Drawing.Point(110, 106);
            this.chkRecordable.AutoSize = true;

            this.chkTimeouted.Text = "Таймаут";
            this.chkTimeouted.Location = new System.Drawing.Point(210, 106);
            this.chkTimeouted.AutoSize = true;

            this.chkRdm.Text = "RDM";
            this.chkRdm.Location = new System.Drawing.Point(320, 106);
            this.chkRdm.AutoSize = true;

            // --- Строка 3: Замена B ---
            this.lblSumBFrom.Text = "Замена B: от/до";
            this.lblSumBFrom.Location = new System.Drawing.Point(10, 140);
            this.lblSumBFrom.AutoSize = true;

            this.udSumBFrom.Location = new System.Drawing.Point(140, 138);
            this.udSumBFrom.Width = 70;
            this.udSumBFrom.Maximum = 65535;

            this.lblSumBTo.Text = "/";
            this.lblSumBTo.Location = new System.Drawing.Point(218, 140);
            this.lblSumBTo.AutoSize = true;

            this.udSumBTo.Location = new System.Drawing.Point(232, 138);
            this.udSumBTo.Width = 70;
            this.udSumBTo.Maximum = 65535;

            // --- Строка 4: Замена C ---
            this.lblSumCFrom.Text = "Замена C: от/до";
            this.lblSumCFrom.Location = new System.Drawing.Point(10, 176);
            this.lblSumCFrom.AutoSize = true;

            this.udSumCFrom.Location = new System.Drawing.Point(140, 174);
            this.udSumCFrom.Width = 70;
            this.udSumCFrom.Maximum = 65535;

            this.lblSumCTo.Text = "/";
            this.lblSumCTo.Location = new System.Drawing.Point(218, 176);
            this.lblSumCTo.AutoSize = true;

            this.udSumCTo.Location = new System.Drawing.Point(232, 174);
            this.udSumCTo.Width = 70;
            this.udSumCTo.Maximum = 65535;

            // =========================================================
            // Кнопки
            // =========================================================
            this.okButton.Text = "OK";
            this.okButton.Location = new System.Drawing.Point(330, 344);
            this.okButton.Size = new System.Drawing.Size(75, 27);
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += OkButton_Click;

            this.cancelButton.Text = "Отмена";
            this.cancelButton.Location = new System.Drawing.Point(415, 344);
            this.cancelButton.Size = new System.Drawing.Size(75, 27);
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += CancelButton_Click;

            // =========================================================
            // ConveyorPropertiesForm
            // =========================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 384);
            this.Controls.Add(this.gbStreams);
            this.Controls.Add(this.gbProcessing);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Конвейер";

            this.gbStreams.ResumeLayout(false);
            this.gbStreams.PerformLayout();
            this.gbProcessing.ResumeLayout(false);
            this.gbProcessing.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.udSourceAUniverse).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSourceBUniverse).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSourceCUniverse).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udDestUniverse).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udFrequency).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumBFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumBTo).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumCFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.udSumCTo).EndInit();
            this.ResumeLayout(false);
        }

        private GroupBox gbStreams;
        private GroupBox gbProcessing;
        private Button okButton;
        private Button cancelButton;

        private Label lblSourceA; private ComboBox cbSourceAProtocol; private NumericUpDown udSourceAUniverse; private Label lblSourceAUni;
        private Label lblSourceB; private ComboBox cbSourceBProtocol; private NumericUpDown udSourceBUniverse; private Label lblSourceBUni;
        private Label lblSourceC; private ComboBox cbSourceCProtocol; private NumericUpDown udSourceCUniverse; private Label lblSourceCUni;
        private Label lblDest; private ComboBox cbDestProtocol; private NumericUpDown udDestUniverse; private Label lblDestUni;

        private Label lblSourceSelect; private ComboBox cbSourceSelect;
        private Label lblSumType; private ComboBox cbSumType;
        private Label lblFrequency; private NumericUpDown udFrequency;
        private CheckBox chkRecordable, chkTimeouted, chkRdm;
        private Label lblSumBFrom; private NumericUpDown udSumBFrom;
        private Label lblSumBTo; private NumericUpDown udSumBTo;
        private Label lblSumCFrom; private NumericUpDown udSumCFrom;
        private Label lblSumCTo; private NumericUpDown udSumCTo;
    }
}