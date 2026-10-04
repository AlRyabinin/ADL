using Extension.Packet;
using System.Collections.Generic;

namespace AdlpLib.Core.Packet.Config
{
    /// <summary>
    /// Список идентификаторов параметров для запроса чтения (COMMAND_GET_PARAM).
    /// Аналог str_adlp_cmd_param_request_list в C++.
    /// </summary>
    internal class ParamDataRequestList : RawPacket
    {
        public const int HeaderSize = 2;

        public ParamDataRequestList(byte[] Raw) : base(Raw)
        {
        }

        public ParamDataRequestList(ParamDataId[] Ids) : base(HeaderSize + Ids.Length * ParamDataId.Size)
        {
            Flags = 0;
            Count = Ids.Length;
            int Offset = HeaderSize;
            for (int i = 0; i < Ids.Length; i++)
            {
                WriteArray(Offset, Ids[i].getPacket(), ParamDataId.Size);
                Offset += ParamDataId.Size;
            }
        }

        /// <summary>
        /// Количество идентификаторов
        /// </summary>
        public int Count
        {
            get { return ReadByte(0); }
            set { WriteByte(0, value); }
        }

        /// <summary>
        /// Приоритет команды
        /// </summary>
        public int Flags
        {
            get { return ReadByte(1); }
            set { WriteByte(1, value); }
        }

        /// <summary>
        /// Идентификаторы параметров
        /// </summary>
        public ParamDataId[] Fields
        {
            get
            {
                var Res = new List<ParamDataId>();
                int Offset = HeaderSize;
                for (int i = 0; i < Count; i++)
                {
                    Res.Add(new ParamDataId(ReadArray(Offset, ParamDataId.Size)));
                    Offset += ParamDataId.Size;
                }
                return Res.ToArray();
            }
        }
    }
}
