using AdlpdsConfig.Controls.Database;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;

namespace AdlpdsConfig.Controls
{
    /// <summary>
    /// Окно детального просмотра конфигурации устройства.
    /// Показывает общие сведения и списки сущностей на вкладках.
    /// </summary>
    public partial class DeviceDetailsForm : Form
    {
        private readonly TabControl _tabs = new();
        private readonly DeviceDetailsViewModel _vm;

        public DeviceDetailsForm(DeviceConfigDtoBase cfg)
        {
            if(cfg == null) throw new ArgumentNullException(nameof(cfg));

            string typeCode = cfg switch
            {
                ExecConfigDto => "EXEC",
                PuConfigDto => "PU",
                _ => cfg.GetType().Name
            };

            _vm = DeviceDetailsViewModel.From(cfg, typeCode);

            Text = _vm.Title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(640, 400);

            BuildUi();
        }

        private void BuildUi()
        {
            _tabs.Dock = DockStyle.Fill;

            // Вкладка «Общее» — всегда
            _tabs.TabPages.Add(Wrap("Общее", GridOf(_vm.Common)));

            // PU-вкладки
            if(_vm.PuChannels.Count > 0 || _vm.PuGroups.Count > 0 ||
                _vm.PuPictures.Count > 0 || _vm.PuButtons.Count > 0)
            {
                _tabs.TabPages.Add(Wrap($"Каналы ({_vm.PuChannels.Count})", GridOf(_vm.PuChannels)));
                _tabs.TabPages.Add(Wrap($"Группы ({_vm.PuGroups.Count})", GridOf(_vm.PuGroups)));
                _tabs.TabPages.Add(Wrap($"Картины ({_vm.PuPictures.Count})", GridOf(_vm.PuPictures)));
                _tabs.TabPages.Add(Wrap($"Кнопки ({_vm.PuButtons.Count})", GridOf(_vm.PuButtons)));
            }

            // EXEC-вкладки
            if(_vm.ExecChannels.Count > 0 || _vm.ExecStreams.Count > 0)
            {
                _tabs.TabPages.Add(Wrap($"Каналы ({_vm.ExecChannels.Count})", GridOf(_vm.ExecChannels)));
                _tabs.TabPages.Add(Wrap($"Потоки ({_vm.ExecStreams.Count})", GridOf(_vm.ExecStreams)));
            }

            Controls.Add(_tabs);
        }

        /// <summary>
        /// Создаёт грид для списка ViewModel-объектов.
        /// Заголовки колонок берутся из <see cref="ViewHeaders"/> по типу элемента.
        /// </summary>
        private static DataGridView GridOf<T>(List<T> items)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoGenerateColumns = true,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText,
                DataSource = items
            };

            grid.DataBindingComplete += (_, _) =>
            {
                var headers = ViewHeaders.Get<T>();
                if(headers == null) return;

                foreach(DataGridViewColumn col in grid.Columns)
                {
                    string key = !string.IsNullOrEmpty(col.DataPropertyName)
                        ? col.DataPropertyName
                        : col.Name;

                    if(!string.IsNullOrEmpty(key) && headers.TryGetValue(key, out var header))
                        col.HeaderText = header;
                }
            };

            return grid;
        }

        private static TabPage Wrap(string title, Control content)
        {
            var page = new TabPage(title);
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }
    }
}