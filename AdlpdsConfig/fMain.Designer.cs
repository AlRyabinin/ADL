namespace AdlpdsConfig;

partial class fMain : Form
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tTimer = new System.Windows.Forms.Timer(components);
        msMain = new MenuStrip();
        miBase = new ToolStripMenuItem();
        miDbOpen = new ToolStripMenuItem();
        miDbNew = new ToolStripMenuItem();
        базаДанныхToolStripMenuItem = new ToolStripMenuItem();
        bdOpen = new ToolStripMenuItem();
        bdDelete = new ToolStripMenuItem();
        miDevice = new ToolStripMenuItem();
        miEditor = new ToolStripMenuItem();
        miSaveConfiguration = new ToolStripMenuItem();
        miLoadConfiguration = new ToolStripMenuItem();
        miMulticastConfiguration = new ToolStripMenuItem();
        miDevicesGroup = new ToolStripMenuItem();
        miSetMac = new ToolStripMenuItem();
        miSetNet = new ToolStripMenuItem();
        miSetName = new ToolStripMenuItem();
        miSetUuid = new ToolStripMenuItem();
        miFirmware = new ToolStripMenuItem();
        miBackup = new ToolStripMenuItem();
        miResetBackup = new ToolStripMenuItem();
        statusStrip1 = new StatusStrip();
        lStatusDevCount = new ToolStripStatusLabel();
        lDbPath = new Label();
        lDevices = new ListView();
        lvCType = new ColumnHeader();
        lvCBlock = new ColumnHeader();
        lvCIP = new ColumnHeader();
        lvCUUID = new ColumnHeader();
        lvCPreset = new ColumnHeader();
        lvCBackup = new ColumnHeader();
        msMain.SuspendLayout();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // tTimer
        // 
        tTimer.Interval = 500;
        tTimer.Tick += tTimer_Tick;
        // 
        // msMain
        // 
        msMain.Items.AddRange(new ToolStripItem[] { miBase, базаДанныхToolStripMenuItem, miDevice });
        msMain.Location = new Point(0, 0);
        msMain.Name = "msMain";
        msMain.Padding = new Padding(7, 2, 0, 2);
        msMain.Size = new Size(869, 24);
        msMain.TabIndex = 0;
        msMain.Text = "msMain";
        // 
        // miBase
        // 
        miBase.DropDownItems.AddRange(new ToolStripItem[] { miDbOpen, miDbNew });
        miBase.Name = "miBase";
        miBase.Size = new Size(43, 20);
        miBase.Text = "База";
        // 
        // miDbOpen
        // 
        miDbOpen.Name = "miDbOpen";
        miDbOpen.Size = new Size(157, 22);
        miDbOpen.Text = "Открыть базу…";
        miDbOpen.Click += miDbOpen_Click;
        // 
        // miDbNew
        // 
        miDbNew.Name = "miDbNew";
        miDbNew.Size = new Size(157, 22);
        miDbNew.Text = "Создать базу…";
        miDbNew.Click += miDbNew_Click;
        // 
        // базаДанныхToolStripMenuItem
        // 
        базаДанныхToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { bdOpen, bdDelete });
        базаДанныхToolStripMenuItem.Name = "базаДанныхToolStripMenuItem";
        базаДанныхToolStripMenuItem.Size = new Size(86, 20);
        базаДанныхToolStripMenuItem.Text = "База данных";
        // 
        // bdOpen
        // 
        bdOpen.Name = "bdOpen";
        bdOpen.Size = new Size(121, 22);
        bdOpen.Text = "Открыть";
        bdOpen.Click += bdOpen_Click;
        // 
        // bdDelete
        // 
        bdDelete.Name = "bdDelete";
        bdDelete.Size = new Size(121, 22);
        bdDelete.Text = "Удалить";
        bdDelete.Click += bdDelete_Click;
        // 
        // miDevice
        // 
        miDevice.DropDownItems.AddRange(new ToolStripItem[] { miEditor, miSaveConfiguration, miLoadConfiguration, miMulticastConfiguration, miDevicesGroup, miSetMac, miSetNet, miSetName, miSetUuid, miFirmware, miBackup, miResetBackup });
        miDevice.Name = "miDevice";
        miDevice.Size = new Size(82, 20);
        miDevice.Text = "Устройство";
        // 
        // miEditor
        // 
        miEditor.Name = "miEditor";
        miEditor.Size = new Size(302, 22);
        miEditor.Text = "Редактор настроек…";
        miEditor.Click += miEditor_Click;
        // 
        // miSaveConfiguration
        // 
        miSaveConfiguration.Enabled = false;
        miSaveConfiguration.Name = "miSaveConfiguration";
        miSaveConfiguration.Size = new Size(302, 22);
        miSaveConfiguration.Text = "Сохранить конфигурацию";
        miSaveConfiguration.Click += miSaveConfiguration_Click;
        // 
        // miLoadConfiguration
        // 
        miLoadConfiguration.Enabled = false;
        miLoadConfiguration.Name = "miLoadConfiguration";
        miLoadConfiguration.Size = new Size(302, 22);
        miLoadConfiguration.Text = "Загрузить конфигурацию";
        miLoadConfiguration.Click += miLoadConfiguration_Click;
        // 
        // miMulticastConfiguration
        // 
        miMulticastConfiguration.Name = "miMulticastConfiguration";
        miMulticastConfiguration.Size = new Size(302, 22);
        miMulticastConfiguration.Text = "Мультикаст";
        miMulticastConfiguration.Click += MulticastToolStripMenuItem_Click;
        // 
        // miDevicesGroup
        // 
        miDevicesGroup.Name = "miDevicesGroup";
        miDevicesGroup.Size = new Size(302, 22);
        miDevicesGroup.Text = "Группа устройств";
        miDevicesGroup.Click += miDevicesGroup_Click;
        // 
        // miSetMac
        // 
        miSetMac.Name = "miSetMac";
        miSetMac.Size = new Size(302, 22);
        miSetMac.Text = "MAC-адрес…";
        miSetMac.Click += miSetMac_Click;
        // 
        // miSetNet
        // 
        miSetNet.Name = "miSetNet";
        miSetNet.Size = new Size(302, 22);
        miSetNet.Text = "IP / маска / шлюз…";
        miSetNet.Click += miSetNet_Click;
        // 
        // miSetName
        // 
        miSetName.Name = "miSetName";
        miSetName.Size = new Size(302, 22);
        miSetName.Text = "Имя…";
        miSetName.Click += miSetName_Click;
        // 
        // miSetUuid
        // 
        miSetUuid.Name = "miSetUuid";
        miSetUuid.Size = new Size(302, 22);
        miSetUuid.Text = "UUID…";
        miSetUuid.Click += miSetUuid_Click;
        // 
        // miFirmware
        // 
        miFirmware.Name = "miFirmware";
        miFirmware.Size = new Size(302, 22);
        miFirmware.Text = "Прошивка…";
        miFirmware.Click += miFirmware_Click;
        // 
        // miBackup
        // 
        miBackup.Name = "miBackup";
        miBackup.Size = new Size(302, 22);
        miBackup.Text = "Бэкап настроек…";
        miBackup.Click += miBackup_Click;
        // 
        // miResetBackup
        // 
        miResetBackup.Name = "miResetBackup";
        miResetBackup.Size = new Size(302, 22);
        miResetBackup.Text = "Перепроверить возможности устройства";
        miResetBackup.Click += miResetBackup_Click;
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { lStatusDevCount });
        statusStrip1.Location = new Point(0, 398);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Padding = new Padding(1, 0, 12, 0);
        statusStrip1.Size = new Size(869, 22);
        statusStrip1.TabIndex = 1;
        statusStrip1.Text = "statusStrip1";
        // 
        // lStatusDevCount
        // 
        lStatusDevCount.Name = "lStatusDevCount";
        lStatusDevCount.Size = new Size(13, 17);
        lStatusDevCount.Text = "0";
        // 
        // lDbPath
        // 
        lDbPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lDbPath.AutoEllipsis = true;
        lDbPath.Location = new Point(12, 34);
        lDbPath.Name = "lDbPath";
        lDbPath.Size = new Size(845, 20);
        lDbPath.TabIndex = 2;
        lDbPath.Text = "База не выбрана";
        // 
        // lDevices
        // 
        lDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lDevices.Columns.AddRange(new ColumnHeader[] { lvCType, lvCBlock, lvCIP, lvCUUID, lvCPreset, lvCBackup });
        lDevices.FullRowSelect = true;
        lDevices.Location = new Point(12, 60);
        lDevices.Name = "lDevices";
        lDevices.Size = new Size(845, 332);
        lDevices.TabIndex = 3;
        lDevices.UseCompatibleStateImageBehavior = false;
        lDevices.View = View.Details;
        lDevices.SelectedIndexChanged += lDevices_SelectedIndexChanged;
        lDevices.DoubleClick += lDevices_DoubleClick;
        // 
        // lvCType
        // 
        lvCType.Text = "Тип";
        lvCType.Width = 80;
        // 
        // lvCBlock
        // 
        lvCBlock.Text = "Блок";
        lvCBlock.Width = 55;
        // 
        // lvCIP
        // 
        lvCIP.Text = "IP";
        lvCIP.Width = 120;
        // 
        // lvCUUID
        // 
        lvCUUID.Text = "UUID";
        lvCUUID.Width = 260;
        // 
        // lvCPreset
        // 
        lvCPreset.Text = "Конфигурация";
        lvCPreset.Width = 206;
        // 
        // lvCBackup
        // 
        lvCBackup.Text = "Backup";
        lvCBackup.Width = 55;
        // 
        // fMain
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(869, 420);
        Controls.Add(lDevices);
        Controls.Add(lDbPath);
        Controls.Add(statusStrip1);
        Controls.Add(msMain);
        MainMenuStrip = msMain;
        Name = "fMain";
        Text = "ADLPDS Configurator";
        FormClosing += fMain_FormClosing;
        Load += fMain_Load;
        msMain.ResumeLayout(false);
        msMain.PerformLayout();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Timer tTimer;
    private MenuStrip msMain;
    private ToolStripMenuItem miBase;
    private ToolStripMenuItem miDbOpen;
    private ToolStripMenuItem miDbNew;
    private ToolStripMenuItem miDevice;
    private ToolStripMenuItem miEditor;
    private ToolStripMenuItem miSetMac;
    private ToolStripMenuItem miSetNet;
    private ToolStripMenuItem miSetName;
    private ToolStripMenuItem miSetUuid;
    private ToolStripMenuItem miFirmware;
    private ToolStripMenuItem miBackup;
    private ToolStripMenuItem miResetBackup;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel lStatusDevCount;
    private Label lDbPath;
    private ListView lDevices;
    private ColumnHeader lvCType;
    private ColumnHeader lvCBlock;
    private ColumnHeader lvCIP;
    private ColumnHeader lvCUUID;
    private ColumnHeader lvCPreset;
    private ColumnHeader lvCBackup;
    private ToolStripMenuItem miSaveConfiguration;
    private ToolStripMenuItem miLoadConfiguration;
    private ToolStripMenuItem базаДанныхToolStripMenuItem;
    private ToolStripMenuItem bdOpen;
    private ToolStripMenuItem bdDelete;
    private ToolStripMenuItem miDevicesGroup;
    private ToolStripMenuItem miMulticastConfiguration;
}