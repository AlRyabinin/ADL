using ADLDeviceEmulator.Models;
using AdlpLib.Protocol;
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
    private readonly Dictionary<ParamKey, AdlpFrame.ParamValue> _values = new();
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
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceName, AdlpFrame.ParamValue.String(_config.DeviceName));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceBlock, AdlpFrame.ParamValue.U16((ushort)Math.Clamp(_config.Block, 0, ushort.MaxValue)));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceIp, AdlpFrame.ParamValue.U32(AdlpFrame.IpToU32(_config.IpAddress)));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceMask, AdlpFrame.ParamValue.U32(AdlpFrame.IpToU32(_config.Mask)));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceGateway, AdlpFrame.ParamValue.U32(AdlpFrame.IpToU32(_config.Gateway)));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceConfiguration, AdlpFrame.ParamValue.U8(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceUuid, AdlpFrame.ParamValue.Raw(_config.Uuid.ToByteArray()));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceFirmware, AdlpFrame.ParamValue.U8(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceStartChannel,
            AdlpFrame.ParamValue.U16((ushort)Math.Clamp(1 + (Math.Max(1, _config.Block) - 1) * 24, 1, ushort.MaxValue)));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDevicePriority, AdlpFrame.ParamValue.U8(5));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDevicePictureSwitchTime, AdlpFrame.ParamValue.U16(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDevicePageCount, AdlpFrame.ParamValue.U8(1));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceSwapX, AdlpFrame.ParamValue.U8(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceSwapY, AdlpFrame.ParamValue.U8(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceBrightness, AdlpFrame.ParamValue.U8(100));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceBrightnessSleep, AdlpFrame.ParamValue.U8(100));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceSleepTimeout, AdlpFrame.ParamValue.U16(0));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceStatusTransport, AdlpFrame.ParamValue.U8(_config.Transport));
        Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceStatusMulticastAddress, AdlpFrame.ParamValue.U32(AdlpFrame.IpToU32(_config.OwnAddress)));

        if(TryParseMac(_config.Mac, out byte[] mac))
            Set(AdlpFrame.ObjectDevice, 0, AdlpFrame.ParamDeviceMac, AdlpFrame.ParamValue.Raw(mac));

        var initialSubs = _config.GetSubscriptionList();
        for(int i = 0; i < _igmpSlots.Length; i++)
        {
            _igmpSlots[i] = i < initialSubs.Length ? initialSubs[i] : string.Empty;
            Set(AdlpFrame.ObjectIgmp, (ushort)(i + 1), AdlpFrame.ParamIgmpAddress,
                AdlpFrame.ParamValue.U32(AdlpFrame.IpToU32(_igmpSlots[i])));
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
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelIndex, AdlpFrame.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelName, AdlpFrame.ParamValue.String($"Channel {i + 1}"));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelControl, AdlpFrame.ParamValue.U8(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelStreamId, AdlpFrame.ParamValue.U32(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelPatch, AdlpFrame.ParamValue.U16(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelWide, AdlpFrame.ParamValue.U8(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelPreheat, AdlpFrame.ParamValue.U8(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelLimit, AdlpFrame.ParamValue.U8(100));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelCurve, AdlpFrame.ParamValue.U8(0));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelSwitchDelay, AdlpFrame.ParamValue.U32(0));
            }
        }
        else if(type is "PU" or "PU24")
        {
            for(int i = 0; i < indexes.Length; i++)
            {
                ushort local = (ushort)(i + 1);
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelIndex, AdlpFrame.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelName, AdlpFrame.ParamValue.String($"Control {i + 1}"));
                Set(AdlpFrame.ObjectChannel, local, AdlpFrame.ParamChannelType, AdlpFrame.ParamValue.U8(1));
            }
        }
        else if(type == "INDIGATE")
        {
            for(int i = 0; i < indexes.Length; i++)
            {
                ushort local = (ushort)(i + 1);
                Set(AdlpFrame.ObjectPixelgateChannel, local, AdlpFrame.ParamChannelIndex, AdlpFrame.ParamValue.U16((ushort)indexes[i]));
                Set(AdlpFrame.ObjectPixelgateChannel, local, AdlpFrame.ParamChannelName, AdlpFrame.ParamValue.String($"Line {i + 1}"));
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
        if(!AdlpFrame.TryParsePacket(bytes, out var packet, out string error) || packet == null)
        {
            Log?.Invoke($"RX INVALID from {remote}: {error}");
            return;
        }

        if((packet.Flags & AdlpFrame.ResponseFlag) != 0)
            return;

        switch(packet.Command)
        {
            case AdlpFrame.CommandGetParam:
                HandleGetParam(packet, remote);
                break;
            case AdlpFrame.CommandSetParam:
                HandleSetParam(packet, remote);
                break;
            case AdlpFrame.CommandGetStatus:
                Log?.Invoke($"GET_STATUS from {remote}");
                SendStatus(remote);
                break;
            default:
                Log?.Invoke($"RX unknown command={packet.Command} from {remote}");
                break;
        }
    }

    private void HandleGetParam(AdlpFrame.ParsedPacket packet, IPEndPoint remote)
    {
        if(!AdlpFrame.TryParseRequestIds(packet.Payload, out var ids))
        {
            Log?.Invoke($"GET_PARAM malformed from {remote}");
            return;
        }

        List<(AdlpFrame.ParamId Id, AdlpFrame.ParamValue Value)> fields = new();
        lock(_sync)
        {
            foreach(var id in ids)
            {
                if(_values.TryGetValue(new ParamKey(id.ObjectType, id.Index, id.Param), out var value))
                    fields.Add((id, value));
            }
        }

        byte[] payload = AdlpFrame.BuildParamList(fields);
        SendPacket(AdlpFrame.CommandGetParam, packet.Tid, AdlpFrame.ResponseFlag, payload, remote);
        Log?.Invoke($"GET_PARAM {fields.Count}/{ids.Count} fields from {remote}");
    }

    private void HandleSetParam(AdlpFrame.ParsedPacket packet, IPEndPoint remote)
    {
        if(!AdlpFrame.TryParseFields(packet.Payload, out var fields))
        {
            Log?.Invoke($"SET_PARAM malformed from {remote}");
            return;
        }

        List<(AdlpFrame.ParamId Id, AdlpFrame.ParamValue Value)> echoes = new();
        lock(_sync)
        {
            foreach(var (id, value) in fields)
            {
                ApplyValue(id, value);
                if(_values.TryGetValue(new ParamKey(id.ObjectType, id.Index, id.Param), out var current))
                    echoes.Add((id, current));
            }
        }

        byte[] payload = AdlpFrame.BuildParamList(echoes);
        SendPacket(AdlpFrame.CommandSetParam, packet.Tid, AdlpFrame.ResponseFlag, payload, remote);
        Log?.Invoke($"SET_PARAM {fields.Count} fields from {remote}");
        ConfigurationChanged?.Invoke(_config);
    }

    private void ApplyValue(AdlpFrame.ParamId id, AdlpFrame.ParamValue value)
    {
        _values[new ParamKey(id.ObjectType, id.Index, id.Param)] = value;

        if(id.ObjectType == AdlpFrame.ObjectDevice)
        {
            switch(id.Param)
            {
                case AdlpFrame.ParamDeviceName:
                    _config.DeviceName = value.AsString();
                    break;
                case AdlpFrame.ParamDeviceBlock:
                    _config.Block = value.Data.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(value.Data) : _config.Block;
                    break;
                case AdlpFrame.ParamDeviceIp:
                    _config.IpAddress = AdlpFrame.U32ToIp(value.AsU32());
                    break;
                case AdlpFrame.ParamDeviceMask:
                    _config.Mask = AdlpFrame.U32ToIp(value.AsU32());
                    break;
                case AdlpFrame.ParamDeviceGateway:
                    _config.Gateway = AdlpFrame.U32ToIp(value.AsU32());
                    break;
                case AdlpFrame.ParamDeviceMac:
                    if(value.Data.Length >= 6)
                        _config.Mac = string.Join(":", value.Data.Take(6).Select(b => b.ToString("X2")));
                    break;
                case AdlpFrame.ParamDeviceUuid:
                    if(value.Data.Length == 16)
                        _config.Uuid = new Guid(value.Data);
                    break;
                case AdlpFrame.ParamDeviceStatusTransport:
                    _config.Transport = value.AsU8();
                    break;
                case AdlpFrame.ParamDeviceStatusMulticastAddress:
                    _config.OwnAddress = value.AsU32() == 0 ? string.Empty : AdlpFrame.U32ToIp(value.AsU32());
                    break;
            }
        }
        else if(id.ObjectType == AdlpFrame.ObjectIgmp && id.Param == AdlpFrame.ParamIgmpAddress && id.Index >= 1 && id.Index <= 32)
        {
            uint raw = value.AsU32();
            _igmpSlots[id.Index - 1] = raw == 0 ? string.Empty : AdlpFrame.U32ToIp(raw);
            _config.SetSubscriptionList(_igmpSlots.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        else if((id.ObjectType == AdlpFrame.ObjectChannel || id.ObjectType == AdlpFrame.ObjectPixelgateChannel)
            && id.Param == AdlpFrame.ParamChannelIndex)
        {
            var channelIndexes = _values
                .Where(x => (x.Key.ObjectType == AdlpFrame.ObjectChannel || x.Key.ObjectType == AdlpFrame.ObjectPixelgateChannel)
                    && x.Key.Param == AdlpFrame.ParamChannelIndex)
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
        byte[] payload = AdlpFrame.BuildStatusPayload(_config.Uuid, _config.Type, port, block);
        SendPacket(AdlpFrame.CommandStatus, NextTid(), 0, payload, target);
    }

    private void SendPacket(ushort command, ushort tid, ushort flags, byte[] payload, IPEndPoint target)
    {
        try
        {
            byte[] bytes = AdlpFrame.BuildPacket(command, tid, flags, payload);
            _udp?.Send(bytes, bytes.Length, target);
        }
        catch(Exception ex)
        {
            Log?.Invoke($"ERROR send {_config.Display}: {ex.Message}");
        }
    }

    private ushort NextTid() => unchecked((ushort)Interlocked.Increment(ref _tid));

    private void Set(byte objectType, ushort index, ushort param, AdlpFrame.ParamValue value)
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
