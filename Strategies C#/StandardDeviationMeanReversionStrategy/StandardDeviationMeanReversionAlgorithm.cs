using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Strategies.StandardDeviationMeanReversionStrategy
{
    public class StandardDeviationMeanReversionAlgorithm : QCAlgorithm
    {
        public string symbol = "SPY";

        private StandardDeviation _stdDev;

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, symbol, Resolution.Daily);

            _stdDev = STD(symbol, 30, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
        }

        public override void OnEndOfDay()
        {
            Plot("Charts", "Price", Securities[symbol].Price);
            Plot("Charts", "STDDEV", _stdDev);
        }
    }
}