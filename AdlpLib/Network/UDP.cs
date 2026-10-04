using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace Network
{
    public class UDPPacketEventArgs
    {
        public IPEndPoint Sender;

        public byte[] Packet;

        public UDPPacketEventArgs(IPEndPoint A, byte[] D)
        {
            Sender = A;
            Packet = D;
        }
    }

    public delegate void UDPPacketEventHandler(object sender, UDPPacketEventArgs E);

    public abstract class UDP
    {
        private NetworkInterface mI;
        protected int mPort;

        public event UDPPacketEventHandler onPacketReceive;
        private bool mFilterLocal = true;

        public virtual bool Bound => false;

        public UDP(NetworkInterface I, int Port)
        {
            mI = I;
            mPort = Port;
        }

        /// <summary>
        /// Получить алрес сокета
        /// </summary>
        /// <returns></returns>
        public IPAddress getIP()
        {
            return mI.Address;
        }


        /// <summary>
        /// Получить порт
        /// </summary>
        /// <returns></returns>
        public int getPort()
        {
            return mPort;
        }

        /// <summary>
        /// Переподключить сокет
        /// </summary>
        /// <param name="I"></param>
        public void Rebind(NetworkInterface I)
        {
            if (mI.Address.Equals(I.Address)) return;

            RebindInternal(I, mPort);
            mI = I;
        }

        /// <summary>
        /// Отключить
        /// </summary>
        /// <param name="I"></param>
        public abstract void Unbind();

        /// <summary>
        /// Не принимать пакеты с адреса этого сокета
        /// </summary>
        public bool FilterLoopback
        {
            get { return mFilterLocal; }
            set { mFilterLocal = value; }
        }

        /// <summary>
        /// Переподключить
        /// </summary>
        /// <param name="I"></param>
        protected abstract void RebindInternal(NetworkInterface I, int Port);

        /// <summary>
        /// Адресный пакет
        /// </summary>
        /// <param name="EP"></param>
        /// <param name="Packet"></param>
        public abstract void Send(IPEndPoint EP, byte[] Packet);

        /// <summary>
        /// Адресный пакет
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Packet"></param>
        public abstract void Send(IPAddress IP, int Port, byte[] Packet);

        /// <summary>
        /// Широковещательный пакет
        /// </summary>
        /// <param name="Packet"></param>
        public abstract void Send(int Port, byte[] Packet);

        /// <summary>
        /// Вызвать обарботчик принятого покета
        /// </summary>
        /// <param name="A"></param>
        /// <param name="Data"></param>
        protected void fireOnPacket(IPEndPoint EP, byte[] Data)
        {
            if (mFilterLocal && mI.Address.Equals(EP.Address) && (mPort == EP.Port)) return;

            if (onPacketReceive != null) onPacketReceive(this, new UDPPacketEventArgs(EP, Data));
        }
    }
}
