using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace ADLDeviceEmulator.Protocol;

internal static class AdlpdsProtocol
{
    public const int HeaderSize = 12;

    public const ushort CommandGetParam = 0;
    public const ushort CommandSetParam = 1;
    public const ushort CommandStatus = 3;
    public const ushort CommandGetStatus = 11;
    public const ushort ResponseFlag = 0x8000;

    public const byte ObjectDevice = 1;
    public const byte ObjectChannel = 2;
    public const byte ObjectStream = 3;
    public const byte ObjectGroup = 4;
    public const byte ObjectPicture = 5;
    public const byte ObjectPixelgateChannel = 6;
    public const byte ObjectIgmp = 7;
    public const byte ObjectControlButton = 100;

    public const ushort ParamDeviceName = 1;
    public const ushort ParamDeviceBlock = 2;
    public const ushort ParamDeviceIp = 3;
    public const ushort ParamDeviceMask = 4;
    public const ushort ParamDeviceGateway = 5;
    public const ushort ParamDeviceMac = 6;
    public const ushort ParamDeviceConfiguration = 7;
    public const ushort ParamDeviceUuid = 8;
    public const ushort ParamDeviceFirmware = 9;
    public const ushort ParamChannelName = 10;
    public const ushort ParamChannelIndex = 11;
    public const ushort ParamChannelControl = 12;
    public const ushort ParamChannelStreamId = 13;
    public const ushort ParamChannelPatch = 14;
    public const ushort ParamChannelWide = 15;
    public const ushort ParamChannelType = 16;
    public const ushort ParamChannelCurve = 17;
    public const ushort ParamChannelPreheat = 18;
    public const ushort ParamChannelLimit = 19;
    public const ushort ParamChannelSwitchDelay = 20;
    public const ushort ParamDevicePriority = 30;
    public const ushort ParamDeviceStartChannel = 31;
    public const ushort ParamDevicePictureSwitchTime = 32;
    public const ushort ParamStreamId = 60;
    public const ushort ParamStreamStart = 61;
    public const ushort ParamPictureName = 90;
    public const ushort ParamControlButtonPage = 100;
    public const ushort ParamControlButtonLocal = 101;
    public const ushort ParamControlButtonPosX = 102;
    public const ushort ParamControlButtonPosY = 103;
    public const ushort ParamControlButtonIcon = 104;
    public const ushort ParamControlButtonColor = 105;
    public const ushort ParamDevicePageCount = 120;
    public const ushort ParamDeviceSwapX = 121;
    public const ushort ParamDeviceSwapY = 122;
    public const ushort ParamDeviceBrightness = 123;
    public const ushort ParamDeviceBrightnessSleep = 124;
    public const ushort ParamDeviceSleepTimeout = 125;
    public const ushort ParamDeviceStatusTransport = 128;
    public const ushort ParamDeviceStatusMulticastAddress = 129;
    public const ushort ParamIgmpAddress = 200;
    public const ushort ParamGroupIndex = 50;
    public const ushort ParamGroupChannelStart = 1000;

    public const byte DataRaw = 2;
    public const byte DataU8 = 3;
    public const byte DataU16 = 4;
    public const byte DataU32 = 5;
    public const byte DataString = 10;

