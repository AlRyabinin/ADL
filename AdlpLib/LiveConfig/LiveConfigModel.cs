using AdlpLib.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// Типы устройств конфигуратора
    /// </summary>
    public static class LiveDeviceTypes
    {
        /// <summary>Исполнительное устройство (IKB)</summary>
        public const string Exec = "IKB";

        /// <summary>Управляющее устройство (PU24)</summary>
        public const string Control = "PU24";

        /// <summary>Цветной ПУ (графический пульт управления)</summary>
        public const string ControlPanel = "PU";

        /// <summary>Гейт (INDIGATE — пиксельгейт)</summary>
        public const string Gate = "INDIGATE";

        /// <summary>
        /// Тип по строке типа устройства
        /// </summary>
        public static bool IsExec(string DeviceType) => string.Equals(DeviceType, Exec, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Управляющее устройство: PU24 или цветной ПУ (идентификатор «PU»)
        /// </summary>
        public static bool IsControl(string DeviceType)
        {
            return string.Equals(DeviceType, Control, StringComparison.OrdinalIgnoreCase)
                || string.Equals(DeviceType, ControlPanel, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Гейт (INDIGATE — пиксельгейт). Класс гейтов расширяемый: далее появятся и другие типы.
        /// </summary>
        public static bool IsGate(string DeviceType)
        {
            return string.Equals(DeviceType, Gate, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Цветной ПУ (графический пульт управления, идентификатор «PU»)
        /// </summary>
        public static bool IsColorPanel(string DeviceType)
        {
            return string.Equals(DeviceType, ControlPanel, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Канал исполнительного устройства
    /// </summary>
    public class LiveChannelCfg
    {
        /// <summary>Локальный номер канала (1..N)</summary>
        public int Local { get; set; }

        /// <summary>Глобальный ADLDC-номер канала</summary>
        public int GlobalIndex { get; set; }

        /// <summary>Имя канала (ANSI)</summary>
        public string Name { get; set; } = "";

        /// <summary>Режим управления: "adlp" или "dmx"</summary>
        public string Control { get; set; } = "adlp";

        /// <summary>Широкий (16 бит) DMX</summary>
        public bool Wide { get; set; }

        /// <summary>Идентификатор потока (упакованный u32: universe | type &lt;&lt; 16)</summary>
        public int StreamId { get; set; }

        /// <summary>
        /// Тип потока канала (старшие 2 байта StreamId). Не сериализуется.
        /// device — поток управления устройством (поток, выбранный в списке каналов).
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string Protocol
        {
            get => LiveConfigDefaults.ProtocolName((StreamType)(((uint)StreamId >> 16) & 0xFF));
            set => StreamId = (StreamId & 0xFFFF) | ((int)LiveConfigDefaults.StreamTypeFor(value) << 16);
        }

        /// <summary>
        /// Вселенная потока канала (младшие 2 байта StreamId). Не сериализуется.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int Universe
        {
            get => StreamId & 0xFFFF;
            set => StreamId = (StreamId & unchecked((int)0xFFFF0000)) | (value & 0xFFFF);
        }

        /// <summary>Патч адреса (1..512, 0 - нет)</summary>
        public int Patch { get; set; }

        /// <summary>Преднакал (0..255)</summary>
        public int Preheat { get; set; }

        /// <summary>Лимит (0..100)</summary>
        public int Limit { get; set; } = 100;

        /// <summary>Кривая (тип характеристики)</summary>
        public int Curve { get; set; }

        /// <summary>Задержка переключения, мс</summary>
        public int SwitchDelay { get; set; }

        public override string ToString()
        {
            return $"Ch{Local} [{GlobalIndex}] \"{Name}\" {(Control == "dmx" ? "DMX" : "ADLP")} st={StreamId} p={Patch}";
        }
    }

    /// <summary>
    /// Поток управления исполнительного устройства
    /// </summary>
    public class LiveStreamCfg
    {
        /// <summary>Индекс потока (1..N)</summary>
        public int Index { get; set; }

        /// <summary>Протокол: нет / dmx / artnet / sACN</summary>
        public string Protocol { get; set; } = "нет";

        /// <summary>Вселенная потока</summary>
        public int Universe { get; set; }

        /// <summary>Начальный адрес</summary>
        public int Address { get; set; } = 1;

        /// <summary>
        /// Упакованный идентификатор потока
        /// </summary>
        public int GetPackedStreamId()
        {
            return (int)LiveConfigDefaults.PackStreamId(Protocol, Universe);
        }

        public override string ToString()
        {
            return $"Stream{Index} {Protocol}/{Universe} @{Address}";
        }
    }

    /// <summary>
    /// Конфигурация исполнительного устройства
    /// </summary>
    public class LiveExecConfig
    {
        /// <summary>Номер блока ADLD (из него вычисляется базовый канал)</summary>
        public int Block { get; set; } = 1;

        /// <summary>Потоки управления</summary>
        public List<LiveStreamCfg> Streams { get; set; } = new List<LiveStreamCfg>();

        /// <summary>Каналы</summary>
        public List<LiveChannelCfg> Channels { get; set; } = new List<LiveChannelCfg>();

        /// <summary>
        /// Базовый канал устройства (1 + (Block-1)*24)
        /// </summary>
        public int GetBaseChannel()
        {
            return 1 + (Block - 1) * LiveConfigDefaults.ChannelsPerBlock;
        }
    }

    /// <summary>
    /// Канал управляющего устройства (кнопка выбора канала/группы/картины)
    /// </summary>
    public class LiveControlChannelCfg
    {
        /// <summary>Локальный номер канала управления (1..48)</summary>
        public int Local { get; set; }

        /// <summary>Имя канала (ANSI)</summary>
        public string Name { get; set; } = "";

        /// <summary>ADLDC-номер канала (для типа CHANNEL)</summary>
        public int Index { get; set; }

        /// <summary>
        /// Тип канала (ChannelType): 0 - нет, 1 - канал, 2 - группа, 3 - картина
        /// </summary>
        public int Type { get; set; } = 1;

        /// <summary>
        /// Имя типа для интерфейса. Не сериализуется.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string TypeName
        {
            get => TypeNameFor(Type);
            set => Type = TypeFor(value);
        }

        internal static string TypeNameFor(int Type)
        {
            switch (Type)
            {
                case (int)ChannelType.CHTYPE_CHANNEL: return "Канал";
                case (int)ChannelType.CHTYPE_GROUP: return "Группа";
                case (int)ChannelType.CHTYPE_PICTURE: return "Картина";
                default: return "Нет";
            }
        }

        internal static int TypeFor(string Name)
        {
            switch (Name)
            {
                case "Канал": return (int)ChannelType.CHTYPE_CHANNEL;
                case "Группа": return (int)ChannelType.CHTYPE_GROUP;
                case "Картина": return (int)ChannelType.CHTYPE_PICTURE;
                default: return (int)ChannelType.CHTYPE_NONE;
            }
        }

        public override string ToString()
        {
            return $"Ctrl{Local} [{Index}] \"{Name}\" {(ChannelType)Type}";
        }
    }

    /// <summary>
    /// Группа управляющего устройства
    /// </summary>
    public class LiveControlGroupCfg
    {
        /// <summary>Локальный номер группы (1..24)</summary>
        public int Local { get; set; }

        /// <summary>Индекс группы (номер на пульте)</summary>
        public int Index { get; set; }

        /// <summary>ADLDC-номера каналов группы (слоты 1..100, 0 - пусто)</summary>
        public List<int> Channels { get; set; } = new List<int>();

        /// <summary>
        /// Список каналов группы через запятую. Не сериализуется.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string ChannelsText
        {
            get => string.Join(", ", Channels.Where(C => C > 0));
            set
            {
                Channels.Clear();
                if (string.IsNullOrWhiteSpace(value))
                    return;

                foreach (var Part in value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(Part, out int Ch) && (Ch > 0))
                        Channels.Add(Ch);
                }
            }
        }

        public override string ToString()
        {
            return $"Grp{Local} [{Index}] {{{ChannelsText}}}";
        }
    }

    /// <summary>
    /// Картина управляющего устройства
    /// </summary>
    public class LiveControlPictureCfg
    {
        /// <summary>Локальный номер картины (1..24)</summary>
        public int Local { get; set; }

        /// <summary>Имя картины (ANSI)</summary>
        public string Name { get; set; } = "";

        public override string ToString()
        {
            return $"Pic{Local} \"{Name}\"";
        }
    }

    /// <summary>
    /// Кнопка цветного ПУ
    /// </summary>
    public class LiveControlButtonCfg
    {
        /// <summary>Локальный номер кнопки (1..96)</summary>
        public int Local { get; set; }

        /// <summary>Номер страницы</summary>
        public int Page { get; set; } = 1;

        /// <summary>Номер локального канала</summary>
        public int LocalChannel { get; set; }

        /// <summary>Координата X кнопки на странице</summary>
        public int PosX { get; set; }

        /// <summary>Координата Y кнопки на странице</summary>
        public int PosY { get; set; }

        /// <summary>Номер иконки</summary>
        public int Icon { get; set; }

        /// <summary>Номер цвета (из набора)</summary>
        public int Color { get; set; }

        public override string ToString()
        {
            return $"Btn{Local} (p{Page} ch{LocalChannel} x{PosX} y{PosY} i{Icon} c{Color})";
        }
    }

    /// <summary>
    /// Конфигурация управляющего устройства (ПУ)
    /// </summary>
    public class LiveControlConfig
    {
        /// <summary>Номер блока ADLD</summary>
        public int Block { get; set; } = 1;

        /// <summary>Приоритет устройства</summary>
        public int Priority { get; set; } = 5;

        /// <summary>Время перехода между картинами, мс</summary>
        public int PictureSwitchTime { get; set; }

        /// <summary>Число страниц кнопок (цветной ПУ)</summary>
        public int PageCount { get; set; } = 1;

        /// <summary>Поменять ось X сенсора (цветной ПУ)</summary>
        public int SwapX { get; set; }

        /// <summary>Поменять ось Y сенсора (цветной ПУ)</summary>
        public int SwapY { get; set; }

        /// <summary>Яркость, % (цветной ПУ)</summary>
        public int Brightness { get; set; } = 100;

        /// <summary>Яркость при сне, % (цветной ПУ)</summary>
        public int BrightnessSleep { get; set; } = 100;

        /// <summary>Время до сна, с (цветной ПУ)</summary>
        public int SleepTimeout { get; set; }

        /// <summary>Каналы управления</summary>
        public List<LiveControlChannelCfg> Channels { get; set; } = new List<LiveControlChannelCfg>();

        /// <summary>Группы</summary>
        public List<LiveControlGroupCfg> Groups { get; set; } = new List<LiveControlGroupCfg>();

        /// <summary>Картины</summary>
        public List<LiveControlPictureCfg> Pictures { get; set; } = new List<LiveControlPictureCfg>();

        /// <summary>Кнопки цветного ПУ</summary>
        public List<LiveControlButtonCfg> Buttons { get; set; } = new List<LiveControlButtonCfg>();
    }

    /// <summary>
    /// Канал отображения гейта (пиксельгейт, PARAM_OBJECT_PIXELGATE_CHANNEL).
    /// Отображает состояние линии управления: имя + номер канала.
    /// </summary>
    public class LivePixelgateChannelCfg
    {
        /// <summary>Локальный номер канала (1..N)</summary>
        [DisplayName("Локальный")]
        public int Local { get; set; }

        /// <summary>Имя канала (ANSI)</summary>
        [DisplayName("Имя")]
        public string Name { get; set; } = "";

        /// <summary>Номер канала (линии управления)</summary>
        [DisplayName("Канал")]
        public int Index { get; set; }

        public override string ToString()
        {
            return $"PG {Local} [{Index}] \"{Name}\"";
        }
    }

    /// <summary>
    /// Конфигурация гейта.
    /// Класс гейтов комбинированный: каналы процессинга данных + каналы отображения
    /// состояния линий управления (PARAM_OBJECT_PIXELGATE_CHANNEL: имя и номер канала).
    /// </summary>
    public class LiveGateConfig
    {
        /// <summary>Номер блока ADLD</summary>
        public int Block { get; set; } = 1;

        /// <summary>Каналы отображения состояния линий управления</summary>
        public List<LivePixelgateChannelCfg> Channels { get; set; } = new List<LivePixelgateChannelCfg>();
    }

    /// <summary>
    /// Генерация конфигураций по умолчанию
    /// </summary>
    public static class LiveConfigDefaults
    {
        /// <summary>Число каналов в блоке ADLD</summary>
        public const int ChannelsPerBlock = 24;

        /// <summary>Число каналов управляющего устройства (ПУ)</summary>
        public const int ControlChannels = 48;

        /// <summary>Число групп управляющего устройства</summary>
        public const int ControlGroups = 24;

        /// <summary>Число картин управляющего устройства</summary>
        public const int ControlPictures = 24;

        /// <summary>Число слотов каналов группы</summary>
        public const int ControlGroupSlots = 100;

        /// <summary>Число кнопок цветного ПУ</summary>
        public const int ControlButtons = 96;

        /// <summary>Число каналов отображения гейта (пиксельгейт)</summary>
        public const int GateChannels = 20;

        /// <summary>
        /// Упаковать идентификатор потока (u32: universe | type &lt;&lt; 16)
        /// </summary>
        public static uint PackStreamId(string Protocol, int Universe)
        {
            var Type = StreamTypeFor(Protocol);
            return (uint)Universe | ((uint)Type << 16);
        }

        /// <summary>
        /// Тип потока по имени протокола
        /// </summary>
        public static StreamType StreamTypeFor(string Protocol)
        {
            switch (Protocol?.Trim().ToLowerInvariant())
            {
                case "":
                case "нет":
                case "none": return StreamType.STREAM_TYPE_NONE;
                case "dmx": return StreamType.STREAM_TYPE_DMX;
                case "artnet": return StreamType.STREAM_TYPE_ARTNET;
                case "acn":
                case "sacn": return StreamType.STREAM_TYPE_ACN;
                case "device":
                default: return StreamType.STREAM_TYPE_DEVICE;
            }
        }

        /// <summary>
        /// Имя протокола по типу потока
        /// </summary>
        public static string ProtocolName(StreamType Type)
        {
            switch (Type)
            {
                case StreamType.STREAM_TYPE_NONE: return "нет";
                case StreamType.STREAM_TYPE_DMX: return "dmx";
                case StreamType.STREAM_TYPE_ARTNET: return "artnet";
                case StreamType.STREAM_TYPE_ACN: return "sACN";
                case StreamType.STREAM_TYPE_DEVICE:
                default: return "device";
            }
        }

        /// <summary>
        /// Создать конфигурацию исполнительного устройства по умолчанию для указанного блока.
        /// Каналы "Канал %d" (ADLDC-номер), управление ADLP, поток device, патча нет,
        /// преднакал 0, лимит 100, кривая 0. Список потоков пуст.
        /// </summary>
        /// <param name="Block">Номер блока ADLD</param>
        /// <param name="Channels">Число каналов</param>
        public static LiveExecConfig CreateExecDefault(int Block, int Channels = ChannelsPerBlock)
        {
            int Base = 1 + (Block - 1) * ChannelsPerBlock;

            var Cfg = new LiveExecConfig { Block = Block };

            int DeviceStreamPacked = (int)PackStreamId("device", 0);

            for (int i = 1; i <= Channels; i++)
            {
                Cfg.Channels.Add(new LiveChannelCfg
                {
                    Local = i,
                    GlobalIndex = Base + i - 1,
                    Name = $"Канал {Base + i - 1}",
                    Control = "adlp",
                    Wide = false,
                    StreamId = DeviceStreamPacked,
                    Patch = 0,
                    Preheat = 0,
                    Limit = 100,
                    Curve = 0,
                    SwitchDelay = 0
                });
            }

            return Cfg;
        }

        /// <summary>
        /// Создать конфигурацию управляющего устройства по умолчанию для указанного блока.
        /// Каналы "Канал %d" с ADLDC-номером, тип "канал"; группы 1..N (пустые),
        /// картины 1..N (без имени). Для цветного ПУ (color=true) добавляются кнопки
        /// настроек по умолчанию (страница 1, без канала).
        /// </summary>
        /// <param name="Block">Номер блока ADLD</param>
        /// <param name="Color">Цветной ПУ (добавить кнопки)</param>
        public static LiveControlConfig CreateControlDefault(int Block, bool Color = true)
        {
            int Base = 1 + (Block - 1) * ChannelsPerBlock;

            var Cfg = new LiveControlConfig { Block = Block };

            for (int i = 1; i <= ControlChannels; i++)
            {
                Cfg.Channels.Add(new LiveControlChannelCfg
                {
                    Local = i,
                    Name = $"Канал {Base + i - 1}",
                    Index = Base + i - 1,
                    Type = (int)ChannelType.CHTYPE_CHANNEL
                });
            }

            for (int i = 1; i <= ControlGroups; i++)
            {
                Cfg.Groups.Add(new LiveControlGroupCfg
                {
                    Local = i,
                    Index = i,
                    Channels = new List<int>()
                });
            }

            for (int i = 1; i <= ControlPictures; i++)
            {
                Cfg.Pictures.Add(new LiveControlPictureCfg { Local = i, Name = "" });
            }

            if (Color)
            {
                for (int i = 1; i <= ControlButtons; i++)
                {
                    Cfg.Buttons.Add(new LiveControlButtonCfg { Local = i, Page = 1 });
                }
            }

            return Cfg;
        }

        /// <summary>
        /// Создать конфигурацию гейта по умолчанию для указанного блока.
        /// Каналы отображения "Линия i" с номером канала (ADLDC-номер).
        /// </summary>
        /// <param name="Block">Номер блока ADLD</param>
        /// <param name="Channels">Число каналов отображения</param>
        public static LiveGateConfig CreateGateDefault(int Block, int Channels = GateChannels)
        {
            int Base = 1 + (Block - 1) * ChannelsPerBlock;

            var Cfg = new LiveGateConfig { Block = Block };

            for (int i = 1; i <= Channels; i++)
            {
                Cfg.Channels.Add(new LivePixelgateChannelCfg
                {
                    Local = i,
                    Name = $"Линия {Base + i - 1}",
                    Index = Base + i - 1
                });
            }

            return Cfg;
        }
    }
}