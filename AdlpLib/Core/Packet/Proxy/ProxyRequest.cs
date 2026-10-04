using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Proxy
{
    public class ProxyRequest : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int Size = 2;

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public ProxyRequest(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public ProxyRequest(ProxyCommand Command) : base(Size)
        {
            this.Command = Command;
        }

        /// <summary>
        /// Код команды
        /// </summary>
        public ProxyCommand Command
        {
            get { return (ProxyCommand)ReadByte(0); }
            set { WriteByte(0, Convert.ToInt32(value)); }
        }
    }
}
