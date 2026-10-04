using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpdsConfig.Controls.Database
{
    /// <summary>
    /// Строка общей информации об устройстве на вкладке «Общее».
    /// </summary>
    public class CommonFieldView
    {
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
    }

    // =========================================================================
    //  PU
    // =========================================================================

    /// <summary>Строка таблицы «Каналы PU».</summary>
    public class PuChannelView
    {
        /// <summary>Локальный номер канала (1..48).</summary>
        public int Local { get; set; }

        /// <summary>Имя канала (ANSI).</summary>
        public string Name { get; set; } = "";

        /// <summary>ADLDC-номер канала.</summary>
        public int AdlpIndex { get; set; }

        /// <summary>Тип канала в человеко-читаемом виде: «Канал», «Группа», «Картина», «Нет».</summary>
        public string TypeName { get; set; } = "";

        /// <summary>Создаёт ViewModel из DTO.</summary>
        public static PuChannelView From(LiveControlChannelCfg c) => new()
        {
            Local = c.Local,
            Name = c.Name ?? "",
            AdlpIndex = c.Index,
            TypeName = c.TypeName
        };
    }

    /// <summary>Строка таблицы «Группы PU».</summary>
    public class PuGroupView
    {
        /// <summary>Локальный номер группы (1..24).</summary>
        public int Local { get; set; }

        /// <summary>Индекс группы (номер на пульте).</summary>
        public int Index { get; set; }

        /// <summary>Число каналов, привязанных к группе.</summary>
        public int ChannelsCount { get; set; }

        /// <summary>Список ADLDC-номеров каналов через запятую.</summary>
        public string Channels { get; set; } = "";

        public static PuGroupView From(LiveControlGroupCfg g) => new()
        {
            Local = g.Local,
            Index = g.Index,
            ChannelsCount = g.Channels?.Count ?? 0,
            Channels = g.ChannelsText ?? ""
        };
    }

    /// <summary>Строка таблицы «Картины PU».</summary>
    public class PuPictureView
    {
        /// <summary>Локальный номер картины (1..24).</summary>
        public int Local { get; set; }

        /// <summary>Имя картины (ANSI).</summary>
        public string Name { get; set; } = "";

        public static PuPictureView From(LiveControlPictureCfg p) => new()
        {
            Local = p.Local,
            Name = p.Name ?? ""
        };
    }

    /// <summary>Строка таблицы «Кнопки PU».</summary>
    public class PuButtonView
    {
        /// <summary>Локальный номер кнопки (1..96).</summary>
        public int Local { get; set; }

        /// <summary>Номер страницы.</summary>
        public int Page { get; set; }

        /// <summary>Номер связанного локального канала (0 — не привязана).</summary>
        public int LocalChannel { get; set; }

        /// <summary>Координата X кнопки на странице.</summary>
        public int PosX { get; set; }

        /// <summary>Координата Y кнопки на странице.</summary>
        public int PosY { get; set; }

        /// <summary>Номер иконки.</summary>
        public int Icon { get; set; }

        /// <summary>Номер цвета из набора.</summary>
        public int Color { get; set; }

        public static PuButtonView From(LiveControlButtonCfg b) => new()
        {
            Local = b.Local,
            Page = b.Page,
            LocalChannel = b.LocalChannel,
            PosX = b.PosX,
            PosY = b.PosY,
            Icon = b.Icon,
            Color = b.Color
        };
    }

    // =========================================================================
    //  EXEC
    // =========================================================================

    /// <summary>Строка таблицы «Каналы EXEC».</summary>
    public class ExecChannelView
    {
        /// <summary>Локальный номер канала.</summary>
        public int Local { get; set; }

        /// <summary>Глобальный ADLDC-номер.</summary>
        public int GlobalIndex { get; set; }

        /// <summary>Имя канала (ANSI).</summary>
        public string Name { get; set; } = "";

        /// <summary>Режим управления: «adlp» или «dmx».</summary>
        public string Control { get; set; } = "";

        /// <summary>Широкий (16-бит) DMX.</summary>
        public string Wide { get; set; } = "нет";

        /// <summary>Протокол потока, вычисляется из StreamId.</summary>
        public string Protocol { get; set; } = "";

        /// <summary>Вселенная потока, вычисляется из StreamId.</summary>
        public int Universe { get; set; }

        /// <summary>Патч адреса (1..512, 0 — нет).</summary>
        public int Patch { get; set; }

        /// <summary>Преднакал (0..255).</summary>
        public int Preheat { get; set; }

        /// <summary>Лимит (0..100).</summary>
        public int Limit { get; set; }

        /// <summary>Кривая (тип характеристики).</summary>
        public int Curve { get; set; }

        /// <summary>Задержка переключения, мс.</summary>
        public int SwitchDelay { get; set; }

        public static ExecChannelView From(LiveChannelCfg c) => new()
        {
            Local = c.Local,
            GlobalIndex = c.GlobalIndex,
            Name = c.Name ?? "",
            Control = c.Control ?? "",
            Wide = c.Wide ? "да" : "нет",
            Protocol = c.Protocol,   // вычисляется из StreamId
            Universe = c.Universe,   // вычисляется из StreamId
            Patch = c.Patch,
            Preheat = c.Preheat,
            Limit = c.Limit,
            Curve = c.Curve,
            SwitchDelay = c.SwitchDelay
        };
    }

    /// <summary>Строка таблицы «Потоки EXEC».</summary>
    public class ExecStreamView
    {
        /// <summary>Индекс потока (1..N).</summary>
        public int Index { get; set; }

        /// <summary>Протокол: нет / dmx / artnet / sACN.</summary>
        public string Protocol { get; set; } = "";

        /// <summary>Вселенная потока.</summary>
        public int Universe { get; set; }

        /// <summary>Начальный адрес.</summary>
        public int Address { get; set; }

        /// <summary>Упакованный идентификатор потока (universe | type &lt;&lt; 16).</summary>
        public int PackedId { get; set; }

        public static ExecStreamView From(LiveStreamCfg s) => new()
        {
            Index = s.Index,
            Protocol = s.Protocol ?? "",
            Universe = s.Universe,
            Address = s.Address,
            PackedId = s.GetPackedStreamId()
        };
    }

    // =========================================================================
    //  Общая шапка устройства
    // =========================================================================

    /// <summary>
    /// Набор ViewModel-ов для одного устройства — то, что показывает форма деталей.
    /// </summary>
    public class DeviceDetailsViewModel
    {
        public string Title { get; set; } = "";
        public List<CommonFieldView> Common { get; set; } = new();
        public List<PuChannelView> PuChannels { get; set; } = new();
        public List<PuGroupView> PuGroups { get; set; } = new();
        public List<PuPictureView> PuPictures { get; set; } = new();
        public List<PuButtonView> PuButtons { get; set; } = new();
        public List<ExecChannelView> ExecChannels { get; set; } = new();
        public List<ExecStreamView> ExecStreams { get; set; } = new();

        /// <summary>
        /// Строит ViewModel из DTO и заранее определённого кода типа.
        /// </summary>
        public static DeviceDetailsViewModel From(DeviceConfigDtoBase cfg, string typeCode)
        {
            var vm = new DeviceDetailsViewModel
            {
                Title = $"Устройство: [{DevicesForm.DisplayTypeName(typeCode)}] {cfg.Mac}"
            };

            // Общие поля
            vm.Common.Add(new CommonFieldView { Field = "Тип конфигурации", Value = DevicesForm.DisplayTypeName(typeCode) });
            vm.Common.Add(new CommonFieldView { Field = "Код типа в БД", Value = typeCode });
            vm.Common.Add(new CommonFieldView { Field = "Id в БД", Value = cfg.Id.ToString() });
            vm.Common.Add(new CommonFieldView { Field = "MAC", Value = cfg.Mac ?? "" });
            vm.Common.Add(new CommonFieldView { Field = "IP", Value = cfg.Ip ?? "" });
            vm.Common.Add(new CommonFieldView { Field = "Маска", Value = cfg.Mask ?? "" });
            vm.Common.Add(new CommonFieldView { Field = "Шлюз", Value = cfg.Gateway ?? "" });
            vm.Common.Add(new CommonFieldView { Field = "Block", Value = cfg.Block.ToString() });

            // Специфичные поля
            switch(cfg)
            {
                case ExecConfigDto exec:
                    vm.Common.Add(new CommonFieldView { Field = "BaseChannel", Value = exec.BaseChannel ?? "" });
                    vm.ExecChannels = (exec.Channels ?? new()).Select(ExecChannelView.From).ToList();
                    vm.ExecStreams = (exec.Streams ?? new()).Select(ExecStreamView.From).ToList();
                    break;

                case PuConfigDto pu:
                    vm.Common.Add(new CommonFieldView { Field = "Priority", Value = pu.Priority.ToString() });
                    vm.Common.Add(new CommonFieldView { Field = "PictureSwitchTime", Value = pu.PictureSwitchTime.ToString() });
                    vm.Common.Add(new CommonFieldView { Field = "PageCount", Value = pu.PageCount.ToString() });
                    vm.Common.Add(new CommonFieldView { Field = "SwapX / SwapY", Value = $"{pu.SwapX} / {pu.SwapY}" });
                    vm.Common.Add(new CommonFieldView { Field = "Brightness", Value = pu.Brightness.ToString() });
                    vm.Common.Add(new CommonFieldView { Field = "BrightnessSleep", Value = pu.BrightnessSleep.ToString() });
                    vm.Common.Add(new CommonFieldView { Field = "SleepTimeout", Value = pu.SleepTimeout.ToString() });

                    vm.PuChannels = (pu.Channels ?? new()).Select(PuChannelView.From).ToList();
                    vm.PuGroups = (pu.Groups ?? new()).Select(PuGroupView.From).ToList();
                    vm.PuPictures = (pu.Pictures ?? new()).Select(PuPictureView.From).ToList();
                    vm.PuButtons = (pu.Buttons ?? new()).Select(PuButtonView.From).ToList();
                    break;
            }

            return vm;
        }
    }
}
