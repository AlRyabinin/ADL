using System.Drawing;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class DeviceEditorForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel rootLayoutPanel = null!;
    private CheckBox _enabled = null!;
    private Label typeLabel = null!;
    private TextBox _type = null!;
    private Label nameLabel = null!;
    private TextBox _name = null!;
    private Label uuidLabel = null!;
    private FlowLayoutPanel uuidPanel = null!;
    private TextBox _uuid = null!;
    private Button generateUuidButton = null!;
    private Label blockLabel = null!;
    private NumericUpDown _block = null!;
    private Label portLabel = null!;
    private NumericUpDown _port = null!;
    private Label ipLabel = null!;
    private TextBox _ip = null!;
    private Label maskLabel = null!;
    private TextBox _mask = null!;
    private Label gatewayLabel = null!;
    private TextBox _gateway = null!;
    private Label macLabel = null!;
    private TextBox _mac = null!;
    private Label channelsLabel = null!;
    private TextBox _channels = null!;
    private Label transportLabel = null!;
    private ComboBox _transport = null!;
    private Label ownAddressLabel = null!;
    private TextBox _ownAddress = null!;
    private Label subscriptionsLabel = null!;
    private TextBox _subscriptions = null!;
    private Label noteLabel = null!;
    private FlowLayoutPanel buttonsPanel = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if(disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayoutPanel = new TableLayoutPanel();
        _enabled = new CheckBox();
        typeLabel = new Label();
        _type = new TextBox();
        nameLabel = new Label();
        _name = new TextBox();
        uuidLabel = new Label();
        uuidPanel = new FlowLayoutPanel();
        _uuid = new TextBox();
        generateUuidButton = new Button();
        blockLabel = new Label();
        _block = new NumericUpDown();
        portLabel = new Label();
        _port = new NumericUpDown();
        ipLabel = new Label();
        _ip = new TextBox();
        maskLabel = new Label();
        _mask = new TextBox();
        gatewayLabel = new Label();
        _gateway = new TextBox();
        macLabel = new Label();
        _mac = new TextBox();
        channelsLabel = new Label();
        _channels = new TextBox();
        transportLabel = new Label();
        _transport = new ComboBox();
        ownAddressLabel = new Label();
        _ownAddress = new TextBox();
        subscriptionsLabel = new Label();
        _subscriptions = new TextBox();
        noteLabel = new Label();
        buttonsPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        rootLayoutPanel.SuspendLayout();
        uuidPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_block).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_port).BeginInit();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayoutPanel
        // 
        rootLayoutPanel.AutoScroll = true;
        rootLayoutPanel.ColumnCount = 2;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205F));
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(_enabled, 0, 0);
        rootLayoutPanel.Controls.Add(typeLabel, 0, 1);
        rootLayoutPanel.Controls.Add(_type, 1, 1);
        rootLayoutPanel.Controls.Add(nameLabel, 0, 2);
        rootLayoutPanel.Controls.Add(_name, 1, 2);
        rootLayoutPanel.Controls.Add(uuidLabel, 0, 3);
        rootLayoutPanel.Controls.Add(uuidPanel, 1, 3);
        rootLayoutPanel.Controls.Add(blockLabel, 0, 4);
        rootLayoutPanel.Controls.Add(_block, 1, 4);
        rootLayoutPanel.Controls.Add(portLabel, 0, 5);
        rootLayoutPanel.Controls.Add(_port, 1, 5);
        rootLayoutPanel.Controls.Add(ipLabel, 0, 6);
        rootLayoutPanel.Controls.Add(_ip, 1, 6);
        rootLayoutPanel.Controls.Add(maskLabel, 0, 7);
        rootLayoutPanel.Controls.Add(_mask, 1, 7);
        rootLayoutPanel.Controls.Add(gatewayLabel, 0, 8);
        rootLayoutPanel.Controls.Add(_gateway, 1, 8);
        rootLayoutPanel.Controls.Add(macLabel, 0, 9);
        rootLayoutPanel.Controls.Add(_mac, 1, 9);
        rootLayoutPanel.Controls.Add(channelsLabel, 0, 10);
        rootLayoutPanel.Controls.Add(_channels, 1, 10);
        rootLayoutPanel.Controls.Add(transportLabel, 0, 11);
        rootLayoutPanel.Controls.Add(_transport, 1, 11);
        rootLayoutPanel.Controls.Add(ownAddressLabel, 0, 12);
        rootLayoutPanel.Controls.Add(_ownAddress, 1, 12);
        rootLayoutPanel.Controls.Add(subscriptionsLabel, 0, 13);
        rootLayoutPanel.Controls.Add(_subscriptions, 1, 13);
        rootLayoutPanel.Controls.Add(noteLabel, 0, 14);
        rootLayoutPanel.Controls.Add(buttonsPanel, 0, 15);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Location = new Point(0, 0);
        rootLayoutPanel.Name = "rootLayoutPanel";
        rootLayoutPanel.Padding = new Padding(12);
        rootLayoutPanel.RowCount = 16;
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.RowStyles.Add(new RowStyle());
        rootLayoutPanel.Size = new Size(760, 601);
        rootLayoutPanel.TabIndex = 0;
        // 
        // _enabled
        // 
        _enabled.AutoSize = true;
        _enabled.Checked = true;
        _enabled.CheckState = CheckState.Checked;
        rootLayoutPanel.SetColumnSpan(_enabled, 2);
        _enabled.Location = new Point(15, 16);
        _enabled.Margin = new Padding(3, 4, 3, 8);
        _enabled.Name = "_enabled";
        _enabled.Size = new Size(148, 19);
        _enabled.TabIndex = 0;
        _enabled.Text = "Устройство включено";
        _enabled.UseVisualStyleBackColor = true;
        // 
        // typeLabel
        // 
        typeLabel.Anchor = AnchorStyles.Left;
        typeLabel.AutoSize = true;
        typeLabel.Location = new Point(15, 53);
        typeLabel.Margin = new Padding(3, 8, 8, 3);
        typeLabel.Name = "typeLabel";
        typeLabel.Size = new Size(165, 15);
        typeLabel.TabIndex = 1;
        typeLabel.Text = "Тип ADLPDS (до 8 символов)";
        // 
        // _type
        // 
        _type.Dock = DockStyle.Fill;
        _type.Location = new Point(220, 47);
        _type.Margin = new Padding(3, 4, 3, 4);
        _type.Name = "_type";
        _type.Size = new Size(525, 23);
        _type.TabIndex = 2;
        // 
        // nameLabel
        // 
        nameLabel.Anchor = AnchorStyles.Left;
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(15, 84);
        nameLabel.Margin = new Padding(3, 8, 8, 3);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(123, 15);
        nameLabel.TabIndex = 3;
        nameLabel.Text = "Название устройства";
        // 
        // _name
        // 
        _name.Dock = DockStyle.Fill;
        _name.Location = new Point(220, 78);
        _name.Margin = new Padding(3, 4, 3, 4);
        _name.Name = "_name";
        _name.Size = new Size(525, 23);
        _name.TabIndex = 4;
        // 
        // uuidLabel
        // 
        uuidLabel.Anchor = AnchorStyles.Left;
        uuidLabel.AutoSize = true;
        uuidLabel.Location = new Point(15, 115);
        uuidLabel.Margin = new Padding(3, 8, 8, 3);
        uuidLabel.Name = "uuidLabel";
        uuidLabel.Size = new Size(34, 15);
        uuidLabel.TabIndex = 5;
        uuidLabel.Text = "UUID";
        // 
        // uuidPanel
        // 
        uuidPanel.AutoSize = true;
        uuidPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uuidPanel.Controls.Add(_uuid);
        uuidPanel.Controls.Add(generateUuidButton);
        uuidPanel.Dock = DockStyle.Left;
        uuidPanel.Location = new Point(217, 105);
        uuidPanel.Margin = new Padding(0);
        uuidPanel.Name = "uuidPanel";
        uuidPanel.Size = new Size(492, 30);
        uuidPanel.TabIndex = 6;
        uuidPanel.WrapContents = false;
        // 
        // _uuid
        // 
        _uuid.Location = new Point(3, 4);
        _uuid.Margin = new Padding(3, 4, 8, 3);
        _uuid.Name = "_uuid";
        _uuid.Size = new Size(390, 23);
        _uuid.TabIndex = 0;
        // 
        // generateUuidButton
        // 
        generateUuidButton.AutoSize = true;
        generateUuidButton.Location = new Point(404, 2);
        generateUuidButton.Margin = new Padding(3, 2, 3, 2);
        generateUuidButton.Name = "generateUuidButton";
        generateUuidButton.Size = new Size(85, 25);
        generateUuidButton.TabIndex = 1;
        generateUuidButton.Text = "Новый UUID";
        generateUuidButton.UseVisualStyleBackColor = true;
        generateUuidButton.Click += generateUuidButton_Click;
        // 
        // blockLabel
        // 
        blockLabel.Anchor = AnchorStyles.Left;
        blockLabel.AutoSize = true;
        blockLabel.Location = new Point(15, 145);
        blockLabel.Margin = new Padding(3, 8, 8, 3);
        blockLabel.Name = "blockLabel";
        blockLabel.Size = new Size(34, 15);
        blockLabel.TabIndex = 7;
        blockLabel.Text = "Блок";
        // 
        // _block
        // 
        _block.Location = new Point(220, 139);
        _block.Margin = new Padding(3, 4, 3, 4);
        _block.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _block.Name = "_block";
        _block.Size = new Size(120, 23);
        _block.TabIndex = 8;
        // 
        // portLabel
        // 
        portLabel.Anchor = AnchorStyles.Left;
        portLabel.AutoSize = true;
        portLabel.Location = new Point(15, 176);
        portLabel.Margin = new Padding(3, 8, 8, 3);
        portLabel.Name = "portLabel";
        portLabel.Size = new Size(125, 15);
        portLabel.TabIndex = 9;
        portLabel.Text = "UDP-порт устройства";
        // 
        // _port
        // 
        _port.Location = new Point(220, 170);
        _port.Margin = new Padding(3, 4, 3, 4);
        _port.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _port.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _port.Name = "_port";
        _port.Size = new Size(120, 23);
        _port.TabIndex = 10;
        _port.Value = new decimal(new int[] { 21001, 0, 0, 0 });
        // 
        // ipLabel
        // 
        ipLabel.Anchor = AnchorStyles.Left;
        ipLabel.AutoSize = true;
        ipLabel.Location = new Point(15, 207);
        ipLabel.Margin = new Padding(3, 8, 8, 3);
        ipLabel.Name = "ipLabel";
        ipLabel.Size = new Size(81, 15);
        ipLabel.TabIndex = 11;
        ipLabel.Text = "IP устройства";
        // 
        // _ip
        // 
        _ip.Dock = DockStyle.Fill;
        _ip.Location = new Point(220, 201);
        _ip.Margin = new Padding(3, 4, 3, 4);
        _ip.Name = "_ip";
        _ip.Size = new Size(525, 23);
        _ip.TabIndex = 12;
        // 
        // maskLabel
        // 
        maskLabel.Anchor = AnchorStyles.Left;
        maskLabel.AutoSize = true;
        maskLabel.Location = new Point(15, 238);
        maskLabel.Margin = new Padding(3, 8, 8, 3);
        maskLabel.Name = "maskLabel";
        maskLabel.Size = new Size(42, 15);
        maskLabel.TabIndex = 13;
        maskLabel.Text = "Маска";
        // 
        // _mask
        // 
        _mask.Dock = DockStyle.Fill;
        _mask.Location = new Point(220, 232);
        _mask.Margin = new Padding(3, 4, 3, 4);
        _mask.Name = "_mask";
        _mask.Size = new Size(525, 23);
        _mask.TabIndex = 14;
        // 
        // gatewayLabel
        // 
        gatewayLabel.Anchor = AnchorStyles.Left;
        gatewayLabel.AutoSize = true;
        gatewayLabel.Location = new Point(15, 269);
        gatewayLabel.Margin = new Padding(3, 8, 8, 3);
        gatewayLabel.Name = "gatewayLabel";
        gatewayLabel.Size = new Size(40, 15);
        gatewayLabel.TabIndex = 15;
        gatewayLabel.Text = "Шлюз";
        // 
        // _gateway
        // 
        _gateway.Dock = DockStyle.Fill;
        _gateway.Location = new Point(220, 263);
        _gateway.Margin = new Padding(3, 4, 3, 4);
        _gateway.Name = "_gateway";
        _gateway.Size = new Size(525, 23);
        _gateway.TabIndex = 16;
        // 
        // macLabel
        // 
        macLabel.Anchor = AnchorStyles.Left;
        macLabel.AutoSize = true;
        macLabel.Location = new Point(15, 300);
        macLabel.Margin = new Padding(3, 8, 8, 3);
        macLabel.Name = "macLabel";
        macLabel.Size = new Size(127, 15);
        macLabel.TabIndex = 17;
        macLabel.Text = "MAC (необязательно)";
        // 
        // _mac
        // 
        _mac.Dock = DockStyle.Fill;
        _mac.Location = new Point(220, 294);
        _mac.Margin = new Padding(3, 4, 3, 4);
        _mac.Name = "_mac";
        _mac.Size = new Size(525, 23);
        _mac.TabIndex = 18;
        // 
        // channelsLabel
        // 
        channelsLabel.Anchor = AnchorStyles.Left;
        channelsLabel.AutoSize = true;
        channelsLabel.Location = new Point(15, 331);
        channelsLabel.Margin = new Padding(3, 8, 8, 3);
        channelsLabel.Name = "channelsLabel";
        channelsLabel.Size = new Size(104, 15);
        channelsLabel.TabIndex = 19;
        channelsLabel.Text = "Индексы каналов";
        // 
        // _channels
        // 
        _channels.Dock = DockStyle.Fill;
        _channels.Location = new Point(220, 325);
        _channels.Margin = new Padding(3, 4, 3, 4);
        _channels.Name = "_channels";
        _channels.Size = new Size(525, 23);
        _channels.TabIndex = 20;
        // 
        // transportLabel
        // 
        transportLabel.Anchor = AnchorStyles.Left;
        transportLabel.AutoSize = true;
        transportLabel.Location = new Point(15, 362);
        transportLabel.Margin = new Padding(3, 8, 8, 3);
        transportLabel.Name = "transportLabel";
        transportLabel.Size = new Size(57, 15);
        transportLabel.TabIndex = 21;
        transportLabel.Text = "Transport";
        // 
        // _transport
        // 
        _transport.DropDownStyle = ComboBoxStyle.DropDownList;
        _transport.Items.AddRange(new object[] { "Broadcast (0)", "Multicast (1)" });
        _transport.Location = new Point(220, 356);
        _transport.Margin = new Padding(3, 4, 3, 4);
        _transport.Name = "_transport";
        _transport.Size = new Size(260, 23);
        _transport.TabIndex = 22;
        // 
        // ownAddressLabel
        // 
        ownAddressLabel.Anchor = AnchorStyles.Left;
        ownAddressLabel.AutoSize = true;
        ownAddressLabel.Location = new Point(15, 393);
        ownAddressLabel.Margin = new Padding(3, 8, 8, 3);
        ownAddressLabel.Name = "ownAddressLabel";
        ownAddressLabel.Size = new Size(123, 15);
        ownAddressLabel.TabIndex = 23;
        ownAddressLabel.Text = "Свой multicast-адрес";
        // 
        // _ownAddress
        // 
        _ownAddress.Dock = DockStyle.Fill;
        _ownAddress.Location = new Point(220, 387);
        _ownAddress.Margin = new Padding(3, 4, 3, 4);
        _ownAddress.Name = "_ownAddress";
        _ownAddress.Size = new Size(525, 23);
        _ownAddress.TabIndex = 24;
        // 
        // subscriptionsLabel
        // 
        subscriptionsLabel.Anchor = AnchorStyles.Left;
        subscriptionsLabel.AutoSize = true;
        subscriptionsLabel.Location = new Point(15, 465);
        subscriptionsLabel.Margin = new Padding(3, 8, 8, 3);
        subscriptionsLabel.Name = "subscriptionsLabel";
        subscriptionsLabel.Size = new Size(101, 15);
        subscriptionsLabel.TabIndex = 25;
        subscriptionsLabel.Text = "Подписки (до 32)";
        // 
        // _subscriptions
        // 
        _subscriptions.Dock = DockStyle.Fill;
        _subscriptions.Location = new Point(220, 418);
        _subscriptions.Margin = new Padding(3, 4, 3, 4);
        _subscriptions.Multiline = true;
        _subscriptions.Name = "_subscriptions";
        _subscriptions.ScrollBars = ScrollBars.Vertical;
        _subscriptions.Size = new Size(525, 104);
        _subscriptions.TabIndex = 26;
        // 
        // noteLabel
        // 
        noteLabel.AutoSize = true;
        rootLayoutPanel.SetColumnSpan(noteLabel, 2);
        noteLabel.Dock = DockStyle.Fill;
        noteLabel.ForeColor = SystemColors.GrayText;
        noteLabel.Location = new Point(15, 534);
        noteLabel.Margin = new Padding(3, 8, 3, 8);
        noteLabel.Name = "noteLabel";
        noteLabel.Size = new Size(730, 15);
        noteLabel.TabIndex = 27;
        noteLabel.Text = "Каждое запущенное устройство получает отдельный UDP-порт. UUID + Block должны быть уникальны.";
        // 
        // buttonsPanel
        // 
        buttonsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonsPanel.AutoSize = true;
        buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        rootLayoutPanel.SetColumnSpan(buttonsPanel, 2);
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(582, 565);
        buttonsPanel.Margin = new Padding(0, 8, 0, 0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(166, 33);
        buttonsPanel.TabIndex = 28;
        buttonsPanel.WrapContents = false;
        // 
        // saveButton
        // 
        saveButton.AutoSize = true;
        saveButton.Location = new Point(87, 4);
        saveButton.Margin = new Padding(4);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(75, 25);
        saveButton.TabIndex = 0;
        saveButton.Text = "Сохранить";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += saveButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(4, 4);
        cancelButton.Margin = new Padding(4);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Отмена";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // DeviceEditorForm
        // 
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(760, 601);
        Controls.Add(rootLayoutPanel);
        MinimumSize = new Size(680, 640);
        Name = "DeviceEditorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Виртуальное устройство";
        rootLayoutPanel.ResumeLayout(false);
        rootLayoutPanel.PerformLayout();
        uuidPanel.ResumeLayout(false);
        uuidPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_block).EndInit();
        ((System.ComponentModel.ISupportInitialize)_port).EndInit();
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }

}
