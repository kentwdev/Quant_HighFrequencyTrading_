using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect
{
    public class SimpleRiskParityStrategy : QCAlgorithm
    {
        private string spy = "SPY";
        private string[] equities = { /*"IVV", "HDV",*/ "USMV", "EUSA", "MTUM", /*"BRK-B",*/ "MDY" /*, "SPLV", "VDC", "VHT"*/ };
        private string[] commodities = { "IAU" };
        private string[] treasuries = { "TLO", "IEF", "IEI" };
        private string[] bonds = { "AGG", "BND", "BIV", "VCIT" };
        private string[] forex = { "USDSEK" };

        private List<string> securities => equities.Union(commodities).Union(treasuries).Union(bonds).ToList();
        private List<string> riskyAssets => equities.Union(commodities).ToList();
        private List<string> hedges => treasuries.Union(bonds).ToList();

        private decimal _leverage = 2m;

        public override void Initialize()
        {
            SetStartDate(2007, 6, 1);
            SetEndDate(DateTime.Now.Date.AddDays(-1));
            SetCash(10000);

            AddSecurity(SecurityType.Equity, spy, Resolution.Minute);

            foreach (var security in securities)
            {
                AddSecurity(SecurityType.Equity, security, Resolution.Minute);
                Securities[security].SetLeverage(8m);
            }

            Schedule.On(DateRules.Every(DayOfWeek.Tuesday), TimeRules.At(11, 0), () =>
            {
                foreach (var security in hedges.Where(s => Securities[s].Price > 0m))
                {
                    SetHoldings(security, _leverage * 1.06m / (hedges.Where(s => Securities[s].Price > 0m).Count()));
                }

                foreach (var security in riskyAssets.Where(s => Securities[s].Price > 0m))
                {
                    SetHoldings(security, _leverage * 0.27m / (riskyAssets.Where(s => Securities[s].Price > 0m).Count()));
                }
            });
        }

        public void OnData(TradeBars data)
        {
            /*foreach (var security in riskyAssets.Where(s => data.ContainsKey(s) && Math.Abs(Portfolio[s].UnrealizedProfitPercent) > 0.15m))
	        {
	        	SetHoldings(security, 0m);
	        }*/
        }
    }
}