using System;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class VarianceVolatilityModel : IVolatilityModel
    {
        private readonly Variance _variance;
        private decimal _previousPrice;

        public decimal Volatility => (decimal) Math.Sqrt(decimal.ToDouble(_variance));

        public VarianceVolatilityModel(Variance variance)
        {
            _variance = variance;
        }

        public void Update(Security security, BaseData data)
        {
            _variance.Update(data.EndTime, data.Price - _previousPrice);
            _previousPrice = data.Price;
        }
    }
}