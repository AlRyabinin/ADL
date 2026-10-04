using AdlpLib.Backup;
using AdlpLib.Core;
using AdlpdsConfig.Service;
using System.IO;

namespace AdlpdsConfig;

internal partial class fBackup : Form
{
    private readonly AdlpdsService mSvc;
    private readonly Device mDevice;

    private enum Op : byte { None, Scan, Save }

    private Op mOp = Op.None;
    private bool mScanDone = false;
    private string mSaveFileName = "";
    private int[] mSaveIndices = Array.Empty<int>();
    private BackupFile? mSelectedBackup = null;

    public fBackup(AdlpdsService svc, Device device)
    {
        InitializeComponent();

        mSvc = svc;
        mDevice = device;

        Text = $"Бэкап: {device.Name} block {device.Block}";
        lInfo.Text = $"{device.Name} block {device.Block}   IP={device.Endpoint?.Address}   UUID={device.Uuid}";

        UpdateButtons();
    }

    private void fBackup_Load(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(AdlpdsService.BackupsDir);
        RefreshBackups();
        tPoll.Start();
    }

    /// <summary>
    /// Начать (или дождаться) старта скана секций
    /// </summary>
    private void StartScan()
    {
        if (mScanDone || mOp != Op.None)
            return;

        if (mSvc.RequestBackup(mDevice, false))
        {
            mOp = Op.Scan;
            SetStatus("Скан секций...");
            UpdateButtons();
        }
        // Иначе устройство занято другим процессом - повторяем в следующем тике.
    }

    private void tPoll_Tick(object? sender, EventArgs e)
    {
        if (mOp == Op.None)
        {
            if (!mScanDone)
                StartScan();
            UpdateButtons();
            return;
        }

        var bd = mSvc.GetBackedupDevice(mDevice);
        if (bd == null || !bd.IsCompleted())
            return;

        switch (mOp)
        {
            case Op.Scan:
                if (bd.BackupSupported)
                {
                    if (bd.IsCompletedOk())
                    {
                        FillSections(bd.GetSections());
                        SetStatus($"Скан завершён. Секций: {dgSections.Rows.Count}.");
                    }
                    else
                        SetStatus("Ошибка скана: " + (bd.ErrorMessage ?? "неизвестно"));
                }
                else
                    SetStatus("Устройство не поддерживает бэкап настроек.");

                mScanDone = true;
                mOp = Op.None;
                break;

            case Op.Save:
                if (bd.IsCompletedOk())
                {
                    if (mSvc.SaveBackup(mDevice, mSaveFileName, mSaveIndices))
                    {
                        SetStatus($"Бэкап сохранён: {Path.GetFileName(mSaveFileName)}");
                        RefreshBackups();
                    }
                    else
                        SetStatus("Ошибка сохранения файла бэкапа.");
                }
                else
                    SetStatus("Ошибка выгрузки данных: " + (bd.ErrorMessage ?? "неизвестно"));

                mOp = Op.None;
                break;
        }

        UpdateButtons();
    }

    private void FillSections(BackupSectionData[] sections)
    {
        dgSections.Rows.Clear();
        foreach (var s in sections)
        {
            int nominal = s.BlockCount * s.BlockSize;
            var idx = dgSections.Rows.Add(true, s.Index, s.Type.ToString(), s.Name, s.BlockCount, s.BlockSize, nominal);
            dgSections.Rows[idx].Tag = (int)s.Index;
        }
    }

    private int[] GetCheckedIndices()
    {
        var result = new List<int>();
        foreach (DataGridViewRow row in dgSections.Rows)
        {
            if (row.Cells[0].Value is bool b && b)
                result.Add((int)row.Tag!);
        }
        return result.ToArray();
    }

    private void bScan_Click(object? sender, EventArgs e)
    {
        mScanDone = false;
        dgSections.Rows.Clear();
        mSelectedBackup = null;
        StartScan();
        UpdateButtons();
    }

    private void bSelectAll_Click(object? sender, EventArgs e)
    {
        bool all = dgSections.Rows.Count > 0;
        foreach (DataGridViewRow row in dgSections.Rows)
        {
            if (!Convert.ToBoolean(row.Cells[0].Value))
            {
                all = false;
                break;
            }
        }

        foreach (DataGridViewRow row in dgSections.Rows)
            row.Cells[0].Value = !all;

        UpdateButtons();
    }

