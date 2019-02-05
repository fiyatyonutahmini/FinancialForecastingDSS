using System;

namespace FinancialForecastingDSS.indicators
{
    [Serializable()]
    class IndicatorException : Exception
    {
        public static readonly string DATA_NOT_ENOUGH_MESSAGE = "The amount of data in the database does not meet the requirements of the desired indicator.";

        public IndicatorException() : base() { }

        public IndicatorException(string message) : base(message) { }
    }
}
