namespace AdlpLib.Models.Devices
{
    public interface IDeviceCommon
    {
        string Name { get; set; }
        string Mac { get; set; }
        string Ip { get; set; }
        string Mask { get; set; }
        string Gateway { get; set; }
        int Block { get; set; }
        string MulticastAdress { get; set; }
    }
}
