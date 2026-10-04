using AdlpdsConfig.Controls.Devices;
using AdlpdsConfig.Utils;
using AdlpLib.Configuration.Application;
using AdlpLib.Models.Devices;
using AdlpLib.Models.DevicesGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.DevicesGroup
{
    public partial class miDevicesGroup : Form
    {
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

                groupNameTextBox.Text = SelectedGroup?.Name ?? "";
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
            CreateButton.Enabled = !string.IsNullOrWhiteSpace(groupNameTextBox.Text);
        }

        private async void GroupsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(_loading) return;

            var sel = SelectedGroup;
            groupNameTextBox.Text = sel?.Name ?? "";

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
            var selectedGroup = SelectedGroup;
            if(selectedGroup == null)
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

                using(var editDlg = new EditDeviceParametersForm(newDevice))
                {
                    if(editDlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    try
                    {
                        DeviceConfigDtoBase editedDevice = editDlg.CurrentDevice;

                        CreateDeviceButton.Enabled = false;
                        UseWaitCursor = true;

                        long deviceId = await _devicesRepository.SaveAsync(newDevice);
                        newDevice.Id = deviceId;

                        DevicesGroupDto groupDto = await _groupRepository.LoadAsync(selectedGroup.Id);
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

                        await ReloadAsync(selectedGroup.Id);

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
                $"Удалить устройство «{device.Ip}» полностью?\r\n" +
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
    }
}