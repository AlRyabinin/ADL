using System;
using System.Collections.Generic;
using System.Text;

namespace AdlpLib.Core.Packet.Status
{
    /// <summary>
    /// GET_STATUS request payload.
    /// </summary>
    public class GetStatusRequest
    {
        public const int MaxTypes = 8;
        public const int TypeNameSize = 8;
        public const byte Version = 1;

        public ushort FieldMask { get; set; } = 0;

        public List<string> Types { get; } = new();

        public byte[] getPayload()
        {
            byte[] payload = new byte[5 + MaxTypes * TypeNameSize];

            payload[0] = Version;
            payload[1] = 0;
            payload[2] = (byte)(FieldMask & 0xFF);
            payload[3] = (byte)((FieldMask >> 8) & 0xFF);

            int count = Math.Min(Types.Count, MaxTypes);
            payload[4] = (byte)count;

            for(int i = 0; i < count; i++)
            {
                byte[] name = Encoding.ASCII.GetBytes(
                    Types[i] ?? string.Empty);

                int length = Math.Min(name.Length, TypeNameSize);

                Array.Copy(name, 0, payload,
                    5 + i * TypeNameSize, length);
            }

            return payload;
        }
    }
}