using AdlpLib.Core.Packet.Config;

namespace AdlpLib.LiveConfig
{
    /// <summary>
    /// Параметр для запроса чтения (идентификатор + ожидаемый тип данных для расчёта размера ответа).
    /// Аналог str_get_param_item в C++.
    /// </summary>
    public class GetParamItem
    {
        /// <summary>
        /// Идентификатор параметра
        /// </summary>
        public ParamDataId Id;

        /// <summary>
        /// Ожидаемый тип данных ответа (для расчёта размера поля ответа)
        /// </summary>
        public ParamDataType ExpectedType;

        public GetParamItem(ParamDataId Id, ParamDataType ExpectedType)
        {
            this.Id = Id;
            this.ExpectedType = ExpectedType;
        }
    }
}
