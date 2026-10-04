using AdlpLib.Configuration.Application;

namespace AdlpDBConfigurator
{
    /// <summary>
    /// Главное окно конфигуратора: выбор типа СУБД, ввод параметров подключения,
    /// проверка подключения и сохранение конфигурации.
    /// </summary>
    partial class MainForm : Form
    {
        private readonly ComboBox _dbTypeCombo = new();

        // --- SQLite ---
        private readonly GroupBox _sqliteGroup = new();
        private readonly TextBox _sqlitePathBox = new();
        private readonly Button _browseBtn = new();

        // --- MySQL ---
        private readonly GroupBox _mySqlGroup = new();
        private readonly TextBox _serverBox = new();
        private readonly NumericUpDown _portBox = new();
        private readonly TextBox _databaseBox = new();
        private readonly TextBox _userBox = new();
        private readonly TextBox _passwordBox = new();
        private readonly CheckBox _integratedSecurityCheck = new();

        // --- Общие ---
        private readonly Button _testBtn = new();
        private readonly Button _saveBtn = new();
        private readonly Button _cancelBtn = new();
        private readonly LinkLabel _openFolderLink = new();
        private readonly Label _statusLabel = new();

        private AppConfig _config = new();

        /// <summary>
        /// Создаёт окно, строит интерфейс и загружает текущую конфигурацию с диска.
        /// </summary>
        public MainForm()
        {
            Text = "Настройка подключения к БД";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            // Высота увеличена под вторую группу настроек
            ClientSize = new Size(560, 380);

            BuildUi();
            ApplyTypeVisibility();
            LoadConfig();
        }

        /// <summary>
        /// Создаёт и размещает элементы управления на форме.
        /// Порядок сверху вниз: тип СУБД → параметры (SQLite/MySQL) → ссылка на папку → статус → кнопки.
        /// </summary>
        private void BuildUi()
        {
            var typeLabel = new Label
            {
                Text = "Тип БД:",
                Location = new Point(15, 20),
                AutoSize = true
            };

            _dbTypeCombo.Location = new Point(120, 17);
            _dbTypeCombo.Width = 200;
            _dbTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _dbTypeCombo.Items.Clear();
            foreach(var t in Enum.GetValues<DatabaseType>())
                _dbTypeCombo.Items.Add(t);
            _dbTypeCombo.SelectedIndexChanged += (_, _) => ApplyTypeVisibility();

            BuildSqliteGroup();
            BuildMySqlGroup();

            _openFolderLink.Text = $"Папка конфигурации: {AppConfigService.ConfigDirectory}";
            _openFolderLink.Location = new Point(15, 300);
            _openFolderLink.AutoSize = true;
            _openFolderLink.LinkClicked += (_, _) => OpenConfigFolder();

            _statusLabel.Location = new Point(15, 325);
            _statusLabel.Size = new Size(530, 22);
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.AutoSize = false;

            _testBtn.Text = "Проверить подключение";
            _testBtn.Location = new Point(15, 350);
            _testBtn.Width = 180;
            _testBtn.Click += (_, _) => TestConnection();

            _cancelBtn.Text = "Отмена";
            _cancelBtn.Location = new Point(300, 350);
            _cancelBtn.Width = 110;
            _cancelBtn.Click += (_, _) => Close();

            _saveBtn.Text = "Сохранить";
            _saveBtn.Location = new Point(420, 350);
            _saveBtn.Width = 110;
            _saveBtn.Click += (_, _) => SaveAndClose();

            Controls.Add(typeLabel);
            Controls.Add(_dbTypeCombo);
            Controls.Add(_sqliteGroup);
            Controls.Add(_mySqlGroup);
            _mySqlGroup.Visible = false;
            Controls.Add(_openFolderLink);
            Controls.Add(_statusLabel);
            Controls.Add(_testBtn);
            Controls.Add(_cancelBtn);
            Controls.Add(_saveBtn);
        }

