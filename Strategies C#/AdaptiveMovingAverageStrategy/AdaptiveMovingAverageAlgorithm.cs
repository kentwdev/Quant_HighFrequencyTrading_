using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace Strategies.AdaptiveMovingAverageStrategy
{
    public class AdaptiveMovingAverageAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";

        private KaufmanAdaptiveMovingAverage _ama;

        private AverageTrueRange _atr;

        private OrderTicket _openStopMarketOrder;

        private CommodityChannelIndex _cci;

        private readonly RollingWindow<KaufmanAdaptiveMovingAverage> _previous =
            new RollingWindow<KaufmanAdaptiveMovingAverage>(1);

        public override void Initialize()
        {
            SetCash(10000);
            SetStartDate(2007, 1, 1);
            SetEndDate(DateTime.Now);

            // Request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _ama = new KaufmanAdaptiveMovingAverage(Symbol, 5);

            _cci = new CommodityChannelIndex(5, MovingAverageType.Kama);

            _atr = new AverageTrueRange(Symbol, 14, MovingAverageType.Kama);

            SetWarmup(3);
        }

        public override void OnData(Slice data)
        {
            _ama.Update(data[Symbol] as IndicatorDataPoint);

            if (!_ama.IsReady) return;

            var holdings = Portfolio[Symbol].Quantity;

            if (holdings <= 0 && _ama > _previous[0])
            {
                Log("Buy >> " + Securities[Symbol].Price);
                SetHoldings(Symbol, 1.0);
            }

            if (holdings >= 0 && _ama < _previous[0])
            {
                Log("Sell >> " + Securities[Symbol].Price);
                SetHoldings(Symbol, -1.0);
            }

            _previous.Add(_ama);
        }
    }
}