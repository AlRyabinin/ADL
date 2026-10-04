using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpdsConfig.Controls.Database
{
    /// <summary>
    /// Центральный реестр русских подписей для колонок ViewModel-ов.
    /// </summary>
    public static class ViewHeaders
    {
        private static readonly Dictionary<Type, Dictionary<string, string>> Map = new()
        {
            [typeof(CommonFieldView)] = new()
            {
                ["Field"] = "Поле",
                ["Value"] = "Значение"
            },
            [typeof(PuChannelView)] = new()
            {
                ["Local"] = "№",
                ["Name"] = "Имя",
                ["AdlpIndex"] = "ADLDC",
                ["TypeName"] = "Тип"
            },
            [typeof(PuGroupView)] = new()
            {
                ["Local"] = "№",
                ["Index"] = "Индекс",
                ["ChannelsCount"] = "Каналов",
                ["Channels"] = "Список каналов"
            },
            [typeof(PuPictureView)] = new()
            {
                ["Local"] = "№",
                ["Name"] = "Имя картины"
            },
            [typeof(PuButtonView)] = new()
            {
                ["Local"] = "№",
                ["Page"] = "Стр.",
                ["LocalChannel"] = "Канал",
                ["PosX"] = "X",
                ["PosY"] = "Y",
                ["Icon"] = "Иконка",
                ["Color"] = "Цвет"
            },
            [typeof(ExecChannelView)] = new()
            {
                ["Local"] = "№",
                ["GlobalIndex"] = "Глоб.",
                ["Name"] = "Имя",
                ["Control"] = "Управление",
                ["Wide"] = "Wide",
                ["Protocol"] = "Протокол",
                ["Universe"] = "Вселенная",
                ["Patch"] = "Patch",
                ["Preheat"] = "Преднакал",
                ["Limit"] = "Лимит",
                ["Curve"] = "Кривая",
                ["SwitchDelay"] = "Задержка, мс"
            },
            [typeof(ExecStreamView)] = new()
            {
                ["Index"] = "№",
                ["Protocol"] = "Протокол",
                ["Universe"] = "Вселенная",
                ["Address"] = "Адрес",
                ["PackedId"] = "PackedId"
            }
        };

        /// <summary>Возвращает словарь подписей для типа ViewModel или null, если их нет.</summary>
        public static Dictionary<string, string>? Get<T>() =>
            Map.TryGetValue(typeof(T), out var d) ? d : null;
    }
}
