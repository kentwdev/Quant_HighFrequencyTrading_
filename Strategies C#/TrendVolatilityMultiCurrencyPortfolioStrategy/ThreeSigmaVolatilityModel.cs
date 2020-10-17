using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    /// <summary>
    /// Provides an implementation of <see cref="IVolatilityModel"/> that computes the
    /// relative standard deviation as the volatility of the security
    /// </summary>
    public class ThreeSigmaVolatilityModel : IVolatilityModel
    {
        private readonly StandardDeviation _standardDeviation;

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public decimal Volatility => _standardDeviation * 2.5m;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Securities.RelativeStandardDeviationVolatilityModel"/> class
        /// </summary>
        /// <param name="periodSpan">The time span representing one 'period' length</param>
        /// <param name="periods">The nuber of 'period' lengths to wait until updating the value</param>
        public ThreeSigmaVolatilityModel(StandardDeviation standardDeviation)
        {
            _standardDeviation = standardDeviation;
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data"></param>
        public void Update(Security security, BaseData data)
        {
            _standardDeviation.Update(data.EndTime, data.Price);
        }
    }
}