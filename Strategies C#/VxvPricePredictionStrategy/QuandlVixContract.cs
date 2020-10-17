using QuantConnect.Data.Custom;

namespace Strategies.VxvPricePredictionStrategy
{
    public class QuandlVixContract : Quandl
    {
        public QuandlVixContract() : base("VIX CLOSE")
        {
        }
    }
}