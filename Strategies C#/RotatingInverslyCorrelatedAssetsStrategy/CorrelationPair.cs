using System;
using System.Collections.Generic;

namespace Strategies.RotatingInverslyCorrelatedAssetsStrategy
{
    public class CorrelationPair
    {
        public DateTime Date;
        public Dictionary<string, double> Prices;

        public CorrelationPair()
        {
            Prices = new Dictionary<string, double>();
            Date = new DateTime();
        }

        public void Add(string symbol, decimal price)
        {
            Prices.Add(symbol, Convert.ToDouble(price));
        }

        public CorrelationPair(DateTime date)
        {
            Date = date.Date;
            Prices = new Dictionary<string, double>();
        }
    }
}