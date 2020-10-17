using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies
{
    public class MomentumZScoreAlgorithm : QCAlgorithm
    {
        private string symbol = "SPY";

        private int tradingFreq = 1;
        private int n = 150;

        private SimpleMovingAverage mean;
        private StandardDeviation sigma;

        public override void Initialize()
        {
            SetStartDate(2001, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            mean = SMA(symbol, n, Resolution.Daily);
            sigma = STD(symbol, n, Resolution.Daily);

            var history = History(symbol, TimeSpan.FromDays(150), Resolution.Daily);

            foreach (var tb in history)
            {
                mean.Update(tb.EndTime, tb.Close);
                sigma.Update(tb.EndTime, tb.Close);
            }

            Schedule.On(DateRules.EveryDay(), TimeRules.At(11, 0), () =>
            {
                var z = (Securities[symbol].Price - mean) / sigma;
                var target = 2.0 / (1 + Math.Exp((double)(-1.2m * z)));

                if (z >= -4 && z <= 4)
                {
                    SetHoldings(symbol, target);
                    SetHoldings("TLT", 2 - target);
                }
                else
                {
                    SetHoldings(symbol, 0);
                    SetHoldings("TLT", 1);
                }
            });
        }

        public void OnData(TradeBars data)
        {
        }
    }
}