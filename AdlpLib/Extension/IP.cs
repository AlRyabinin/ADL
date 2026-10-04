using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Extension.IP
{
    public static class IP
    {
        public static int GetSimilarLength(this IPAddress A, IPAddress B)
        {
            byte[] bA = A.GetAddressBytes();
            byte[] bB = B.GetAddressBytes();

            int Temp = 0;

            for (int i = 0; i < 4; i++)
            {
                int a = bA[i];
                int b = bB[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((b & (7 - j)) != (a & (7 - j))) break;

                    Temp++;
                }
            }

            return Temp;
        }

        public static ulong getLong(this IPAddress A)
        {
            ulong Result = 0;
            byte[] bA = A.GetAddressBytes();

            for (int i = 0; i < bA.Length; i++) Result |= ((ulong)bA[i] << (i * 8));

            return Result;
        }

        public static IPAddress getBroadcastAddress(this IPAddress A)
        {
            var Bytes = A.GetAddressBytes();
            Bytes[3] = 0xFF;

            return new IPAddress(Bytes);
        }

        public static IPAddress getBroadcastAddress(this IPAddress A, int Mask)
        {
            var Bytes = A.GetAddressBytes();
            Bytes[3] |= Convert.ToByte(Mask & 0xFF);
            Bytes[2] |= Convert.ToByte((Mask >> 8) & 0xFF);
            Bytes[1] |= Convert.ToByte((Mask >> 16) & 0xFF);
            Bytes[0] |= Convert.ToByte((Mask >> 24) & 0xFF);

            return new IPAddress(Bytes);
        }
    }
}
