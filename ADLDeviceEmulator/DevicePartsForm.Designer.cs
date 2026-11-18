using System.Drawing;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class DevicePartsForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootLayoutPanel = null!;
    private Label _deviceLabel = null!;
    private FlowLayoutPanel partsPanel = null!;
    private Button _deviceParametersButton = null!;
    private Button _execChannelsButton = null!;
    private Button _streamsButton = null!;
    private Button _controlChannelsButton = null!;
    private Button _groupsButton = null!;
    private Button _picturesButton = null!;
    private Button _buttonsButton = null!;
    private Button _pixelgateChannelsButton = null!;
    private Button _gateConveyorsButton = null!;
    private Button _igmpButton = null!;
    private Label noteLabel = null!;
    private FlowLayoutPanel bottomPanel = null!;
    private Button _closeButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if(disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayoutPanel = new TableLayoutPanel();
        _deviceLabel = new Label();
        partsPanel = new FlowLayoutPanel();
        _deviceParametersButton = new Button();
        _execChannelsButton = new Button();
        _streamsButton = new Button();
        _controlChannelsButton = new Button();
        _groupsButton = new Button();
        _picturesButton = new Button();
        _buttonsButton = new Button();
        _pixelgateChannelsButton = new Button();
        _gateConveyorsButton = new Button();
        _igmpButton = new Button();
        noteLabel = new Label();
        bottomPanel = new FlowLayoutPanel();
        _closeButton = new Button();
        _cancelButton = new Button();
        rootLayoutPanel.SuspendLayout();
        partsPanel.SuspendLayout();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        //
        // rootLayoutPanel
        //
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(_deviceLabel, 0, 0);
        rootLayoutPanel.Controls.Add(partsPanel, 0, 1);
        rootLayoutPanel.Controls.Add(noteLabel, 0, 2);
        rootLayoutPanel.Controls.Add(bottomPanel, 0, 3);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Padding = new Padding(12);
        rootLayoutPanel.RowCount = 4;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayoutPanel.Name = "rootLayoutPanel";
        //
        // _deviceLabel
        //
        _deviceLabel.Dock = DockStyle.Fill;
        _deviceLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        _deviceLabel.Text = "Тип / имя / UUID";
        _deviceLabel.TextAlign = ContentAlignment.MiddleLeft;
        _deviceLabel.Name = "_deviceLabel";
        //
        // partsPanel
        //
        partsPanel.AutoScroll = true;
        partsPanel.Controls.Add(_deviceParametersButton);
        partsPanel.Controls.Add(_execChannelsButton);
        partsPanel.Controls.Add(_streamsButton);
        partsPanel.Controls.Add(_controlChannelsButton);
        partsPanel.Controls.Add(_groupsButton);
        partsPanel.Controls.Add(_picturesButton);
        partsPanel.Controls.Add(_buttonsButton);
        partsPanel.Controls.Add(_pixelgateChannelsButton);
        partsPanel.Controls.Add(_gateConveyorsButton);
        partsPanel.Controls.Add(_igmpButton);
        partsPanel.Dock = DockStyle.Fill;
        partsPanel.Padding = new Padding(4);
        partsPanel.WrapContents = true;
        partsPanel.Name = "partsPanel";
        //
        // section buttons
        //
        ConfigurePartButton(_deviceParametersButton, "Параметры протокола…", deviceParametersButton_Click);
        ConfigurePartButton(_execChannelsButton, "Каналы EXEC…", execChannelsButton_Click);
        ConfigurePartButton(_streamsButton, "Потоки…", streamsButton_Click);
        ConfigurePartButton(_controlChannelsButton, "Каналы ПУ…", controlChannelsButton_Click);
        ConfigurePartButton(_groupsButton, "Группы…", groupsButton_Click);
        ConfigurePartButton(_picturesButton, "Картины…", picturesButton_Click);
        ConfigurePartButton(_buttonsButton, "Кнопки ПУ…", buttonsButton_Click);
        ConfigurePartButton(_pixelgateChannelsButton, "Каналы PixelGate…", pixelgateChannelsButton_Click);
        ConfigurePartButton(_gateConveyorsButton, "Конвейеры Gate…", gateConveyorsButton_Click);
        ConfigurePartButton(_igmpButton, "IGMP-подписки…", igmpButton_Click);
        //
        // noteLabel
        //
        noteLabel.Dock = DockStyle.Fill;
        noteLabel.ForeColor = SystemColors.GrayText;
        noteLabel.Text = "Каждый раздел открывается в отдельном окне. Изменения передаются эмулятору после нажатия «Сохранить».";
        noteLabel.TextAlign = ContentAlignment.MiddleLeft;
        noteLabel.Name = "noteLabel";
        //
        // bottomPanel
        //
        bottomPanel.Controls.Add(_closeButton);
        bottomPanel.Controls.Add(_cancelButton);
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.FlowDirection = FlowDirection.RightToLeft;
        bottomPanel.Name = "bottomPanel";
        //
        // _closeButton
        //
        _closeButton.AutoSize = true;
        _closeButton.Text = "Готово";
        _closeButton.UseVisualStyleBackColor = true;
        _closeButton.Click += closeButton_Click;
        //
        // _cancelButton
        //
        _cancelButton.AutoSize = true;
        _cancelButton.Text = "Отмена";
        _cancelButton.UseVisualStyleBackColor = true;
        _cancelButton.Click += cancelButton_Click;
        //
        // DevicePartsForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(760, 520);
        Controls.Add(rootLayoutPanel);
        MinimumSize = new Size(620, 420);
        Name = "DevicePartsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Состав устройства";
        rootLayoutPanel.ResumeLayout(false);
        partsPanel.ResumeLayout(false);
        bottomPanel.ResumeLayout(false);
        bottomPanel.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigurePartButton(Button button, string text, EventHandler onClick)
    {
        button.AutoSize = true;
        button.MinimumSize = new Size(220, 42);
        button.Margin = new Padding(8);
        button.Text = text;
        button.UseVisualStyleBackColor = true;
        button.Click += onClick;
    }
}
