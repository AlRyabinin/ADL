using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Firmware
{
    public class FirmwareRequest : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int HeaderSize = 8;

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public FirmwareRequest(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public FirmwareRequest(FirmwareCommand Command, uint Param, byte[] Data) : base(HeaderSize + Data.Length)
        {
            this.Param = Param;
            this.Command = Command;
            this.Size = Convert.ToUInt32(Data.Length);

            WriteArray(HeaderSize, Data, Data.Length);
        }

        /// <summary>
        /// Параметр команды
        /// </summary>
        public uint Param
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// Параметр команды
        /// </summary>
        public uint Size
        {
            get { return ReadUInt16(4); }
            set { WriteUInt16(4, value); }
        }

        /// <summary>
        /// Команда
        /// </summary>
        public FirmwareCommand Command
        {
            get { return (FirmwareCommand)ReadByte(6); }
            set { WriteByte(6, Convert.ToByte(value)); }
        }

        /// <summary>
        /// Флаги
        /// </summary>
        public uint Flags
        {
            get { return ReadByte(7); }
            set { WriteByte(7, Convert.ToByte(value)); }
        }

        /// <summary>
        /// Данные
        /// </summary>
        public byte[] Data => ReadArray(HeaderSize, Size);
    }
}
