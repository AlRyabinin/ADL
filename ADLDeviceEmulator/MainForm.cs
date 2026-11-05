using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using ADLDeviceEmulator.Models;
using ADLDeviceEmulator.Runtime;

namespace ADLDeviceEmulator;

public sealed partial class MainForm : Form
{
    private readonly BindingList<VirtualDeviceConfig> _devices = new();
    private readonly Dictionary<VirtualDeviceConfig, VirtualDeviceServer> _servers = new();
    private readonly object _saveLock = new();
    private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "devices.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public MainForm()
    {
        InitializeComponent();

        // Не запускаем сеть и не читаем devices.json в конструкторе Visual Studio Designer.
        if(LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _grid.DataSource = _devices;

        LoadNetworkAddresses();
        LoadOrCreateDefaults();
        UpdateButtons();
    }

    private void createDeviceButton_Click(object? sender, EventArgs e) => AddDevice();

    private async void editButton_Click(object? sender, EventArgs e) => await EditSelectedAsync();

    private async void deleteButton_Click(object? sender, EventArgs e) => await RemoveSelectedAsync();

    private async void demoSetButton_Click(object? sender, EventArgs e) => await LoadDemoSetAsync();

    private void loadButton_Click(object? sender, EventArgs e) => LoadConfigFromFile();

    private void saveButton_Click(object? sender, EventArgs e) => SaveConfigToFile();

    private async void startSelectedButton_Click(object? sender, EventArgs e) => await StartSelectedDeviceAsync();

    private async void stopSelectedButton_Click(object? sender, EventArgs e) => await StopSelectedDeviceAsync();

    private async void startAllButton_Click(object? sender, EventArgs e) => await StartDevicesAsync();

    private async void stopAllButton_Click(object? sender, EventArgs e) => await StopDevicesAsync();

    private void grid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        AppendLog($"Ошибка отображения таблицы: {e.Exception?.Message}");
    }

    private void grid_SelectionChanged(object? sender, EventArgs e) => UpdateButtons();

