using System.Net;
using System.Net.NetworkInformation;

namespace AdlpdsConfig.Controls;

public partial class fNetSelectDialog : Form
{
    public Network.NetworkInterface? SelectedInterface { get; private set; }

    public fNetSelectDialog()
    {
        InitializeComponent();
        LoadInterfaces();
    }

    private void LoadInterfaces()
    {
        var ifaces = Network.NetworkSelector.getInterfaces();
        lvInterfaces.BeginUpdate();
        foreach (var iface in ifaces)
        {
            var ip = iface.Address?.ToString() ?? "";
            var name = iface.Name;
            var mask = "";

            // Look up real adapter info
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        ua.Address.ToString() == ip)
                    {
                        if (name == ip || string.IsNullOrEmpty(name))
                            name = ni.Name;
                        mask = ua.IPv4Mask?.ToString() ?? "";
                        goto next;
                    }
                }
            }
            next:
            var item = new ListViewItem("");
            item.SubItems.Add(ip);
            item.SubItems.Add(mask);
            item.SubItems.Add(name);
            item.Tag = iface;
            lvInterfaces.Items.Add(item);
        }

        // Numbering
        for (int i = 0; i < lvInterfaces.Items.Count; i++)
            lvInterfaces.Items[i].Text = (i + 1).ToString();

        lvInterfaces.EndUpdate();
        if (lvInterfaces.Items.Count > 0)
            lvInterfaces.Items[0].Selected = true;
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        if (lvInterfaces.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select a network interface.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        SelectedInterface = (Network.NetworkInterface)lvInterfaces.SelectedItems[0].Tag;
    }

    private void lvInterfaces_DoubleClick(object? sender, EventArgs e)
    {
        if (lvInterfaces.SelectedItems.Count > 0)
        {
            SelectedInterface = (Network.NetworkInterface)lvInterfaces.SelectedItems[0].Tag;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
