// EF Core entities for the existing ADL database schema.
// Keep these properties aligned with AdlpSchemaSql.cs.
#nullable enable
using System;

namespace AdlpLib.Persistence.Entities;

public sealed class DeviceEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConfigType { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int? SID { get; set; }
    public string Mac { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Mask { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public int Block { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class PuDeviceEntity
{
    public long DeviceId { get; set; }
    public int Priority { get; set; }
    public int PictureSwitchTime { get; set; }
    public int PageCount { get; set; }
    public int SwapX { get; set; }
    public int SwapY { get; set; }
    public int Brightness { get; set; }
    public int BrightnessSleep { get; set; }
    public int SleepTimeout { get; set; }
}

public sealed class PuChannelEntity
{
    public long Id { get; set; }
    public int Local { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AdlpIndex { get; set; }
    public int Type { get; set; }
}

public sealed class PuDeviceChannelEntity
{
    public long DeviceId { get; set; }
    public long ChannelId { get; set; }
    public int Position { get; set; }
}

public sealed class PuGroupEntity
{
    public long Id { get; set; }
    public int Local { get; set; }
    public int AdlpIndex { get; set; }
}

public sealed class PuDeviceGroupEntity
{
    public long DeviceId { get; set; }
    public long GroupId { get; set; }
    public int Position { get; set; }
}

public sealed class PuGroupChannelEntity
{
    public long GroupId { get; set; }
    public int ChannelAdlpIndex { get; set; }
    public int Slot { get; set; }
}

public sealed class PuPictureEntity
{
    public long Id { get; set; }
    public int Local { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class PuDevicePictureEntity
{
    public long DeviceId { get; set; }
    public long PictureId { get; set; }
    public int Position { get; set; }
}

public sealed class PuButtonEntity
{
    public long Id { get; set; }
    public int Local { get; set; }
    public int Page { get; set; }
    public int LocalChannel { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Icon { get; set; }
    public int Color { get; set; }
}

public sealed class PuDeviceButtonEntity
{
    public long DeviceId { get; set; }
    public long ButtonId { get; set; }
    public int Position { get; set; }
}

public sealed class PuButtonChannelEntity
{
    public long DeviceId { get; set; }
    public long ButtonId { get; set; }
    public int ChannelLocal { get; set; }
}

public sealed class ExecDeviceEntity
{
    public long DeviceId { get; set; }
    public string BaseChannel { get; set; } = string.Empty;
}

public sealed class ExecStreamEntity
{
    public long Id { get; set; }
    public int StreamIndex { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int Universe { get; set; }
    public int Address { get; set; }
}

public sealed class ExecDeviceStreamEntity
{
    public long DeviceId { get; set; }
    public long StreamId { get; set; }
    public int Position { get; set; }
}

public sealed class ExecChannelEntity
{
    public long Id { get; set; }
    public int Local { get; set; }
    public int GlobalIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Control { get; set; } = string.Empty;
    public bool Wide { get; set; }
    public int Patch { get; set; }
    public int Preheat { get; set; }
    public int LimitValue { get; set; }
    public int Curve { get; set; }
    public int SwitchDelay { get; set; }
}

public sealed class ExecDeviceChannelEntity
{
    public long DeviceId { get; set; }
    public long ChannelId { get; set; }
    public int Position { get; set; }
}

public sealed class ExecChannelStreamEntity
{
    public long DeviceId { get; set; }
    public long ChannelId { get; set; }
    public int StreamIndex { get; set; }
}

public sealed class AuditContextEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class NetGateDeviceEntity
{
    public long DeviceId { get; set; }
    public int ConveyorCount { get; set; }
}

public sealed class NetGateConveyorEntity
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public int Local { get; set; }
    public string SourceAProtocol { get; set; } = string.Empty;
    public int SourceAUniverse { get; set; }
    public string SourceBProtocol { get; set; } = string.Empty;
    public int SourceBUniverse { get; set; }
    public string SourceCProtocol { get; set; } = string.Empty;
    public int SourceCUniverse { get; set; }
    public string DestinationProtocol { get; set; } = string.Empty;
    public int DestinationUniverse { get; set; }
    public int SourceSelect { get; set; }
    public int SumType { get; set; }
    public int Frequency { get; set; }
    public bool Recordable { get; set; }
    public bool Timeouted { get; set; }
    public bool RdmEnabled { get; set; }
    public int SumBFrom { get; set; }
    public int SumBTo { get; set; }
    public int SumCFrom { get; set; }
    public int SumCTo { get; set; }
}

public sealed class NetGateChannelEntity
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public int Local { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AdlpIndex { get; set; }
}

public sealed class NetGatePictureEntity
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public int Local { get; set; }
    public int Picture { get; set; }
    public int FadeIn { get; set; }
    public int FadeOut { get; set; }
    public int Mux1 { get; set; }
    public int Mux2 { get; set; }
    public int Mux3 { get; set; }
    public int Mux4 { get; set; }
}

public sealed class DeviceGroupEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class DeviceGroupDeviceEntity
{
    public long GroupId { get; set; }
    public long DeviceId { get; set; }
    public int Position { get; set; }
}