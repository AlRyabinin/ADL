using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet
{
    public class BasePacket : RawPacket
    {
        internal const int HeaderSize = 12;

        public BasePacket(byte[] Raw) : base(Raw)
        {

        }

        public BasePacket(byte Command, byte[] Payload) : base(12 + Payload.Length)
        {
            Version = 0;
            Cmd = Command;
            Size = Payload.Length;
            Crc = new CRC16().ComputeChecksum(Payload);

            WriteArray(12, Payload, Payload.Length);
        }

        /// <summary>
        /// Версия протокола
        /// </summary>
        public int Version
        {
            get { return ReadUInt16(0); }
            set { WriteUInt16(0, value); }
        }

        /// <summary>
        /// Номер транзакции
        /// </summary>
        public int Tid
        {
            get { return ReadUInt16(2); }
            set { WriteUInt16(2, value); }
        }

        /// <summary>
        /// Длина данных, не включая заголовок
        /// </summary>
        public int Size
        {
            get { return ReadUInt16(4); }
            private set { WriteUInt16(4, value); }
        }

        /// <summary>
        /// Контрольная сумма данных
        /// </summary>
        public int Crc
        {
            get { return ReadUInt16(6); }
            private set { WriteUInt16(6, value); }
        }

        /// <summary>
        /// Код команды
        /// </summary>
        public int Cmd
        {
            get { return ReadUInt16(8); }
            set { WriteUInt16(8, value); }
        }

        /// <summary>
        /// Флаги
        /// </summary>
        public HeaderFlags Flags
        {
            get { return (HeaderFlags)ReadUInt16(10); }
            set { WriteUInt16(10, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        public byte[] Payload => ReadArray(12, Size);

        /// <summary>
        /// Рассчитанное CRC
        /// </summary>
        private int CalcCrc => new CRC16().ComputeChecksum(Payload);

        /// <summary>
        /// Является ли пакет корректным
        /// </summary>
        internal bool Correct => ((Size + HeaderSize) == getLength()) && (Crc == CalcCrc);

        /// <summary>
        /// Код команды
        /// </summary>
        public PacketCode Command
        {
            get { return (PacketCode)Cmd; }
            set { Cmd = Convert.ToInt32(value); }
        }
    }
}
