#nullable enable
using AdlpLib.Configuration.Application;
using AdlpLib.Persistence.Entities;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices
{
    /// <summary>
    /// Управление справочником listSID.
    /// PKlistSID генерируется существующим BEFORE INSERT-триггером MySQL.
    /// </summary>
    public partial class ListSidManagerForm : Form
    {
        private readonly DeviceConfigRepository _repository;
        private BindingList<ListSidEntity> _sidItems = new();
        private bool _loading;

        /// <summary>Ключ последней созданной записи. Можно использовать для выбора в ComboBox.</summary>
        public string? CreatedPkListSid { get; private set; }

        public ListSidManagerForm(DeviceConfigRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            InitializeComponent();
        }

        private async void ListSidManagerForm_Load(object? sender, EventArgs e)
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

                var records = await _repository.GetListSidsAsync();
                _sidItems = new BindingList<ListSidEntity>(records);
                _gridSids.DataSource = _sidItems;

                if(!string.IsNullOrWhiteSpace(selectKey))
                {
                    foreach(DataGridViewRow row in _gridSids.Rows)
                    {
                        if(row.DataBoundItem is ListSidEntity item &&
                           string.Equals(item.PkListSid, selectKey, StringComparison.OrdinalIgnoreCase))
                        {
                            row.Selected = true;
                            _gridSids.CurrentCell = row.Cells[0];
                            break;
                        }
                    }
                }

                _lblCount.Text = $"Записей: {_sidItems.Count}";
                _lblStatus.Text = "Справочник загружен.";
            }
            catch(Exception ex)
            {
                _lblStatus.Text = "Не удалось загрузить справочник.";
                MessageBox.Show(
                    this,
                    "Не удалось загрузить список SID:\r\n" + ex.GetBaseException().Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
                UseWaitCursor = false;
                _btnRefresh.Enabled = true;
                _btnCreate.Enabled = true;
            }
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            if(_loading)
                return;

            string name = _txtNameObjSid.Text.Trim();
            string comment = _txtComment.Text.Trim();

            if(string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Укажите название NameObjSID.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNameObjSid.Focus();
                return;
            }

            if(name.Length > 64)
            {
                MessageBox.Show(this, "NameObjSID не должен превышать 64 символа.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNameObjSid.Focus();
                return;
            }

            if(string.IsNullOrWhiteSpace(comment))
            {
                MessageBox.Show(this, "Укажите комментарий.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtComment.Focus();
                return;
            }

            if(comment.Length > 255)
            {
                MessageBox.Show(this, "Комментарий не должен превышать 255 символов.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtComment.Focus();
                return;
            }

            try
            {
                UseWaitCursor = true;
                _btnCreate.Enabled = false;
                _btnRefresh.Enabled = false;
                _lblStatus.Text = "Создание записи…";

                // PKlistSID и regWebUser/DateRec создаются существующими механизмами БД.
                var created = await _repository.CreateListSidAsync(name, comment);
                CreatedPkListSid = created.PkListSid;

                _txtNameObjSid.Clear();
                _txtComment.Clear();

                await ReloadAsync(CreatedPkListSid);
                _lblStatus.Text = $"Создан SID: {CreatedPkListSid}";
                _txtNameObjSid.Focus();
            }
            catch(Exception ex)
            {
                _lblStatus.Text = "Ошибка создания SID.";
                MessageBox.Show(
                    this,
                    "Не удалось создать запись SID:\r\n" + ex.GetBaseException().Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            await ReloadAsync(CreatedPkListSid);
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
