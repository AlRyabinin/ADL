using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Proxy
{
    public class ProxyResponse : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int Size = 2;

        /// <summary>
        /// Ответ на запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public ProxyResponse(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Запрос на проксирование
        /// </summary>
        /// <param name="Raw"></param>
        public ProxyResponse(ProxyStatus Result) : base(Size)
        {
            this.Result = Result;
        }

        /// <summary>
        /// Код команды
        /// </summary>
        public ProxyStatus Result
        {
            get { return (ProxyStatus)ReadByte(0); }
            set { WriteByte(0, Convert.ToInt32(value)); }
        }
    }
}
