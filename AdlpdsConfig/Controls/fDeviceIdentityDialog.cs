using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AdlpdsConfig.Controls;

/// <summary>
/// Предложение добавить в БД SID и UUID устройства, которых там нет.
/// Поля, уже имеющиеся в БД, показываются только для чтения.
/// </summary>
public partial class fDeviceIdentityDialog : Form
{
    private static readonly Regex SidRegex = new("^[0-9A-F]{8}$", RegexOptions.Compiled);

    private bool _sidInDb;
    private bool _uuidInDb;
    private bool _uuidUsable = true;

    private readonly int _deviceTypeId;

    public string Sid => tbSid.Text.Trim().ToUpperInvariant();

    public string SidName => tbSidName.Text.Trim();

    public string SidComment => tbSidComment.Text.Trim();

    public string Sn => tbSn.Text.Trim();

    /// <summary>UUID в формате D (верхний регистр) или null, если не указан.</summary>
    public string? Uuid =>
        Guid.TryParse(tbUuid.Text.Trim(), out var g) ? g.ToString("D").ToUpperInvariant() : null;

    /// <summary>Конструктор для дизайнера.</summary>
    public fDeviceIdentityDialog()
    {
        InitializeComponent();
    }

    public fDeviceIdentityDialog(
        string deviceTitle,
        string? sid, bool sidInDb,
        string? uuid, bool uuidInDb, bool uuidUsable,
        string defaultSn,
        int deviceTypeId,
        string note) : this()
    {
        _sidInDb = sidInDb;
        _uuidInDb = uuidInDb;
        _uuidUsable = uuidUsable;

        lblHeader.Text = $"Устройство {deviceTitle}: этих данных нет в БД. Добавить?";
        lblNote.Text = note ?? "";

        tbSid.Text = sid ?? "";
        tbSid.ReadOnly = sidInDb;
        lblSid.Text = sidInDb ? "SID (есть в БД):" : "SID:";

        tbSidName.Text = sid != null ? $"SID {sid}" : "";
        tbSidName.Enabled = !sidInDb;

        tbSidComment.Text = $"Добавлено с устройства {deviceTitle}";
        tbSidName.Enabled = !sidInDb;

        tbUuid.Text = uuid ?? "";
        tbUuid.ReadOnly = uuidInDb;
        lblUuid.Text = uuidInDb ? "UUID (есть в БД):" : "UUID:";

        tbSn.Text = defaultSn;
        tbSn.Enabled = !uuidInDb;

        if(!uuidUsable)
            HideUuidRows();
    }

    /// <summary>UUID не поддерживается (SQLite или неизвестный тип): убираем UUID/SN и подтягиваем кнопки.</summary>
    private void HideUuidRows()
    {
        int shift = tbSn.Bottom - tbSidName.Bottom;

        lblUuid.Visible = tbUuid.Visible = false;
        lblSn.Visible = tbSn.Visible = false;

        btnAdd.Top -= shift;
        btnCancel.Top -= shift;
        ClientSize = new Size(ClientSize.Width, ClientSize.Height - shift);
    }

    private void btnAdd_Click(object? sender, EventArgs e)
    {
        string? error = GetValidationError();

        if(error != null)
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private string? GetValidationError()
    {
        if(!_sidInDb)
        {
            if(!SidRegex.IsMatch(Sid))
                return "SID должен состоять из 8 шестнадцатеричных символов (0-9, A-F).";

            if(SidName.Length == 0)
                return "Укажите название SID.";
        }

        bool addUuid = _uuidUsable && !_uuidInDb && tbUuid.Text.Trim().Length > 0;

        if(addUuid)
        {
            if(!Guid.TryParse(tbUuid.Text.Trim(), out _))
                return "UUID указан в неверном формате.";

            if(Sn.Length == 0)
                return "Укажите SN.";
        }

        if(_sidInDb && !addUuid)
            return "Нечего добавлять: укажите UUID.";

        return null;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        string randomSid = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
        string md5Sid = Convert.ToHexString(MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(randomSid)))[..2];

        tbSid.Text = md5Sid + randomSid;

        tbSidName.Text = $"SID {md5Sid + randomSid}";
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        string sn = tbSn.Text.Trim();

        if(!Regex.IsMatch(sn, @"^SN :\d{3}-\d{4}-\d{6}$"))
        {
            MessageBox.Show(
                this,
                "Введите SN в формате SN :091-0191-006910.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            tbSn.Focus();
            return;
        }

        tbUuid.Text = GenerateUUID(sn, _deviceTypeId);
    }

    private static string GenerateUUID(string sn, int deviceTypeId)
    {
        string source = $"{sn.Trim().ToUpperInvariant()}|{deviceTypeId}";

        byte[] hash = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(source));

        byte[] uuidBytes = hash[..16];

        // UUID версии 8 (пользовательский алгоритм на SHA-256).
        uuidBytes[7] = (byte)((uuidBytes[7] & 0x0F) | 0x80);
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);

        return new Guid(uuidBytes).ToString("D").ToUpperInvariant();
    }
}
