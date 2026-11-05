using System.Drawing;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel rootLayoutPanel = null!;
    private FlowLayoutPanel toolbarPanel = null!;
    private Label bindAddressLabel = null!;
    private ComboBox _bindAddress = null!;
    private Label targetAddressLabel = null!;
    private TextBox _targetAddress = null!;
    private Label targetPortLabel = null!;
    private NumericUpDown _targetPort = null!;
    private Button createDeviceButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;
    private Button demoSetButton = null!;
    private Button loadButton = null!;
    private Button saveButton = null!;
    private Button _startSelectedButton = null!;
    private Button _stopSelectedButton = null!;
    private Button _startAllButton = null!;
    private Button _stopAllButton = null!;
    private DataGridView _grid = null!;
    private DataGridViewCheckBoxColumn enabledColumn = null!;
    private DataGridViewTextBoxColumn typeColumn = null!;
    private DataGridViewTextBoxColumn deviceNameColumn = null!;
    private DataGridViewTextBoxColumn uuidColumn = null!;
    private DataGridViewTextBoxColumn blockColumn = null!;
    private DataGridViewTextBoxColumn portColumn = null!;
    private DataGridViewTextBoxColumn ipAddressColumn = null!;
    private DataGridViewTextBoxColumn macColumn = null!;
    private DataGridViewTextBoxColumn channelIndexesColumn = null!;
    private DataGridViewTextBoxColumn ownAddressColumn = null!;
    private DataGridViewTextBoxColumn subscriptionsColumn = null!;
    private DataGridViewTextBoxColumn runtimeStatusColumn = null!;
    private TableLayoutPanel logLayoutPanel = null!;
    private Label _status = null!;
    private RichTextBox _log = null!;

    protected override void Dispose(bool disposing)
    {
        if(disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayoutPanel = new TableLayoutPanel();
        toolbarPanel = new FlowLayoutPanel();
        bindAddressLabel = new Label();
        _bindAddress = new ComboBox();
        targetAddressLabel = new Label();
        _targetAddress = new TextBox();
        targetPortLabel = new Label();
        _targetPort = new NumericUpDown();
        createDeviceButton = new Button();
        _editButton = new Button();
        _deleteButton = new Button();
        demoSetButton = new Button();
        loadButton = new Button();
        saveButton = new Button();
        _startSelectedButton = new Button();
        _stopSelectedButton = new Button();
        _startAllButton = new Button();
        _stopAllButton = new Button();
        _grid = new DataGridView();
        enabledColumn = new DataGridViewCheckBoxColumn();
        typeColumn = new DataGridViewTextBoxColumn();
        deviceNameColumn = new DataGridViewTextBoxColumn();
        uuidColumn = new DataGridViewTextBoxColumn();
        blockColumn = new DataGridViewTextBoxColumn();
        portColumn = new DataGridViewTextBoxColumn();
        ipAddressColumn = new DataGridViewTextBoxColumn();
        macColumn = new DataGridViewTextBoxColumn();
        channelIndexesColumn = new DataGridViewTextBoxColumn();
        ownAddressColumn = new DataGridViewTextBoxColumn();
        subscriptionsColumn = new DataGridViewTextBoxColumn();
        runtimeStatusColumn = new DataGridViewTextBoxColumn();
        logLayoutPanel = new TableLayoutPanel();
        _status = new Label();
        _log = new RichTextBox();
        ((System.ComponentModel.ISupportInitialize)_targetPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        rootLayoutPanel.SuspendLayout();
        toolbarPanel.SuspendLayout();
        logLayoutPanel.SuspendLayout();
        SuspendLayout();
        //
        // rootLayoutPanel
        //
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(toolbarPanel, 0, 0);
        rootLayoutPanel.Controls.Add(_grid, 0, 1);
        rootLayoutPanel.Controls.Add(logLayoutPanel, 0, 2);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Padding = new Padding(8);
        rootLayoutPanel.RowCount = 3;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 67F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        rootLayoutPanel.Name = "rootLayoutPanel";
        //
        // toolbarPanel
        //
        toolbarPanel.AutoSize = true;
        toolbarPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        toolbarPanel.Controls.Add(bindAddressLabel);
        toolbarPanel.Controls.Add(_bindAddress);
        toolbarPanel.Controls.Add(targetAddressLabel);
        toolbarPanel.Controls.Add(_targetAddress);
        toolbarPanel.Controls.Add(targetPortLabel);
        toolbarPanel.Controls.Add(_targetPort);
        toolbarPanel.Controls.Add(createDeviceButton);
        toolbarPanel.Controls.Add(_editButton);
        toolbarPanel.Controls.Add(_deleteButton);
        toolbarPanel.Controls.Add(demoSetButton);
        toolbarPanel.Controls.Add(loadButton);
        toolbarPanel.Controls.Add(saveButton);
        toolbarPanel.Controls.Add(_startSelectedButton);
        toolbarPanel.Controls.Add(_stopSelectedButton);
        toolbarPanel.Controls.Add(_startAllButton);
        toolbarPanel.Controls.Add(_stopAllButton);
        toolbarPanel.Dock = DockStyle.Fill;
        toolbarPanel.Margin = new Padding(0, 0, 0, 8);
        toolbarPanel.Name = "toolbarPanel";
        toolbarPanel.Padding = new Padding(0, 0, 0, 4);
        toolbarPanel.WrapContents = true;
        //
        // bindAddressLabel
        //
        bindAddressLabel.AutoSize = true;
        bindAddressLabel.Margin = new Padding(4, 9, 2, 0);
        bindAddressLabel.Name = "bindAddressLabel";
        bindAddressLabel.Text = "Локальный интерфейс:";
        //
        // _bindAddress
        //
        _bindAddress.DropDownStyle = ComboBoxStyle.DropDownList;
        _bindAddress.Margin = new Padding(2, 4, 8, 3);
        _bindAddress.Name = "_bindAddress";
        _bindAddress.Size = new Size(250, 23);
        //
        // targetAddressLabel
        //
        targetAddressLabel.AutoSize = true;
        targetAddressLabel.Margin = new Padding(8, 9, 2, 0);
        targetAddressLabel.Name = "targetAddressLabel";
        targetAddressLabel.Text = "Куда отправлять STATUS:";
        //
        // _targetAddress
        //
        _targetAddress.Margin = new Padding(2, 4, 4, 3);
        _targetAddress.Name = "_targetAddress";
        _targetAddress.Size = new Size(145, 23);
        _targetAddress.Text = "255.255.255.255";
        //
        // targetPortLabel
        //
        targetPortLabel.AutoSize = true;
        targetPortLabel.Margin = new Padding(4, 9, 2, 0);
        targetPortLabel.Name = "targetPortLabel";
        targetPortLabel.Text = "Порт:";
        //
        // _targetPort
        //
        _targetPort.Margin = new Padding(2, 4, 8, 3);
        _targetPort.Maximum = 65535;
        _targetPort.Minimum = 1;
        _targetPort.Name = "_targetPort";
        _targetPort.Size = new Size(75, 23);
        _targetPort.Value = 20000;
        //
        // Buttons
        //
        createDeviceButton.AutoSize = true;
        createDeviceButton.Height = 28;
        createDeviceButton.Margin = new Padding(4, 2, 4, 2);
        createDeviceButton.Name = "createDeviceButton";
        createDeviceButton.Text = "Создать устройство";
        createDeviceButton.UseVisualStyleBackColor = true;
        _editButton.AutoSize = true;
        _editButton.Height = 28;
        _editButton.Margin = new Padding(4, 2, 4, 2);
        _editButton.Name = "_editButton";
        _editButton.Text = "Изменить";
        _editButton.UseVisualStyleBackColor = true;
        _deleteButton.AutoSize = true;
        _deleteButton.Height = 28;
        _deleteButton.Margin = new Padding(4, 2, 4, 2);
        _deleteButton.Name = "_deleteButton";
        _deleteButton.Text = "Удалить";
        _deleteButton.UseVisualStyleBackColor = true;
        demoSetButton.AutoSize = true;
        demoSetButton.Height = 28;
        demoSetButton.Margin = new Padding(4, 2, 4, 2);
        demoSetButton.Name = "demoSetButton";
        demoSetButton.Text = "Демо-набор";
        demoSetButton.UseVisualStyleBackColor = true;
        loadButton.AutoSize = true;
        loadButton.Height = 28;
        loadButton.Margin = new Padding(4, 2, 4, 2);
        loadButton.Name = "loadButton";
        loadButton.Text = "Загрузить";
        loadButton.UseVisualStyleBackColor = true;
        saveButton.AutoSize = true;
        saveButton.Height = 28;
        saveButton.Margin = new Padding(4, 2, 4, 2);
        saveButton.Name = "saveButton";
        saveButton.Text = "Сохранить";
        saveButton.UseVisualStyleBackColor = true;
        _startSelectedButton.AutoSize = true;
        _startSelectedButton.Height = 28;
        _startSelectedButton.Margin = new Padding(4, 2, 4, 2);
        _startSelectedButton.Name = "_startSelectedButton";
        _startSelectedButton.Text = "Запустить выбранное";
        _startSelectedButton.UseVisualStyleBackColor = true;
        _stopSelectedButton.AutoSize = true;
        _stopSelectedButton.Height = 28;
        _stopSelectedButton.Margin = new Padding(4, 2, 4, 2);
        _stopSelectedButton.Name = "_stopSelectedButton";
        _stopSelectedButton.Text = "Остановить выбранное";
        _stopSelectedButton.UseVisualStyleBackColor = true;
        _startAllButton.AutoSize = true;
        _startAllButton.Height = 28;
        _startAllButton.Margin = new Padding(4, 2, 4, 2);
        _startAllButton.Name = "_startAllButton";
        _startAllButton.Text = "Запустить все";
        _startAllButton.UseVisualStyleBackColor = true;
        _stopAllButton.AutoSize = true;
        _stopAllButton.Height = 28;
        _stopAllButton.Margin = new Padding(4, 2, 4, 2);
        _stopAllButton.Name = "_stopAllButton";
        _stopAllButton.Text = "Остановить все";
        _stopAllButton.UseVisualStyleBackColor = true;
        //
        // _grid
        //
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = true;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.Dock = DockStyle.Fill;
        _grid.Location = new Point(11, 42);
        _grid.MultiSelect = false;
        _grid.Name = "_grid";
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.TabIndex = 1;
        // Column definitions are kept in the designer so they can be edited visually.
        enabledColumn.DataPropertyName = "Enabled";
        enabledColumn.HeaderText = "Вкл.";
        enabledColumn.Name = "Enabled";
        enabledColumn.ReadOnly = true;
        enabledColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        enabledColumn.Width = 48;
        typeColumn.DataPropertyName = "Type";
        typeColumn.HeaderText = "Тип";
        typeColumn.Name = "Type";
        typeColumn.ReadOnly = true;
        typeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        typeColumn.Width = 85;
        deviceNameColumn.DataPropertyName = "DeviceName";
        deviceNameColumn.HeaderText = "Название";
        deviceNameColumn.Name = "DeviceName";
        deviceNameColumn.ReadOnly = true;
        deviceNameColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        deviceNameColumn.Width = 150;
        uuidColumn.DataPropertyName = "Uuid";
        uuidColumn.HeaderText = "UUID";
        uuidColumn.Name = "Uuid";
        uuidColumn.ReadOnly = true;
        uuidColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        uuidColumn.Width = 275;
        blockColumn.DataPropertyName = "Block";
        blockColumn.HeaderText = "Блок";
        blockColumn.Name = "Block";
        blockColumn.ReadOnly = true;
        blockColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        blockColumn.Width = 55;
        portColumn.DataPropertyName = "Port";
        portColumn.HeaderText = "UDP-порт";
        portColumn.Name = "Port";
        portColumn.ReadOnly = true;
        portColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        portColumn.Width = 70;
        ipAddressColumn.DataPropertyName = "IpAddress";
        ipAddressColumn.HeaderText = "IP устройства";
        ipAddressColumn.Name = "IpAddress";
        ipAddressColumn.ReadOnly = true;
        ipAddressColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        ipAddressColumn.Width = 110;
        macColumn.DataPropertyName = "Mac";
        macColumn.HeaderText = "MAC";
        macColumn.Name = "Mac";
        macColumn.ReadOnly = true;
        macColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        macColumn.Width = 130;
        channelIndexesColumn.DataPropertyName = "ChannelIndexes";
        channelIndexesColumn.HeaderText = "Индексы каналов";
        channelIndexesColumn.Name = "ChannelIndexes";
        channelIndexesColumn.ReadOnly = true;
        channelIndexesColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        channelIndexesColumn.Width = 150;
        ownAddressColumn.DataPropertyName = "OwnAddress";
        ownAddressColumn.HeaderText = "Свой multicast";
        ownAddressColumn.Name = "OwnAddress";
        ownAddressColumn.ReadOnly = true;
        ownAddressColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        ownAddressColumn.Width = 130;
        subscriptionsColumn.DataPropertyName = "Subscriptions";
        subscriptionsColumn.HeaderText = "Подписки";
        subscriptionsColumn.Name = "Subscriptions";
        subscriptionsColumn.ReadOnly = true;
        subscriptionsColumn.SortMode = DataGridViewColumnSortMode.Automatic;
        subscriptionsColumn.Width = 180;
        runtimeStatusColumn.HeaderText = "Состояние";
        runtimeStatusColumn.Name = "RuntimeStatus";
        runtimeStatusColumn.ReadOnly = true;
        runtimeStatusColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        runtimeStatusColumn.Width = 95;
        _grid.Columns.AddRange(new DataGridViewColumn[]
        {
            enabledColumn, typeColumn, deviceNameColumn, uuidColumn, blockColumn,
            portColumn, ipAddressColumn, macColumn, channelIndexesColumn,
            ownAddressColumn, subscriptionsColumn, runtimeStatusColumn
        });
        //
        // logLayoutPanel
        //
        logLayoutPanel.ColumnCount = 1;
        logLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        logLayoutPanel.Controls.Add(_status, 0, 0);
        logLayoutPanel.Controls.Add(_log, 0, 1);
        logLayoutPanel.Dock = DockStyle.Fill;
        logLayoutPanel.Margin = new Padding(0, 8, 0, 0);
        logLayoutPanel.Name = "logLayoutPanel";
        logLayoutPanel.RowCount = 2;
        logLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        logLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        //
        // _status
        //
        _status.Dock = DockStyle.Fill;
        _status.Margin = new Padding(3);
        _status.Name = "_status";
        _status.Text = "Остановлено. Создай устройство или загрузи демо-набор.";
        _status.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _log
        //
        _log.Dock = DockStyle.Fill;
        _log.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _log.Name = "_log";
        _log.ReadOnly = true;
        _log.WordWrap = false;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1550, 900);
        Controls.Add(rootLayoutPanel);
        MinimumSize = new Size(1120, 680);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ADL Device Emulator";
        createDeviceButton.Click += createDeviceButton_Click;
        _editButton.Click += editButton_Click;
        _deleteButton.Click += deleteButton_Click;
        demoSetButton.Click += demoSetButton_Click;
        loadButton.Click += loadButton_Click;
        saveButton.Click += saveButton_Click;
        _startSelectedButton.Click += startSelectedButton_Click;
        _stopSelectedButton.Click += stopSelectedButton_Click;
        _startAllButton.Click += startAllButton_Click;
        _stopAllButton.Click += stopAllButton_Click;
        _grid.DataError += grid_DataError;
        _grid.SelectionChanged += grid_SelectionChanged;
        _grid.CellDoubleClick += grid_CellDoubleClick;
        _grid.CellFormatting += GridCellFormatting;
        ((System.ComponentModel.ISupportInitialize)_targetPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        rootLayoutPanel.ResumeLayout(false);
        rootLayoutPanel.PerformLayout();
        toolbarPanel.ResumeLayout(false);
        toolbarPanel.PerformLayout();
        logLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

}
