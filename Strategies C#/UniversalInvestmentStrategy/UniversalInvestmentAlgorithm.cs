using System;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.UniversalInvestmentStrategy
{
    public class UniversalInvestmentAlgorithm : QCAlgorithm
    {
        private const int LookbackPeriod = 50; // 50 - 80 days

        public string spy = "SPY";
        public string tlt = "TLT";

        private StandardDeviation _sd;
        private RollingWindow<Tuple<decimal, decimal>> _dailyReturns;

        private Resolution _dataResolution = Resolution.Daily;

        private double _volatilityFactor = 2.0;

        private decimal _previousPortfolioValue;

        private Tuple<TradeBar, TradeBar> _previousTradeBar;

        private decimal _spyAllocation;
        private decimal _tltAllocation;

        public override void Initialize()
        {
            SetStartDate(2015, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, spy, _dataResolution);
            AddSecurity(SecurityType.Equity, tlt, _dataResolution);

            _sd = new StandardDeviation(LookbackPeriod - 1);
            _dailyReturns = new RollingWindow<Tuple<decimal, decimal>>(LookbackPeriod - 1);

            var spyHistory = History(spy, TimeSpan.FromDays(LookbackPeriod), _dataResolution);
            var tltHistory = History(tlt, TimeSpan.FromDays(LookbackPeriod), _dataResolution);
            var history = spyHistory.Zip(tltHistory, (spyBar, tltBar) => new Tuple<TradeBar, TradeBar>(spyBar, tltBar));
            var highestSharpe = 0m;

            for (decimal spyAllocation = 0.0m, tltAllocation = 1.0m; spyAllocation <= 1.0m; spyAllocation += 0.1m, tltAllocation -= 0.1m)
            {
                var previousBar = history.First();

                foreach (var tradeBar in history.Skip(1))
                {
                    var spyReturn = spyAllocation * (tradeBar.Item1.Close - previousBar.Item1.Close) / previousBar.Item1.Close;
                    var tltReturn = tltAllocation * (tradeBar.Item2.Close - previousBar.Item2.Close) / previousBar.Item2.Close;

                    _dailyReturns.Add(new Tuple<decimal, decimal>(spyReturn, tltReturn));
                    _sd.Update(tradeBar.Item1.EndTime, spyReturn + tltReturn);
                    previousBar = tradeBar;
                }

                var mean = _dailyReturns.Select(dailyReturn => dailyReturn.Item1 + dailyReturn.Item2).Average();
                var sharpe = mean / Convert.ToDecimal(Math.Pow(Convert.ToDouble(_sd), _volatilityFactor));

                if (sharpe > highestSharpe)
                {
                    highestSharpe = sharpe;
                    _spyAllocation = spyAllocation;
                    _tltAllocation = tltAllocation;
                }

                _sd.Reset();
                _dailyReturns.Reset();
            }
        }

        public override void OnData(Slice slice)
        {
            if (_dailyReturns.Samples == 0)
            {
                SetHoldings(spy, _spyAllocation);
                SetHoldings(tlt, _tltAllocation);
            }
            else if (_dailyReturns.IsReady)
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
                    }
                }

                _sd.Reset();
                _dailyReturns.Reset();
                _previousTradeBar = null;
                return;
            }

            if (_previousTradeBar != null)
            {
                UpdateDailyReturn();
            }
            _previousTradeBar = new Tuple<TradeBar, TradeBar>(slice[spy], slice[tlt]);
        }

        private void UpdateDailyReturn()
        {
            var spyReturn = Securities[spy].Close - _previousTradeBar.Item1.Close / _previousTradeBar.Item1.Close;
            var tltReturn = Securities[tlt].Close - _previousTradeBar.Item2.Close / _previousTradeBar.Item2.Close;

            _dailyReturns.Add(new Tuple<decimal, decimal>(spyReturn, tltReturn));
        }

        public override void OnEndOfDay()
        {
            //Plot("Charts", "Price", Securities[symbol].Price);
            //Plot("Charts", "PP", _pp);
        }

        private decimal VolatilityScaledSharpeRatio(decimal spyAllocation, decimal tltAllocation)
        {
            var dailyReturns = _dailyReturns.Select(dailyReturn => spyAllocation*dailyReturn.Item1 + tltAllocation*dailyReturn.Item2);
            var mean = dailyReturns.Average();

            foreach (var dr in dailyReturns)
            {
                _sd.Update(DateTime.Now, dr);
            }

            var sharpe = mean / Convert.ToDecimal(Math.Pow(Convert.ToDouble(_sd), _volatilityFactor));

            return sharpe;
        }
    }
}