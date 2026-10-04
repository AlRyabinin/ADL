using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Firmware
{
    public class FirmwareResponse : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int HeaderSize = 2;

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public FirmwareResponse(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public FirmwareResponse(FirmwareResponseCode Code, byte[] Data) : base(HeaderSize + Data.Length)
        {
            this.Code = Code;
            this.Size = Convert.ToUInt32(Data.Length);

            WriteArray(HeaderSize, Data, Data.Length);
        }

        /// <summary>
        /// Параметр команды
        /// </summary>
        public FirmwareResponseCode Code
        {
            get { return (FirmwareResponseCode)ReadByte(0); }
            set { WriteByte(0, Convert.ToByte(value)); }
        }

        /// <summary>
        /// Параметр команды
        /// </summary>
        public uint Size
        {
            get { return ReadByte(1); }
            set { WriteByte(1, Convert.ToByte(value)); }
        }

        /// <summary>
        /// Данные
        /// </summary>
        public byte[] Data => ReadArray(HeaderSize, Size);

        /// <summary>
        /// Данные как строка
        /// </summary>
        public string Result => ReadANSIString(HeaderSize, Convert.ToInt32(Size));
    }
}
