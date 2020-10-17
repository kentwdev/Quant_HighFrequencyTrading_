using QuantConnect.Indicators;
using System;
using System.Linq;

namespace Strategies.UniversalInvestmentStrategy
{
    public class SymbolData
    {
        private StandardDeviation _sd;
        private RollingWindow<Tuple<decimal, decimal>> _dailyReturns;

        private RollingWindow<decimal> _spyCloses;
        private RollingWindow<decimal> _tltCloses;

        private decimal _spyAllocation;
        private decimal _tltAllocation;
        private double _volatilityFactor = 4.0;

        public SymbolData(StandardDeviation sd, RollingWindow<Tuple<decimal, decimal>> dailyReturns)
        {
            _sd = sd;
            _dailyReturns = dailyReturns;
        }

        public void UpdateWeights()
        {
            var highestSharpe = 0m;
            for (decimal spyAllocation = 0.0m, tltAllocation = 1.0m;
                spyAllocation <= 1.0m;
                spyAllocation += 0.1m, tltAllocation -= 0.1m)
            {
                var sharpe = VolatilityScaledSharpeRatio(spyAllocation, tltAllocation);
                if (sharpe > highestSharpe)
                {
                    _spyAllocation = spyAllocation;
                    _tltAllocation = tltAllocation;
                    highestSharpe = sharpe;
                }
            }

            _sd.Reset();
            _dailyReturns.Reset();
        }

        public void UpdateDailyReturns(decimal spyClose, decimal tltClose)
        {
            if (_spyCloses.Samples > 0 && _tltCloses.Samples > 0)
            {
                var spyReturn = (spyClose - _spyCloses[0]) / _spyCloses[0];
                var tltReturn = (tltClose - _tltCloses[0]) / _tltCloses[0];
                _dailyReturns.Add(new Tuple<decimal, decimal>(spyReturn, tltReturn));
            }

            _spyCloses.Add(spyClose);
            _tltCloses.Add(tltClose);
        }

        private decimal VolatilityScaledSharpeRatio(decimal spyAllocation, decimal tltAllocation)
        {
            var dailyReturns = _dailyReturns.Select(dailyReturn => spyAllocation * dailyReturn.Item1 + tltAllocation * dailyReturn.Item2);
            var mean = dailyReturns.Average();

            foreach (var dr in dailyReturns)
            {
                _sd.Update(DateTime.Now, dr);
            }

            var sharpe = mean / (decimal)Math.Pow((double)_sd.Current.Value, _volatilityFactor);

            return sharpe;
        }
    }
}