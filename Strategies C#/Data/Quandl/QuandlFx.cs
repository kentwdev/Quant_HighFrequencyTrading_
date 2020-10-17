namespace Strategies.Data.Quandl
{
    public class QuandlFx : QuantConnect.Data.Custom.Quandl
    {
        public QuandlFx() : base(valueColumnName: "Value")
        {
        }
    }
}