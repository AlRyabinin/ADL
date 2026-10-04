using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdlpLib.Core
{
    public class CRC16
    {
        public ushort ComputeChecksum(byte[] data)
        {
            var crc16 = new CrcSharp.Crc(new CrcSharp.CrcParameters(16, 0x8005, 0xffff, 0x0, true, true));

            return Convert.ToUInt16(crc16.CalculateAsNumeric(data));
        }
    }
}
