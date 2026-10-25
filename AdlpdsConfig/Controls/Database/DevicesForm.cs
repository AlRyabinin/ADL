using AdlpLib.Configuration.Application;
using AdlpLib.Models.Devices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AdlpdsConfig.Controls.Database
{
    /// <summary>
    /// Окно со списком сохранённых устройств: просмотр, импорт из JSON,
    /// экспорт в JSON и удаление.
    /// </summary>
    public class DevicesForm : Form
    {
        private readonly DataGridView _grid = new();
        private readonly Button _loadBtn = new();
        private readonly Button _saveBtn = new();
        private readonly Button _deleteBtn = new();
        private readonly Button _refreshBtn = new();
        private readonly Button _closeBtn = new();
        private readonly Label _countLabel = new();
        private readonly Button _detailsBtn = new();

        private readonly DeviceConfigRepository? _repo;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DevicesForm(DeviceConfigRepository? repo)
        {
            _repo = repo;

            Text = "Список устройств";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 460);
            MinimumSize = new Size(640, 360);
            ShowInTaskbar = false;

            BuildUi();
            Shown += async (_, _) => await LoadDataAsync();
        }

        // =====================================================================
        //  UI
        // =====================================================================

        private void BuildUi()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 48 };

            _loadBtn.Text = "Загрузить…";
            _loadBtn.Width = 110;
            _loadBtn.Location = new Point(12, 10);
            _loadBtn.Click += async (_, _) => await ImportFromJsonAsync();

            _saveBtn.Text = "Сохранить…";
            _saveBtn.Width = 110;
            _saveBtn.Location = new Point(130, 10);
            _saveBtn.Click += async (_, _) => await ExportSelectedToJsonAsync();

            _deleteBtn.Text = "Удалить";
            _deleteBtn.Width = 100;
            _deleteBtn.Location = new Point(248, 10);
            _deleteBtn.Click += async (_, _) => await DeleteSelectedAsync();

            _refreshBtn.Text = "Обновить";
            _refreshBtn.Width = 100;
            _refreshBtn.Location = new Point(356, 10);
            _refreshBtn.Click += async (_, _) => await LoadDataAsync();

            _detailsBtn.Text = "Открыть";
            _detailsBtn.Width = 100;
            _detailsBtn.Location = new Point(464, 10);
            _detailsBtn.Click += async (_, _) => await OpenDetailsAsync();

            topPanel.Controls.Add(_detailsBtn);
            topPanel.Controls.Add(_loadBtn);
            topPanel.Controls.Add(_saveBtn);
            topPanel.Controls.Add(_deleteBtn);
            topPanel.Controls.Add(_refreshBtn);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionChanged += (_, _) => UpdateButtons();
            _grid.CellDoubleClick += async (_, _) => await OpenDetailsAsync();
            _grid.CellFormatting += Grid_CellFormatting;

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                DataPropertyName = "Id",
                FillWeight = 8
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ConfigType",
                HeaderText = "Тип",
                FillWeight = 12
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Mac",
                HeaderText = "MAC-адрес",
                DataPropertyName = "Mac",
                FillWeight = 26
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ip",
                HeaderText = "IP-адрес",
                DataPropertyName = "Ip",
                FillWeight = 20
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Mask",
                HeaderText = "Маска",
                DataPropertyName = "Mask",
                FillWeight = 18
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Gateway",
                HeaderText = "Шлюз",
                DataPropertyName = "Gateway",
                FillWeight = 18
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Block",
                HeaderText = "Блок",
                DataPropertyName = "Block",
                FillWeight = 8
            });

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            _countLabel.AutoSize = true;
            _countLabel.ForeColor = Color.DimGray;
            _countLabel.Location = new Point(12, 14);
            _countLabel.Text = "Записей: 0";

            _closeBtn.Text = "Закрыть";
            _closeBtn.Width = 110;
            _closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _closeBtn.Location = new Point(bottomPanel.Width - 122, 8);
            _closeBtn.Click += (_, _) => Close();
            bottomPanel.Resize += (_, _) =>
                _closeBtn.Location = new Point(bottomPanel.Width - 122, 8);

            bottomPanel.Controls.Add(_countLabel);
            bottomPanel.Controls.Add(_closeBtn);

            Controls.Add(_grid);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _loadBtn.Enabled = !busy && _repo != null;
            _refreshBtn.Enabled = !busy && _repo != null;
            _detailsBtn.Enabled = !busy && _repo != null && _grid.CurrentRow != null;
            _saveBtn.Enabled = !busy && _repo != null && _grid.CurrentRow != null;
            _deleteBtn.Enabled = !busy && _repo != null && _grid.CurrentRow != null;
        }

        private void UpdateButtons()
        {
            bool hasRepo = _repo != null;
            bool hasSelect = _grid.CurrentRow != null &&
                             _grid.CurrentRow.DataBoundItem is DeviceHeader;

            _detailsBtn.Enabled = hasRepo && hasSelect;
            _loadBtn.Enabled = hasRepo;
            _saveBtn.Enabled = hasRepo && hasSelect;
            _deleteBtn.Enabled = hasRepo && hasSelect;
            _refreshBtn.Enabled = hasRepo;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if(e.RowIndex < 0) return;
            if(_grid.Columns[e.ColumnIndex].Name != "ConfigType") return;

            var row = _grid.Rows[e.RowIndex];
            if(row.DataBoundItem is not DeviceHeader h) return;

            e.Value = DisplayTypeName(h.ConfigType);
            e.FormattingApplied = true;
        }

        internal static string DisplayTypeName(string configType) =>
            configType switch
        {
            "EXEC" => "IKB",
            "DIM220" => "DIM220",
            "DIMCABINET" => "Dimmer cabinet",
            "PU" => "PU",
            _ => configType
        };

        // =====================================================================
        //  ЗАГРУЗКА СПИСКА
        // =====================================================================

        private async Task LoadDataAsync()
        {
            if(_repo == null)
            {
                _grid.DataSource = null;
                _countLabel.ForeColor = Color.Firebrick;
                _countLabel.Text = "Нет подключения к БД";
                UpdateButtons();
                return;
            }

            SetBusy(true);
            try
            {
                var headers = await _repo.GetAllHeadersAsync();
                _grid.DataSource = headers;
                _countLabel.ForeColor = Color.DimGray;
                _countLabel.Text = $"Записей: {headers.Count}";
            }
            catch(Exception ex)
            {
                _grid.DataSource = null;
                _countLabel.ForeColor = Color.Firebrick;
                _countLabel.Text = "Ошибка загрузки";
                MessageBox.Show(this,
                    $"Не удалось загрузить список устройств:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdateButtons();
            }
        }

        // =====================================================================
        //  ИМПОРТ ИЗ JSON
        // =====================================================================

        private async Task ImportFromJsonAsync()
        {
            if(_repo == null) return;

            using var dlg = new OpenFileDialog
            {
                Title = "Импорт конфигураций устройств",
                Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if(dlg.ShowDialog(this) != DialogResult.OK) return;

            int added = 0, updated = 0, failed = 0;
            var errors = new StringBuilder();

            SetBusy(true);
            try
            {
                foreach(var path in dlg.FileNames)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(path);

                        var cfg = JsonSerializer.Deserialize<DeviceConfigDtoBase>(json, JsonOptions)
                                  ?? throw new InvalidOperationException(
                                        "Не удалось разобрать конфигурацию.");

                        if(string.IsNullOrWhiteSpace(cfg.Mac))
                            throw new InvalidOperationException("В файле не указан MAC.");

                        string configType = DeviceConfigRepository.ConfigTypeOf(cfg);
                        var existingId = await _repo.FindDeviceIdByMacAsync(cfg.Mac, configType);

                        bool isUpdate = existingId.HasValue;
                        if(isUpdate) cfg.Id = existingId!.Value;

                        await _repo.SaveAsync(cfg);

                        if(isUpdate) updated++; else added++;
                    }
                    catch(Exception ex)
                    {
                        failed++;
                        errors.AppendLine($"• {Path.GetFileName(path)}: {ex.Message}");
                    }
                }
            }
            finally
            {
                SetBusy(false);
            }

            await LoadDataAsync();

            var summary = new StringBuilder();
            summary.AppendLine("Импорт завершён.");
            summary.AppendLine();
            summary.AppendLine($"Добавлено:  {added}");
            summary.AppendLine($"Обновлено:  {updated}");
            summary.AppendLine($"С ошибками: {failed}");

            if(failed > 0)
            {
                summary.AppendLine();
                summary.AppendLine("Детали ошибок:");
                summary.Append(errors);
            }

            MessageBox.Show(this,
                summary.ToString(),
                "Импорт конфигураций",
                MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private async Task OpenDetailsAsync()
        {
            if(_repo == null) return;
            if(_grid.CurrentRow?.DataBoundItem is not DeviceHeader header) return;

            DeviceConfigDtoBase? cfg;
            SetBusy(true);
            try
            {
                cfg = await _repo.LoadAsync(header.Id);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, $"Не удалось прочитать конфигурацию:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetBusy(false);
            }

            if(cfg == null)
            {
                MessageBox.Show(this, "Устройство не найдено в БД.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new DeviceDetailsForm(cfg);
            dlg.ShowDialog(this);
        }

        private async Task ExportSelectedToJsonAsync()
        {
            if(_repo == null) return;
            if(_grid.CurrentRow?.DataBoundItem is not DeviceHeader header) return;

            DeviceConfigDtoBase? cfg;
            SetBusy(true);
            try
            {
                cfg = await _repo.LoadAsync(header.Id);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось прочитать конфигурацию из БД:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetBusy(false);
            }

            if(cfg == null)
            {
                MessageBox.Show(this, "Устройство не найдено в БД.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Сохранить конфигурацию",
                Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
                DefaultExt = "json",
                AddExtension = true,
                FileName = $"Config_{cfg.GetType().Name.Replace("ConfigDto", "")}_" +
                           $"{DateTime.Now:yyyyMMdd_HHmmss}.json",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if(dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var json = JsonSerializer.Serialize<DeviceConfigDtoBase>(cfg, JsonOptions);
                await File.WriteAllTextAsync(dlg.FileName, json);

                MessageBox.Show(this,
                    $"Конфигурация сохранена:\n{dlg.FileName}",
                    "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось сохранить файл:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if(_repo == null) return;
            if(_grid.CurrentRow?.DataBoundItem is not DeviceHeader header) return;

            var confirm = MessageBox.Show(this,
                $"Удалить устройство?\n\n" +
                $"Тип: {header.ConfigType}\n" +
                $"MAC: {header.Mac}\n" +
                $"IP:  {header.Ip}\n\n" +
                "Все связанные каналы, группы, картины, кнопки и потоки будут удалены.",
                "Удаление",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if(confirm != DialogResult.Yes) return;

            SetBusy(true);
            try
            {
                await _repo.DeleteAsync(header.Id);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось удалить устройство:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetBusy(false);
            }

            await LoadDataAsync();
        }
    }
}