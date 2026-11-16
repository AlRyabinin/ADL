using AdlpdsConfig.Controls;
using AdlpdsConfig.Controls.Database;
using AdlpdsConfig.Controls.DevicesGroup;
using AdlpLib.Configuration.Application;
using AdlpLib.Core;
using AdlpLib.Devices;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using AdlpLib.Models.DevicesGroup;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;

namespace AdlpdsConfig
{
    public partial class fMain : Form
    {
        private Service.AdlpdsService Svc;

        /// <summary>
        /// Кэш поддержки бэкапа по ключу устройства (итог автопробы).
        /// Yes/No запоминаются, пока устройство живо; перепроба — только по явному действию пользователя.
        /// </summary>
        private readonly Dictionary<string, bool> mBackupSupport = new();

        /// <summary>
        /// Устройства, для которых автопроба бэкапа уже запущена (повторно не стартуем)
        /// </summary>
        private readonly HashSet<string> mBackupProbed = new();

        /// <summary>Активная база конфигураций (пресеты устройств); null — не выбрана</summary>
        private IDeviceConfigStore Store;

        /// <summary>
        /// Ключ хранилища (DbKey), под которым строка списка была создана.
        /// При смене UUID устройства пресеты переносятся на новый ключ.
        /// </summary>
        private readonly Dictionary<string, Guid> mRowDbKey = new();

        AppConfig _appConfig = AppConfigService.Load();

        private DeviceConfigRepository? _repo;

        private DevicesGroupRepository? _devicesGroupRepository;

        public fMain()
        {
            InitializeComponent();

            miSetMac.Enabled = false;
            miSetNet.Enabled = false;
            miSetName.Enabled = false;
            miSetUuid.Enabled = false;
            miBackup.Enabled = false;
            miFirmware.Enabled = false;
            miMulticastConfiguration.Enabled = false;

            InitService();
        }

        private void InitService()
        {
            var args = Environment.GetCommandLineArgs();
            var cfgArgs = (args.Length > 1) ? args.Skip(1).ToArray() : Array.Empty<string>();

            Network.NetworkInterface NI = null;
            try
            {
                var cfg = AdlpLib.Configuration.Configurations.Load(cfgArgs, out _);
                NI = cfg.Network.CreateInterface();
            }
            catch(FileNotFoundException)
            {
            }

            if(NI == null)
            {
                using var dlg = new fNetSelectDialog();
                if(dlg.ShowDialog(this) != DialogResult.OK)
                    Environment.Exit(0);
                NI = dlg.SelectedInterface!;
            }
            var N = new Net(NI);

            Svc = new Service.AdlpdsService(N);

            tTimer.Start();
        }
        private void StopService()
        {
            tTimer.Stop();
            Svc?.Dispose();
        }

