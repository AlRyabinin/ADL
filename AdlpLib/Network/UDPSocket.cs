using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

using Extension.IP;

namespace Network
{
    public class UDPSocket : UDP
    {
        private UdpState mState = null;
        private UdpFromState mFromState = null;
        private UdpClient mUDP;
        private IPEndPoint mBroadcast;
        private object Locker = new object();

        private byte[] mBuffer = new byte[24];

        /// <summary>
        /// Привязался ли порт
        /// </summary>
        public override bool Bound => mUDP != null;

        public UDPSocket(NetworkInterface I, int Port)
            : base (I, Port)
        {
            BindClient(I.Address, Port);
        }

        private class UdpState
        {
            public IPEndPoint e;
            public UdpClient u;
            public bool StopRequest = false;

            public UdpState(IPEndPoint E, UdpClient C)
            {
                e = E;
                u = C;
            }
        }

        private class UdpFromState
        {
            public EndPoint e;
            public UdpClient u;
            public bool StopRequest = false;

            public UdpFromState(EndPoint E, UdpClient C)
            {
                e = E;
                u = C;
            }
        }

        /// <summary>
        /// Адресный пакет
        /// </summary>
        /// <param name="EP"></param>
        /// <param name="Packet"></param>
        public override void Send(IPEndPoint EP, byte[] Packet)
        {
            try
            {
                mUDP.Send(Packet, Packet.Length, EP);
            }
            catch (Exception E)
            { 
                Debug.WriteLine($"UDP Socket send error: {E}");
            }
        }

        /// <summary>
        /// Широковещательный пакет
        /// </summary>
        /// <param name="EP"></param>
        /// <param name="Packet"></param>
        public override void Send(int Port, byte[] Packet)
        {
            try
            {
                mUDP.Send(Packet, Packet.Length, new IPEndPoint(mBroadcast.Address, Port));
            }
            catch (Exception E)
            {
                Debug.WriteLine($"UDP Socket broadcast send error: {E}");
            }
        }

        /// <summary>
        /// Адресный пакет
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Packet"></param>
        public override void Send(IPAddress IP, int Port, byte[] Packet)
        {
            try
            {
                mUDP.Send(Packet, Packet.Length, new IPEndPoint(IP, Port));
            }
            catch (Exception E)
            {
                Debug.WriteLine($"UDP Socket send error: {E}");
            }
        }

        /// <summary>
        /// Переподключить
        /// </summary>
        /// <param name="I"></param>
        protected override void RebindInternal(NetworkInterface I, int Port)
        {
            Unbind();
            BindClient(I.Address, Port);
        }

        /// <summary>
        /// Отключить
        /// </summary>
        /// <param name="I"></param>
        public override void Unbind()
        {
            if (mState != null)
                mState.StopRequest = true;
        }

        /// <summary>
        /// Подключить приёмник
        /// </summary>
        /// <param name="A"></param>
        /// <param name="Port"></param>
        private void BindClient(IPAddress A, int Port)
        {
            IPEndPoint localEp = new IPEndPoint(A, Port);
            EndPoint remEp = new IPEndPoint(IPAddress.Any, Port);
            UdpClient U = new UdpClient();
            U.Client.ReceiveBufferSize = 4096*8;
            U.DontFragment = true;
            U.ExclusiveAddressUse = false;
            U.EnableBroadcast = true;
            U.MulticastLoopback = false;
            // U.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
                U.Client.Bind(localEp);

                Debug.WriteLine("Bind to " + A.ToString() + ":" + Port.ToString());
                mState = new UdpState(localEp, U);
                mFromState = new UdpFromState(remEp, U);
                U.BeginReceive(new AsyncCallback(DataReceived), mState);
                /* U.Client.BeginReceiveFrom(mBuffer, 0, mBuffer.Length, 0, ref remEp, new AsyncCallback(DataFromReceived), mFromState);
                 */
                mUDP = U;

                if (A.getLong() == 0)
                    mBroadcast = new IPEndPoint(IPAddress.Broadcast, Port);
                else
                {
                    var mBroadcastAddress = A.GetAddressBytes();
                    if (mBroadcastAddress[0] < 100)
                    {
                        mBroadcastAddress[1] = 255;
                        mBroadcastAddress[2] = 255;
                    }
                    mBroadcastAddress[3] = 255;

                    mBroadcast = new IPEndPoint(new IPAddress(mBroadcastAddress), Port);
                }
            }
            catch(Exception E)
            {
                Debug.WriteLine($"UDP Socket bind error: {E}");
            }
        }

        /// <summary>
        /// Принят пакет
        /// </summary>
        /// <param name="R"></param>
        private void DataFromReceived(IAsyncResult R)
        {
            UdpFromState S = R.AsyncState as UdpFromState;

            Debug.WriteLine("DataFromReceived from ");
            try
            {
                if (S.u != null)
                {
                    S.u.Client.EndReceiveFrom(R, ref S.e);
                }
            }
            catch (ObjectDisposedException E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }
            catch (Exception E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }
        }

        private void ProcessReceive(IAsyncResult R, UdpState S)
        {
            Byte[] data = null;
            bool NeedReceive = true;

            try
            {
                if (S.u != null)
                {
                    lock (Locker)
                    {
                        data = S.u.EndReceive(R, ref S.e);
                    }
                    if (S.StopRequest)
                    {
                        S.u.Close();
                        NeedReceive = false;
                    }
                    else
                    {
                        NeedReceive = false;
                        StartNextReceive(S);

                        if (data != null) 
                            fireOnPacket(S.e as IPEndPoint, data);
                    }
                }
            }
           /* catch (ObjectDisposedException E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }*/
            catch (Exception E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }

            if(NeedReceive)
                StartNextReceive(S);
        }

        private void StartNextReceive(UdpState S)
        {
            try
            {
                lock (Locker)
                {
                    S.u.BeginReceive(new AsyncCallback(DataReceived), S);
                }
            }
           /* catch (ObjectDisposedException E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }*/
            catch (Exception E)
            {
                Debug.WriteLine("UDPSocket: " + E.Message);
            }
        }

        /// <summary>
        /// Принят пакет
        /// </summary>
        /// <param name="R"></param>
        private void DataReceived(IAsyncResult R)
        {
            UdpState S = R.AsyncState as UdpState;

            ProcessReceive(R, S);
        }
    }
}
