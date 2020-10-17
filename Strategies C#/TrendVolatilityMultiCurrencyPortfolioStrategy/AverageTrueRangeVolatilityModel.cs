using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class AverageTrueRangeVolatilityModel : IVolatilityModel
    {
        private readonly AverageTrueRange _atr;
        private decimal _previousPrice;

        public decimal Volatility => _atr;

        public AverageTrueRangeVolatilityModel(AverageTrueRange atr)
        {
            _atr = atr;
        }

        public void Update(Security security, BaseData data)
        {
        }
    }
}