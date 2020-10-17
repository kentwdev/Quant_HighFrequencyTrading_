using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.FisherTransformStrategy
{
    public class FisherTransformAlgorithm : QCAlgorithm
    {
        public string symbol = "SPY";
        private decimal _previousRsi;

        private RelativeStrengthIndex _rsi;

        private FisherTransform _fisher;

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);

            _fisher = new FisherTransform(symbol, 14);
            _rsi = new RelativeStrengthIndex(symbol, 14, MovingAverageType.Exponential).Of(_fisher);

            var fiveConsolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(60));
            SubscriptionManager.AddConsolidator(symbol, fiveConsolidator);

            fiveConsolidator.DataConsolidated += (sender, bar) => _fisher.Update(bar);
            fiveConsolidator.DataConsolidated += OnHour;
        }

        public override void OnData(Slice slice)
        {
            if (!_rsi.IsReady) return;

            if (_rsi < 0.5m && _previousRsi > 0.5m)
            {
                SetHoldings(symbol, -1m);
            }
            else if (_rsi > -0.5m && _previousRsi < -0.5m)
            {
                SetHoldings(symbol, 1m);
            }

            _previousRsi = _rsi;
        }

        public void OnHour(object sender, TradeBar data)
        {
            Plot("Charts", "Price", data.Close);
            Plot("Charts", "RSI", _rsi);
        }
    }
}