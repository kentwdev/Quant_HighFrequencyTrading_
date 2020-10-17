using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect
{
    public class DownsideProtectionModel : TradeBarIndicator
    {
        public decimal Rf { get; set; }
        private RollingWindow<decimal> HistoricalPrices { get; set; }

        public decimal NonAssetExposure => 1m - Exposure;
        public decimal Exposure => (TmomRule() + MaRule()) / 2m;

        public override bool IsReady => HistoricalPrices.IsReady;

        public DownsideProtectionModel() : this("DownsideProtectionModel")
        {
        }

        public DownsideProtectionModel(string name, int rf = 0) : base(name)
        {
            Rf = rf;
            HistoricalPrices = new RollingWindow<decimal>(252);
        }

        // Returns False if you should reduce exposure, True if you should increase it.
        public decimal TmomRule()
        {
            var startingPrice = HistoricalPrices.First();
            var currentPrice = HistoricalPrices.Last();
            var twelveMonthReturn = currentPrice / startingPrice - 1m;
            var excessReturn = twelveMonthReturn - Rf;
            return excessReturn > 0 ? 1 : excessReturn < 0 ? -1 : 0;
        }

        // Returns False if you should reduce exposure, True if you should increase it.
        public decimal MaRule()
        {
            var currentPrice = HistoricalPrices.Last();
            var movingAveragePrice = HistoricalPrices.Sum() / HistoricalPrices.Count();
            return currentPrice > movingAveragePrice ? 1 : currentPrice < movingAveragePrice ? -1 : 0;
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            HistoricalPrices.Add(input.Price);
            Rf = input.Price / HistoricalPrices[0] - 1m;

            return Rf;
        }
    }
}