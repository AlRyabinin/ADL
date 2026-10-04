using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Status
{
    public class Status : RawPacket
    {
        const int HeaderSize = 36;

        public Status(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Type">Тип блока</param>
        /// <param name="Capability">Способности блока</param>
        /// <param name="Port">Порт</param>
        /// <param name="Block">Номер блока</param>
        /// <param name="Payload">Данные</param>
        public Status(Guid Uuid, string Type, BlockCapability Capability, uint SystemId, int Port, int Block, StatusFlags Flags, StatusField[] Fields) : base(HeaderSize + Fields.Sum(F => F.getLength()))
        {
            this.Uuid = Uuid;
            this.Type = Type;
            this.Capability = Capability;
            this.SystemId = SystemId;
            this.Port = Port;
            this.Block = Block;
            this.Flags = Flags;
            this.FieldCount = Fields.Length;

            int Offset = HeaderSize;
            foreach(var F in Fields)
            {
                WriteArray(Offset, F.getPacket(), F.getLength());

                Offset += F.getLength();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid Uuid
        {
            get { return new Guid(ReadArray(0, 16)); }
            set { WriteArray(0, value.ToByteArray(), 16); }
        }

        /// <summary>
        /// Тип блока
        /// </summary>
        public string Type
        {
            get { return ReadString(16, 8); }
            set { WriteString(16, value, 8); }
        }


        /// <summary>
        /// Номер системы
        /// </summary>
        public uint SystemId
        {
            get { return ReadUInt32(24); }
            set { WriteUInt32(24, value); }
        }

        /// <summary>
        /// Способности блока
        /// </summary>
        public BlockCapability Capability
        {
            get { return (BlockCapability)ReadUInt16(28); }
            set { WriteUInt16(28, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Порт
        /// </summary>
        public int Port
        {
            get { return ReadUInt16(30); }
            set { WriteUInt16(30, value); }
        }

        /// <summary>
        /// Номер блока
        /// </summary>
        public int Block
        {
            get { return ReadUInt16(32); }
            set { WriteUInt16(32, value); }
        }

        /// <summary>
        /// Флаги (запрос инициализации)
        /// </summary>
        public StatusFlags Flags
        {
            get { return (StatusFlags)ReadByte(34); }
            set { WriteByte(34, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Количество полей
        /// </summary>
        public int FieldCount
        {
            get { return ReadByte(35); }
            set { WriteByte(35, value); }
        }

        /// <summary>
        /// Поля с данными
        /// </summary>
        public StatusField[] Fields
        {
            get
            {
                var Res = new List<StatusField>();

                int Offset = HeaderSize;
                for (int i = 0; i < FieldCount; i++)
                {
                    var SF = new StatusField(ReadArray(Offset, 4));
                    var Size = SF.Size;
                    SF = new StatusField(ReadArray(Offset, Size));
                    Res.Add(SF);

                    Offset += SF.Size;
                }
                return Res.ToArray();
            }
        }

        [Flags]
        public enum StatusFlags
        {
            STATUS_FLAG_CONFIGURATION_REQUEST = 0x01,
            STATUS_FLAG_FIRMWARE_REQUEST      = 0x02,
        }
    }
}
