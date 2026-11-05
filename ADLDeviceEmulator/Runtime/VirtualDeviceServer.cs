using ADLDeviceEmulator.Models;
using ADLDeviceEmulator.Protocol;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ADLDeviceEmulator.Runtime;

internal sealed class VirtualDeviceServer : IAsyncDisposable
{
    private readonly VirtualDeviceConfig _config;
    private readonly IPEndPoint _statusTarget;
    private readonly object _sync = new();
    private readonly Dictionary<ParamKey, AdlpdsProtocol.ParamValue> _values = new();
    private readonly string[] _igmpSlots = new string[32];

    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _statusTask;
    private int _tid;

    public event Action<string>? Log;
    public event Action<VirtualDeviceConfig>? ConfigurationChanged;

    public VirtualDeviceServer(VirtualDeviceConfig config, IPAddress bindAddress, IPEndPoint statusTarget)
    {
        _config = config;
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
        _cts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_cts.Token);
        _statusTask = StatusLoopAsync(_cts.Token);

        Log?.Invoke($"START {_config.Display} UUID={_config.Uuid} bind={_bindAddress}:{_config.Port} -> {_statusTarget}");
        SendStatus(_statusTarget);
    }

    private IPAddress _bindAddress = IPAddress.Any;

    private void InitializeParameterStore(IPAddress bindAddress)
    {
        _bindAddress = bindAddress;
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceName, AdlpdsProtocol.ParamValue.String(_config.DeviceName));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBlock, AdlpdsProtocol.ParamValue.U16((ushort)Math.Clamp(_config.Block, 0, ushort.MaxValue)));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceIp, AdlpdsProtocol.ParamValue.U32(AdlpdsProtocol.IpToU32(_config.IpAddress)));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceMask, AdlpdsProtocol.ParamValue.U32(AdlpdsProtocol.IpToU32(_config.Mask)));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceGateway, AdlpdsProtocol.ParamValue.U32(AdlpdsProtocol.IpToU32(_config.Gateway)));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceConfiguration, AdlpdsProtocol.ParamValue.U8(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceUuid, AdlpdsProtocol.ParamValue.Raw(_config.Uuid.ToByteArray()));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceFirmware, AdlpdsProtocol.ParamValue.U8(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStartChannel,
            AdlpdsProtocol.ParamValue.U16((ushort)Math.Clamp(1 + (Math.Max(1, _config.Block) - 1) * 24, 1, ushort.MaxValue)));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePriority, AdlpdsProtocol.ParamValue.U8(5));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePictureSwitchTime, AdlpdsProtocol.ParamValue.U16(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDevicePageCount, AdlpdsProtocol.ParamValue.U8(1));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSwapX, AdlpdsProtocol.ParamValue.U8(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSwapY, AdlpdsProtocol.ParamValue.U8(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBrightness, AdlpdsProtocol.ParamValue.U8(100));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceBrightnessSleep, AdlpdsProtocol.ParamValue.U8(100));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceSleepTimeout, AdlpdsProtocol.ParamValue.U16(0));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStatusTransport, AdlpdsProtocol.ParamValue.U8(_config.Transport));
        Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceStatusMulticastAddress, AdlpdsProtocol.ParamValue.U32(AdlpdsProtocol.IpToU32(_config.OwnAddress)));

        if(TryParseMac(_config.Mac, out byte[] mac))
            Set(AdlpdsProtocol.ObjectDevice, 0, AdlpdsProtocol.ParamDeviceMac, AdlpdsProtocol.ParamValue.Raw(mac));

        var initialSubs = _config.GetSubscriptionList();
        for(int i = 0; i < _igmpSlots.Length; i++)
        {
            _igmpSlots[i] = i < initialSubs.Length ? initialSubs[i] : string.Empty;
            Set(AdlpdsProtocol.ObjectIgmp, (ushort)(i + 1), AdlpdsProtocol.ParamIgmpAddress,
                AdlpdsProtocol.ParamValue.U32(AdlpdsProtocol.IpToU32(_igmpSlots[i])));
        }

        InitializeChannels();
    }

    private void InitializeChannels()
    {
        int[] indexes = _config.GetChannelIndexes();
        string type = (_config.Type ?? string.Empty).Trim().ToUpperInvariant();

        if(type is "IKB" or "EXEC" or "DIM220" or "DIMCABINET")
        {
            for(int i = 0; i < indexes.Length; i++)
            {
                ushort local = (ushort)(i + 1);
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelIndex, AdlpdsProtocol.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String($"Channel {i + 1}"));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelControl, AdlpdsProtocol.ParamValue.U8(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelStreamId, AdlpdsProtocol.ParamValue.U32(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelPatch, AdlpdsProtocol.ParamValue.U16(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelWide, AdlpdsProtocol.ParamValue.U8(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelPreheat, AdlpdsProtocol.ParamValue.U8(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelLimit, AdlpdsProtocol.ParamValue.U8(100));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelCurve, AdlpdsProtocol.ParamValue.U8(0));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelSwitchDelay, AdlpdsProtocol.ParamValue.U32(0));
            }
        }
        else if(type is "PU" or "PU24")
        {
            for(int i = 0; i < indexes.Length; i++)
            {
                ushort local = (ushort)(i + 1);
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelIndex, AdlpdsProtocol.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String($"Control {i + 1}"));
                Set(AdlpdsProtocol.ObjectChannel, local, AdlpdsProtocol.ParamChannelType, AdlpdsProtocol.ParamValue.U8(1));
            }
        }
        else if(type == "INDIGATE")
        {
            for(int i = 0; i < indexes.Length; i++)
            {
                ushort local = (ushort)(i + 1);
                Set(AdlpdsProtocol.ObjectPixelgateChannel, local, AdlpdsProtocol.ParamChannelIndex, AdlpdsProtocol.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpdsProtocol.ObjectPixelgateChannel, local, AdlpdsProtocol.ParamChannelName, AdlpdsProtocol.ParamValue.String($"Line {i + 1}"));
            }
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
            catch(OperationCanceledException) when(ct.IsCancellationRequested)
            {
                break;
            }
            catch(ObjectDisposedException)
            {
                break;
            }
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
        Log?.Invoke($"SET_PARAM {fields.Count} fields from {remote}");
        ConfigurationChanged?.Invoke(_config);
    }

    private void ApplyValue(AdlpdsProtocol.ParamId id, AdlpdsProtocol.ParamValue value)
    {
        _values[new ParamKey(id.ObjectType, id.Index, id.Param)] = value;

        if(id.ObjectType == AdlpdsProtocol.ObjectDevice)
        {
            switch(id.Param)
            {
                case AdlpdsProtocol.ParamDeviceName:
                    _config.DeviceName = value.AsString();
                    break;
                case AdlpdsProtocol.ParamDeviceBlock:
                    _config.Block = value.Data.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(value.Data) : _config.Block;
                    break;
                case AdlpdsProtocol.ParamDeviceIp:
                    _config.IpAddress = AdlpdsProtocol.U32ToIp(value.AsU32());
                    break;
                case AdlpdsProtocol.ParamDeviceMask:
                    _config.Mask = AdlpdsProtocol.U32ToIp(value.AsU32());
                    break;
                case AdlpdsProtocol.ParamDeviceGateway:
                    _config.Gateway = AdlpdsProtocol.U32ToIp(value.AsU32());
                    break;
                case AdlpdsProtocol.ParamDeviceMac:
                    if(value.Data.Length >= 6)
                        _config.Mac = string.Join(":", value.Data.Take(6).Select(b => b.ToString("X2")));
                    break;
                case AdlpdsProtocol.ParamDeviceUuid:
                    if(value.Data.Length == 16)
                        _config.Uuid = new Guid(value.Data);
                    break;
                case AdlpdsProtocol.ParamDeviceStatusTransport:
                    _config.Transport = value.AsU8();
                    break;
                case AdlpdsProtocol.ParamDeviceStatusMulticastAddress:
                    _config.OwnAddress = value.AsU32() == 0 ? string.Empty : AdlpdsProtocol.U32ToIp(value.AsU32());
                    break;
            }
        }
        else if(id.ObjectType == AdlpdsProtocol.ObjectIgmp && id.Param == AdlpdsProtocol.ParamIgmpAddress && id.Index >= 1 && id.Index <= 32)
        {
            uint raw = value.AsU32();
            _igmpSlots[id.Index - 1] = raw == 0 ? string.Empty : AdlpdsProtocol.U32ToIp(raw);
            _config.SetSubscriptionList(_igmpSlots.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        else if((id.ObjectType == AdlpdsProtocol.ObjectChannel || id.ObjectType == AdlpdsProtocol.ObjectPixelgateChannel)
            && id.Param == AdlpdsProtocol.ParamChannelIndex)
        {
            var channelIndexes = _values
                .Where(x => (x.Key.ObjectType == AdlpdsProtocol.ObjectChannel || x.Key.ObjectType == AdlpdsProtocol.ObjectPixelgateChannel)
                    && x.Key.Param == AdlpdsProtocol.ParamChannelIndex)
                .OrderBy(x => x.Key.Index)
                .Select(x => x.Value.Data.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(x.Value.Data) : (ushort)0)
                .Where(x => x > 0);
            _config.ChannelIndexes = string.Join(",", channelIndexes);
        }
    }

    private async Task StatusLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while(await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                SendStatus(_statusTarget);
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested)
        {
        }
    }

    private void SendStatus(IPEndPoint target)
    {
        ushort port = (ushort)Math.Clamp(_config.Port, 1, ushort.MaxValue);
        ushort block = (ushort)Math.Clamp(_config.Block, 0, ushort.MaxValue);
        byte[] payload = AdlpdsProtocol.BuildStatusPayload(_config.Uuid, _config.Type, port, block);
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

    private void Set(byte objectType, ushort index, ushort param, AdlpdsProtocol.ParamValue value)
        => _values[new ParamKey(objectType, index, param)] = value;

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
            _cts.Dispose();
            _cts = null;
            Log?.Invoke($"STOP {_config.Display}");
        }
    }

    private readonly record struct ParamKey(byte ObjectType, ushort Index, ushort Param);
}
