using System;

namespace AdlpLib.Core.Packet.Config
{
    /// <summary>
    /// Утилиты для работы с кодами параметров.
    /// </summary>
    public static class ParamCodeExtensions
    {
        /// <summary>
        /// Привод числового кода параметра (например, полученного из сети) к типизированному <see cref="ParamCode"/>.
        /// Возвращает <c>null</c>, если код неизвестен.
        /// </summary>
        public static ParamCode? ToParamCode(int code)
        {
            return Enum.IsDefined(typeof(ParamCode), code) ? (ParamCode)code : null;
        }
    }
}
