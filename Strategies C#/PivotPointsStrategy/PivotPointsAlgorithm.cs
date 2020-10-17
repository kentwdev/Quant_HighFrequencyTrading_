using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Strategies.PivotPointsStrategy
{
    public class PivotPointsnAlgorithm : QCAlgorithm
    {
        public string symbol = "SPY";

        private PivotPoints _pp;

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, symbol, Resolution.Daily);

            _pp = new PivotPoints(symbol);
        }

        public override void OnData(Slice slice)
        {
            _pp.Update(slice[symbol]);

            if (!_pp.IsReady)
            {
                return;
            }

            if (slice[symbol].Price > _pp.S1)
            {
                SetHoldings(symbol, 1m);
            }
            else if (slice[symbol].Price < _pp.R1)
            {
                SetHoldings(symbol, -1m);
            }
        }

        public override void OnEndOfDay()
        {
            Plot("Charts", "Price", Securities[symbol].Price);
            Plot("Charts", "PP", _pp);
        }
    }
}