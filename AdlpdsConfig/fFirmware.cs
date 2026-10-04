using AdlpLib.Core;
using AdlpLib.Core.Modules.Types;
using AdlpLib.Fw;
using AdlpdsConfig.Service;
using System.IO;

namespace AdlpdsConfig;

internal partial class fFirmware : Form
{
    private readonly AdlpdsService mSvc;
    private readonly Device mDevice;

    private AdlFirmwareFile? mFirmware = null;
    private FirmwaredDevice? mFwDev = null;
    private bool mDone = false;

    private FirmwaredDevice? mProbeDev = null;
    private DateTime mProbeStart;
    private bool mProbeDone = false;

    /// <summary>Тип устройства (из DETECT idx 0; пока не определён — из сканирования)</summary>
    private string mDeviceType = "";

    public fFirmware(AdlpdsService svc, Device device)
    {
        InitializeComponent();

        mSvc = svc;
        mDevice = device;

        mDeviceType = device.Name;

        Text = $"Прошивка: {device.Name} block {device.Block}";
        lInfo.Text = $"{device.Name} block {device.Block}   IP={device.Endpoint?.Address}   UUID={device.Uuid}";
        lType.Text = $"Тип: {mDeviceType}";

        UpdateButtons();
    }

    private sealed class FirmwareListItem
    {
        public string FileName;
        public AdlFirmwareFile? File;
        public string Type;

        public FirmwareListItem(string fileName, AdlFirmwareFile? file, string type = "")
        {
            FileName = fileName;
            File = file;
            Type = type;
        }

        public override string ToString()
        {
            if (File != null)
                return $"{File.DeviceType} {File.SoftwareVersion} | {Path.GetFileName(FileName)}";
            if (Type.Length > 0)
                return $"{Type} | {Path.GetFileName(FileName)}";
            return Path.GetFileName(FileName);
        }
    }

    private bool TypeMatches(string? type)
    {
        if (string.IsNullOrWhiteSpace(mDeviceType))
            return true;
        if (string.IsNullOrWhiteSpace(type))
            return false;
        return string.Equals(type.Trim(), mDeviceType.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void fFirmware_Load(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(AdlpdsService.FirmwareDir);
        RefreshFiles();
        tPoll.Start();

        // Определим тип устройства через DETECT (idx 0 — тип, 1 — версия ПО, 2 — производитель)
        mProbeDev = mSvc.ProbeFirmwareDevice(mDevice);
        mProbeStart = DateTime.Now;
    }

    private void UpdateProbe()
    {
        if (mProbeDev == null || mProbeDone)
            return;

        var type = mProbeDev.DeviceName;
        if (type != null && (DateTime.Now - mProbeStart).TotalMilliseconds > 300)
        {
            mProbeDone = true;
            mDeviceType = type;
            lType.Text = $"Тип: {mDeviceType}";
            SetStatus($"Тип устройства: {mDeviceType}, ПО: {mProbeDev.DeviceVersion}, производитель: {mProbeDev.DeviceManufacturer}");
            RefreshFiles();
        }
        else if ((DateTime.Now - mProbeStart).TotalSeconds > 35)
        {
            // Устройство не ответило на DETECT — остаёмся на типе из сканирования
            mProbeDone = true;
            SetStatus("Не удалось определить тип с устройства; используется тип из сканирования.");
        }
    }

    private void RefreshFiles()
    {
        lbFiles.Items.Clear();
        var dir = AdlpdsService.FirmwareDir;
        if (Directory.Exists(dir))
        {
            foreach (var f in Directory.GetFiles(dir, "*.adlfw"))
            {
                var file = mSvc.LoadFirmwareFile(f);
                if (file != null && TypeMatches(file.DeviceType))
                    lbFiles.Items.Add(new FirmwareListItem(f, file));
            }
            foreach (var f in Directory.GetFiles(dir, "*.yaml"))
            {
                string type;
                try { type = AdlFirmwareSpec.Load(f, out _).DeviceType; }
                catch { continue; }
                if (TypeMatches(type))
                    lbFiles.Items.Add(new FirmwareListItem(f, null, type));
            }
            foreach (var f in Directory.GetFiles(dir, "*.yml"))
            {
                string type;
                try { type = AdlFirmwareSpec.Load(f, out _).DeviceType; }
                catch { continue; }
                if (TypeMatches(type))
                    lbFiles.Items.Add(new FirmwareListItem(f, null, type));
            }
        }

        if (lbFiles.Items.Count == 0)
            SetStatus($"Нет прошивок для типа «{mDeviceType}» в каталоге firmware.");

        UpdateButtons();
    }

    private void lbFiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lbFiles.SelectedItem is FirmwareListItem item)
        {
            mFirmware = item.File ?? mSvc.LoadFirmwareAny(item.FileName);
            ShowFirmware(item.FileName);
        }
        UpdateButtons();
    }

