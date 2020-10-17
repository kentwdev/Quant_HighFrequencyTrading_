using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.InternalBarStrengthStrategy
{
    public class InternalBarStrengthAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";

        private RelativeStrengthIndex _rsi;

        public override void Initialize()
        {
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _rsi = new RelativeStrengthIndex(Symbol, 3, MovingAverageType.Simple);
        }

        public override void OnData(Slice data)
        {
            var ibs = data[Symbol].Close - data[Symbol].Low / data[Symbol].High - data[Symbol].Low;

            if (Portfolio[Symbol].Invested)
            {
                Liquidate(Symbol);
                return;
            }

            if (data.Time.DayOfWeek == DayOfWeek.Friday)
            {
                return;
            }

            if (_rsi < 10 && ibs <= 0.5m)
            {
                SetHoldings(Symbol, 1.0);
            }
            else if (_rsi > 40 || ibs > 0.5m)
            {
                Liquidate(Symbol);
            }
            else if (_rsi > 90 && ibs >= 0.5m)
            {
                SetHoldings(Symbol, -1.0);
            }
            else if (_rsi < 50 || ibs < 0.5m)
            {
                Liquidate(Symbol);
            }
        }
    }
}