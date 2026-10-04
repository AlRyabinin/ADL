using AdlpLib.Core;
using AdlpLib.Core.Packet;
using AdlpLib.Core.Packet.Config;
using System;
using System.Collections.Generic;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// Кодирование/декодирование конфигурации в параметры протокола (GET_PARAM/SET_PARAM)
    /// </summary>
    public static class LiveConfigCodec
    {
        /// <summary>
        /// Максимальная полезная нагрузка ответа (без заголовка пакета и заголовка списка ParamDataList)
        /// </summary>
        private const int MaxResponseFields = 1500 - BasePacket.HeaderSize - ParamDataList.HeaderSize;

        /// <summary>
        /// Рассчитать максимальный размер поля ответа для типа данных (в байтах)
        /// Аналог get_param_data_max_size в C++ (configurator_base_module.cpp)
        /// </summary>
        public static int GetResponseFieldSize(ParamDataType DataType)
        {
            int payload;
            switch (DataType)
            {
                case ParamDataType.PARAM_DATA_TYPE_U8:
                case ParamDataType.PARAM_DATA_TYPE_S8:
                    payload = 1;
                    break;
                case ParamDataType.PARAM_DATA_TYPE_U16:
                case ParamDataType.PARAM_DATA_TYPE_S16:
                    payload = 2;
                    break;
                case ParamDataType.PARAM_DATA_TYPE_U32:
                case ParamDataType.PARAM_DATA_TYPE_S32:
                case ParamDataType.PARAM_DATA_TYPE_FLOAT:
                    payload = 4;
                    break;
                case ParamDataType.PARAM_DATA_TYPE_STR:
                case ParamDataType.PARAM_DATA_TYPE_RAW:
                default:
                    payload = Config.MAX_PARAM_TEXT_SIZE;
                    break;
            }
            return ParamDataId.Size + 2 + payload;
        }

        /// <summary>
        /// Идентификатор параметра исполнительного канала
        /// </summary>
        public static ParamDataId ChannelId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_CHANNEL, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра потока
        /// </summary>
        public static ParamDataId StreamId(int Index, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_STREAM, 0, Index, Code);
        }

        /// <summary>
        /// Идентификатор параметра канала управляющего устройства
        /// </summary>
        public static ParamDataId ControlChannelId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_CHANNEL, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра группы управляющего устройства
        /// </summary>
        public static ParamDataId ControlGroupId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_GROUP, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра картины управляющего устройства
        /// </summary>
        public static ParamDataId ControlPictureId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_PICTURE, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра кнопки цветного ПУ
        /// </summary>
        public static ParamDataId ControlButtonId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_CONTROL_BUTTON, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра канала пиксельгейта (PARAM_OBJECT_PIXELGATE_CHANNEL)
        /// </summary>
        public static ParamDataId PixelgateChannelId(int Local, ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_PIXELGATE_CHANNEL, 0, Local, Code);
        }

        /// <summary>
        /// Идентификатор параметра устройства
        /// </summary>
        public static ParamDataId DeviceId(ParamCode Code)
        {
            return new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, Code);
        }

        /// <summary>
        /// Сформировать запрос чтения конфигурации исполнительного устройства.
        /// Возвращает список параметров с указанием ожидаемого типа для расчёта размера ответа.
        /// </summary>
        public static GetParamItem[] BuildExecReadItems(int ChannelCount, int StreamCount)
        {
            var Items = new List<GetParamItem>();

            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceStartChannel), ParamDataType.PARAM_DATA_TYPE_U16));

            for (int i = 1; i <= ChannelCount; i++)
            {
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelIndex), ParamDataType.PARAM_DATA_TYPE_U16));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelName), ParamDataType.PARAM_DATA_TYPE_STR));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelControl), ParamDataType.PARAM_DATA_TYPE_U8));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelStreamId), ParamDataType.PARAM_DATA_TYPE_U32));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelPatch), ParamDataType.PARAM_DATA_TYPE_U16));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelWide), ParamDataType.PARAM_DATA_TYPE_U8));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelPreheat), ParamDataType.PARAM_DATA_TYPE_U8));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelLimit), ParamDataType.PARAM_DATA_TYPE_U8));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelCurve), ParamDataType.PARAM_DATA_TYPE_U8));
                Items.Add(new GetParamItem(ChannelId(i, ParamCode.ChannelSwitchDelay), ParamDataType.PARAM_DATA_TYPE_U32));
            }

            for (int s = 1; s <= StreamCount; s++)
            {
                Items.Add(new GetParamItem(StreamId(s, ParamCode.StreamId), ParamDataType.PARAM_DATA_TYPE_U32));
                Items.Add(new GetParamItem(StreamId(s, ParamCode.StreamStart), ParamDataType.PARAM_DATA_TYPE_U16));
            }

            return Items.ToArray();
        }

        /// <summary>
        /// Применить ответ устройства к конфигурации исполнительного устройства
        /// </summary>
        public static void ApplyExecResponse(LiveExecConfig Cfg, ParamDataField[] Fields)
        {
            if (Fields == null || Cfg == null)
                return;

foreach (var F in Fields)
            {
                // Устройство не содержит объект (нет данных, тип undefined) — пропускаем,
                // чтобы не создавать фиктивные каналы/потоки в конфигурации.
                if (F.Value.DataSize <= ParamDataValue.Size)
                    continue;

                var Id = F.Id;
                if (Id.ObjectType == ObjectCode.PARAM_OBJECT_DEVICE)
                {
                    if (Id.Param == ParamCode.DeviceStartChannel)
                    {
                        int Base = F.Value.GetU16Value();
                        if (Base >= 1)
                            Cfg.Block = ((Base - 1) / LiveConfigDefaults.ChannelsPerBlock) + 1;
                    }
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_CHANNEL)
                {
                    var Ch = Cfg.Channels.Find(C => C.Local == Id.Index);
                    if (Ch == null)
                    {
                        Ch = new LiveChannelCfg { Local = Id.Index, GlobalIndex = Cfg.GetBaseChannel() + Id.Index - 1 };
                        Cfg.Channels.Add(Ch);
                    }

                    switch (Id.Param)
                    {
                        case ParamCode.ChannelIndex:
                            Ch.GlobalIndex = F.Value.GetU16Value();
                            break;
                        case ParamCode.ChannelName:
                            Ch.Name = F.Value.GetSTRValue();
                            break;
                        case ParamCode.ChannelControl:
                            Ch.Control = (F.Value.GetU8Value() != 0) ? "dmx" : "adlp";
                            break;
                        case ParamCode.ChannelStreamId:
                            Ch.StreamId = (int)F.Value.GetU32Value();
                            break;
                        case ParamCode.ChannelPatch:
                            Ch.Patch = F.Value.GetU16Value();
                            break;
                        case ParamCode.ChannelWide:
                            Ch.Wide = F.Value.GetU8Value() != 0;
                            break;
                        case ParamCode.ChannelPreheat:
                            Ch.Preheat = F.Value.GetU8Value();
                            break;
                        case ParamCode.ChannelLimit:
                            Ch.Limit = F.Value.GetU8Value();
                            break;
                        case ParamCode.ChannelCurve:
                            Ch.Curve = F.Value.GetU8Value();
                            break;
                        case ParamCode.ChannelSwitchDelay:
                            Ch.SwitchDelay = (int)F.Value.GetU32Value();
                            break;
                    }
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_STREAM)
                {
                    var St = Cfg.Streams.Find(S => S.Index == Id.Index);
                    if (St == null)
                    {
                        St = new LiveStreamCfg { Index = Id.Index };
                        Cfg.Streams.Add(St);
                    }

                    switch (Id.Param)
                    {
                        case ParamCode.StreamId:
                        {
                            if(F.Value.DataType != ParamDataType.PARAM_DATA_TYPE_U32)
                                break;

                            uint Packed = F.Value.GetU32Value();
                            St.Protocol = LiveConfigDefaults.ProtocolName((AdlpLib.Core.StreamType)((Packed >> 16) & 0xFF));
                            St.Universe = (int)(Packed & 0xFFFF);
                            break;
                        }
                        case ParamCode.StreamStart:
                            if (F.Value.DataType != ParamDataType.PARAM_DATA_TYPE_U16)
                                break;

                            St.Address = F.Value.GetU16Value();
                            break;
                    }
                }
            }

            Cfg.Channels.Sort((A, B) => A.Local.CompareTo(B.Local));
            Cfg.Streams.Sort((A, B) => A.Index.CompareTo(B.Index));
        }

        /// <summary>
        /// Сформировать запись конфигурации исполнительного устройства (SET_PARAM).
        /// Завершающим полем идёт DeviceFirmware=0 — именно его установка (даже если
        /// оно уже 0) заставляет устройство сохранить конфигурацию в постоянную память.
        /// </summary>
        public static ParamDataField[] BuildExecWriteFields(LiveExecConfig Cfg)
        {
            var List = new List<ParamDataField>();

            if (Cfg == null)
                return List.ToArray();

            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceStartChannel), new ParamDataValue(Convert.ToUInt16(Cfg.GetBaseChannel()))));

            foreach (var Ch in Cfg.Channels)
            {
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelIndex), new ParamDataValue(Convert.ToUInt16(Ch.GlobalIndex))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelName), new ParamDataValue(Ch.Name)));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelControl), new ParamDataValue(Convert.ToByte(Ch.Control == "dmx" ? 1 : 0))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelStreamId), new ParamDataValue(Convert.ToUInt32(Ch.StreamId))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelPatch), new ParamDataValue(Convert.ToUInt16(Ch.Patch))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelWide), new ParamDataValue(Convert.ToByte(Ch.Wide ? 1 : 0))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelPreheat), new ParamDataValue(Convert.ToByte(Ch.Preheat))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelLimit), new ParamDataValue(Convert.ToByte(Ch.Limit))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelCurve), new ParamDataValue(Convert.ToByte(Ch.Curve))));
                List.Add(new ParamDataField(ChannelId(Ch.Local, ParamCode.ChannelSwitchDelay), new ParamDataValue(Convert.ToUInt32(Ch.SwitchDelay))));
            }

            foreach (var St in Cfg.Streams)
            {
                List.Add(new ParamDataField(StreamId(St.Index, ParamCode.StreamId), new ParamDataValue(Convert.ToUInt32(St.GetPackedStreamId()))));
                List.Add(new ParamDataField(StreamId(St.Index, ParamCode.StreamStart), new ParamDataValue(Convert.ToUInt16(St.Address))));
            }

            // Устройство применяет набор параметров целиком по завершении записи:
            // установка DeviceConfiguration в 0 фиксирует применённую конфигурацию
            // (см. configurator_base_module / ConfigGenerator: deviceconfig u8 0).
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceConfiguration), new ParamDataValue((byte)0)));

            // Запись в постоянную память выполняется установкой DeviceFirmware в 0,
            // даже если он и так 0 (см. configurator_base_module.cpp: RemoveFirmwareRequest).
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceFirmware), new ParamDataValue((byte)0)));

            return List.ToArray();
        }

        /// <summary>
        /// Сформировать запрос чтения конфигурации управляющего устройства (ПУ).
        /// Каналы (имя/индекс/тип), группы (индекс + все слоты каналов), картины (имена),
        /// настройки экрана (яркость, сенсор, страницы, сон);
        /// для цветного ПУ (color=true) дополнительно кнопки.
        /// </summary>
        public static GetParamItem[] BuildControlReadItems(int ChannelCount, int GroupCount, int PictureCount, bool Color)
        {
            var Items = new List<GetParamItem>();

            Items.Add(new GetParamItem(DeviceId(ParamCode.DevicePriority), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DevicePictureSwitchTime), ParamDataType.PARAM_DATA_TYPE_U16));

            Items.Add(new GetParamItem(DeviceId(ParamCode.DevicePageCount), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceTouchscreenSwapX), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceTouchscreenSwapY), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceBrightness), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceBrightnessSleep), ParamDataType.PARAM_DATA_TYPE_U8));
            Items.Add(new GetParamItem(DeviceId(ParamCode.DeviceSleepTimeout), ParamDataType.PARAM_DATA_TYPE_U16));

            for (int i = 1; i <= ChannelCount; i++)
            {
                Items.Add(new GetParamItem(ControlChannelId(i, ParamCode.ChannelIndex), ParamDataType.PARAM_DATA_TYPE_U16));
                Items.Add(new GetParamItem(ControlChannelId(i, ParamCode.ChannelName), ParamDataType.PARAM_DATA_TYPE_STR));
                Items.Add(new GetParamItem(ControlChannelId(i, ParamCode.ChannelType), ParamDataType.PARAM_DATA_TYPE_U8));
            }

            for (int g = 1; g <= GroupCount; g++)
            {
                Items.Add(new GetParamItem(ControlGroupId(g, ParamCode.GroupIndex), ParamDataType.PARAM_DATA_TYPE_U16));
                for (int s = 1; s <= LiveConfigDefaults.ControlGroupSlots; s++)
                {
                    Items.Add(new GetParamItem(ControlGroupId(g, (ParamCode)((int)ParamCode.GroupChannelStart + s - 1)), ParamDataType.PARAM_DATA_TYPE_U16));
                }
            }

            for (int p = 1; p <= PictureCount; p++)
            {
                Items.Add(new GetParamItem(ControlPictureId(p, ParamCode.PictureName), ParamDataType.PARAM_DATA_TYPE_STR));
            }
            if (Color)
            {
                for (int b = 1; b <= LiveConfigDefaults.ControlButtons; b++)
                {
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonPage), ParamDataType.PARAM_DATA_TYPE_U8));
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonLocal), ParamDataType.PARAM_DATA_TYPE_U16));
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonPosX), ParamDataType.PARAM_DATA_TYPE_U16));
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonPosY), ParamDataType.PARAM_DATA_TYPE_U16));
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonIcon), ParamDataType.PARAM_DATA_TYPE_U8));
                    Items.Add(new GetParamItem(ControlButtonId(b, ParamCode.ControlButtonColor), ParamDataType.PARAM_DATA_TYPE_U8));
                }
            }

            return Items.ToArray();
        }

        /// <summary>
        /// Применить ответ устройства к конфигурации управляющего устройства
        /// </summary>
        public static void ApplyControlResponse(LiveControlConfig Cfg, ParamDataField[] Fields)
        {
            if (Fields == null || Cfg == null)
                return;

            foreach (var F in Fields)
            {
                // Устройство не содержит объект (нет данных, тип undefined) — пропускаем,
                // чтобы не создавать фиктивные записи в конфигурации.
                if (F.Value.DataSize <= ParamDataValue.Size)
                    continue;

                var Id = F.Id;
                if (Id.ObjectType == ObjectCode.PARAM_OBJECT_DEVICE)
                {
                    switch (Id.Param)
                    {
                        case ParamCode.DevicePriority:
                            Cfg.Priority = F.Value.GetU8Value();
                            break;
                        case ParamCode.DevicePictureSwitchTime:
                            Cfg.PictureSwitchTime = F.Value.GetU16Value();
                            break;
                        case ParamCode.DevicePageCount:
                            Cfg.PageCount = F.Value.GetU8Value();
                            break;
                        case ParamCode.DeviceTouchscreenSwapX:
                            Cfg.SwapX = F.Value.GetU8Value();
                            break;
                        case ParamCode.DeviceTouchscreenSwapY:
                            Cfg.SwapY = F.Value.GetU8Value();
                            break;
                        case ParamCode.DeviceBrightness:
                            Cfg.Brightness = F.Value.GetU8Value();
                            break;
                        case ParamCode.DeviceBrightnessSleep:
                            Cfg.BrightnessSleep = F.Value.GetU8Value();
                            break;
                        case ParamCode.DeviceSleepTimeout:
                            Cfg.SleepTimeout = F.Value.GetU16Value();
                            break;
                    }
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_CHANNEL)
                {
                    var Ch = Cfg.Channels.Find(C => C.Local == Id.Index);
                    if (Ch == null)
                    {
                        Ch = new LiveControlChannelCfg { Local = Id.Index };
                        Cfg.Channels.Add(Ch);
                    }

                    switch (Id.Param)
                    {
                        case ParamCode.ChannelIndex:
                            Ch.Index = F.Value.GetU16Value();
                            break;
                        case ParamCode.ChannelName:
                            Ch.Name = F.Value.GetSTRValue();
                            break;
                        case ParamCode.ChannelType:
                            Ch.Type = F.Value.GetU8Value();
                            break;
                    }
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_GROUP)
                {
                    var Grp = Cfg.Groups.Find(G => G.Local == Id.Index);
                    if (Grp == null)
                    {
                        Grp = new LiveControlGroupCfg { Local = Id.Index };
                        Cfg.Groups.Add(Grp);
                    }

                    if (Id.Param == ParamCode.GroupIndex)
                    {
                        Grp.Index = F.Value.GetU16Value();
                    }
                    else if ((Id.Param >= ParamCode.GroupChannelStart) && (Id.Param <= (ParamCode)((int)ParamCode.GroupChannelStart + LiveConfigDefaults.ControlGroupSlots - 1)))
                    {
                        int Slot = 1 + (int)Id.Param - (int)ParamCode.GroupChannelStart;
                        while (Grp.Channels.Count < LiveConfigDefaults.ControlGroupSlots)
                            Grp.Channels.Add(0);
                        Grp.Channels[Slot - 1] = F.Value.GetU16Value();
                    }
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_PICTURE)
                {
                    var Pic = Cfg.Pictures.Find(P => P.Local == Id.Index);
                    if (Pic == null)
                    {
                        Pic = new LiveControlPictureCfg { Local = Id.Index };
                        Cfg.Pictures.Add(Pic);
                    }

                    if (Id.Param == ParamCode.PictureName)
                        Pic.Name = F.Value.GetSTRValue();
                }
                else if (Id.ObjectType == ObjectCode.PARAM_OBJECT_CONTROL_BUTTON)
                {
                    var Btn = Cfg.Buttons.Find(B => B.Local == Id.Index);
                    if (Btn == null)
                    {
                        Btn = new LiveControlButtonCfg { Local = Id.Index };
                        Cfg.Buttons.Add(Btn);
                    }

                    switch (Id.Param)
                    {
                        case ParamCode.ControlButtonPage:
                            Btn.Page = F.Value.GetU8Value();
                            break;
                        case ParamCode.ControlButtonLocal:
                            Btn.LocalChannel = F.Value.GetU16Value();
                            break;
                        case ParamCode.ControlButtonPosX:
                            Btn.PosX = F.Value.GetU16Value();
                            break;
                        case ParamCode.ControlButtonPosY:
                            Btn.PosY = F.Value.GetU16Value();
                            break;
                        case ParamCode.ControlButtonIcon:
                            Btn.Icon = F.Value.GetU8Value();
                            break;
                        case ParamCode.ControlButtonColor:
                            Btn.Color = F.Value.GetU8Value();
                            break;
                    }
                }
            }

            Cfg.Channels.Sort((A, B) => A.Local.CompareTo(B.Local));
            Cfg.Groups.Sort((A, B) => A.Local.CompareTo(B.Local));
            Cfg.Pictures.Sort((A, B) => A.Local.CompareTo(B.Local));
            Cfg.Buttons.Sort((A, B) => A.Local.CompareTo(B.Local));
        }

        /// <summary>
        /// Сформировать запись конфигурации управляющего устройства (SET_PARAM).
        /// Для цветного ПУ (color=true) дополнительно пишутся кнопки и настройки экрана.
        /// Завершающим полем идёт DeviceFirmware=0 — запись в постоянную память.
        /// </summary>
        public static ParamDataField[] BuildControlWriteFields(LiveControlConfig Cfg, bool Color)
        {
            var List = new List<ParamDataField>();

            if (Cfg == null)
                return List.ToArray();

            List.Add(new ParamDataField(DeviceId(ParamCode.DevicePriority), new ParamDataValue(Convert.ToByte(Cfg.Priority))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DevicePictureSwitchTime), new ParamDataValue(Convert.ToUInt16(Cfg.PictureSwitchTime))));

            foreach (var Ch in Cfg.Channels)
            {
                List.Add(new ParamDataField(ControlChannelId(Ch.Local, ParamCode.ChannelIndex), new ParamDataValue(Convert.ToUInt16(Ch.Index))));
                List.Add(new ParamDataField(ControlChannelId(Ch.Local, ParamCode.ChannelName), new ParamDataValue(Ch.Name)));
                List.Add(new ParamDataField(ControlChannelId(Ch.Local, ParamCode.ChannelType), new ParamDataValue(Convert.ToByte(Ch.Type))));
            }

            foreach (var Grp in Cfg.Groups)
            {
                List.Add(new ParamDataField(ControlGroupId(Grp.Local, ParamCode.GroupIndex), new ParamDataValue(Convert.ToUInt16(Grp.Index))));

                for (int s = 1; s <= LiveConfigDefaults.ControlGroupSlots; s++)
                {
                    int Ch = (Grp.Channels != null) && (s <= Grp.Channels.Count) ? Grp.Channels[s - 1] : 0;
                    List.Add(new ParamDataField(
                        ControlGroupId(Grp.Local, (ParamCode)((int)ParamCode.GroupChannelStart + s - 1)),
                        new ParamDataValue(Convert.ToUInt16(Ch))));
                }
            }

            foreach (var Pic in Cfg.Pictures)
            {
                List.Add(new ParamDataField(ControlPictureId(Pic.Local, ParamCode.PictureName), new ParamDataValue(Pic.Name)));
            }

            List.Add(new ParamDataField(DeviceId(ParamCode.DevicePageCount), new ParamDataValue(Convert.ToByte(Cfg.PageCount))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceTouchscreenSwapX), new ParamDataValue(Convert.ToByte(Cfg.SwapX))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceTouchscreenSwapY), new ParamDataValue(Convert.ToByte(Cfg.SwapY))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceBrightness), new ParamDataValue(Convert.ToByte(Cfg.Brightness))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceBrightnessSleep), new ParamDataValue(Convert.ToByte(Cfg.BrightnessSleep))));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceSleepTimeout), new ParamDataValue(Convert.ToUInt16(Cfg.SleepTimeout))));

            if (Color)
            {
                foreach (var Btn in Cfg.Buttons)
                {
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonPage), new ParamDataValue(Convert.ToByte(Btn.Page))));
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonLocal), new ParamDataValue(Convert.ToUInt16(Btn.LocalChannel))));
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonPosX), new ParamDataValue(Convert.ToUInt16(Btn.PosX))));
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonPosY), new ParamDataValue(Convert.ToUInt16(Btn.PosY))));
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonIcon), new ParamDataValue(Convert.ToByte(Btn.Icon))));
                    List.Add(new ParamDataField(ControlButtonId(Btn.Local, ParamCode.ControlButtonColor), new ParamDataValue(Convert.ToByte(Btn.Color))));
                }
            }

            // Устройство применяет набор параметров целиком по завершении записи:
            // установка DeviceConfiguration в 0 фиксирует применённую конфигурацию
            // (см. configurator_base_module / ConfigGenerator: deviceconfig u8 0).
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceConfiguration), new ParamDataValue((byte)0)));

            // Запись в постоянную память выполняется установкой DeviceFirmware в 0
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceFirmware), new ParamDataValue((byte)0)));

            return List.ToArray();
        }

        /// <summary>
        /// Сформировать запрос чтения конфигурации гейта (каналы отображения линий управления).
        /// Для каждого канала: имя (STR) и номер (U16).
        /// </summary>
        public static GetParamItem[] BuildGateReadItems(int ChannelCount)
        {
            var Items = new List<GetParamItem>();

            for (int i = 1; i <= ChannelCount; i++)
            {
                Items.Add(new GetParamItem(PixelgateChannelId(i, ParamCode.ChannelName), ParamDataType.PARAM_DATA_TYPE_STR));
                Items.Add(new GetParamItem(PixelgateChannelId(i, ParamCode.ChannelIndex), ParamDataType.PARAM_DATA_TYPE_U16));
            }

            return Items.ToArray();
        }

        /// <summary>
        /// Применить ответ устройства к конфигурации гейта
        /// </summary>
        public static void ApplyGateResponse(LiveGateConfig Cfg, ParamDataField[] Fields)
        {
            if (Fields == null || Cfg == null)
                return;

            foreach (var F in Fields)
            {
                // Устройство не содержит объект (нет данных, тип undefined) — пропускаем
                if (F.Value.DataSize <= ParamDataValue.Size)
                    continue;

                var Id = F.Id;
                if (Id.ObjectType == ObjectCode.PARAM_OBJECT_PIXELGATE_CHANNEL)
                {
                    var Ch = Cfg.Channels.Find(C => C.Local == Id.Index);
                    if (Ch == null)
                    {
                        Ch = new LivePixelgateChannelCfg { Local = Id.Index };
                        Cfg.Channels.Add(Ch);
                    }

                    switch (Id.Param)
                    {
                        case ParamCode.ChannelIndex:
                            Ch.Index = F.Value.GetU16Value();
                            break;
                        case ParamCode.ChannelName:
                            Ch.Name = F.Value.GetSTRValue();
                            break;
                    }
                }
            }

            Cfg.Channels.Sort((A, B) => A.Local.CompareTo(B.Local));
        }

        /// <summary>
        /// Сформировать запись конфигурации гейта (SET_PARAM).
        /// Каналы отображения линий управления: имя и номер.
        /// Завершающим полем DeviceConfiguration=0 + DeviceFirmware=0 — применение и запись.
        /// </summary>
        public static ParamDataField[] BuildGateWriteFields(LiveGateConfig Cfg)
        {
            var List = new List<ParamDataField>();

            if (Cfg == null)
                return List.ToArray();

            foreach (var Ch in Cfg.Channels)
            {
                List.Add(new ParamDataField(PixelgateChannelId(Ch.Local, ParamCode.ChannelName), new ParamDataValue(Ch.Name)));
                List.Add(new ParamDataField(PixelgateChannelId(Ch.Local, ParamCode.ChannelIndex), new ParamDataValue(Convert.ToUInt16(Ch.Index))));
            }

            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceConfiguration), new ParamDataValue((byte)0)));
            List.Add(new ParamDataField(DeviceId(ParamCode.DeviceFirmware), new ParamDataValue((byte)0)));

            return List.ToArray();
        }
    }
}