    private void bOpen_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Открыть файл прошивки",
            Filter = "Прошивка (*.adlfw)|*.adlfw|Спека (*.yaml;*.yml)|*.yaml;*.yml|Все файлы (*.*)|*.*",
            InitialDirectory = AdlpdsService.FirmwareDir
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var file = mSvc.LoadFirmwareAny(dlg.FileName);
        if (file == null)
        {
            MessageBox.Show("Не удалось прочитать файл прошивки.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!TypeMatches(file.DeviceType))
        {
            MessageBox.Show($"Тип прошивки «{file.DeviceType}» не соответствует устройству «{mDeviceType}».", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        mFirmware = file;
        ShowFirmware(dlg.FileName);
        UpdateButtons();
    }

    private void ShowFirmware(string fileName)
    {
        if (mFirmware == null)
        {
            SetStatus("Файл не загружен.");
            lFwInfo.Text = "";
            return;
        }

        lFwInfo.Text =
            $"Тип: {mFirmware.DeviceType}\r\n" +
            $"Ядро: {mFirmware.KernelVersion}\r\n" +
            $"ПО: {mFirmware.SoftwareVersion}\r\n" +
            $"Сборка: {mFirmware.BuildDate:dd.MM.yyyy HH:mm}\r\n" +
            $"Размер: {mFirmware.Data.Length:N0} байт\r\n" +
            $"Описание: {mFirmware.Description}\r\n" +
            $"Комментарий: {mFirmware.Comment}\r\n" +
            $"Файл: {Path.GetFileName(fileName)}";

        SetStatus("Выбран файл: " + Path.GetFileName(fileName));
    }

    private void bFirmware_Click(object? sender, EventArgs e)
    {
        if (mFirmware == null)
            return;
        if (mFwDev != null && !mDone)
        {
            SetStatus("Прошивка уже идёт.");
            return;
        }

        var fw = mSvc.FirmwareDevice(mDevice, mFirmware);
        if (fw == null)
        {
            MessageBox.Show("Не удалось начать прошивку: устройство уже прошивается или недоступно.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        mFwDev = fw;
        mDone = false;
        pbProgress.Value = 0;
        lState.Text = "Запрос прошивки отправлен.";
        UpdateButtons();
    }

    private void tPoll_Tick(object? sender, EventArgs e)
    {
        UpdateProbe();

        if (mFwDev == null)
        {
            UpdateButtons();
            return;
        }

        if (mDone)
        {
            UpdateButtons();
            return;
        }

        if (!mFwDev.IsCompleted())
        {
            pbProgress.Value = mFwDev.ProgressPercent;
            lState.Text = $"{mFwDev.StateText}  [{mFwDev.CompleteBytes:N0} / {mFwDev.DataLength:N0}]";
            return;
        }

        mDone = true;
        pbProgress.Value = mFwDev.ProgressPercent;

        if (mFwDev.IsCompletedOk())
        {
            pbProgress.Value = 100;
            lState.Text = "Прошивка успешно завершена.";
            SetStatus("Готово.");
        }
        else
        {
            lState.Text = "Прошивка завершилась с ошибкой.";
            SetStatus("Ошибка. См. консоль.");
        }

        UpdateButtons();
    }

    private bool IsBusy => mFwDev != null && !mDone;

    private void UpdateButtons()
    {
        bFirmware.Enabled = mFirmware != null && !IsBusy;
        bOpen.Enabled = !IsBusy;
        bRefresh.Enabled = !IsBusy;
    }

    private void bRefresh_Click(object? sender, EventArgs e)
    {
        RefreshFiles();
    }

    private void bClose_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void SetStatus(string text)
    {
        lStatus.Text = text;
    }
}