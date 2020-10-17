using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class ThreeSigmaVolatilityModel : IVolatilityModel
    {
        private StandardDeviation _standardDeviation;

        public decimal Volatility => 2.5m * _standardDeviation;

        public ThreeSigmaVolatilityModel(StandardDeviation standardDeviation)
        {
            _standardDeviation = standardDeviation;
        }

        public void Update(Security security, BaseData data)
        {
            //_standardDeviation.Update(data.EndTime, data.Price);
        }

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return new List<HistoryRequest>();
        }
    }
}