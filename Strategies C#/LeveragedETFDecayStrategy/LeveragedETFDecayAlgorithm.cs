using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace Strategies.LeveragedETFDecayStrategy
{
    public class LeveragedETFDecayAlgorithm : QCAlgorithm
    {
        private string faz = "FAZ";

        public override void Initialize()
        {
            SetStartDate(2009, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(3000);

            AddSecurity(SecurityType.Equity, faz, Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio[faz].Invested)
            {
                SetHoldings(faz, -1.0m);
            }
        }
    }
}