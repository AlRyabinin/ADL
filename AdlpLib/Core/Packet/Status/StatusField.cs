using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Status
{
    public class StatusField : RawPacket
    {
        /// <summary>
        /// Размер поля
        /// </summary>
        public const int HeaderSize = 4;

        public StatusField(byte[] Raw) : base(Raw)
        {

        }

        public StatusField(StatusFieldType Type, byte[] Payload) : base(Payload.Length + HeaderSize)
        {
            this.Type = Type;
            this.Size = Payload.Length;
            Flags = 0;

            WriteArray(HeaderSize, Payload, Payload.Length);
        }

        /// <summary>
        /// Полный размер поля
        /// </summary>
        public int Size
        {
            get { return ReadUInt16(0); }
            set { WriteUInt16(0, value); }
        }

        /// <summary>
        /// Тип поля
        /// </summary>
        public StatusFieldType Type
        {
            get { return (StatusFieldType)ReadByte(2); }
            set { WriteByte(2, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Флаги
        /// </summary>
        public int Flags
        {
            get { return ReadByte(3); }
            set { WriteByte(3, value); }
        }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        public byte[] Payload => ReadArray(HeaderSize, Size);
    }
}