    private void bSave_Click(object? sender, EventArgs e)
    {
        var indices = GetCheckedIndices();
        if (indices.Length == 0)
        {
            MessageBox.Show("Отметьте галочками секции для сохранения.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!mScanDone)
        {
            MessageBox.Show("Сначала выполните скан секций.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Сохранить бэкап",
            Filter = "Бэкап (*.bak)|*.bak|Все файлы (*.*)|*.*",
            InitialDirectory = AdlpdsService.BackupsDir,
            FileName = $"{mDevice.Name}_{mDevice.Block}_{DateTime.Now:yyyyMMdd-HHmmss}.bak"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        if (!mSvc.RequestBackup(mDevice, true))
        {
            SetStatus("Устройство занято другим процессом, повторите позже.");
            return;
        }

        mSaveFileName = dlg.FileName;
        mSaveIndices = indices;
        mOp = Op.Save;
        SetStatus("Загрузка данных с устройства...");
        UpdateButtons();
    }

    private void bLoad_Click(object? sender, EventArgs e)
    {
        var file = mSelectedBackup;
        if (file == null)
            return;

        var indices = GetCheckedIndices();
        if (indices.Length == 0)
        {
            MessageBox.Show("Отметьте галочками секции для загрузки.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!file.Matches(mDevice.Name, mDevice.Block))
        {
            MessageBox.Show(
                $"Бэкап не подходит к устройству:\nфайл: {file.DeviceType} block {file.DeviceBlock}\nустройство: {mDevice.Name} block {mDevice.Block}",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (file.Uuid != Guid.Empty && mDevice.Uuid != Guid.Empty && file.Uuid != mDevice.Uuid)
        {
            if (MessageBox.Show(
                    "UUID бэкапа отличается от UUID устройства (замена по поломке). Продолжить?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
        }

        if (mSvc.UploadBackup(mDevice, file, indices))
            SetStatus($"Бэкап отправлен на устройство (секций: {indices.Length}).");
        else
            MessageBox.Show("Не удалось отправить бэкап на устройство.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void bFromFile_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Открыть файл бэкапа",
            Filter = "Бэкап (*.bak)|*.bak|Все файлы (*.*)|*.*",
            InitialDirectory = AdlpdsService.BackupsDir
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var file = mSvc.LoadBackupFile(dlg.FileName);
        if (file == null)
        {
            MessageBox.Show("Не удалось прочитать файл бэкапа.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        mSelectedBackup = file;
        SetStatus($"Выбран файл: {dlg.FileName}");
        UpdateButtons();
    }

    private void bRefresh_Click(object? sender, EventArgs e)
    {
        RefreshBackups();
    }

    private void lbBackups_SelectedIndexChanged(object? sender, EventArgs e)
    {
        mSelectedBackup = (lbBackups.SelectedItem as BackupListItem)?.File;
        UpdateButtons();
    }

    private void RefreshBackups()
    {
        lbBackups.Items.Clear();
        mSelectedBackup = null;

        var dir = AdlpdsService.BackupsDir;
        if (Directory.Exists(dir))
        {
            foreach (var f in Directory.GetFiles(dir, "*.bak"))
            {
                var file = mSvc.LoadBackupFile(f);
                if (file == null || !file.Matches(mDevice.Name, mDevice.Block))
                    continue;

                var time = (file.Timestamp != DateTime.MinValue)
                    ? file.Timestamp
                    : System.IO.File.GetLastWriteTime(f);
                lbBackups.Items.Add(new BackupListItem(file, time));
            }
        }

        UpdateButtons();
    }

    private void dgSections_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgSections.IsCurrentCellDirty && dgSections.CurrentCell is DataGridViewCheckBoxCell)
            dgSections.EndEdit();
    }

    private void dgSections_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
            UpdateButtons();
    }

    private bool IsBusy => mOp != Op.None;

    private void UpdateButtons()
    {
        bool hasChecked = GetCheckedIndices().Length > 0;

        bScan.Enabled = !IsBusy;
        bSelectAll.Enabled = !IsBusy && dgSections.Rows.Count > 0;
        bSave.Enabled = !IsBusy && mScanDone && dgSections.Rows.Count > 0 && hasChecked;
        bLoad.Enabled = !IsBusy && mSelectedBackup != null && dgSections.Rows.Count > 0 && hasChecked;
        bRefresh.Enabled = !IsBusy;
        bFromFile.Enabled = !IsBusy;
    }

    private void SetStatus(string text)
    {
        lStatus.Text = text;
    }

    private sealed class BackupListItem
    {
        public BackupFile File;
        public DateTime Time;

        public BackupListItem(BackupFile file, DateTime time)
        {
            File = file;
            Time = time;
        }

        public override string ToString()
        {
            return $"{File.DeviceType} block{File.DeviceBlock} | {Time:dd.MM.yyyy HH:mm} | {File.Uuid} | секций: {File.Sections.Count}";
        }
    }
}