        /// <summary>
        /// Создаёт группу настроек SQLite.
        /// </summary>
        private void BuildSqliteGroup()
        {
            _sqliteGroup.Text = "SQLite";
            _sqliteGroup.Location = new Point(15, 55);
            _sqliteGroup.Size = new Size(530, 100);

            var pathLabel = new Label
            {
                Text = "Файл БД:",
                Location = new Point(15, 30),
                AutoSize = true
            };

            _sqlitePathBox.Location = new Point(15, 55);
            _sqlitePathBox.Width = 410;

            _browseBtn.Text = "...";
            _browseBtn.Location = new Point(435, 54);
            _browseBtn.Width = 40;
            _browseBtn.Click += (_, _) => BrowseSqliteFile();

            _sqliteGroup.Controls.Add(pathLabel);
            _sqliteGroup.Controls.Add(_sqlitePathBox);
            _sqliteGroup.Controls.Add(_browseBtn);
        }

        /// <summary>
        /// Создаёт группу настроек MySQL.
        /// </summary>
        private void BuildMySqlGroup()
        {
            _mySqlGroup.Text = "MySQL";
            _mySqlGroup.Location = new Point(15, 55);
            _mySqlGroup.Size = new Size(530, 235);

            var serverLabel = new Label
            {
                Text = "Сервер:",
                Location = new Point(15, 30),
                AutoSize = true
            };

            _serverBox.Location = new Point(130, 27);
            _serverBox.Width = 260;

            var portLabel = new Label
            {
                Text = "Порт:",
                Location = new Point(400, 30),
                AutoSize = true
            };

            _portBox.Location = new Point(445, 27);
            _portBox.Width = 70;
            _portBox.Minimum = 1;
            _portBox.Maximum = 65535;
            _portBox.Value = 3306;

            var dbLabel = new Label
            {
                Text = "База данных:",
                Location = new Point(15, 65),
                AutoSize = true
            };

            _databaseBox.Location = new Point(130, 62);
            _databaseBox.Width = 385;

            var userLabel = new Label
            {
                Text = "Пользователь:",
                Location = new Point(15, 100),
                AutoSize = true
            };

            _userBox.Location = new Point(130, 97);
            _userBox.Width = 385;

            var passwordLabel = new Label
            {
                Text = "Пароль:",
                Location = new Point(15, 135),
                AutoSize = true
            };

            _passwordBox.Location = new Point(130, 132);
            _passwordBox.Width = 385;
            _passwordBox.UseSystemPasswordChar = true;

            _integratedSecurityCheck.Text = "Вход по учётной записи Windows";
            _integratedSecurityCheck.Location = new Point(130, 165);
            _integratedSecurityCheck.AutoSize = true;

            _mySqlGroup.Controls.Add(serverLabel);
            _mySqlGroup.Controls.Add(_serverBox);
            _mySqlGroup.Controls.Add(portLabel);
            _mySqlGroup.Controls.Add(_portBox);
            _mySqlGroup.Controls.Add(dbLabel);
            _mySqlGroup.Controls.Add(_databaseBox);
            _mySqlGroup.Controls.Add(userLabel);
            _mySqlGroup.Controls.Add(_userBox);
            _mySqlGroup.Controls.Add(passwordLabel);
            _mySqlGroup.Controls.Add(_passwordBox);
            _mySqlGroup.Controls.Add(_integratedSecurityCheck);
        }

        /// <summary>
        /// Загружает конфигурацию с диска и переносит её значения в элементы управления.
        /// </summary>
        private void LoadConfig()
        {
            _config = AppConfigService.Load();

            _dbTypeCombo.SelectedItem = _config.DatabaseType;
            if(_dbTypeCombo.SelectedIndex < 0)
                _dbTypeCombo.SelectedIndex = 0;

            // SQLite
            _sqlitePathBox.Text = _config.Sqlite.DatabasePath;

            // MySQL
            _serverBox.Text = _config.MySql.Server;
            _portBox.Value = _config.MySql.Port;
            _databaseBox.Text = _config.MySql.Database;
            _userBox.Text = _config.MySql.User;
            _passwordBox.Text = _config.MySql.Password;
            _integratedSecurityCheck.Checked = _config.MySql.IntegratedSecurity;

            ApplyTypeVisibility();
        }

