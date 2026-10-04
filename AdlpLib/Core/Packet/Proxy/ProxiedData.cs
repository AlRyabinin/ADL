using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Proxy
{
    class ProxiedData : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int HeaderSize = 14;

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public ProxiedData(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Данные от прокси
        /// </summary>
        /// <param name="Raw"></param>
        public ProxiedData(IPEndPoint From, IPEndPoint To, BasePacket Packet) : base(HeaderSize + Packet.getLength())
        {
            this.To = To;
            this.From = From;

            this.Size = Packet.getLength();

            WriteArray(HeaderSize, Packet.getPacket(), Packet.getLength());
        }

        /// <summary>
        /// Отправитель
        /// </summary>
        public IPEndPoint From
        {
            get { return new IPEndPoint(AddressFrom, PortFrom); }
            set { AddressFrom = value.Address; PortFrom = value.Port; }
        }

        /// <summary>
        /// Получатель
        /// </summary>
        public IPEndPoint To
        {
            get { return new IPEndPoint(AddressTo, PortTo); }
            set { AddressTo = value.Address; PortTo = value.Port; }
        }

        /// <summary>
        /// Адрес отправителя
        /// </summary>
        private IPAddress AddressFrom
        {
            get
            {
                var Raw = ReadArray(0, 4);
                return new IPAddress(Raw);
            }
            set
            {
                WriteArray(0, value.GetAddressBytes(), 4);
            }
        }

        /// <summary>
        /// Порт отправителя
        /// </summary>
        private int PortFrom
        {
            get { return ReadUInt16(8); }
            set { WriteUInt16(8, value); }
        }

        /// <summary>
        /// Адрес отправителя
        /// </summary>
        private IPAddress AddressTo
        {
            get
            {
                var Raw = ReadArray(4, 4);
                return new IPAddress(Raw);
            }
            set
            {
                WriteArray(4, value.GetAddressBytes(), 4);
            }
        }

        /// <summary>
        /// Порт отправителя
        /// </summary>
        private int PortTo
        {
            get { return ReadUInt16(10); }
            set { WriteUInt16(10, value); }
        }

        /// <summary>
        /// Размер
        /// </summary>
        public int Size
        {
            get { return ReadUInt16(12); }
            set { WriteUInt16(12, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        public BasePacket Payload => new BasePacket(ReadArray(14, Size));
    }
}
