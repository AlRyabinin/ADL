using ADLDeviceEmulator.Models;
using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public enum DevicePartKind
{
    DeviceParameters,
    ExecChannels,
    Streams,
    ControlChannels,
    Groups,
    Pictures,
    Buttons,
    PixelgateChannels,
    GateConveyors,
    Igmp
}

public sealed partial class DevicePartEditorForm : Form
{
    private VirtualDeviceConfig? _config;
    private DevicePartKind _kind;
    private IList? _rows;
    private Func<object>? _createRow;
    private Action? _commit;
    private VirtualDeviceParameters? _workingParameters;

    // Конструктор для WinForms Designer.
    public DevicePartEditorForm()
    {
        InitializeComponent();
        if(LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;
    }

    public DevicePartEditorForm(VirtualDeviceConfig config, DevicePartKind kind)
    {
        InitializeComponent();
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _kind = kind;
        _config.EnsurePartsInitialized();
        ConfigureEditor();
    }

    private void ConfigureEditor()
    {
        if(_config == null) return;

        Text = $"{PartTitle(_kind)} — {_config.Display}";
        _titleLabel.Text = $"{PartTitle(_kind)} устройства «{_config.DeviceName}»";

        switch(_kind)
        {
            case DevicePartKind.DeviceParameters:
                _workingParameters = _config.Parameters.Clone();
                _propertyGrid.SelectedObject = _workingParameters;
                _propertyGrid.Visible = true;
                _grid.Visible = false;
                _addButton.Visible = false;
                _removeButton.Visible = false;
                _hintLabel.Text = "Параметры соответствуют идентификаторам ParamCode из ADLPDS.";
                _commit = () => _config.Parameters = _workingParameters!;
                break;

            case DevicePartKind.ExecChannels:
                BindRows(_config.ExecChannels, x => x.Clone(),
                    () => new EmulatorExecChannel { Name = "Новый канал", Control = "adlp", StreamId = VirtualDeviceConfig.PackStreamId("device", 0), Limit = 100 },
                    rows => _config.ExecChannels = rows);
                _hintLabel.Text = "Локальный номер — индекс объекта протокола; GlobalIndex — глобальный индекс ADLDC.";
                break;

            case DevicePartKind.Streams:
                BindRows(_config.Streams, x => x.Clone(),
                    () => new EmulatorStream { Protocol = "dmx", Address = 1 },
                    rows => _config.Streams = rows);
                _hintLabel.Text = "PackedId формируется автоматически из Protocol и Universe.";
                break;

            case DevicePartKind.ControlChannels:
                BindRows(_config.ControlChannels, x => x.Clone(),
                    () => new EmulatorControlChannel { Name = "Новый канал ПУ", Type = 1 },
                    rows => _config.ControlChannels = rows);
                _hintLabel.Text = "Type: 0 — нет, 1 — канал, 2 — группа, 3 — картина.";
                break;

            case DevicePartKind.Groups:
                BindRows(_config.Groups, x => x.Clone(),
                    () => new EmulatorGroup(),
                    rows => _config.Groups = rows);
                _hintLabel.Text = "В Channels укажи индексы каналов через запятую. Протокол содержит до 100 слотов на группу.";
                break;

            case DevicePartKind.Pictures:
                BindRows(_config.Pictures, x => x.Clone(),
                    () => new EmulatorPicture(),
                    rows => _config.Pictures = rows);
                _hintLabel.Text = "Картины передают имя и параметры Fade, Preset, Mux.";
                break;

            case DevicePartKind.Buttons:
                BindRows(_config.Buttons, x => x.Clone(),
                    () => new EmulatorControlButton { Page = 1 },
                    rows => _config.Buttons = rows);
                _hintLabel.Text = "Кнопки цветного ПУ: страница, локальный канал, координаты, иконка и цвет.";
                break;

            case DevicePartKind.PixelgateChannels:
                BindRows(_config.PixelgateChannels, x => x.Clone(),
                    () => new EmulatorPixelgateChannel { Name = "Новая линия" },
                    rows => _config.PixelgateChannels = rows);
                _hintLabel.Text = "PARAM_OBJECT_PIXELGATE_CHANNEL: локальный номер, имя и глобальный индекс линии.";
                break;

            case DevicePartKind.GateConveyors:
                BindRows(_config.GateConveyors, x => x.Clone(),
                    () => new EmulatorGateConveyor(),
                    rows => _config.GateConveyors = rows);
                _hintLabel.Text = "Поля U32 передаются как есть. Дополнительные Frequency/Timeouted/RdmEnabled пока локальные: их ParamCode в используемом LiveConfigCodec отсутствуют.";
                break;

            case DevicePartKind.Igmp:
                BindRows(_config.IgmpEntries, x => x.Clone(),
                    () => new EmulatorIgmpEntry(),
                    rows =>
                    {
                        _config.IgmpEntries = rows.OrderBy(x => x.Slot).ToList();
                        _config.Subscriptions = string.Join(";", _config.IgmpEntries
                            .Where(x => !string.IsNullOrWhiteSpace(x.Address))
                            .OrderBy(x => x.Slot)
                            .Select(x => x.Address.Trim()));
                        _config.PartsModelInitialized = true;
                    });
                _hintLabel.Text = "Слоты IGMP 1–32. Адрес должен быть IPv4 multicast (224.0.0.0–239.255.255.255).";
                break;
        }
    }

    private void BindRows<T>(List<T> source, Func<T, T> clone, Func<T> factory, Action<List<T>> assign)
        where T : class, ILocalPart
    {
        var binding = new BindingList<T>(source.Select(clone).ToList());
        _rows = (IList)binding;
        _createRow = () =>
        {
            T row = factory();
            row.Local = NextLocal();
            return row;
        };
        _commit = () => assign(binding.ToList());
        _grid.AutoGenerateColumns = true;
        _grid.DataSource = binding;
        _propertyGrid.Visible = false;
        _grid.Visible = true;
    }

    private int NextLocal()
    {
        if(_rows == null) return 1;
        int max = 0;
        foreach(object? row in _rows)
            if(row is ILocalPart local)
                max = Math.Max(max, local.Local);
        return max + 1;
    }

    private void addButton_Click(object? sender, EventArgs e)
    {
        if(_rows == null || _createRow == null) return;
        object row = _createRow();
        _rows.Add(row);
        if(_grid.Rows.Count > 0)
        {
            int last = _grid.Rows.Count - 1;
            _grid.CurrentCell = _grid.Rows[last].Cells[0];
        }
    }

    private void removeButton_Click(object? sender, EventArgs e)
    {
        if(_rows == null) return;
        var selected = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(x => x.DataBoundItem)
            .Where(x => x != null)
            .ToList();
        if(selected.Count == 0 && _grid.CurrentRow?.DataBoundItem is object current)
            selected.Add(current);

        foreach(object row in selected)
            _rows.Remove(row);
    }

    private void saveButton_Click(object? sender, EventArgs e)
    {
        if(_config == null || _commit == null) return;

        _grid.EndEdit();
        ValidateChildren();
        if(!ValidateRows()) return;

        _commit();
        _config.SyncLegacyChannelIndexes();
        DialogResult = DialogResult.OK;
        Close();
    }

    private bool ValidateRows()
    {
        if(_rows == null) return true;

        var localValues = _rows.Cast<object>().OfType<ILocalPart>().Select(x => x.Local).ToList();
        if(localValues.Any(x => x < 1 || x > ushort.MaxValue) || localValues.Distinct().Count() != localValues.Count)
        {
            MessageBox.Show(this, "Локальные номера должны быть уникальными числами от 1 до 65535.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if(_kind == DevicePartKind.Igmp)
        {
            var rows = _rows.Cast<EmulatorIgmpEntry>().ToList();
            if(rows.Count > 32 || rows.Any(x => x.Slot > 32))
            {
                MessageBox.Show(this, "Допускается не более 32 IGMP-слотов с номерами 1–32.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if(rows.Any(x => !string.IsNullOrWhiteSpace(x.Address) && !IsMulticastAddress(x.Address)))
            {
                MessageBox.Show(this, "Каждый заполненный IGMP-адрес должен быть IPv4 multicast (224.0.0.0–239.255.255.255). Пустые слоты разрешены.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var nonEmptyAddresses = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Address))
                .Select(x => x.Address.Trim());
            if(nonEmptyAddresses.Distinct(StringComparer.OrdinalIgnoreCase).Count() != nonEmptyAddresses.Count())
            {
                MessageBox.Show(this, "Один multicast-адрес нельзя указывать в нескольких слотах.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        int? maxRows = _kind switch
        {
            DevicePartKind.ExecChannels => 24,
            DevicePartKind.ControlChannels => 48,
            DevicePartKind.Groups => 24,
            DevicePartKind.Pictures => 24,
            DevicePartKind.Buttons => 96,
            DevicePartKind.PixelgateChannels => 20,
            DevicePartKind.Igmp => 32,
            _ => null
        };
        if(maxRows.HasValue && _rows.Count > maxRows.Value)
        {
            MessageBox.Show(this, $"Для этого раздела допускается максимум {maxRows.Value} записей.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if(_kind == DevicePartKind.Groups && _rows.Cast<EmulatorGroup>().Any(x =>
            (x.Channels ?? string.Empty)
                .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Length > 100))
        {
            MessageBox.Show(this, "В группе допускается максимум 100 каналов.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private static bool IsMulticastAddress(string value)
    {
        if(!IPAddress.TryParse((value ?? string.Empty).Trim(), out var ip) ||
           ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;
        byte first = ip.GetAddressBytes()[0];
        return first >= 224 && first <= 239;
    }

    private static string PartTitle(DevicePartKind kind) => kind switch
    {
        DevicePartKind.DeviceParameters => "Параметры протокола",
        DevicePartKind.ExecChannels => "Каналы EXEC",
        DevicePartKind.Streams => "Потоки",
        DevicePartKind.ControlChannels => "Каналы ПУ",
        DevicePartKind.Groups => "Группы",
        DevicePartKind.Pictures => "Картины",
        DevicePartKind.Buttons => "Кнопки ПУ",
        DevicePartKind.PixelgateChannels => "Каналы PixelGate",
        DevicePartKind.GateConveyors => "Конвейеры Gate",
        DevicePartKind.Igmp => "IGMP-подписки",
        _ => "Часть устройства"
    };

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