        /// <summary>
        /// Показывает секцию настроек, соответствующую выбранному типу СУБД.
        /// </summary>
        private void ApplyTypeVisibility()
        {
            var selected = _dbTypeCombo.SelectedItem is DatabaseType t
        ? t
        : DatabaseType.SQLite;

            _sqliteGroup.Visible = selected == DatabaseType.SQLite;
            _mySqlGroup.Visible = selected == DatabaseType.MySQL;
        }

        /// <summary>
        /// Открывает диалог выбора файла БД SQLite и записывает результат в поле пути.
        /// </summary>
        private void BrowseSqliteFile()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Выберите файл базы данных SQLite",
                Filter = "SQLite DB (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*",
                FileName = string.IsNullOrWhiteSpace(_sqlitePathBox.Text)
                    ? "data.db"
                    : Path.GetFileName(_sqlitePathBox.Text),
                InitialDirectory = SafeDirOf(_sqlitePathBox.Text)
                    ?? AppConfigService.ConfigDirectory,
                OverwritePrompt = false
            };

            if(dlg.ShowDialog(this) == DialogResult.OK)
                _sqlitePathBox.Text = dlg.FileName;
        }

        /// <summary>
        /// Пытается получить существующий каталог из указанного пути.
        /// </summary>
        private static string? SafeDirOf(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                return Directory.Exists(dir) ? dir : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Проверяет подключение с текущими параметрами и выводит результат в статусную строку.
        /// </summary>
        private void TestConnection()
        {
            var cfg = CollectFromUi();
            var (ok, msg) = DatabaseTester.Test(cfg);
            _statusLabel.ForeColor = ok ? Color.Green : Color.Firebrick;
            _statusLabel.Text = msg;
        }

        /// <summary>
        /// Собирает конфигурацию из элементов управления, сохраняет её,
        /// показывает результат и закрывает окно при успехе.
        /// </summary>
        private void SaveAndClose()
        {
            try
            {
                var cfg = CollectFromUi();
                AppConfigService.Save(cfg);

                _statusLabel.ForeColor = Color.Green;
                _statusLabel.Text = "Конфигурация сохранена.";

                MessageBox.Show(
                    this,
                    "Конфигурация успешно сохранена.",
                    "Сохранение",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                _statusLabel.ForeColor = Color.Firebrick;
                _statusLabel.Text = "Ошибка сохранения.";

                MessageBox.Show(
                    this,
                    $"Не удалось сохранить конфигурацию:\n{ex.Message}",
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Формирует объект конфигурации по значениям, введённым пользователем.
        /// </summary>
        /// <returns>Новая конфигурация на основе текущего состояния UI.</returns>
        private AppConfig CollectFromUi() => new()
        {
            DatabaseType = (DatabaseType)_dbTypeCombo.SelectedItem!,
            Sqlite = new SqliteSettings
            {
                DatabasePath = _sqlitePathBox.Text.Trim()
            },
            MySql = new MySqlSettings
            {
                Server = _serverBox.Text.Trim(),
                Port = (int)_portBox.Value,
                Database = _databaseBox.Text.Trim(),
                User = _userBox.Text.Trim(),
                Password = _passwordBox.Text,
                IntegratedSecurity = _integratedSecurityCheck.Checked
            }
        };

        /// <summary>
        /// Открывает каталог с файлом конфигурации в проводнике.
        /// </summary>
        private void OpenConfigFolder()
        {
            Directory.CreateDirectory(AppConfigService.ConfigDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppConfigService.ConfigDirectory,
                UseShellExecute = true
            });
        }
    }
}