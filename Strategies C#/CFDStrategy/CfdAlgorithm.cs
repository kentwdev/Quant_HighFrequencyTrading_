using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Strategies.CFDStrategy
{
    public class CfdAlgorithm : QCAlgorithm
    {
        public string symbol = "XAUUSD";

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Cfd, symbol, Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
        }

        public override void OnEndOfDay()
        {
            Plot("Charts", "Price", Securities[symbol].Price);
        }
    }
}