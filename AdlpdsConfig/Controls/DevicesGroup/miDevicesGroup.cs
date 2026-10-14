using AdlpdsConfig.Controls.Devices;
using AdlpdsConfig.Controls.Devices.NetGate;
using AdlpdsConfig.Utils;
using AdlpLib.Configuration.Application;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Models.Devices.PU;
using AdlpLib.Models.DevicesGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.DevicesGroup
{
    public partial class miDevicesGroup : Form
    {
        /// <summary>
        /// Порядок типов устройств для сортировки при расстановке Block.
        /// Всё, чего нет в словаре, уедет в конец списка.
        /// </summary>
        private static readonly Dictionary<string, int> DeviceTypeOrder =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["IKB"] = 1,
                ["DIM220"] = 2,
                ["Dimmer cabinet"] = 3,
                ["NetGate"] = 4,
            };

        private readonly DevicesGroupRepository _groupRepository;
        private readonly DeviceConfigRepository _devicesRepository;

        private bool _loading;

        public miDevicesGroup()
        {
            InitializeComponent();
        }

        public miDevicesGroup(DevicesGroupRepository groupRepo, DeviceConfigRepository deviceRepo) : this()
        {
            _groupRepository = groupRepo ?? throw new ArgumentNullException(nameof(groupRepo));
            _devicesRepository = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(_groupRepository == null || _devicesRepository == null)
            {
                CenteredMessageBox.Show(this,
                    "Репозитории не переданы в форму.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            await LoadGroupsAsync();
        }

        private async Task LoadGroupsAsync()
        {
            try
            {
                UseWaitCursor = true;
                Enabled = false;

                var groups = await _groupRepository.GetAllHeadersAsync();
                BindGroups(groups, null);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось загрузить список групп:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                Enabled = true;
            }

            await LoadGroupDevicesAsync(SelectedGroup?.Id);
        }

        private async Task ReloadAsync(long? selectId = null)
        {
            try
            {
                _loading = true;
                UseWaitCursor = true;
                Enabled = false;

                var groups = await _groupRepository.GetAllHeadersAsync();
                BindGroups(groups, selectId);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось загрузить список групп:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
                UseWaitCursor = false;
                Enabled = true;

                UpdateButtonsState();
            }

            await LoadGroupDevicesAsync(SelectedGroup?.Id);
        }

        private void BindGroups(List<DevicesGroupHeader> groups, long? selectId)
        {
            GroupsComboBox.DataSource = null;
            GroupsComboBox.DisplayMember = nameof(DevicesGroupHeader.DisplayText);
            GroupsComboBox.ValueMember = nameof(DevicesGroupHeader.Id);
            GroupsComboBox.DataSource = groups;

            if(groups.Count == 0)
            {
                GroupsComboBox.SelectedIndex = -1;
            }
            else
            {
                int idx = selectId is null
                    ? 0
                    : Math.Max(0, groups.FindIndex(g => g.Id == selectId.Value));
                GroupsComboBox.SelectedIndex = idx;
            }
        }

        private DevicesGroupHeader SelectedGroup
            => GroupsComboBox.SelectedItem as DevicesGroupHeader;

        private DeviceConfigDtoBase SelectedDevice
            => (DevicesComboBox.SelectedItem as DeviceRow)?.Device;

        private async Task LoadGroupDevicesAsync(long? groupId)
        {
            try
            {
                DevicesComboBox.DataSource = null;

                if(groupId is null) return;

                var devices = await _groupRepository.GetDevicesAsync(groupId.Value);

                var rows = devices.Select(d => new DeviceRow(d)).ToList();

                DevicesComboBox.DisplayMember = nameof(DeviceRow.DisplayText);
                DevicesComboBox.ValueMember = "";
                DevicesComboBox.DataSource = rows;

                DevicesComboBox.SelectedIndex = rows.Count > 0 ? 0 : -1;
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось загрузить устройства группы:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateButtonsState()
        {
            var sel = SelectedGroup;
            DeleteButton.Enabled = sel != null;
        }

        private async void GroupsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(_loading) return;

            var sel = SelectedGroup;

            await LoadGroupDevicesAsync(sel?.Id);

            UpdateButtonsState();
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            string name = AskGroupName("");

            if(string.IsNullOrWhiteSpace(name))
                return;

            try
            {
                CreateButton.Enabled = false;

                long? existing = await _groupRepository.FindGroupIdByNameAsync(name);
                if(existing != null)
                {
                    CenteredMessageBox.Show(this,
                        $"Группа с именем «{name}» уже существует.",
                        "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                long newId = await _groupRepository.SaveAsync(new DevicesGroupDto { Name = name });

                await ReloadAsync(newId);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось создать группу:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CreateButton.Enabled = true;
            }
        }

        private string AskGroupName(string initial)
        {
            using var dlg = new GroupNameDialog(initial);
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.GroupName : null;
        }

        private async void DeleteButton_Click(object sender, EventArgs e)
        {
            var sel = SelectedGroup;
            if(sel == null) return;

            var answer = CenteredMessageBox.Show(this,
                $"Удалить группу «{sel.Name}»?\r\n" +
                "Устройства из неё удалены не будут.",
                "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if(answer != DialogResult.Yes) return;

            try
            {
                DeleteButton.Enabled = false;
                await _groupRepository.DeleteAsync(sel.Id);
                await ReloadAsync();
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось удалить группу:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DeleteButton.Enabled = true;
            }
        }

        private async void CreateDeviceButton_Click(object sender, EventArgs e)
        {
            if(SelectedGroup == null)
            {
                CenteredMessageBox.Show(this, "Сначала выберите группу.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using(var dlg = new CreateDeviceForm())
            {
                if(dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                DeviceConfigDtoBase newDevice = dlg.CreatedDevice;
                if(newDevice == null)
                {
                    CenteredMessageBox.Show(this, "Устройство не создано.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bool editOk;
                switch(newDevice)
                {
                    case NetGateConfigDto ng:
                        using(var editNgDlg = new EditNetGateParametersForm(ng))
                        {
                            editOk = editNgDlg.ShowDialog(this) == DialogResult.OK;
                        }
                        break;

                    default:
                        using(var editDlg = new EditDeviceParametersForm(newDevice))
                        {
                            editOk = editDlg.ShowDialog(this) == DialogResult.OK;
                        }
                        break;
                }

                if(!editOk)
                    return;

                try
                {
                    CreateDeviceButton.Enabled = false;
                    UseWaitCursor = true;

                    long deviceId = await _devicesRepository.SaveAsync(newDevice);
                    newDevice.Id = deviceId;

                    DevicesGroupDto groupDto = await _groupRepository.LoadAsync(SelectedGroup.Id);
                    if(groupDto == null)
                    {
                        CenteredMessageBox.Show(this, "Группа не найдена в БД.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if(!groupDto.DeviceIds.Contains(deviceId))
                    {
                        groupDto.DeviceIds.Add(deviceId);
                    }

                    await _groupRepository.SaveAsync(groupDto);

                    await ReorderAllDevicesAsync();

                    await ReloadAsync(SelectedGroup.Id);

                    CenteredMessageBox.Show(this, "Устройство успешно добавлено и прикреплено к группе.",
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch(Exception ex)
                {
                    CenteredMessageBox.Show(this, $"Ошибка при сохранении: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    CreateDeviceButton.Enabled = true;
                    UseWaitCursor = false;
                }
            }
        }

        private async void DeleteDeviceButton_Click(object sender, EventArgs e)
        {
            var selectedGroup = SelectedGroup;
            var device = SelectedDevice;

            if(selectedGroup == null || device == null)
            {
                MessageBox.Show(this, "Выберите группу и устройство.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var answer = CenteredMessageBox.Show(this,
                $"Удалить устройство «{device.Name} ({device.Mac})» полностью?\r\n" +
                "Оно будет убрано из группы и удалено из БД.",
                "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if(answer != DialogResult.Yes) return;

            try
            {
                DeleteDeviceButton.Enabled = false;
                UseWaitCursor = true;

                var groupDto = await _groupRepository.LoadAsync(selectedGroup.Id);
                if(groupDto != null && groupDto.DeviceIds.Remove(device.Id))
                {
                    await _groupRepository.SaveAsync(groupDto);
                }

                await _devicesRepository.DeleteAsync(device.Id);

                await ReorderAllDevicesAsync();

                await ReloadAsync(selectedGroup.Id);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "Не удалось удалить устройство:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DeleteDeviceButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private async void LoadButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Multiselect = true
            };
            if(ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                Enabled = false;

                // Определяем, в какую группу складывать устройства
                long targetGroupId;
                var currentGroup = SelectedGroup;

                if(currentGroup != null)
                {
                    targetGroupId = currentGroup.Id;
                }
                else
                {
                    // Группы нет — создаём «Импорт»
                    const string importGroupName = "Импорт";
                    long? existing = await _groupRepository.FindGroupIdByNameAsync(importGroupName);
                    targetGroupId = existing ?? await _groupRepository.SaveAsync(
                        new DevicesGroupDto { Name = importGroupName });
                }

                var groupDto = await _groupRepository.LoadAsync(targetGroupId)
                               ?? new DevicesGroupDto { Id = targetGroupId };
                if(groupDto.DeviceIds == null) groupDto.DeviceIds = new List<long>();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                int imported = 0;
                int failed = 0;
                int cancelled = 0;

                foreach(var fileName in ofd.FileNames)
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(fileName);
                        var device = JsonSerializer.Deserialize<DeviceConfigDtoBase>(json, options);

                        if(device == null)
                        {
                            failed++;
                            continue;
                        }

                        // Заполняем обязательные поля из имени файла
                        NormalizeDevice(device, Path.GetFileNameWithoutExtension(fileName));

                        // Открываем диалог редактирования ПЕРЕД сохранением в БД
                        using(var editDlg = new EditDeviceParametersForm(device))
                        {
                            if(editDlg.ShowDialog(this) != DialogResult.OK)
                            {
                                // Пользователь отменил — пропускаем этот файл
                                cancelled++;
                                continue;
                            }

                            DeviceConfigDtoBase editedDevice = editDlg.CurrentDevice;

                            // Id = 0, чтобы репозиторий создал новую запись
                            editedDevice.Id = 0;
                            long deviceId = await _devicesRepository.SaveAsync(editedDevice);
                            editedDevice.Id = deviceId;

                            if(!groupDto.DeviceIds.Contains(deviceId))
                                groupDto.DeviceIds.Add(deviceId);

                            imported++;
                        }
                    }
                    catch(Exception ex)
                    {
                        failed++;
                        System.Diagnostics.Debug.WriteLine(
                            $"Ошибка импорта файла {fileName}: {ex.Message}");
                    }
                }

                await _groupRepository.SaveAsync(groupDto);

                // Пересчитываем блоки и каналы
                await ReorderAllDevicesAsync();
                await ReloadAsync(targetGroupId);

                string msg = $"Загружено устройств: {imported}.";
                if(cancelled > 0) msg += $"\nОтменено пользователем: {cancelled}.";
                if(failed > 0) msg += $"\nНе удалось загрузить: {failed}.";

                CenteredMessageBox.Show(this, msg, "Загрузка завершена",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this, "Ошибка загрузки: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                Enabled = true;
            }
        }

        /// <summary>
        /// Заполняет обязательные поля Name и DeviceType, если они пусты.
        /// DeviceType извлекается из имени файла (например, Config_IKB_... → IKB).
        /// </summary>
        private void NormalizeDevice(DeviceConfigDtoBase device, string fileName)
        {
            // Имя устройства — из имени файла, если не задано
            if(string.IsNullOrWhiteSpace(device.Name))
            {
                device.Name = !string.IsNullOrWhiteSpace(fileName)
                    ? fileName
                    : (!string.IsNullOrWhiteSpace(device.Mac) ? device.Mac : "Device");
            }
        }

        /// <summary>
        /// Пересчитывает поле Block у всех устройств: сначала по порядку типов,
        /// внутри типа — по имени. Block идёт подряд: 1, 2, 3, ...
        /// </summary>
        private async Task ReorderAllDevicesAsync()
        {
            var headers = await _devicesRepository.GetAllHeadersAsync();
            if(headers == null || headers.Count == 0) return;

            var sorted = headers
                .OrderBy(h => DeviceTypeOrder.TryGetValue(h.DeviceType ?? "", out int order)
                              ? order
                              : int.MaxValue)
                .ThenBy(h => h.DeviceType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int baseChannel = 1;
            int block = 1;

            foreach(var h in sorted)
            {
                var dto = await _devicesRepository.LoadAsync(h.Id);
                if(dto == null) { block++; continue; }

                dto.Block = block;

                int channelCount = 0;

                switch(dto)
                {
                    case ExecConfigDto exec:
                        channelCount = exec.Channels?.Count ?? 0;
                        if(channelCount == 0)
                            channelCount = LiveConfigDefaults.ChannelsPerBlock;

                        if(exec.Channels != null)
                        {
                            foreach(var ch in exec.Channels)
                            {
                                ch.GlobalIndex = baseChannel + (ch.Local - 1);
                                ch.Name = $"Канал {baseChannel + (ch.Local - 1)}";
                            }
                        }

                        exec.BaseChannel = baseChannel.ToString();
                        break;

                    case PuConfigDto pu:
                        channelCount = pu.Channels?.Count ?? 0;

                        if(channelCount == 0)
                            channelCount = LiveConfigDefaults.ChannelsPerBlock;

                        if(pu.Channels != null)
                        {
                            foreach(var ch in pu.Channels)
                            {
                                ch.Index = baseChannel + (ch.Local - 1);
                                ch.Name = $"Канал {baseChannel + (ch.Local - 1)}";
                            }
                        }
                        break;
                }

                await _devicesRepository.SaveAsync(dto);

                baseChannel += channelCount;
                block++;
            }
        }
        private async void EditButton_Click(object sender, EventArgs e)
        {
            var selectedGroup = SelectedGroup;
            var device = SelectedDevice;

            if(selectedGroup == null || device == null)
            {
                CenteredMessageBox.Show(this, "Выберите устройство для редактирования.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                EditButton.Enabled = false;
                UseWaitCursor = true;

                var freshDevice = await _devicesRepository.LoadAsync(device.Id);
                if(freshDevice == null)
                {
                    CenteredMessageBox.Show(this, "Устройство не найдено в БД.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using(var editDlg = new EditDeviceParametersForm(freshDevice))
                {
                    if(editDlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    var edited = editDlg.CurrentDevice;
                    await _devicesRepository.SaveAsync(edited);

                    await ReorderAllDevicesAsync();
                    await ReloadAsync(selectedGroup.Id);

                    CenteredMessageBox.Show(this, "Изменения сохранены.", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this, "Ошибка при редактировании: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EditButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            try
            {
                RefreshButton.Enabled = false;
                UseWaitCursor = true;

                await ReorderAllDevicesAsync();

                await ReloadAsync(SelectedGroup?.Id);

                CenteredMessageBox.Show(this,
                    "Сортировка завершена. Поле Block обновлено.",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Ошибка при обновлении: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private async void LoadGroupButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if(ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                Enabled = false;

                string json = await File.ReadAllTextAsync(ofd.FileName);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var package = JsonSerializer.Deserialize<DevicesGroupPackage>(json, options);

                if(package == null || string.IsNullOrWhiteSpace(package.GroupName))
                {
                    CenteredMessageBox.Show(this, "Некорректный файл JSON.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверяем, существует ли группа с таким именем
                long? existingGroupId = await _groupRepository.FindGroupIdByNameAsync(package.GroupName);
                long groupId;

                if(existingGroupId != null)
                {
                    var answer = CenteredMessageBox.Show(this, $"Группа «{package.GroupName}» уже существует. Перезаписать?", "Вопрос", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if(answer != DialogResult.Yes) return;

                    groupId = existingGroupId.Value;
                    var existingGroup = await _groupRepository.LoadAsync(groupId);
                    if(existingGroup != null)
                    {
                        existingGroup.DeviceIds.Clear();
                        await _groupRepository.SaveAsync(existingGroup);
                    }
                }
                else
                {
                    groupId = await _groupRepository.SaveAsync(new DevicesGroupDto { Name = package.GroupName });
                }

                var groupDto = await _groupRepository.LoadAsync(groupId) ?? new DevicesGroupDto { Id = groupId, Name = package.GroupName };
                if(groupDto.DeviceIds == null) groupDto.DeviceIds = new List<long>();

                foreach(var device in package.Devices)
                {
                    device.Id = 0;
                    long deviceId = await _devicesRepository.SaveAsync(device);
                    device.Id = deviceId;

                    if(!groupDto.DeviceIds.Contains(deviceId))
                    {
                        groupDto.DeviceIds.Add(deviceId);
                    }
                }

                await _groupRepository.SaveAsync(groupDto);
                await ReloadAsync(groupId);
                CenteredMessageBox.Show(this, "Группа успешно загружена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this, "Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                Enabled = true;
            }
        }

        private async void SaveGroupButton_Click(object sender, EventArgs e)
        {
            var selectedGroup = SelectedGroup;
            if(selectedGroup == null)
            {
                CenteredMessageBox.Show(this, "Выберите группу для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = $"{selectedGroup.Name}.json" };
            if(sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                Enabled = false;

                var devices = await _groupRepository.GetDevicesAsync(selectedGroup.Id);

                var package = new DevicesGroupPackage
                {
                    GroupName = selectedGroup.Name,
                    Devices = devices
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(package, options);
                await File.WriteAllTextAsync(sfd.FileName, json);

                CenteredMessageBox.Show(this, "Группа успешно сохранена в JSON.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this, "Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                Enabled = true;
            }
        }

        /// <summary>
        /// Переименование выбранной группы.
        /// Открывает GroupNameDialog, при OK — сохраняет новое имя в БД
        /// и перезагружает список.
        /// </summary>
        private async void EditGroupButton_Click(object sender, EventArgs e)
        {
            var selected = SelectedGroup;
            if(selected == null)
            {
                CenteredMessageBox.Show(this, "Сначала выберите группу.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new GroupNameDialog(selected.Name ?? "");
            if(dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string newName = dlg.GroupName?.Trim();
            if(string.IsNullOrWhiteSpace(newName))
                return;

            if(string.Equals(newName, selected.Name, StringComparison.Ordinal))
                return;

            try
            {
                EditGroupButton.Enabled = false;
                UseWaitCursor = true;

                long? existingId = await _groupRepository.FindGroupIdByNameAsync(newName);
                if(existingId != null && existingId.Value != selected.Id)
                {
                    CenteredMessageBox.Show(this,
                        $"Группа с именем «{newName}» уже существует.",
                        "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var groupDto = await _groupRepository.LoadAsync(selected.Id);
                if(groupDto == null)
                {
                    CenteredMessageBox.Show(this,
                        "Группа не найдена в БД.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                groupDto.Name = newName;
                await _groupRepository.SaveAsync(groupDto);

                await ReloadAsync(selected.Id);
            }
            catch(Exception ex)
            {
                CenteredMessageBox.Show(this,
                    "Не удалось переименовать группу:\r\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EditGroupButton.Enabled = true;
                UseWaitCursor = false;
            }
        }
    }


    public class DevicesGroupPackage
    {
        public string GroupName { get; set; }
        public List<DeviceConfigDtoBase> Devices { get; set; }
    }
}