using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace Extension.Array
{
    public static class ArrayEdit
    {
        static Encoding ANSI = Encoding.GetEncoding(1251);

        static public void Fill<T>(this T[] Data, T Value)
        {
            if (Data == null) return;
            for (int i = 0; i < Data.Length; i++) Data[i] = Value;
        }

        static public void Copy<T>(this T[] Data, T[] New)
        {
            if (Data == null) return;
            if (New == null) return;

            int Length = Math.Min(New.Length, Data.Length);

            Buffer.BlockCopy(New, 0, Data, 0, Length);
        }


        /// <summary>
        /// Запись ANSI строки в массив байт
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        /// <param name="Text"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public long WriteANSIString(this byte[] Data, long Offset, string Text, long Length)
        {
            byte[] TempData = Encoding.GetEncoding("windows-1251").GetBytes(Text.ToCharArray());

            Data.WriteArray(Offset, TempData, Length);

            return Offset + Convert.ToUInt32(TempData.Length);
        }

        /// <summary>
        /// Чтение ANSI строки из массива байт
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public string ReadANSIString(this byte[] Data, long Offset, long Length)
        {
            string Temp = "";

            byte[] TempArray = new byte[Length];
            Buffer.BlockCopy(Data, Convert.ToInt32(Offset), TempArray, 0, Convert.ToInt32(Length));

            Temp = ANSI.GetString(TempArray);

            int End = Temp.IndexOf('\0');
            if (End >= 0) Temp = Temp.Substring(0, End);

            return Temp;
        }

        static public long WriteString(this byte[] Data, long Offset, string Text, long Length)
        {
            int i;

            for (i = 0; i < Length; i++)
            {
                if (i < Text.Length)
                {
                    Data[Offset + i] = Convert.ToByte(Text[i]);
                }
                else
                {
                    Data[Offset + i] = 0;
                }
            }

            // Пследний символ всегда 0
            Data[Offset + Length - 1] = 0;

            return Offset + Length;
        }

        static public string ReadString(this byte[] Data, long Offset, long Length)
        {
            string Temp = "";

            for (int i = 0; i < Length; i++)
            {
                byte Char = Data[Offset + i];
                if (Char == 0) break;

                Temp += Convert.ToChar(Char);
            }

            return Temp;
        }

        static public long WriteArray(this byte[] Data, long Offset, byte[] Source, long Length)
        {
            int i;

            for (i = 0; i < Length; i++)
            {
                if (i < Source.Length)
                    Data[Offset + i] = Source[i];
                else
                    Data[Offset + i] = 0;
            }

            return Offset + Length;
        }

        static public int ReadByte(this byte[] Data, long Offset)
        {
            if (Data != null)
            {
                if (Offset < Data.Length)
                    return Data[Offset + 0]; // MSB
            }
            return 0;
        }

        static public long WriteByte(this byte[] Data, long Offset, int Value)
        {
            if (Data != null)
            {
                if (Offset < Data.Length)
                    Data[Offset + 0] = Convert.ToByte(Value & 0xFF); // MSB
            }
            return Offset + 1;
        }

        // LB HB
        static public long WriteUInt16(this byte[] Data, long Offset, int Value)
        {
            if (Data != null)
            {
                Data[Offset + 0] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        // LB HB
        static public long WriteInt16(this byte[] Data, long Offset, int Value)
        {
            if (Data != null)
            {
                short V = Convert.ToInt16(Value);
                Data[Offset + 0] = Convert.ToByte(V & 0xFF); // LSB
                Data[Offset + 1] = Convert.ToByte((V >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        // LB HB
        static public UInt16 ReadUInt16(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 0]) | // LSB
                    (Data[Offset + 1] << 8)); // MSB
        }

        // HB LB
        static public long WriteUInt16BE(this byte[] Data, long Offset, uint Value)
        {
            if (Data != null)
            {
                Data[Offset + 1] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 0] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        // LB HB
        static public UInt16 ReadUInt16BE(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 1]) | // MSB
                    (Data[Offset + 0] << 8)); // LSB
        }

        // LB HB
        static public UInt32 ReadUInt24(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt32((Data[Offset + 0]) | // LSB
                                    (Data[Offset + 1] << 8) |
                                    (Data[Offset + 2] << 16)); // MSB
        }

        // HB LB
        static public long WriteUInt24(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // LSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // 
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // MSB

            return Offset + 3;
        }

        // HB LB
        static public long WriteUInt32(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        // LB HB
        static public UInt32 ReadUInt32(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 0]) |
                    ((uint)Data[Offset + 1] << 8) |
                    ((uint)Data[Offset + 2] << 16) | // MSB
                    ((uint)Data[Offset + 3] << 24)); // LSB
        }

        // HB LB
        static public long WriteUInt32BE(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 3] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 0] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        // LB HB
        static public UInt32 ReadUInt32BE(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 3]) |
                    ((uint)Data[Offset + 2] << 8) |
                    ((uint)Data[Offset + 1] << 16) | // MSB
                    ((uint)Data[Offset + 0] << 24)); // LSB
        }

        // LB HB
        static public UInt64 ReadUInt64(this byte[] Data, long Offset)
        {
            return (((ulong)Data[Offset + 0]) |
                    ((ulong)Data[Offset + 1] << 8) |
                    ((ulong)Data[Offset + 2] << 16) | // MSB
                    ((ulong)Data[Offset + 3] << 24) |
                    ((ulong)Data[Offset + 4] << 32) |
                    ((ulong)Data[Offset + 5] << 40) |
                    ((ulong)Data[Offset + 6] << 48) |
                    ((ulong)Data[Offset + 7] << 56)); // LSB
        }

        // HB LB
        static public long WriteUInt64(this byte[] Data, long Offset, UInt64 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 24) & 0xFF); // MSB
            Data[Offset + 4] = Convert.ToByte((Value >> 32) & 0xFF); // MSB
            Data[Offset + 5] = Convert.ToByte((Value >> 40) & 0xFF); // MSB
            Data[Offset + 6] = Convert.ToByte((Value >> 48) & 0xFF); // MSB
            Data[Offset + 7] = Convert.ToByte((Value >> 56) & 0xFF); // MSB

            return Offset + 8;
        }

        static public long WriteByte(this byte[] Data, long Offset, byte Value)
        {
            Data[Offset] = Value;

            return Offset + 1;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public byte[] ReadArray(this byte[] Data, long Offset, long Length)
        {
            byte[] Result = new byte[Length];
            Result.Fill((byte)0);

            long ResLength = (Data.Length - Offset >= Length) ? Length : Data.Length - Offset;
            if (ResLength > 0)
                Buffer.BlockCopy(Data, Convert.ToInt32(Offset), Result, 0, Convert.ToInt32(ResLength));

            return Result;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public int[] ReadUInt16Array(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt16(Offset + i * 2);

            return Result;
        }

        /// <summary>
        /// Записать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public long WriteUInt16Array(this byte[] Data, long Offset, int[] Source, long Length)
        {
            long L = Math.Max(Source.Length, Length);
            for (int i = 0; i < L; i++)
                if (i > Source.Length)
                    Data.WriteUInt16(Offset + i * 2, 0);
                else
                    Data.WriteUInt16(Offset + i * 2, Source[i]);

            return Offset + L * 2;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public int[] ReadInt16Array(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt16(Data.ReadUInt32(Offset + i * 2));

            return Result;
        }

        /// <summary>
        /// Записать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public long WriteInt16Array(this byte[] Data, long Offset, int[] Source, long Length)
        {
            long L = Math.Max(Source.Length, Length);
            for (int i = 0; i < L; i++)
                if (i > Source.Length)
                    Data.WriteInt16(Offset + i * 2, 0);
                else
                    Data.WriteInt16(Offset + i * 2, Source[i]);

            return Offset + L * 2;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public uint[] ReadUInt32Array(this byte[] Data, long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt32(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public int[] ReadInt32Array(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt32(Data.ReadUInt32(Offset + i * 4));

            return Result;
        }

        /// <summary>
        /// Записать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public long WriteUInt32Array(this byte[] Data, long Offset, uint[] Source, long Length)
        {
            long L = Math.Max(Source.Length, Length);
            for (int i = 0; i < L; i++)
                if (i > Source.Length)
                    Data.WriteUInt32(Offset + i * 4, 0);
                else
                    Data.WriteUInt32(Offset + i * 4, Source[i]);

            return Offset + L * 4;
        }


        static public IPAddress ReadIPv4(this byte[] Data, long Offset)
        {
            return new IPAddress(Data.ReadArray(Offset, 4));
        }

        static public long WriteIPv4(this byte[] Data, long Offset, IPAddress IP)
        {
            Data.WriteArray(Offset, IP.GetAddressBytes(), 4);

            return Offset + 4;
        }

        /// <summary>
        /// Преобразовать в массив int
        /// </summary>
        /// <param name="Arr"></param>
        /// <returns></returns>
        static public int[] ToInt32Array(this byte[] Arr)
        {
            return (from A in Arr select Convert.ToInt32(A)).ToArray();
        }

        /// <summary>
        /// В массив byte
        /// </summary>
        /// <param name="Arr"></param>
        /// <returns></returns>
        static public byte[] ToByteArray(this int[] Arr)
        {
            return (from A in Arr select Convert.ToByte(A & 0xFF)).ToArray();
        }

        /// <summary>
        /// В массив byte
        /// </summary>
        /// <param name="Arr"></param>
        /// <returns></returns>
        static public byte[] ToByteArray(this string Text)
        {
            byte[] Temp = new byte[Text.Length];
            Temp.WriteANSIString(0, Text, Text.Length);

            return Temp;
        }
    }
}