    private async void grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if(e.RowIndex >= 0)
            await EditSelectedAsync();
    }

    private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if(e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "RuntimeStatus") return;
        if(_grid.Rows[e.RowIndex].DataBoundItem is not VirtualDeviceConfig config) return;
        e.Value = _servers.ContainsKey(config) ? "Запущено" : "Остановлено";
        e.FormattingApplied = true;
    }

    private void LoadNetworkAddresses()
    {
        var choices = new List<NetworkAddressChoice>();
        foreach(var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if(nic.OperationalStatus != OperationalStatus.Up) continue;
            foreach(var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if(unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                choices.Add(new NetworkAddressChoice(nic.Name, unicast.Address));
            }
        }
        if(choices.Count == 0)
            choices.Add(new NetworkAddressChoice("Loopback", IPAddress.Loopback));

        foreach(var choice in choices.DistinctBy(x => x.Address.ToString()))
            _bindAddress.Items.Add(choice);

        int preferred = choices.FindIndex(x => !IPAddress.IsLoopback(x.Address));
        if(preferred < 0) preferred = 0;
        _bindAddress.SelectedIndex = Math.Max(0, preferred);
    }

    private IPAddress SelectedBindAddress =>
        (_bindAddress.SelectedItem as NetworkAddressChoice)?.Address ?? IPAddress.Loopback;

    private void LoadOrCreateDefaults()
    {
        if(File.Exists(_configPath))
        {
            try
            {
                var rows = JsonSerializer.Deserialize<List<VirtualDeviceConfig>>(File.ReadAllText(_configPath), JsonOptions);
                if(rows != null)
                {
                    foreach(var row in rows) _devices.Add(row);
                    if(rows.Count > 0) return;
                }
            }
            catch(Exception ex)
            {
                AppendLog($"Не удалось загрузить {_configPath}: {ex.Message}");
            }
        }
        foreach(var row in VirtualDeviceConfig.CreateDemoSet(SelectedBindAddress.ToString()))
            _devices.Add(row);
    }

    private void AddDevice()
    {
        int port = GetNextPort();
        var config = new VirtualDeviceConfig
        {
            Type = "IKB",
            DeviceName = "ADL virtual device",
            Uuid = Guid.NewGuid(),
            Block = 1,
            Port = port,
            IpAddress = SelectedBindAddress.ToString(),
            Mask = "255.255.255.0",
            Gateway = SelectedBindAddress.ToString(),
            ChannelIndexes = "100,101,102"
        };

        using var dialog = new DeviceEditorForm(config, isNew: true);
        if(dialog.ShowDialog(this) != DialogResult.OK) return;
        if(!ValidateUniqueIdentityAndPort(dialog.Result, null)) return;

        _devices.Add(dialog.Result);
        AppendLog($"Создано устройство: {dialog.Result.Display} UUID={dialog.Result.Uuid}");
        if(dialog.Result.Enabled && _servers.Count > 0)
            TryStartDevice(dialog.Result);
        SaveConfigToFile();
        UpdateButtons();
    }

    private async Task EditSelectedAsync()
    {
        var selected = GetSelectedDevice();
        if(selected == null) return;

        bool wasRunning = _servers.ContainsKey(selected);
        using var dialog = new DeviceEditorForm(selected, isNew: false);
        if(dialog.ShowDialog(this) != DialogResult.OK) return;
        var updated = dialog.Result;
        if(!ValidateUniqueIdentityAndPort(updated, selected)) return;

        int index = _devices.IndexOf(selected);
        if(index < 0) return;
        if(wasRunning)
            await StopDeviceAsync(selected);

        _devices[index] = updated;
        AppendLog($"Изменено устройство: {updated.Display} UUID={updated.Uuid}");
        if(updated.Enabled && (wasRunning || _servers.Count > 0))
            TryStartDevice(updated);
        SaveConfigToFile();
        UpdateButtons();
    }

    private async Task RemoveSelectedAsync()
    {
        var selected = GetSelectedDevice();
        if(selected == null) return;

        var answer = MessageBox.Show(this,
            $"Удалить виртуальное устройство?\n\n{selected.Display}\nUUID: {selected.Uuid}",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if(answer != DialogResult.Yes) return;

        await StopDeviceAsync(selected);
        _devices.Remove(selected);
        AppendLog($"Удалено устройство: {selected.Display} UUID={selected.Uuid}");
        SaveConfigToFile();
        UpdateButtons();
    }

    private async Task LoadDemoSetAsync()
    {
        if(_servers.Count > 0)
        {
            var answer = MessageBox.Show(this,
                "Заменить список устройств демонстрационным набором? Запущенные виртуальные устройства будут остановлены.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(answer != DialogResult.Yes) return;
            await StopDevicesAsync();
        }

        _devices.Clear();
        foreach(var row in VirtualDeviceConfig.CreateDemoSet(SelectedBindAddress.ToString()))
            _devices.Add(row);
        AppendLog("Загружен демонстрационный набор: IKB, PU, PU24, INDIGATE.");
        SaveConfigToFile();
        UpdateButtons();
    }

    private void LoadConfigFromFile()
    {
        if(_servers.Count > 0)
        {
            MessageBox.Show(this, "Перед загрузкой файла останови все устройства.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
            FileName = Path.GetFileName(_configPath)
        };
        if(dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var rows = JsonSerializer.Deserialize<List<VirtualDeviceConfig>>(File.ReadAllText(dialog.FileName), JsonOptions)
                ?? new List<VirtualDeviceConfig>();
            if(!ValidateConfigurationSet(rows, showMessage: true)) return;
            _devices.Clear();
            foreach(var row in rows) _devices.Add(row);
            AppendLog($"Загружено устройств: {_devices.Count} из {dialog.FileName}");
            SaveConfigToFile();
            UpdateButtons();
        }
        catch(Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveConfigToFile()
    {
        if(InvokeRequired)
        {
            if(!IsDisposed && IsHandleCreated)
            {
                try { BeginInvoke(new Action(SaveConfigToFile)); } catch { }
            }
            return;
        }

        try
        {
            lock(_saveLock)
                File.WriteAllText(_configPath, JsonSerializer.Serialize(_devices.ToList(), JsonOptions));
        }
        catch(Exception ex)
        {
            AppendLog($"Ошибка сохранения конфигурации: {ex.Message}");
        }
    }

    private async Task StartDevicesAsync()
    {
        var enabled = _devices.Where(x => x.Enabled && !_servers.ContainsKey(x)).ToList();
        if(enabled.Count == 0)
        {
            AppendLog("Нет остановленных включённых устройств для запуска.");
            return;
        }
        if(!ValidateConfigurationSet(_devices.ToList(), showMessage: true)) return;
        if(!TryGetStatusTarget(out var target)) return;

        int started = 0;
        foreach(var config in enabled)
        {
            if(TryStartDevice(config, target)) started++;
        }
        UpdateButtons();
        RefreshGrid();
        _status.Text = $"Запущено устройств: {_servers.Count}; добавлено при запуске: {started}. STATUS -> {target}.";
        SaveConfigToFile();
        await Task.CompletedTask;
    }

    private async Task StartSelectedDeviceAsync()
    {
        var selected = GetSelectedDevice();
        if(selected == null) return;
        selected.Enabled = true;
        if(!ValidateUniqueIdentityAndPort(selected, selected)) return;
        if(!TryGetStatusTarget(out var target)) return;
        if(TryStartDevice(selected, target))
        {
            SaveConfigToFile();
            UpdateButtons();
            RefreshGrid();
        }
        await Task.CompletedTask;
    }

    private async Task StopSelectedDeviceAsync()
    {
        var selected = GetSelectedDevice();
        if(selected == null) return;
        await StopDeviceAsync(selected);
        UpdateButtons();
        RefreshGrid();
    }

    private async Task StopDevicesAsync()
    {
        foreach(var config in _servers.Keys.ToArray())
            await StopDeviceAsync(config);
        _status.Text = "Все виртуальные устройства остановлены.";
        UpdateButtons();
        RefreshGrid();
    }

    private bool TryStartDevice(VirtualDeviceConfig config, IPEndPoint? target = null)
    {
        if(_servers.ContainsKey(config)) return true;
        if(!ValidateUniqueIdentityAndPort(config, config)) return false;
        if(target is null)
        {
            if(!TryGetStatusTarget(out var resolvedTarget)) return false;
            target = resolvedTarget;
        }

        try
        {
            var server = new VirtualDeviceServer(config, SelectedBindAddress, target);
            server.Log += AppendLog;
            server.ConfigurationChanged += HandleServerConfigurationChanged;
            server.Start();
            _servers.Add(config, server);
            UpdateButtons();
            RefreshGrid();
            return true;
        }
        catch(Exception ex)
        {
            AppendLog($"Не удалось запустить {config.Display}: {ex.Message}");
            MessageBox.Show(this,
                $"Не удалось запустить устройство {config.Display}. Проверь порт и сетевой интерфейс.\n\n{ex.Message}",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private async Task StopDeviceAsync(VirtualDeviceConfig config)
    {
        if(!_servers.Remove(config, out var server)) return;
        server.Log -= AppendLog;
        server.ConfigurationChanged -= HandleServerConfigurationChanged;
        await server.DisposeAsync();
        UpdateButtons();
        RefreshGrid();
    }

    private void HandleServerConfigurationChanged(VirtualDeviceConfig config)
    {
        if(IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(new Action(() =>
            {
                SaveConfigToFile();
                RefreshGrid();
            }));
        }
        catch { }
    }

    private bool TryGetStatusTarget(out IPEndPoint target)
    {
        target = new IPEndPoint(IPAddress.Loopback, (int)_targetPort.Value);
        if(!IPAddress.TryParse(_targetAddress.Text.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
        {
            MessageBox.Show(this, "Укажи корректный IPv4-адрес назначения STATUS.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        target = new IPEndPoint(ip, (int)_targetPort.Value);
        return true;
    }

    private bool ValidateConfigurationSet(IReadOnlyCollection<VirtualDeviceConfig> configs, bool showMessage)
    {
        var all = configs.ToList();
        string? error = null;
        if(all.Any(x => string.IsNullOrWhiteSpace(x.Type) || x.Type.Length > 8))
            error = "Тип устройства должен содержать от 1 до 8 символов.";
        else if(all.Any(x => x.Block < 0 || x.Block > ushort.MaxValue || x.Port < 1 || x.Port > 65535))
            error = "Проверь значения Block и UDP-порта.";
        else if(all.Any(x => x.Uuid == Guid.Empty))
            error = "У каждого устройства должен быть ненулевой UUID.";
        else if(all.GroupBy(x => x.Port).Any(g => g.Count() > 1))
            error = "UDP-порты виртуальных устройств должны быть разными.";
        else if(all.GroupBy(x => (x.Uuid, x.Block)).Any(g => g.Count() > 1))
            error = "Пара UUID + Block должна быть уникальной для каждого виртуального устройства.";

        if(error == null) return true;
        AppendLog($"Проверка конфигурации: {error}");
        if(showMessage)
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private bool ValidateUniqueIdentityAndPort(VirtualDeviceConfig candidate, VirtualDeviceConfig? ignore)
    {
        foreach(var other in _devices)
        {
            if(ReferenceEquals(other, ignore)) continue;
            if(other.Port == candidate.Port)
            {
                MessageBox.Show(this, $"UDP-порт {candidate.Port} уже назначен устройству {other.Display}.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if(other.Uuid == candidate.Uuid && other.Block == candidate.Block)
            {
                MessageBox.Show(this, $"UUID + Block уже используются устройством {other.Display}.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        return true;
    }

    private int GetNextPort()
    {
        var used = _devices.Select(x => x.Port).ToHashSet();
        int port = 21001;
        while(used.Contains(port) && port < 65535) port++;
        return port;
    }

    private VirtualDeviceConfig? GetSelectedDevice() =>
        _grid.CurrentRow?.DataBoundItem as VirtualDeviceConfig;

    private void RefreshGrid()
    {
        if(IsDisposed) return;
        if(InvokeRequired)
        {
            try { BeginInvoke(new Action(RefreshGrid)); } catch { }
            return;
        }
        _grid.Refresh();
    }

    private void UpdateButtons()
    {
        if(IsDisposed) return;
        bool selected = GetSelectedDevice() != null;
        bool selectedRunning = selected && _servers.ContainsKey(GetSelectedDevice()!);
        bool anyRunning = _servers.Count > 0;
        bool hasEnabledStopped = _devices.Any(x => x.Enabled && !_servers.ContainsKey(x));

        _editButton.Enabled = selected;
        _deleteButton.Enabled = selected;
        _startSelectedButton.Enabled = selected && !selectedRunning;
        _stopSelectedButton.Enabled = selectedRunning;
        _startAllButton.Enabled = hasEnabledStopped;
        _stopAllButton.Enabled = anyRunning;
        _bindAddress.Enabled = !anyRunning;
        _targetAddress.Enabled = !anyRunning;
        _targetPort.Enabled = !anyRunning;
        _grid.ReadOnly = true;
        _status.Text = $"В списке: {_devices.Count}; запущено: {_servers.Count}. Добавление и удаление доступны без остановки остальных устройств.";
    }

    private void AppendLog(string message)
    {
        if(IsDisposed || !IsHandleCreated) return;
        if(InvokeRequired)
        {
            try { BeginInvoke(new Action<string>(AppendLog), message); } catch { }
            return;
        }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        if(_log.TextLength > 250_000)
            _log.Text = _log.Text[^150_000..];
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        foreach(var server in _servers.Values.ToArray())
        {
            server.Log -= AppendLog;
            server.ConfigurationChanged -= HandleServerConfigurationChanged;
            _ = server.DisposeAsync();
        }
        _servers.Clear();
        base.OnFormClosing(e);
    }

    private sealed record NetworkAddressChoice(string InterfaceName, IPAddress Address)
    {
        public override string ToString() => $"{InterfaceName} — {Address}";
    }
}