        /// <summary>Стабильный ключ устройства для базы (UUID, или детерминированный по адресу)</summary>
        private static Guid DbKey(Device d)
        {
            if(d.Uuid != Guid.Empty)
                return d.Uuid;

            using var md5 = MD5.Create();
            string src = "ADLDS-CFG:" + (d.Endpoint?.ToString() ?? "?");
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(src)));
        }

        /// <summary>Идентификатор устройства для списка: адрес уникален и стабилен, в отличие от UUID</summary>
        private static string EndpointKey(Device d) => $"{d.Endpoint}|{d.Uuid:N}|{d.Block}";

        private string GetSelectedDeviceKey()
        {
            if(lDevices.SelectedItems.Count == 0) return null;
            return (string)lDevices.SelectedItems[0].Tag;
        }

        private Device? GetSelectedDevice()
        {
            var devKey = GetSelectedDeviceKey();
            if(devKey == null) return null;
            return Svc.GetDevices().FirstOrDefault(d => EndpointKey(d) == devKey);
        }

        private async void miSaveConfigurationToDb_Click(object sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null)
                return;

            if(_repo == null)
            {
                MessageBox.Show("Нет подключения к БД конфигураций.");
                return;
            }

            using var dlg = new fConfSaver(Svc, device)
            {
                StartPosition = FormStartPosition.CenterParent
            };

            if(dlg.ShowDialog(this) != DialogResult.OK)
                return;

            if(!dlg.ConfigurationLoaded)
            {
                MessageBox.Show(
                    "Конфигурация не была успешно прочитана. Сохранение отменено.",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var config = dlg.ResultConfig;
            if(config == null)
                return;

            try
            {
                long? existingId = await FindExistingDeviceIdAsync(config);

                if(existingId.HasValue)
                {
                    var existingConfig = await _repo.LoadAsync(existingId.Value);

                    config.Id = existingId.Value;

                    // Не затираем назначенный SID и имя устройства.
                    if(existingConfig != null)
                    {
                        config.PkListSid = existingConfig.PkListSid;

                        if(!string.IsNullOrWhiteSpace(existingConfig.Name))
                            config.Name = existingConfig.Name;
                    }
                }
                else if((!config.Uuid.HasValue || config.Uuid.Value == Guid.Empty) &&
                        string.IsNullOrWhiteSpace(config.Mac))
                {
                    throw new InvalidOperationException(
                        "Устройство не передало UUID и MAC-адрес.");
                }

                long savedId = await _repo.SaveAsync(config);

                MessageBox.Show(
                    $"Конфигурация сохранена в БД.\n\nID записи: {savedId}",
                    "Сохранение завершено",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка сохранения:\n{ex.GetBaseException().Message}",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task<long?> FindExistingDeviceIdAsync(
    DeviceConfigDtoBase config)
        {
            if(_repo == null)
                return null;

            if(config.Uuid.HasValue && config.Uuid.Value != Guid.Empty)
            {
                long? id = await _repo.FindDeviceIdByListUuidAndBlockAsync(config.PkListUuid, config.Block);

                if(id.HasValue)
                    return id;
            }

            if(!string.IsNullOrWhiteSpace(config.Mac))
            {
                string configType = DeviceConfigRepository.ConfigTypeOf(config);

                var matches = (await _repo.GetAllHeadersAsync())
                    .Where(x =>
                        x.Block == config.Block &&
                        string.Equals(
                            x.ConfigType,
                            configType,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            x.Mac,
                            config.Mac.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();

                if(matches.Count > 1)
                    throw new InvalidOperationException(
                        $"Найдено несколько устройств с MAC {config.Mac}.");

                if(matches.Count == 1)
                    return matches[0].Id;
            }

            return null;
        }

        private void UpdateDatabaseSaveButton()
        {
            if(IsDisposed || Disposing || !IsHandleCreated)
                return;

            if(InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(UpdateDatabaseSaveButton));
                }
                catch(InvalidOperationException)
                {
                    
                }

                return;
            }

            var device = GetSelectedDevice();

            miSaveConfigurationToDb.Enabled =
                _repo != null;
        }

        private void lDevices_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var enabled = lDevices.SelectedItems.Count > 0;
            miSaveConfiguration.Enabled = enabled;
            miLoadConfiguration.Enabled = enabled;
            miSetMac.Enabled = enabled;
            miSetNet.Enabled = enabled;
            miSetName.Enabled = enabled;
            miSetUuid.Enabled = enabled;
            miFirmware.Enabled = enabled;
            miMulticastConfiguration.Enabled = enabled;
            UpdateBackupButton();
        }

        private void UpdateBackupButton()
        {
            if(IsDisposed || Disposing || !IsHandleCreated)
                return;

            if(InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(UpdateBackupButton));
                }
                catch(InvalidOperationException)
                {
                }

                return;
            }

            var devKey = GetSelectedDeviceKey();

            miBackup.Enabled = devKey != null &&
                mBackupSupport.TryGetValue(devKey, out bool supported) &&
                supported;
        }

        private void miSaveConfiguration_Click(object sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fConfSaver(Svc, device)
            {
                StartPosition = FormStartPosition.CenterParent
            };

            if(dlg.ShowDialog(this) == DialogResult.OK)
            {
                var configData = dlg.ResultConfig;
                if(configData == null) return;

                string jsonString = JsonSerializer.Serialize<DeviceConfigDtoBase>(configData, new JsonSerializerOptions
                {
                    WriteIndented = true,

                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                using var saveDialog = new SaveFileDialog();
                saveDialog.Title = "Сохранить конфигурацию";
                saveDialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                saveDialog.DefaultExt = "json";
                saveDialog.AddExtension = true;

                saveDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                string deviceName = device.Name ?? "Device";
                saveDialog.FileName = $"Config_{deviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                if(saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string fullPath = saveDialog.FileName;

                    try
                    {
                        File.WriteAllText(fullPath, jsonString);

                        MessageBox.Show($"Конфигурация успешно сохранена по пути:\n{fullPath}",
                            "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void miLoadConfiguration_Click(object sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var openDialog = new OpenFileDialog();
            openDialog.Title = "Загрузить конфигурацию";
            openDialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
            openDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if(openDialog.ShowDialog() != DialogResult.OK) return;

            string json;
            try
            {
                json = File.ReadAllText(openDialog.FileName);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string? fileType = ConfigFile.ReadConfigType(json);

            DeviceConfigDtoBase? cfg;
            try
            {
                cfg = JsonSerializer.Deserialize<DeviceConfigDtoBase>(json)
                      ?? throw new Exception("Не удалось разобрать конфигурацию.");
            }
            catch(Exception ex)
            {
                string hint = fileType == null
                    ? "Файл не содержит поле configType."
                    : $"Тип в файле: {fileType}.";
                MessageBox.Show($"Ошибка парсинга JSON: {ex.Message}\n{hint}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dlg = new fConfSaver(Svc, device) { StartPosition = FormStartPosition.CenterParent };

            switch(cfg)
            {
                case PuColorConfigDto pu: dlg.LoadFromConfig(pu); break;
                case ExecConfigDto ex: dlg.LoadFromConfig(ex); break;
                //Можно дописать новые
                default:
                    MessageBox.Show($"Тип конфигурации «{cfg.GetType().Name}» не поддерживается.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            if(dlg.ShowDialog(this) == DialogResult.OK)
            {
                var result = dlg.ResultConfig;

                if(result == null) return;

                switch(result)
                {
                    case ExecConfigDto exec:
                        var execCfg = BuildExecConfig(exec);
                        Svc.SetNet(device, exec.Ip, exec.Mask, exec.Gateway);
                        bool sent = Svc.WriteExecConfig(device, execCfg, (dev, ok) =>
                            BeginInvoke(new Action(() =>
                            {
                                if(IsDisposed)
                                    return;
                                MessageBox.Show(ok
                                    ? "Конфигурация записана на устройство"
                                    : "Не удалось записать конфигурацию (таймаут).");
                            })));
                        if(!sent)
                        {
                            MessageBox.Show("Не удалось отправить запрос.", Text,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        break;

                    case PuColorConfigDto pu:
                        var ctrlCfg = BuildPUConfig(pu);
                        Svc.SetNet(device, pu.Ip, pu.Mask, pu.Gateway);
                        bool sentCtrl = Svc.WriteControlConfig(device, ctrlCfg, (dev, ok) =>
                            BeginInvoke(new Action(() =>
                            {
                                if(IsDisposed) return;
                                MessageBox.Show(ok
                                    ? "Конфигурация записана на устройство"
                                    : "Не удалось записать конфигурацию (таймаут).");
                            })));
                        if(!sentCtrl)
                        {
                            MessageBox.Show("Не удалось отправить запрос.", Text,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        break;

                    default:
                        MessageBox.Show(
                            $"Тип «{result.GetType().Name}» не поддерживается.",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
        }

        private LiveExecConfig BuildExecConfig(ExecConfigDto cfg)
        {
            var configuration = new LiveExecConfig { Block = cfg.Block };
            configuration.Channels.AddRange(cfg.Channels);
            configuration.Streams.AddRange(cfg.Streams);
            return configuration;
        }

        private LiveControlConfig BuildPUConfig(PuColorConfigDto cfg)
        {
            var configuration = new LiveControlConfig
            {
                Block = cfg.Block,
                Priority = cfg.Priority,
                PictureSwitchTime = cfg.PictureSwitchTime,
                PageCount = cfg.PageCount,
                SwapX = cfg.SwapX,
                SwapY = cfg.SwapY,
                Brightness = cfg.Brightness,
                BrightnessSleep = cfg.BrightnessSleep,
                SleepTimeout = cfg.SleepTimeout
            };

            configuration.Channels.AddRange(cfg.Channels);
            configuration.Groups.AddRange(cfg.Groups);
            configuration.Pictures.AddRange(cfg.Pictures);
            configuration.Buttons.AddRange(cfg.Buttons);

            return configuration;
        }

        private async void miDevicesGroup_Click(object sender, EventArgs e)
        {
            try
            {
                _devicesGroupRepository ??= new DevicesGroupRepository(
                    AppConfigService.BuildConnectionString(_appConfig),
                    _appConfig.DatabaseType);

                _repo ??= new DeviceConfigRepository(
                    AppConfigService.BuildConnectionString(_appConfig),
                    _appConfig.DatabaseType);

                await _devicesGroupRepository.EnsureSchemaAsync();

                var headers = await _devicesGroupRepository.GetAllHeadersAsync();

                if(headers.Count == 0)
                {
                    var answer = MessageBox.Show(this,
                        "Групп устройств ещё нет.\r\n\r\nСоздать первую группу?",
                        "Группы устройств",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if(answer != DialogResult.Yes)
                        return;

                    string name = AskGroupName("");
                    if(string.IsNullOrWhiteSpace(name))
                        return;

                    await _devicesGroupRepository.SaveAsync(new DevicesGroupDto { Name = name });
                }

                using var frm = new miDevicesGroup(_devicesGroupRepository, _repo);
                frm.ShowDialog(this);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось открыть группы устройств:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Спрашивает имя группы. Возвращает null/пустую строку, если отменили.</summary>
        private string AskGroupName(string initial)
        {
            using var dlg = new GroupNameDialog(initial);
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.GroupName : null;
        }

        private void miSetMac_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fMacDialog();
            if(dlg.ShowDialog(this) == DialogResult.OK)
                Svc.SetMac(device, dlg.MacAddress);
        }

        private void miSetNet_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fNetDialog();
            dlg.CurrentIP = device.Endpoint?.Address?.ToString() ?? "";
            if(dlg.ShowDialog(this) == DialogResult.OK)
                Svc.SetNet(device, dlg.IP, dlg.Mask, dlg.Gateway);
        }

        private void miSetName_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fNameDialog();
            dlg.CurrentName = device.Name;
            if(dlg.ShowDialog(this) == DialogResult.OK)
                Svc.SetName(device, dlg.DeviceName);
        }

        private void miSetUuid_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fUuidDialog();
            dlg.CurrentUuid = device.Uuid.ToString();
            if(dlg.ShowDialog(this) == DialogResult.OK)
                Svc.SetUuid(device, dlg.DeviceUuid);
        }

        private void miBackup_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            var backed = Svc.GetBackedupDevice(device);
            if(backed == null || !backed.IsCompleted() || !backed.BackupSupported)
            {
                MessageBox.Show("Устройство не поддерживает бэкап настроек.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new fBackup(Svc, device)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }

        private void miResetBackup_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null)
            {
                MessageBox.Show("Выберите устройство.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Svc.ResetBackup(device);

            var key = EndpointKey(device);
            mBackupSupport.Remove(key);
            mBackupProbed.Remove(key);
        }

        private void miFirmware_Click(object? sender, EventArgs e)
        {
            var device = GetSelectedDevice();
            if(device == null) return;

            using var dlg = new fFirmware(Svc, device)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }

        private void OpenDatabase(string fileName)
        {
            if(InvokeRequired || !IsHandleCreated)
            {
                if(!IsHandleCreated)
                    CreateControl();

                BeginInvoke(new Action(() => OpenDatabase(fileName)));
                return;
            }

            Store = new JsonDeviceConfigStore(fileName);
            lDbPath.Text = fileName;
            AppSettings.ActiveConfigDb = fileName;
            AppSettings.Save();
        }

        private void miDbOpen_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Открыть базу конфигураций",
                Filter = "Config DB (*.json)|*.json|All files (*.*)|*.*"
            };
            if(dlg.ShowDialog(this) == DialogResult.OK)
                OpenDatabase(dlg.FileName);
        }

        private void miDbNew_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Создать базу конфигураций",
                Filter = "Config DB (*.json)|*.json",
                DefaultExt = "json",
                FileName = "devices.json"
            };
            if(dlg.ShowDialog(this) == DialogResult.OK)
                OpenDatabase(dlg.FileName);
        }

        private void miEditor_Click(object? sender, EventArgs e) => OpenEditorForSelected();

        private void lDevices_DoubleClick(object? sender, EventArgs e) => OpenEditorForSelected();

        private void OpenEditorForSelected()
        {
            if(Store == null)
            {
                MessageBox.Show("Выберите базу конфигураций (меню «База» → «Открыть базу…» или «Создать базу…»).",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var device = GetSelectedDevice();
            if(device == null)
                return;

            var key = DbKey(device);

            if(LiveDeviceTypes.IsExec(device.Name ?? ""))
            {
                using var editor = new fExecEditor(Svc, device, key, Store);
                editor.ShowDialog(this);
            }
            else if(LiveDeviceTypes.IsControl(device.Name ?? ""))
            {
                using var editor = new fPuEditor(Svc, device, key, Store);
                editor.ShowDialog(this);
            }
            //else if (LiveDeviceTypes.IsGate(device.Name ?? ""))
            //{
            //    using var editor = new fGateEditor(Svc, device, key, Store);
            //    editor.ShowDialog(this);
            //}
            else
            {
                MessageBox.Show($"Тип устройства «{device.Name}» не поддерживается конфигуратором.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void tTimer_Tick(object sender, EventArgs e)
        {
            var devices = Svc.GetDevices();
            lStatusDevCount.Text = $"Devices: {devices.Length}";

            var newKeys = new Dictionary<string, Device>();
            foreach(var d in devices)
            {
                var key = EndpointKey(d);
                newKeys[key] = d;

                // Авто-проба поддержки бэкапа: только один раз на устройство.
                // Дальнейшие попытки — только по явному действию пользователя (Сброс счётчика бэкапа).
                if(!mBackupProbed.Contains(key) &&
                    !mBackupSupport.ContainsKey(key) &&
                    Svc.GetBackedupDevice(d) == null)
                {
                    Svc.ProbeBackup(d);
                    mBackupProbed.Add(key);
                }
            }

            lDevices.BeginUpdate();
            for(int i = lDevices.Items.Count - 1; i >= 0; i--)
            {
                var key = (string)lDevices.Items[i].Tag;
                if(!newKeys.ContainsKey(key))
                {
                    mBackupSupport.Remove(key);
                    mBackupProbed.Remove(key);
                    mRowDbKey.Remove(key);
                    lDevices.Items.RemoveAt(i);
                }
                else
                    newKeys.Remove(key);
            }
            foreach(var d in newKeys.Values)
            {
                string preset = Store?.GetCurrentPreset(DbKey(d))?.PresetName ?? "";
                var item = new ListViewItem(d.Name);
                item.SubItems.Add(d.Block.ToString());
                item.SubItems.Add(d.Endpoint?.Address?.ToString() ?? "");
                item.SubItems.Add(d.Uuid.ToString());
                item.SubItems.Add(preset);
                item.SubItems.Add("…");
                item.Tag = EndpointKey(d);
                mRowDbKey[EndpointKey(d)] = DbKey(d);
                lDevices.Items.Add(item);
            }
            lDevices.EndUpdate();

            // Обновляем пресет и статус поддержки бэкапа
            foreach(ListViewItem item in lDevices.Items)
            {
                if(item.Tag is not string devKey) continue;
                var dev = devices.FirstOrDefault(ds => EndpointKey(ds) == devKey);
                if(dev == null)
                    continue;

                // Обновляем данные строки по текущему состоянию устройства.
                string currentType = dev.Name ?? "";
                if(item.Text != currentType)
                    item.Text = currentType;

                string currentBlock = dev.Block.ToString();
                if(item.SubItems[1].Text != currentBlock)
                    item.SubItems[1].Text = currentBlock;

                string currentIp = dev.Endpoint?.Address?.ToString() ?? "";
                if(item.SubItems[2].Text != currentIp)
                    item.SubItems[2].Text = currentIp;

                // При смене UUID устройства переносим его пресеты на новый идентификатор
                var dbKey = DbKey(dev);
                if(mRowDbKey.TryGetValue(devKey, out var rowDbKey) && rowDbKey != dbKey)
                {
                    Store?.MoveDevicePresets(rowDbKey, dbKey);
                    mRowDbKey[devKey] = dbKey;
                }

                // Колонка UUID — в соответствии с текущим состоянием устройства
                if(item.SubItems[3].Tag is not Guid syncUuid || syncUuid != dev.Uuid)
                {
                    item.SubItems[3].Text = dev.Uuid == Guid.Empty ? "" : dev.Uuid.ToString();
                    item.SubItems[3].Tag = dev.Uuid;
                }

                string preset = Store?.GetCurrentPreset(dbKey)?.PresetName ?? "";
                if(preset != item.SubItems[4].Text)
                    item.SubItems[4].Text = preset;

                var backed = Svc.GetBackedupDevice(dev);
                if(backed != null && backed.IsCompleted())
                {
                    mBackupSupport[devKey] = backed.BackupSupported;
                    var label = backed.BackupSupported ? "Yes" : "No";
                    if(!string.Equals(item.SubItems[5].Text, label, StringComparison.Ordinal))
                    {
                        item.SubItems[5].Text = label;
                        item.SubItems[5].ForeColor = backed.BackupSupported ? Color.FromArgb(0, 128, 0) : Color.Gray;
                    }
                }
            }
            UpdateBackupButton();
        }

        private async void fMain_Load(object? sender, EventArgs e)
        {
            if(DesignMode)
                return;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AppSettings.Load();
            await InitRepositoryAsync();

            var defPath = Path.Combine(AppContext.BaseDirectory, "configdb", "devices.json");
            if(Store == null && File.Exists(AppSettings.ActiveConfigDb))
                OpenDatabase(AppSettings.ActiveConfigDb);
            else if(Store == null && File.Exists(defPath))
                OpenDatabase(defPath);
        }

        /// <summary>
        /// Пытается создать репозиторий по сохранённой конфигурации подключения
        /// и убедиться, что схема БД создана. Если конфиг ещё не сохранён —
        /// оставляет <c>_repo = null</c>, кнопки БД будут показывать «нет подключения».
        /// </summary>
        private async Task InitRepositoryAsync()
        {
            try
            {
                if(!File.Exists(AppConfigService.ConfigFilePath))
                {
                    _repo = null;
                    return;
                }

                var cfg = AppConfigService.Load();
                var cs = AppConfigService.BuildConnectionString(cfg);

                var repo = new DeviceConfigRepository(cs, cfg.DatabaseType);
                await repo.EnsureSchemaAsync();

                _repo = repo;

                UpdateDatabaseSaveButton();
            }
            catch(Exception exc)
            {
                _repo = null;

                System.Diagnostics.Debug.WriteLine(
                    $"[InitRepository] {exc}");

                ShowDatabaseError(exc.Message);
            }
        }

        private void ShowDatabaseError(string message)
        {
            if(IsDisposed || !IsHandleCreated)
                return;

            if(InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() =>
                        ShowDatabaseError(message)));
                }
                catch(InvalidOperationException)
                {

                }

                return;
            }

            MessageBox.Show(
                this,
                message,
                "Ошибка подключения к БД",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void fMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopService();
            Store?.Dispose();
        }

        /// <summary>
        /// Открывает окно со списком всех сохранённых устройств.
        /// </summary>
        private void bdOpen_Click(object sender, EventArgs e)
        {
            if(_repo == null)
            {
                MessageBox.Show(this,
                    "Конфигурация подключения ещё не сохранена. " +
                    "Сохраните настройки и повторите.",
                    "Нет подключения",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var form = new DevicesForm(_repo);
                form.ShowDialog(this);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось открыть список устройств:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Удаляет все таблицы схемы AdlpConfig после подтверждения и создаёт их заново.
        /// </summary>
        private async void bdDelete_Click(object sender, EventArgs e)
        {
            if(_repo == null)
            {
                MessageBox.Show(this,
                    "Конфигурация подключения ещё не сохранена.",
                    "Нет подключения",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var answer = MessageBox.Show(
                this,
                "Все таблицы приложения будут УДАЛЕНЫ вместе с данными.\n" +
                "После удаления схема будет создана заново (пустая).\n\n" +
                "Продолжить?",
                "Удаление таблиц",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if(answer != DialogResult.Yes) return;

            try
            {
                await _repo.DropSchemaAsync();
                await _repo.EnsureSchemaAsync();

                MessageBox.Show(this,
                    "Все таблицы удалены и созданы заново.",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось удалить таблицы:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void MulticastToolStripMenuItem_Click(
    object sender, EventArgs e)
        {
            var device = GetSelectedDevice();

            if(device == null)
                return;

            if(_repo == null)
            {
                MessageBox.Show("Нет подключения к базе данных.");
                return;
            }

            try
            {
                long? deviceId = null;

                if(device.Uuid != Guid.Empty)
                {
                    deviceId = await _repo.FindDeviceIdByListUuidAndBlockAsync(
                        device.Uuid.ToString("N"),
                        device.Block);
                }

                using var form = new fMulticastEditor(
                    Svc,
                    device,
                    _repo,
                    deviceId);

                form.ShowDialog(this);
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть настройки мультикаста:\n{ex.Message}",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}