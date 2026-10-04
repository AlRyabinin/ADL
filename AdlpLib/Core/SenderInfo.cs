using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public class SenderInfo
    {
        /// <summary>
        /// Адрес отправителя
        /// </summary>
        public IPEndPoint Endpoint;

        /// <summary>
        /// Номер транзакции
        /// </summary>
        public int Tid;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="EP">Адрес отправителя</param>
        /// <param name="Tid">Номер транзакции</param>
        public SenderInfo(IPEndPoint EP, int Tid)
        {
            Endpoint = EP;
            this.Tid = Tid;
        }
    }
}
