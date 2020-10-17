using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class AverageTrueRangeVolatilityModel : IVolatilityModel
    {
        private readonly AverageTrueRange _atr;

        public decimal Volatility => 2.5m * _atr;

        public bool IsWarmingUp => !_atr.IsReady;

        public AverageTrueRangeVolatilityModel(AverageTrueRange atr)
        {
            _atr = atr;
        }

        public void Update(Security security, BaseData data)
        {
        }

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return new List<HistoryRequest>();
        }
    }
}