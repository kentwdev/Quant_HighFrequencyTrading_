using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class VarianceVolatilityModel : IVolatilityModel
    {
        private readonly Variance _variance;

        public decimal Volatility => 2.5m * _variance;

        public VarianceVolatilityModel(Variance variance)
        {
            _variance = variance;
        }

        public void Update(Security security, BaseData data)
        {
            _variance.Update(data.EndTime, data.Price);
        }

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return new List<HistoryRequest>();
        }
    }
}