using AdlpLib.Models.Devices.NetGate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdlpdsConfig.Controls.Devices.NetGate
{
    /// <summary>
    /// Диалог редактирования свойств одного конвейера NetGate.
    /// </summary>
    public partial class ConveyorPropertiesForm : Form
    {
        public NetGateConveyorCfg Conveyor { get; private set; }

        private static readonly string[] Protocols = { "нет", "sACN", "artnet", "dmx" };
        private static readonly string[] SourceSelectItems = { "0 — A", "1 — B", "2 — C" };
        private static readonly string[] SumTypeItems = { "0 — максимум", "1 — HTP", "2 — LTP" };

        public ConveyorPropertiesForm(NetGateConveyorCfg conveyor)
        {
            InitializeComponent();

            Conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));

            // Заголовок
            Text = $"Конвейер {conveyor.Local}";

            // Заполняем combo-боксы
            cbSourceAProtocol.Items.AddRange(Protocols);
            cbSourceBProtocol.Items.AddRange(Protocols);
            cbSourceCProtocol.Items.AddRange(Protocols);
            cbDestProtocol.Items.AddRange(Protocols);

            cbSourceSelect.Items.AddRange(SourceSelectItems);
            cbSumType.Items.AddRange(SumTypeItems);

            // Источники
            cbSourceAProtocol.SelectedItem = conveyor.SourceA?.Protocol ?? "нет";
            udSourceAUniverse.Value = Clamp(conveyor.SourceA?.Universe ?? 0, udSourceAUniverse);

            cbSourceBProtocol.SelectedItem = conveyor.SourceB?.Protocol ?? "нет";
            udSourceBUniverse.Value = Clamp(conveyor.SourceB?.Universe ?? 0, udSourceBUniverse);

            cbSourceCProtocol.SelectedItem = conveyor.SourceC?.Protocol ?? "нет";
            udSourceCUniverse.Value = Clamp(conveyor.SourceC?.Universe ?? 0, udSourceCUniverse);

            cbDestProtocol.SelectedItem = conveyor.Destination?.Protocol ?? "нет";
            udDestUniverse.Value = Clamp(conveyor.Destination?.Universe ?? 0, udDestUniverse);

            // Обработка
            cbSourceSelect.SelectedIndex = Math.Max(0, Math.Min(cbSourceSelect.Items.Count - 1, conveyor.SourceSelect));
            cbSumType.SelectedIndex = Math.Max(0, Math.Min(cbSumType.Items.Count - 1, conveyor.SumType));
            udFrequency.Value = Clamp(conveyor.Frequency, udFrequency);
            chkRecordable.Checked = conveyor.Recordable;
            chkTimeouted.Checked = conveyor.Timeouted;
            chkRdm.Checked = conveyor.RdmEnabled;
            udSumBFrom.Value = Clamp(conveyor.SumBFrom, udSumBFrom);
            udSumBTo.Value = Clamp(conveyor.SumBTo, udSumBTo);
            udSumCFrom.Value = Clamp(conveyor.SumCFrom, udSumCFrom);
            udSumCTo.Value = Clamp(conveyor.SumCTo, udSumCTo);
        }

        private static decimal Clamp(int value, NumericUpDown ud)
            => Math.Max(ud.Minimum, Math.Min(ud.Maximum, value));

        private void OkButton_Click(object sender, EventArgs e)
        {
            Conveyor.SourceA = new NetGateSourceCfg
            {
                Protocol = cbSourceAProtocol.SelectedItem?.ToString() ?? "нет",
                Universe = (int)udSourceAUniverse.Value
            };
            Conveyor.SourceB = new NetGateSourceCfg
            {
                Protocol = cbSourceBProtocol.SelectedItem?.ToString() ?? "нет",
                Universe = (int)udSourceBUniverse.Value
            };
            Conveyor.SourceC = new NetGateSourceCfg
            {
                Protocol = cbSourceCProtocol.SelectedItem?.ToString() ?? "нет",
                Universe = (int)udSourceCUniverse.Value
            };
            Conveyor.Destination = new NetGateSourceCfg
            {
                Protocol = cbDestProtocol.SelectedItem?.ToString() ?? "нет",
                Universe = (int)udDestUniverse.Value
            };

            Conveyor.SourceSelect = cbSourceSelect.SelectedIndex;
            Conveyor.SumType = cbSumType.SelectedIndex;
            Conveyor.Frequency = (int)udFrequency.Value;
            Conveyor.Recordable = chkRecordable.Checked;
            Conveyor.Timeouted = chkTimeouted.Checked;
            Conveyor.RdmEnabled = chkRdm.Checked;
            Conveyor.SumBFrom = (int)udSumBFrom.Value;
            Conveyor.SumBTo = (int)udSumBTo.Value;
            Conveyor.SumCFrom = (int)udSumCFrom.Value;
            Conveyor.SumCTo = (int)udSumCTo.Value;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
