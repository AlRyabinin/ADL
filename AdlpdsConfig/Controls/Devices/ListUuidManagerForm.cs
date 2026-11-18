#nullable enable
using AdlpLib.Configuration.Application;
using AdlpLib.Persistence.Entities;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    /// <summary>
    /// Управление записями listUUID для выбранного SID и типа устройства.
    /// Пользователь вводит SN по маске, PKlistUUID генерируется триггером MySQL.
    /// </summary>
    public partial class ListUuidManagerForm : Form
    {
        // Формат SN в БД: "SN :091-0191-006910".
        private const string SnMask = "SN \\:000-0000-000000";

        private readonly DeviceConfigRepository _repository;
        private readonly string _sid;
        private readonly string _configType;
        private BindingList<ListUuidEntity> _items = new();
        private bool _loading;

        /// <summary>Ключ последней созданной записи. Выбирается в списке после закрытия формы.</summary>
        public string? CreatedPkListUuid { get; private set; }

        public ListUuidManagerForm(DeviceConfigRepository repository, string sid, string configType, string? sidCaption = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _sid = sid?.Trim() ?? throw new ArgumentNullException(nameof(sid));
            _configType = configType ?? throw new ArgumentNullException(nameof(configType));

            InitializeComponent();

            _mtbSn.Mask = SnMask;
            _mtbSn.AsciiOnly = true;
            _mtbSn.PromptChar = '_';
            _mtbSn.TextMaskFormat = MaskFormat.IncludeLiterals;

            _lblContext.Text = $"SID: {sidCaption ?? _sid}     Тип устройства: {_configType}";
        }

        private async void ListUuidManagerForm_Load(object? sender, EventArgs e)
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync(string? selectKey = null)
        {
            try
            {
                _loading = true;
                UseWaitCursor = true;
                _btnRefresh.Enabled = false;
                _btnCreate.Enabled = false;

                var records = await _repository.GetListUuidsAsync(_sid, _configType);
                _items = new BindingList<ListUuidEntity>(records);
                _gridUuids.DataSource = _items;

                if(!string.IsNullOrWhiteSpace(selectKey))
                {
                    foreach(DataGridViewRow row in _gridUuids.Rows)
                    {
                        if(row.DataBoundItem is ListUuidEntity item &&
                           string.Equals(item.PkListUuid?.Trim(), selectKey.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            row.Selected = true;
                            _gridUuids.CurrentCell = row.Cells[0];
                            break;
                        }
                    }
                }

                _lblCount.Text = $"Записей: {_items.Count}";
                _lblStatus.Text = "Справочник загружен.";
            }
            catch(Exception ex)
            {
                _lblStatus.Text = "Не удалось загрузить справочник.";
                MessageBox.Show(this,
                    "Не удалось загрузить список UUID:\r\n" + ex.GetBaseException().Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
                UseWaitCursor = false;
                _btnRefresh.Enabled = true;
                _btnCreate.Enabled = true;
            }
        }

        private void GridUuids_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            void Setup(string name, string? header)
            {
                var col = _gridUuids.Columns[name];
                if(col == null) return;
                if(header == null) col.Visible = false;
                else col.HeaderText = header;
            }

            Setup(nameof(ListUuidEntity.PkListUuid), "UUID");
            Setup(nameof(ListUuidEntity.Sn), "SN");
            Setup(nameof(ListUuidEntity.NumBlock), "№");
            Setup(nameof(ListUuidEntity.RegWebUser), "Создал");
            Setup(nameof(ListUuidEntity.DateRec), "Дата создания");

            Setup(nameof(ListUuidEntity.FkListSid), null);
            Setup(nameof(ListUuidEntity.FkTypeDevices), null);
            Setup(nameof(ListUuidEntity.IdTxtDCon), null);
            Setup(nameof(ListUuidEntity.Mac), null);
            Setup(nameof(ListUuidEntity.Ip), null);
            Setup(nameof(ListUuidEntity.Port), null);
            Setup(nameof(ListUuidEntity.Comment), null);
            Setup(nameof(ListUuidEntity.DateRecOn), null);
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            if(_loading)
                return;

            if(!_mtbSn.MaskCompleted)
            {
                MessageBox.Show(this, "Введите SN полностью: SN :NNN-NNNN-NNNNNN",
                    "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _mtbSn.Focus();
                return;
            }

            string sn = _mtbSn.Text.Trim();

            try
            {
                UseWaitCursor = true;
                _btnCreate.Enabled = false;
                _btnRefresh.Enabled = false;
                _lblStatus.Text = "Создание записи…";

                var created = await _repository.CreateListUuidAsync(_sid, _configType, sn);
                CreatedPkListUuid = created.PkListUuid?.Trim();

                _mtbSn.Clear();

                await ReloadAsync(CreatedPkListUuid);
                _lblStatus.Text = $"Создан UUID: {CreatedPkListUuid}";
                _mtbSn.Focus();
            }
            catch(Exception ex)
            {
                _lblStatus.Text = "Ошибка создания UUID.";
                MessageBox.Show(this,
                    "Не удалось создать запись UUID:\r\n" + ex.GetBaseException().Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _btnCreate.Enabled = true;
                _btnRefresh.Enabled = true;
            }
        }

        private async void BtnRefresh_Click(object? sender, EventArgs e)
        {
            await ReloadAsync(CreatedPkListUuid);
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
