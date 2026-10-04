using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace AdlpLib.Core
{
    public class Net
    {
        /// <summary>
        /// На каком интерфейсе работаем
        /// </summary>
        private NetworkInterface mInterface;

        /// <summary>
        /// Сокет
        /// </summary>
        private UDP mU;

        /// <summary>
        /// Привязанное устройство
        /// </summary>
        private Core mCore = null;

        /// <summary>
        /// Получить свой адрес
        /// </summary>
        public IPAddress IP => mInterface.Address;

        public Net(NetworkInterface I)
        {
            mInterface = I;
        }

        /// <summary>
        /// Приём пакета
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mU_onPacketReceive(object sender, UDPPacketEventArgs e)
        {
            if (!IsMyOwn(e.Sender.Address, e.Sender.Port))
            {
                mCore?.ReceiveUDP(e.Sender, e.Packet);
            }
        }

        /// <summary>
        /// Регистрируем ядро
        /// </summary>
        /// <param name="C">указатель на объект ядра</param>
        public void RegisterCore(Core C)
        {
            mCore = C;
        }

        /// <summary>
        /// Привязать порт
        /// </summary>
        /// <param name="Port">желаемый порт</param>
        /// <returns>true, если успешно</returns>
        public bool BindPort(int Port)
        {
            if(mU == null)
            {
                mU = new UDPSocket(mInterface, Port);
                if (mU.Bound)
                {
                    mU.onPacketReceive += mU_onPacketReceive;

                    return true;
                }
                else
                    mU = null;
            }

            return false;
        }

        /// <summary>
        /// Отвязать порт
        /// </summary>
        public void UnbindPort()
        {
            if (mU != null)
            {
                mU.onPacketReceive -= mU_onPacketReceive;
                mU = null;
            }
        }

        /// <summary>
        /// Привязан ли порт
        /// </summary>
        /// <returns></returns>
        public bool IsBinded() => (mU != null);

        /// <summary>
        /// Проверка IP и порта, не является ли он адресом этого устройства
        /// </summary>
        /// <param name="ip">IP-адрес</param>
        /// <param name="port">порт</param>
        /// <returns>true, если порт и адрес этого устройства</returns>
        private bool IsMyOwn(IPAddress ip, int port)
        {
            return IsBinded() && (mU.getIP() == ip) && (mU.getPort() == port);
        }

        /// <summary>
        /// Отправка данных по заданному IP
        /// </summary>
        /// <param name="ip">адрес назначения</param>
        /// <param name="port">порт назначения</param>
        /// <param name="Data">данные</param>
        public bool Send(IPAddress ip, int port, byte[] Data)
        {
            mU?.Send(ip, port, Data);
            return true;
        }

        /// <summary>
        /// Отправка данных по заданному IP
        /// </summary>
        /// <param name="ip">адрес назначения</param>
        /// <param name="port">порт назначения</param>
        /// <param name="Data">данные</param>
        public bool Send(int port, byte[] Data)
        {
            mU?.Send(port, Data);
            return true;
        }
    }
}