    public readonly record struct ParamId(byte ObjectType, byte Flags, ushort Index, ushort Param)
    {
        public static ParamId Read(ReadOnlySpan<byte> data)
        {
            if(data.Length < 6) throw new InvalidDataException("Короткий ParamDataId.");
            return new ParamId(data[0], data[1], BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2, 2)), BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4, 2)));
        }

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[6];
            bytes[0] = ObjectType;
            bytes[1] = Flags;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2, 2), Index);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), Param);
            return bytes;
        }
    }

    public sealed record ParamValue(byte DataType, byte[] Data)
    {
        public static ParamValue U8(byte value) => new(DataU8, new[] { value });
        public static ParamValue U16(ushort value)
        {
            byte[] data = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(data, value);
            return new ParamValue(DataU16, data);
        }
        public static ParamValue U32(uint value)
        {
            byte[] data = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(data, value);
            return new ParamValue(DataU32, data);
        }
        public static ParamValue Raw(byte[] value) => new(DataRaw, value);
        public static ParamValue String(string value)
        {
            byte[] text = Encoding.ASCII.GetBytes(value ?? string.Empty);
            return new ParamValue(DataString, text.Concat(new byte[] { 0 }).ToArray());
        }

        public uint AsU32() => Data.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(Data) : 0;
        public byte AsU8() => Data.Length > 0 ? Data[0] : (byte)0;
        public string AsString()
        {
            int zero = Array.IndexOf(Data, (byte)0);
            return Encoding.ASCII.GetString(Data, 0, zero >= 0 ? zero : Data.Length);
        }
    }

    public sealed record ParsedPacket(ushort Tid, ushort Command, ushort Flags, byte[] Payload);

    public static byte[] BuildPacket(ushort command, ushort tid, ushort flags, byte[] payload)
    {
        payload ??= Array.Empty<byte>();
        byte[] packet = new byte[HeaderSize + payload.Length];
        // ADLPDS RawPacket uses little-endian fields.
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), 0); // protocol version
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), tid);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), checked((ushort)payload.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6, 2), ComputeCrc16(payload));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(8, 2), command);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(10, 2), flags);
        payload.CopyTo(packet, HeaderSize);
        return packet;
    }

    public static bool TryParsePacket(byte[] bytes, out ParsedPacket? packet, out string error)
    {
        packet = null;
        error = string.Empty;
        if(bytes.Length < HeaderSize)
        {
            error = "Пакет короче заголовка ADLPDS.";
            return false;
        }

        ushort payloadSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4, 2));
        if(bytes.Length != HeaderSize + payloadSize)
        {
            error = $"Размер пакета не совпадает: header={payloadSize}, datagram={bytes.Length - HeaderSize}.";
            return false;
        }

        byte[] payload = bytes.AsSpan(HeaderSize, payloadSize).ToArray();
        ushort crc = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
        ushort actualCrc = ComputeCrc16(payload);
        if(crc != actualCrc)
        {
            error = $"Неверная CRC: packet=0x{crc:X4}, calculated=0x{actualCrc:X4}.";
            return false;
        }

        packet = new ParsedPacket(
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(10, 2)),
            payload);
        return true;
    }

    public static bool TryParseRequestIds(byte[] payload, out List<ParamId> ids)
    {
        ids = new List<ParamId>();
        if(payload.Length < 2) return false;
        int count = payload[0];
        if(payload.Length < 2 + count * 6) return false;
        int offset = 2;
        for(int i = 0; i < count; i++, offset += 6)
            ids.Add(ParamId.Read(payload.AsSpan(offset, 6)));
        return true;
    }

    public static bool TryParseFields(byte[] payload, out List<(ParamId Id, ParamValue Value)> fields)
    {
        fields = new List<(ParamId, ParamValue)>();
        if(payload.Length < 2) return false;
        int count = payload[0];
        int offset = 2;
        for(int i = 0; i < count; i++)
        {
            if(offset + 8 > payload.Length) return false;
            ParamId id = ParamId.Read(payload.AsSpan(offset, 6));
            offset += 6;
            int dataSize = payload[offset];
            byte dataType = payload[offset + 1];
            if(dataSize < 2 || offset + dataSize > payload.Length) return false;
            byte[] data = payload.AsSpan(offset + 2, dataSize - 2).ToArray();
            fields.Add((id, new ParamValue(dataType, data)));
            offset += dataSize;
        }
        return offset == payload.Length;
    }

    public static byte[] BuildParamList(IEnumerable<(ParamId Id, ParamValue Value)> fields)
    {
        var materialized = fields.ToList();
        if(materialized.Count > byte.MaxValue)
            throw new InvalidOperationException("В одном ответе допускается не более 255 полей.");

        using var ms = new MemoryStream();
        ms.WriteByte((byte)materialized.Count);
        ms.WriteByte(0);
        foreach(var (id, value) in materialized)
        {
            ms.Write(id.ToBytes());
            int dataSize = value.Data.Length + 2;
            if(dataSize > byte.MaxValue)
                throw new InvalidOperationException("Поле параметра слишком большое.");
            ms.WriteByte((byte)dataSize);
            ms.WriteByte(value.DataType);
            ms.Write(value.Data);
        }
        return ms.ToArray();
    }

    public static byte[] BuildStatusPayload(Guid uuid, string type, ushort port, ushort block)
    {
        // Status payload from Status.cs: 36-byte fixed header, no optional status fields.
        byte[] payload = new byte[36];
        uuid.ToByteArray().CopyTo(payload, 0);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type ?? string.Empty);
        Array.Copy(typeBytes, 0, payload, 16, Math.Min(8, typeBytes.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(24, 4), 0); // SystemId

        ushort capability = type.ToUpperInvariant() switch
        {
            "IKB" => 0x0021,      // EXEC + CFG
            "PU" or "PU24" => 0x0030, // CTRL + CFG
            "INDIGATE" => 0x0020, // CFG
            _ => 0x0020
        };
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(28, 2), capability);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(30, 2), port);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(32, 2), block);
        payload[34] = 0; // status flags
        payload[35] = 0; // field count; status fields are optional
        return payload;
    }

    public static uint IpToU32(string? value)
    {
        if(!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return 0;
        var b = ip.GetAddressBytes();
        // Same value construction as AdlpdsService.IpToU32; ParamDataValue stores U32 little-endian.
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    public static string U32ToIp(uint value) =>
        $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";

    private static ushort ComputeCrc16(ReadOnlySpan<byte> data)
    {
        // CrcSharp parameters in the source project: width=16, poly=0x8005,
        // init=0xFFFF, refin=true, refout=true, xorout=0 (CRC-16/MODBUS form).
        ushort crc = 0xFFFF;
        foreach(byte b in data)
        {
            crc ^= b;
            for(int bit = 0; bit < 8; bit++)
                crc = (ushort)(((crc & 1) != 0) ? ((crc >> 1) ^ 0xA001) : (crc >> 1));
        }
        return crc;
    }
}
