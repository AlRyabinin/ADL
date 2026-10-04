using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    public class ParamDataValue : RawPacket
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int Size = 2;

        public ParamDataValue(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="DataType">Тип данных</param>
        /// <param name="Value">Значение</param>
        public ParamDataValue(ParamDataType DataType, byte[] Value) : base(Size + Value.Length)
        {
            this.DataType = DataType;
            DataSize = Value.Length + Size;
            WriteArray(Size, Value, Value.Length);
        }

        /// <summary>
        /// Конструктор для типа U8
        /// </summary>
        /// <param name="Value"></param>
        public ParamDataValue(byte Value) : base(Size + 1)
        {
            this.DataType = ParamDataType.PARAM_DATA_TYPE_U8;
            DataSize = 1 + Size;
            WriteByte(Size, Value);
        }

        /// <summary>
        /// Конструктор для типа U16
        /// </summary>
        /// <param name="Value"></param>
        public ParamDataValue(ushort Value) : base (Size + 2)
        {
            this.DataType = ParamDataType.PARAM_DATA_TYPE_U16;
            DataSize = 2 + Size;
            WriteUInt16(Size, Value);
        }

        /// <summary>
        /// Конструктор для типа U32
        /// </summary>
        /// <param name="Value"></param>
        public ParamDataValue(uint Value) : base(Size + 4)
        {
            this.DataType = ParamDataType.PARAM_DATA_TYPE_U32;
            DataSize = 4 + Size;
            WriteUInt32(Size, Value);
        }

        /// <summary>
        /// Конструктор для типа STR
        /// </summary>
        /// <param name="Value"></param>
        public ParamDataValue(string Value) : base(Size + Value.Length + 1)
        {
            this.DataType = ParamDataType.PARAM_DATA_TYPE_STR;
            DataSize = Value.Length + Size + 1;
            WriteANSIString(Size, Value, Value.Length);
            WriteByte(Size + Value.Length, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetFieldSize(byte[] Raw, long Offset)
        {
            return Raw.ReadByte(Offset);
        }

        /// <summary>
        /// Размер поля данных
        /// </summary>
        public int DataSize
        {
            get { return ReadByte(0); }
            set { WriteByte(0, value); }
        }

        /// <summary>
        /// Тип данных
        /// </summary>
        public ParamDataType DataType
        {
            get { return (ParamDataType)ReadByte(1); }
            set { WriteByte(1, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Данные
        /// </summary>
        public byte[] Value
        {
            get
            {
                int dataSize = DataSize;
                if(dataSize < Size)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ParamDataValue] Bad DataSize={dataSize}, " +
                        $"DataType={DataType}, packetLen={getLength()}, " +
                        $"hex={BitConverter.ToString(getPacket())}");
                    return Array.Empty<byte>();
                }
                return ReadArray(Size, dataSize - Size);
            }
            set { WriteArray(Size, value, DataSize - Size); }
        }

        /// <summary>
        /// Прочитаем U8
        /// </summary>
        /// <returns></returns>
        public int GetU8Value() => ReadByte(Size);

        /// <summary>
        /// Прочитаем U16
        /// </summary>
        /// <returns></returns>
        public int GetU16Value() => ReadUInt16(Size);

        /// <summary>
        /// Прочитаем U32
        /// </summary>
        /// <returns></returns>
        public uint GetU32Value() => ReadUInt32(Size);

        /// <summary>
        /// Прочитаем строку
        /// </summary>
        /// <returns></returns>
        public string GetSTRValue() => ReadANSIString(Size, DataSize - Size);
    }
}
