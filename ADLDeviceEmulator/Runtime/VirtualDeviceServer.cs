using ADLDeviceEmulator.Models;
using ADLDeviceEmulator.Protocol;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace ADLDeviceEmulator.Runtime;

internal sealed class VirtualDeviceServer : IAsyncDisposable
{
    private readonly VirtualDeviceConfig _config;
    private readonly IPEndPoint _statusTarget;
    private readonly object _sync = new();
    private readonly Dictionary<ParamKey, AdlpdsProtocol.ParamValue> _values = new();
    private readonly string[] _igmpSlots = new string[32];
    private readonly object _membershipSync = new();
    private readonly HashSet<IPAddress> _joinedMulticastGroups = new();

    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _statusTask;
    private int _tid;
    private IPAddress _bindAddress = IPAddress.Any;

    public event Action<string>? Log;
    public event Action<VirtualDeviceConfig>? ConfigurationChanged;

    public VirtualDeviceServer(VirtualDeviceConfig config, IPAddress bindAddress, IPEndPoint statusTarget)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _statusTarget = statusTarget;
        InitializeParameterStore(bindAddress);
    }

    public bool IsRunning => _udp != null;

    public void Start()
    {
        if(_udp != null) return;

        var udp = new UdpClient(AddressFamily.InterNetwork);
        try
        {
            udp.EnableBroadcast = true;
            udp.Client.ExclusiveAddressUse = true;
            udp.Client.ReceiveBufferSize = 256 * 1024;
            udp.Client.SendBufferSize = 64 * 1024;
            udp.Client.Bind(new IPEndPoint(_bindAddress, _config.Port));
        }
        catch
        {
            udp.Dispose();
            throw;
        }

        _udp = udp;
        RefreshMulticastMemberships();
        _cts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_cts.Token);
        _statusTask = StatusLoopAsync(_cts.Token);

        Log?.Invoke($"START {_config.Display} UUID={_config.Uuid} bind={_bindAddress}:{_config.Port} -> {_statusTarget}");
        SendStatus(_statusTarget);
    }

    /// <summary>Перестраивает таблицу параметров после редактирования частей работающего устройства.</summary>
    public void ReloadConfiguration()
    {
        lock(_sync)
            InitializeParameterStore(_bindAddress);

        RefreshMulticastMemberships();
        Log?.Invoke($"CONFIG RELOAD {_config.Display}: channels={_config.GetChannelIndexes().Length}, streams={_config.Streams.Count}");
    }

    /// <summary>
    /// Синхронизирует реальные IGMP membership сокета с текущими 32 слотами устройства.
    /// Ошибка вступления в одну группу не останавливает работу остальных частей эмулятора.
    /// </summary>
    private void RefreshMulticastMemberships()
    {
        UdpClient? udp = _udp;
        if(udp == null)
            return;

        IPAddress[] desired;
        lock(_sync)
        {
            desired = _config.GetSubscriptionList()
                .Select(address => IPAddress.TryParse(address, out var parsed) ? parsed : null)
                .Where(address => address != null && IsMulticast(address))
                .Cast<IPAddress>()
                .Distinct()
                .ToArray();
        }

        lock(_membershipSync)
        {
            var desiredSet = desired.ToHashSet();

            foreach(var group in _joinedMulticastGroups.Where(x => !desiredSet.Contains(x)).ToArray())
            {
                try
                {
                    udp.DropMulticastGroup(group);
                    _joinedMulticastGroups.Remove(group);
                    Log?.Invoke($"IGMP LEAVE {_config.Display}: {group}:{_config.Port}");
                }
                catch(Exception ex)
                {
                    // Оставляем адрес в set, чтобы следующая синхронизация повторила LEAVE.
                    Log?.Invoke($"IGMP LEAVE ERROR {_config.Display}: {group}: {ex.Message}");
                }
            }

            foreach(var group in desiredSet.Where(x => !_joinedMulticastGroups.Contains(x)))
            {
                try
                {
                    udp.JoinMulticastGroup(group, _bindAddress);
                    _joinedMulticastGroups.Add(group);
                    Log?.Invoke($"IGMP JOIN {_config.Display}: {group}:{_config.Port}");
                }
                catch(Exception ex)
                {
                    Log?.Invoke($"IGMP JOIN ERROR {_config.Display}: {group}: {ex.Message}");
                }
            }
        }
    }

    private void InitializeParameterStore(IPAddress bindAddress)
    {
        _bindAddress = bindAddress;
        _config.EnsurePartsInitialized();
        _values.Clear();
        Array.Clear(_igmpSlots, 0, _igmpSlots.Length);

        var p = _config.Parameters;
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceName, AdlpdsProtocol.ParamValue.String(_config.DeviceName));
        SetU16(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBlock, _config.Block);
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceIp, AdlpdsProtocol.IpToU32(_config.IpAddress));
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceMask, AdlpdsProtocol.IpToU32(_config.Mask));
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceGateway, AdlpdsProtocol.IpToU32(_config.Gateway));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceConfiguration, AdlpdsProtocol.ParamValue.U8(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceUuid, AdlpdsProtocol.ParamValue.Raw(_config.Uuid.ToByteArray()));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceFirmware, AdlpdsProtocol.ParamValue.U8(0));
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePriority, p.Priority);
        SetU16(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStartChannel, p.StartChannel);
        SetU16(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePictureSwitchTime, p.PictureSwitchTime);
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePictureStream, p.PictureStream);
        SetU16(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePictureFrameTime, p.PictureFrameTime);
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSystemId, p.SystemId);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceTimeRequest, p.TimeRequest);
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceLocation, AdlpdsProtocol.ParamValue.String(p.Location));

        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet2Enabled, p.ArtnetNet2Enabled ? 1 : 0);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet10Enabled, p.ArtnetNet10Enabled ? 1 : 0);
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet2Address, AdlpdsProtocol.IpToU32(p.ArtnetNet2Address));
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet2Netmask, AdlpdsProtocol.IpToU32(p.ArtnetNet2Netmask));
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet10Address, AdlpdsProtocol.IpToU32(p.ArtnetNet10Address));
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceArtnetNet10Netmask, AdlpdsProtocol.IpToU32(p.ArtnetNet10Netmask));

        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePageCount, p.PageCount);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSwapX, p.TouchscreenSwapX);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSwapY, p.TouchscreenSwapY);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBrightness, p.Brightness);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBrightnessSleep, p.BrightnessSleep);
        SetU16(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSleepTimeout, p.SleepTimeout);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceAcnVersion, p.AcnVersion);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceTargetTemperature, p.TargetTemperature);
        SetU8(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStatusTransport, _config.Transport);
        SetU32(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStatusMulticastAddress, AdlpdsProtocol.IpToU32(_config.OwnAddress));

        if(TryParseMac(_config.Mac, out byte[] mac))
            Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceMac, AdlpdsProtocol.ParamValue.Raw(mac));

        for(int i = 0; i < _igmpSlots.Length; i++)
        {
            var entry = _config.IgmpEntries.FirstOrDefault(x => x.Slot == i + 1);
            _igmpSlots[i] = entry?.Address ?? string.Empty;
            SetU32(AdlpdsProtocol.ObjectIgmp, (ushort)(i + 1), AdlpdsProtocol.ParamIgmpAddress, AdlpdsProtocol.IpToU32(_igmpSlots[i]));
        }

        InitializeExecChannels();
        InitializeStreams();
        InitializeControlParts();
        InitializeGateParts();
    }

    private void InitializeExecChannels()
    {
        foreach(var channel in _config.ExecChannels.OrderBy(x => x.Local))
        {
            ushort local = ToU16(channel.Local);
            SetU16(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelIndex, channel.GlobalIndex);
            Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String(channel.Name));
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelControl,
                string.Equals(channel.Control, "dmx", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            SetU32(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelStreamId, channel.StreamId);
            SetU16(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelPatch, channel.Patch);
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelWide, channel.Wide ? 1 : 0);
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelType, 1);
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelCurve, channel.Curve);
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelPreheat, channel.Preheat);
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelLimit, channel.Limit);
            SetU32(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelSwitchDelay, (uint)Math.Max(0, channel.SwitchDelay));
        }
    }

    private void InitializeStreams()
    {
        foreach(var stream in _config.Streams.OrderBy(x => x.Index))
        {
            ushort index = ToU16(stream.Index);
            SetU32(AdlpdsProtocol.ObjectStream, index, AdlpdsProtocol.ParamStreamId, stream.PackedId);
            SetU16(AdlpdsProtocol.ObjectStream, index, AdlpdsProtocol.ParamStreamStart, stream.Address);
        }
    }

    private void InitializeControlParts()
    {
        foreach(var channel in _config.ControlChannels.OrderBy(x => x.Local))
        {
            ushort local = ToU16(channel.Local);
            SetU16(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelIndex, channel.Index);
            Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String(channel.Name));
            SetU8(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelType, channel.Type);
        }

        foreach(var group in _config.Groups.OrderBy(x => x.Local))
        {
            ushort local = ToU16(group.Local);
            SetU16(AdlpdsProtocol.ObjectGroup, local, AdlpdsProtocol.ParamGroupIndex, group.Index);
            int[] channels = group.GetChannels();
            for(int slot = 0; slot < 100; slot++)
                SetU16(AdlpdsProtocol.ObjectGroup, local, (ushort)(AdlpdsProtocol.ParamGroupChannelStart + slot), slot < channels.Length ? channels[slot] : 0);
        }

        foreach(var picture in _config.Pictures.OrderBy(x => x.Local))
        {
            ushort local = ToU16(picture.Local);
            Set(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureName, AdlpdsProtocol.ParamValue.String(picture.Name));
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureTriggerPresetWrite, picture.TriggerPresetWrite);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureFadeIn, picture.FadeIn);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureFadeOut, picture.FadeOut);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPicturePreset, picture.Preset);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureMux1, picture.Mux1);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureMux2, picture.Mux2);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureMux3, picture.Mux3);
            SetU8(AdlpdsProtocol.ObjectPicture, local, AdlpdsProtocol.ParamPictureMux4, picture.Mux4);
        }

        foreach(var button in _config.Buttons.OrderBy(x => x.Local))
        {
            ushort local = ToU16(button.Local);
            SetU8(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonPage, button.Page);
            SetU16(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonLocal, button.LocalChannel);
            SetU16(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonPosX, button.PosX);
            SetU16(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonPosY, button.PosY);
            SetU8(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonIcon, button.Icon);
            SetU8(AdlpdsProtocol.ObjectControlButton, local, AdlpdsProtocol.ParamControlButtonColor, button.Color);
        }
    }

    private void InitializeGateParts()
    {
        foreach(var channel in _config.PixelgateChannels.OrderBy(x => x.Local))
        {
            ushort local = ToU16(channel.Local);
            Set(AdlpdsProtocol.ObjectPixelgateChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String(channel.Name));
            Set(AdlpdsProtocol.ObjectPixelgateChannel, local, AdlpdsProtocol.ParamPixelgateLineName, AdlpdsProtocol.ParamValue.String(channel.Name));
            SetU16(AdlpdsProtocol.ObjectPixelgateChannel, local, AdlpdsProtocol.ParamChannelIndex, channel.Index);
        }

        foreach(var conveyor in _config.GateConveyors.OrderBy(x => x.Local))
        {
            ushort local = ToU16(conveyor.Local);
            SetU8(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorRecordable, conveyor.Recordable ? 1 : 0);
            SetU32(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSourceA, conveyor.SourceA);
            SetU32(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSourceB, conveyor.SourceB);
            SetU32(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSourceC, conveyor.SourceC);
            SetU32(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorDestination, conveyor.Destination);
            SetU8(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSourceSelect, conveyor.SourceSelect);
            SetU8(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSumType, conveyor.SumType);
            SetU16(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSumBFrom, conveyor.SumBFrom);
            SetU16(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSumBTo, conveyor.SumBTo);
            SetU16(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSumCFrom, conveyor.SumCFrom);
            SetU16(AdlpdsProtocol.ObjectGateConveyor, local, AdlpdsProtocol.ParamGateConveyorSumCTo, conveyor.SumCTo);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        UdpClient? udp = _udp;
        if(udp == null) return;

        while(!ct.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult received = await udp.ReceiveAsync(ct).ConfigureAwait(false);
                ProcessDatagram(received.Buffer, received.RemoteEndPoint);
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
            catch(ObjectDisposedException) { break; }
            catch(Exception ex)
            {
                if(!ct.IsCancellationRequested)
                    Log?.Invoke($"ERROR receive {_config.Display}: {ex.Message}");
            }
        }
    }

    private void ProcessDatagram(byte[] bytes, IPEndPoint remote)
    {
        if(!AdlpdsProtocol.TryParsePacket(bytes, out var packet, out string error) || packet == null)
        {
            Log?.Invoke($"RX INVALID from {remote}: {error}");
            return;
        }

        if((packet.Flags & AdlpdsProtocol.ResponseFlag) != 0)
            return;

        switch(packet.Command)
        {
            case AdlpdsProtocol.CommandGetParam:
                HandleGetParam(packet, remote);
                break;
            case AdlpdsProtocol.CommandSetParam:
                HandleSetParam(packet, remote);
                break;
            case AdlpdsProtocol.CommandGetStatus:
                Log?.Invoke($"GET_STATUS from {remote}");
                SendStatus(remote);
                break;
            case AdlpdsProtocol.CommandChannelSwitch:
            case AdlpdsProtocol.CommandChannelSwitchEx:
                Log?.Invoke($"RX channel-switch command={packet.Command} from {remote}; payload={Convert.ToHexString(packet.Payload)}; формат команды пока не подтверждён исходниками протокола.");
                break;
            default:
                Log?.Invoke($"RX unknown command={packet.Command} from {remote}");
                break;
        }
    }

    private void HandleGetParam(AdlpdsProtocol.ParsedPacket packet, IPEndPoint remote)
    {
        if(!AdlpdsProtocol.TryParseRequestIds(packet.Payload, out var ids))
        {
            Log?.Invoke($"GET_PARAM malformed from {remote}");
            return;
        }

        List<(AdlpdsProtocol.ParamId Id, AdlpdsProtocol.ParamValue Value)> fields = new();
        lock(_sync)
        {
            foreach(var id in ids)
            {
                if(_values.TryGetValue(new ParamKey(id.ObjectType, id.Index, id.Param), out var value))
                    fields.Add((id, value));
            }
        }

        byte[] payload = AdlpdsProtocol.BuildParamList(fields);
        SendPacket(AdlpdsProtocol.CommandGetParam, packet.Tid, AdlpdsProtocol.ResponseFlag, payload, remote);
        Log?.Invoke($"GET_PARAM {fields.Count}/{ids.Count} fields from {remote}");
    }

    private void HandleSetParam(AdlpdsProtocol.ParsedPacket packet, IPEndPoint remote)
    {
        if(!AdlpdsProtocol.TryParseFields(packet.Payload, out var fields))
        {
            Log?.Invoke($"SET_PARAM malformed from {remote}");
            return;
        }

        List<(AdlpdsProtocol.ParamId Id, AdlpdsProtocol.ParamValue Value)> echoes = new();
        lock(_sync)
        {
            foreach(var (id, value) in fields)
            {
                ApplyValue(id, value);
                if(_values.TryGetValue(new ParamKey(id.ObjectType, id.Index, id.Param), out var current))
                    echoes.Add((id, current));
            }
        }

        byte[] payload = AdlpdsProtocol.BuildParamList(echoes);
        SendPacket(AdlpdsProtocol.CommandSetParam, packet.Tid, AdlpdsProtocol.ResponseFlag, payload, remote);
        RefreshMulticastMemberships();
        Log?.Invoke($"SET_PARAM {fields.Count} fields from {remote}");
        ConfigurationChanged?.Invoke(_config);
    }

    private void ApplyValue(AdlpdsProtocol.ParamId id, AdlpdsProtocol.ParamValue value)
    {
        _values[new ParamKey(id.ObjectType, id.Index, id.Param)] = value;
        ushort index = id.Index;
        byte u8 = value.AsU8();
        ushort u16 = value.AsU16();
        uint u32 = value.AsU32();
        var p = _config.Parameters;

        if(id.ObjectType == AdlpdsProtocol.ObjectDevice)
        {
            switch(id.Param)
            {
                case AdlpdsProtocol.ParamDeviceName: _config.DeviceName = value.AsString(); break;
                case AdlpdsProtocol.ParamDeviceBlock:
                    _config.Block = u16;
                    break;
                case AdlpdsProtocol.ParamDeviceIp: _config.IpAddress = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceMask: _config.Mask = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceGateway: _config.Gateway = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceMac:
                    if(value.Data.Length >= 6)
                        _config.Mac = string.Join(":", value.Data.Take(6).Select(b => b.ToString("X2")));
                    break;
                case AdlpdsProtocol.ParamDeviceUuid:
                    if(value.Data.Length == 16) _config.Uuid = new Guid(value.Data);
                    break;
                case AdlpdsProtocol.ParamDevicePriority: p.Priority = u8; break;
                case AdlpdsProtocol.ParamDeviceStartChannel:
                    p.StartChannel = u16;
                    if(u16 >= 1)
                        _config.Block = ((u16 - 1) / 24) + 1;
                    break;
                case AdlpdsProtocol.ParamDevicePictureSwitchTime: p.PictureSwitchTime = u16; break;
                case AdlpdsProtocol.ParamDevicePictureStream: p.PictureStream = u32; break;
                case AdlpdsProtocol.ParamDevicePictureFrameTime: p.PictureFrameTime = u16; break;
                case AdlpdsProtocol.ParamDeviceSystemId: p.SystemId = u32; break;
                case AdlpdsProtocol.ParamDeviceTimeRequest: p.TimeRequest = u8; break;
                case AdlpdsProtocol.ParamDeviceLocation: p.Location = value.AsString(); break;
                case AdlpdsProtocol.ParamDeviceArtnetNet2Enabled: p.ArtnetNet2Enabled = u8 != 0; break;
                case AdlpdsProtocol.ParamDeviceArtnetNet10Enabled: p.ArtnetNet10Enabled = u8 != 0; break;
                case AdlpdsProtocol.ParamDeviceArtnetNet2Address: p.ArtnetNet2Address = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceArtnetNet2Netmask: p.ArtnetNet2Netmask = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceArtnetNet10Address: p.ArtnetNet10Address = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDeviceArtnetNet10Netmask: p.ArtnetNet10Netmask = AdlpdsProtocol.U32ToIp(u32); break;
                case AdlpdsProtocol.ParamDevicePageCount: p.PageCount = u8; break;
                case AdlpdsProtocol.ParamDeviceSwapX: p.TouchscreenSwapX = u8; break;
                case AdlpdsProtocol.ParamDeviceSwapY: p.TouchscreenSwapY = u8; break;
                case AdlpdsProtocol.ParamDeviceBrightness: p.Brightness = u8; break;
                case AdlpdsProtocol.ParamDeviceBrightnessSleep: p.BrightnessSleep = u8; break;
                case AdlpdsProtocol.ParamDeviceSleepTimeout: p.SleepTimeout = u16; break;
                case AdlpdsProtocol.ParamDeviceAcnVersion: p.AcnVersion = u8; break;
                case AdlpdsProtocol.ParamDeviceTargetTemperature: p.TargetTemperature = u8; break;
                case AdlpdsProtocol.ParamDeviceStatusTransport: _config.Transport = u8; break;
                case AdlpdsProtocol.ParamDeviceStatusMulticastAddress:
                    _config.OwnAddress = u32 == 0 ? string.Empty : AdlpdsProtocol.U32ToIp(u32);
                    break;
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectIgmp && id.Param == AdlpdsProtocol.ParamIgmpAddress && index is >= 1 and <= 32)
        {
            _igmpSlots[index - 1] = u32 == 0 ? string.Empty : AdlpdsProtocol.U32ToIp(u32);
            _config.IgmpEntries = _igmpSlots
                .Select((address, slot) => new EmulatorIgmpEntry { Slot = slot + 1, Address = address })
                .Where(x => !string.IsNullOrWhiteSpace(x.Address))
                .ToList();
            _config.Subscriptions = string.Join(";", _config.IgmpEntries.OrderBy(x => x.Slot).Select(x => x.Address));
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectStream)
        {
            var stream = FindOrCreate(_config.Streams, x => x.Index == index, () => new EmulatorStream { Index = index, Address = 1 });
            if(id.Param == AdlpdsProtocol.ParamStreamId)
            {
                stream.Protocol = VirtualDeviceConfig.StreamProtocolName(u32);
                stream.Universe = (int)(u32 & 0xFFFF);
            }
            else if(id.Param == AdlpdsProtocol.ParamStreamStart)
                stream.Address = u16;
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectChannel)
        {
            if(VirtualDeviceConfig.IsExecType(VirtualDeviceConfig.NormalizeType(_config.Type)))
            {
                var channel = FindOrCreate(_config.ExecChannels, x => x.Local == index,
                    () => new EmulatorExecChannel { Local = index, Name = $"Channel {index}", Limit = 100 });
                ApplyExecChannel(channel, id.Param, value, u8, u16, u32);
                _config.SyncLegacyChannelIndexes();
            }
            else if(VirtualDeviceConfig.IsControlType(VirtualDeviceConfig.NormalizeType(_config.Type)))
            {
                var channel = FindOrCreate(_config.ControlChannels, x => x.Local == index,
                    () => new EmulatorControlChannel { Local = index, Name = $"Control {index}", Type = 1 });
                switch(id.Param)
                {
                    case AdlpdsProtocol.ParamChannelIndex: channel.Index = u16; break;
                    case AdlpdsProtocol.ParamChannelName: channel.Name = value.AsString(); break;
                    case AdlpdsProtocol.ParamChannelType: channel.Type = u8; break;
                }
                _config.SyncLegacyChannelIndexes();
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectPixelgateChannel)
        {
            var channel = FindOrCreate(_config.PixelgateChannels, x => x.Local == index,
                () => new EmulatorPixelgateChannel { Local = index, Name = $"Line {index}" });
            if(id.Param == AdlpdsProtocol.ParamChannelName || id.Param == AdlpdsProtocol.ParamPixelgateLineName)
                channel.Name = value.AsString();
            else if(id.Param == AdlpdsProtocol.ParamChannelIndex)
                channel.Index = u16;
            _config.SyncLegacyChannelIndexes();
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectGroup)
        {
            var group = FindOrCreate(_config.Groups, x => x.Local == index,
                () => new EmulatorGroup { Local = index });
            if(id.Param == AdlpdsProtocol.ParamGroupIndex)
                group.Index = u16;
            else if(id.Param >= AdlpdsProtocol.ParamGroupChannelStart && id.Param < AdlpdsProtocol.ParamGroupChannelEnd)
            {
                int slot = id.Param - AdlpdsProtocol.ParamGroupChannelStart;
                int[] channels = group.GetChannels();
                var expanded = Enumerable.Range(0, Math.Max(channels.Length, slot + 1))
                    .Select(i => i < channels.Length ? channels[i] : 0).ToArray();
                expanded[slot] = u16;
                group.Channels = string.Join(",", expanded);
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectPicture)
        {
            var picture = FindOrCreate(_config.Pictures, x => x.Local == index,
                () => new EmulatorPicture { Local = index });
            switch(id.Param)
            {
                case AdlpdsProtocol.ParamPictureName: picture.Name = value.AsString(); break;
                case AdlpdsProtocol.ParamPictureTriggerPresetWrite: picture.TriggerPresetWrite = u8; break;
                case AdlpdsProtocol.ParamPictureFadeIn: picture.FadeIn = u8; break;
                case AdlpdsProtocol.ParamPictureFadeOut: picture.FadeOut = u8; break;
                case AdlpdsProtocol.ParamPicturePreset: picture.Preset = u8; break;
                case AdlpdsProtocol.ParamPictureMux1: picture.Mux1 = u8; break;
                case AdlpdsProtocol.ParamPictureMux2: picture.Mux2 = u8; break;
                case AdlpdsProtocol.ParamPictureMux3: picture.Mux3 = u8; break;
                case AdlpdsProtocol.ParamPictureMux4: picture.Mux4 = u8; break;
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectControlButton)
        {
            var button = FindOrCreate(_config.Buttons, x => x.Local == index,
                () => new EmulatorControlButton { Local = index, Page = 1 });
            switch(id.Param)
            {
                case AdlpdsProtocol.ParamControlButtonPage: button.Page = u8; break;
                case AdlpdsProtocol.ParamControlButtonLocal: button.LocalChannel = u16; break;
                case AdlpdsProtocol.ParamControlButtonPosX: button.PosX = u16; break;
                case AdlpdsProtocol.ParamControlButtonPosY: button.PosY = u16; break;
                case AdlpdsProtocol.ParamControlButtonIcon: button.Icon = u8; break;
                case AdlpdsProtocol.ParamControlButtonColor: button.Color = u8; break;
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectGateConveyor)
        {
            var conveyor = FindOrCreate(_config.GateConveyors, x => x.Local == index,
                () => new EmulatorGateConveyor { Local = index });
            switch(id.Param)
            {
                case AdlpdsProtocol.ParamGateConveyorRecordable: conveyor.Recordable = u8 != 0; break;
                case AdlpdsProtocol.ParamGateConveyorSourceA: conveyor.SourceA = u32; break;
                case AdlpdsProtocol.ParamGateConveyorSourceB: conveyor.SourceB = u32; break;
                case AdlpdsProtocol.ParamGateConveyorSourceC: conveyor.SourceC = u32; break;
                case AdlpdsProtocol.ParamGateConveyorDestination: conveyor.Destination = u32; break;
                case AdlpdsProtocol.ParamGateConveyorSourceSelect: conveyor.SourceSelect = u8; break;
                case AdlpdsProtocol.ParamGateConveyorSumType: conveyor.SumType = u8; break;
                case AdlpdsProtocol.ParamGateConveyorSumBFrom: conveyor.SumBFrom = u16; break;
                case AdlpdsProtocol.ParamGateConveyorSumBTo: conveyor.SumBTo = u16; break;
                case AdlpdsProtocol.ParamGateConveyorSumCFrom: conveyor.SumCFrom = u16; break;
                case AdlpdsProtocol.ParamGateConveyorSumCTo: conveyor.SumCTo = u16; break;
            }
        }
    }

    private static void ApplyExecChannel(EmulatorExecChannel channel, ushort param,
        AdlpdsProtocol.ParamValue value, byte u8, ushort u16, uint u32)
    {
        switch(param)
        {
            case AdlpdsProtocol.ParamChannelName: channel.Name = value.AsString(); break;
            case AdlpdsProtocol.ParamChannelIndex: channel.GlobalIndex = u16; break;
            case AdlpdsProtocol.ParamChannelControl: channel.Control = u8 == 1 ? "dmx" : "adlp"; break;
            case AdlpdsProtocol.ParamChannelStreamId: channel.StreamId = u32; break;
            case AdlpdsProtocol.ParamChannelPatch: channel.Patch = u16; break;
            case AdlpdsProtocol.ParamChannelWide: channel.Wide = u8 != 0; break;
            case AdlpdsProtocol.ParamChannelCurve: channel.Curve = u8; break;
            case AdlpdsProtocol.ParamChannelPreheat: channel.Preheat = u8; break;
            case AdlpdsProtocol.ParamChannelLimit: channel.Limit = u8; break;
            case AdlpdsProtocol.ParamChannelSwitchDelay: channel.SwitchDelay = (int)Math.Min(u32, (uint)int.MaxValue); break;
        }
    }

    private static T FindOrCreate<T>(List<T> items, Func<T, bool> predicate, Func<T> create)
    {
        var item = items.FirstOrDefault(predicate);
        if(item is not null) return item;
        item = create();
        items.Add(item);
        return item;
    }

    private static ushort ToU16(int value) => (ushort)Math.Clamp(value, 0, ushort.MaxValue);
    private void Set(byte objectType, ushort index, ushort param, AdlpdsProtocol.ParamValue value)
        => _values[new ParamKey(objectType, index, param)] = value;
    private void SetU8(byte objectType, ushort index, ushort param, int value)
        => Set(objectType, index, param, AdlpdsProtocol.ParamValue.U8((byte)Math.Clamp(value, 0, byte.MaxValue)));
    private void SetU16(byte objectType, ushort index, ushort param, int value)
        => Set(objectType, index, param, AdlpdsProtocol.ParamValue.U16(ToU16(value)));
    private void SetU32(byte objectType, ushort index, ushort param, uint value)
        => Set(objectType, index, param, AdlpdsProtocol.ParamValue.U32(value));

    private async Task StatusLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while(await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                SendStatus(_statusTarget);
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested) { }
    }

    private void SendStatus(IPEndPoint target)
    {
        ushort port = (ushort)Math.Clamp(_config.Port, 1, ushort.MaxValue);
        ushort block = (ushort)Math.Clamp(_config.Block, 0, ushort.MaxValue);
        byte[] payload = AdlpdsProtocol.BuildStatusPayload(_config.Uuid, _config.Type, port, block);

        // Если включён multicast-режим и собственная группа задана, STATUS идёт в неё.
        if(_config.Transport == 1 && IPAddress.TryParse(_config.OwnAddress, out var group) && IsMulticast(group))
            target = new IPEndPoint(group, _statusTarget.Port);

        SendPacket(AdlpdsProtocol.CommandStatus, NextTid(), 0, payload, target);
    }

    private void SendPacket(ushort command, ushort tid, ushort flags, byte[] payload, IPEndPoint target)
    {
        try
        {
            byte[] bytes = AdlpdsProtocol.BuildPacket(command, tid, flags, payload);
            _udp?.Send(bytes, bytes.Length, target);
        }
        catch(Exception ex)
        {
            Log?.Invoke($"ERROR send {_config.Display}: {ex.Message}");
        }
    }

    private ushort NextTid() => unchecked((ushort)Interlocked.Increment(ref _tid));

    private static bool IsMulticast(IPAddress address)
    {
        if(address.AddressFamily != AddressFamily.InterNetwork) return false;
        byte first = address.GetAddressBytes()[0];
        return first >= 224 && first <= 239;
    }

    private static bool TryParseMac(string? macText, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if(string.IsNullOrWhiteSpace(macText)) return false;
        string compact = macText.Replace(":", "").Replace("-", "").Replace(".", "").Trim();
        if(compact.Length != 12) return false;
        try
        {
            bytes = Enumerable.Range(0, 6)
                .Select(i => Convert.ToByte(compact.Substring(i * 2, 2), 16))
                .ToArray();
            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if(_cts == null) return;
        try
        {
            _cts.Cancel();
            _udp?.Close();
            if(_receiveTask != null) await _receiveTask.ConfigureAwait(false);
            if(_statusTask != null) await _statusTask.ConfigureAwait(false);
        }
        catch { }
        finally
        {
            _udp?.Dispose();
            _udp = null;
            lock(_membershipSync)
                _joinedMulticastGroups.Clear();
            _cts.Dispose();
            _cts = null;
            Log?.Invoke($"STOP {_config.Display}");
        }
    }

    private readonly record struct ParamKey(byte ObjectType, ushort Index, ushort Param);
}
