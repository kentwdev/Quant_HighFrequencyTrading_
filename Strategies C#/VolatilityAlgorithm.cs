using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Extensions.Math;
using Accord.Math;
using Accord.Math.Optimization;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies
{
    public class VolatilityAlgorithm : QCAlgorithm
    {
        private string[] _stocks = {"SSO", "TMF", "IJR", "IJH", "XIV"};
        private string _vxx = "VXX";
        private string _xiv = "XIV";
        private string _spy = "SPY";
        private string _shortSpy = "SH";

        private string[] _equities = { "SSO", "TMF", "IJR", "IJH", "XIV", "VXX", "SPY", "SH" };

        private int _n = 0;
        private int[] _s;
        private int[] _x0;
        private double[] _x1;

        private Dictionary<Symbol, SymbolData> _symbolData;

        private BollingerBands _bb;
        private RollingWindow<decimal> _spyWvf; 

        private const int WvfLimit = 14;

        public override void Initialize()
        {
            SetStartDate(2015, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            foreach (var equity in _equities)
            {
                AddSecurity(SecurityType.Equity, equity, Resolution.Minute);
            }

            _s = new int[_stocks.Length];
            _x0 = new int[_stocks.Length];
            _x1 = Enumerable.Repeat(1d / _stocks.Length, _stocks.Length).ToArray();
            _symbolData = new Dictionary<Symbol, SymbolData>();

            _bb = new BollingerBands(_spy, 22, 1.05m, MovingAverageType.Simple);
            _spyWvf = new RollingWindow<decimal>(22);

            foreach (var stock in _stocks)
            {
                var closes = new RollingWindow<decimal>(17 * 390);
                var dailyCloses = new RollingWindow<TradeBar>(29);

                var dailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
                dailyConsolidator.DataConsolidated += (sender, consolidated) => _symbolData[consolidated.Symbol].UpdateDaily(consolidated);
                SubscriptionManager.AddConsolidator(stock, dailyConsolidator);

                _symbolData.Add(stock, new SymbolData(closes, dailyCloses));
            }

            var spyDailyCloses = new RollingWindow<TradeBar>(28);

            var spyDailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            spyDailyConsolidator.DataConsolidated += (sender, consolidated) => _symbolData[consolidated.Symbol].UpdateDaily(consolidated);
            SubscriptionManager.AddConsolidator(_spy, spyDailyConsolidator);
            _symbolData.Add(_spy, new SymbolData(null, spyDailyCloses));


            var vxxDailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            vxxDailyConsolidator.DataConsolidated += (sender, consolidated) => _symbolData[consolidated.Symbol].UpdateDaily(consolidated);
            SubscriptionManager.AddConsolidator(_vxx, vxxDailyConsolidator);

            _symbolData.Add(_spy, new SymbolData(null, spyDailyCloses));

            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketOpen("SPY", 15d), () =>
            {
                if (_symbolData[_spy].IsReady) return;

                var vxxCloses = _symbolData[_vxx].DailyHistory.Select(tb => tb.Close);
                var vxxLows = _symbolData[_vxx].DailyHistory.Select(tb => tb.Low);

                // William's VIX Fix indicator a.k.a. the Synthetic VIX
                var previousMax = vxxCloses.Take(28).Max();
                var previousWvf = 100 * (previousMax - vxxLows.Skip(27).First()) / previousMax;

                var max = vxxCloses.Skip(1).Max();
                var wvf = 100 * (max - vxxLows.Last()) / max;

                if (previousWvf < WvfLimit && wvf >= WvfLimit)
                {
                    SetHoldings(_vxx, 0);
                    SetHoldings(_xiv, 0.07);
                }
                else if (previousWvf > WvfLimit && wvf <= WvfLimit)
                {
                    SetHoldings(_vxx, 0.07);
                    SetHoldings(_xiv, 0);
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketOpen("SPY", 15d), () =>
            {
                if (_symbolData[_spy].IsReady) return;

                var spyCloses = _symbolData[_spy].NDaysDailyHistory(22).Select(tb => tb.Close);
                var spyLows = _symbolData[_spy].NDaysDailyHistory(22).Select(tb => tb.Low);
                var max = spyCloses.Max();
                var wvf = 100 * (max - spyLows.Last()) / max;
                _bb.Update(DateTime.Now, wvf);
                _spyWvf.Add(wvf);

                var rangeHigh = _spyWvf.Max() * 0.9m;

                var latestClose = _symbolData[_spy].NDaysDailyHistory(1).First().Close;
                var spy_higher_then_Xdays_back = latestClose > _symbolData[_spy].NDaysDailyHistory(3).First().Close;
                var spy_lower_then_longterm = latestClose > _symbolData[_spy].NDaysDailyHistory(40).First().Close;
                var spy_lower_then_midterm = latestClose > _symbolData[_spy].NDaysDailyHistory(14).First().Close;

                // Alerts Criteria
                var alert2 = !(_spyWvf[0] >= _bb.UpperBand && _spyWvf[0] >= rangeHigh) &&
                             (_spyWvf[1] >= _bb.UpperBand && _spyWvf[2] >= rangeHigh);

                if ((alert2 || spy_higher_then_Xdays_back) && (spy_lower_then_longterm || spy_lower_then_midterm))
                {
                    SetHoldings(_spy, 0.3);
                    SetHoldings(_shortSpy, 0);
                }
                else
                {
                    SetHoldings(_spy, 0);
                    SetHoldings(_shortSpy, 0.3);
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketOpen("SPY", 15d), () =>
            {
                if (_symbolData[_spy].IsReady) return;

                var returns = new List<double[]>(); // 28?

                foreach (var stock in _stocks)
                {
                    _symbolData[stock].UpdateWeights();
                    returns.Add(_symbolData[stock].PercentReturn.Select(pr => (double) (pr / _symbolData[stock].Norm)).ToArray());
                }

                var retNorm = _symbolData.Select(s => s.Value.Norm);
                var retNormMax = retNorm.Max();
                var epsFactor = retNormMax > 0 ? 0.9m : 1.0m;
                var eps = (double) (epsFactor * retNormMax);

                var constraints = new List<LinearConstraint>();

                constraints.Add(new LinearConstraint(Enumerable.Repeat(1.0, _stocks.Length).ToArray())
                {
                    ShouldBe = ConstraintType.EqualTo,
                    Value = 1
                });

                constraints.Add(new LinearConstraint(retNorm.Select(x => (double) x).ToArray())
                {
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = eps + eps * 0.01
                }); // Add and subtract small bounce to achieve inequality?

                constraints.Add(new LinearConstraint(retNorm.Select(x => (double) x).ToArray())
                {
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = eps - eps * 0.01
                }); // Add and subtract small bounce to achieve inequality?

                constraints.Add(new LinearConstraint(_stocks.Length)
                {
                    VariablesAtIndices = Enumerable.Range(0, _stocks.Length).ToArray(),
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = 0
                });

                constraints.Add(new LinearConstraint(_stocks.Length)
                {
                    VariablesAtIndices = Enumerable.Range(0, _stocks.Length).ToArray(),
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = 1
                });

                var initialGuess = new double[_stocks.Length];
                var f = new QuadraticObjectiveFunction(() => Variance(initialGuess, returns.ToMatrix()));

                var solver = new GoldfarbIdnani(f, constraints);
                solver.Minimize();

                var weights = _x1;
                var totalWeight = weights.Sum();

                if (solver.Status == GoldfarbIdnaniStatus.Success)
                {
                    weights = solver.Solution;
                }

                for (var i = 0; i < _stocks.Length; i++)
                {
                    SetHoldings(_stocks[i], weights[i] / totalWeight);
                }
            });
        }

        public override void OnData(Slice slice)
        {
            foreach (var stock in _stocks)
            {
                _symbolData[stock].Update(slice[stock].Close);
            }
        }

        public double Variance(double[] x, double[,] y)
        {
            var aCov = y.Covariance();

            return x.Dot(aCov.Dot(x));
        }

        public double JacVariance(double[] x, double[,] y)
        {
            var aCov = y.Transpose().Covariance().Flatten();

            return 2d * aCov.Dot(x);
        }

        public class SymbolData
        {
            private readonly RollingWindow<TradeBar> _dailyCloses;
            private readonly RollingWindow<decimal> _closes;
            public decimal Mean;
            public decimal StandardDeviation;

            public decimal Norm => Mean / StandardDeviation;

            public IEnumerable<TradeBar> DailyHistory => _dailyCloses.Reverse();

            public IEnumerable<TradeBar> NDaysDailyHistory(int n) => _dailyCloses.Take(n).Reverse();

            public IEnumerable<decimal> PercentReturn => PercentChange(_closes.Reverse().ToList());

            public bool IsReady => _closes.IsReady;

            public SymbolData(RollingWindow<decimal> closes, RollingWindow<TradeBar> dailyCloses)
            {
                _closes = closes;
                _dailyCloses = dailyCloses;
            }
            public void Update(decimal close)
            {
                _closes.Add(close);
            }

            public void UpdateDaily(TradeBar bar)
            {
                _dailyCloses.Add(bar);
            }

            public void UpdateWeights()
            {
                var percentChanges = PercentReturn.ToList();
                var mean = percentChanges.Average();

                var sumOfSquaresOfDifferences = percentChanges.Select(val => (val - mean) * (val - mean)).Sum();
                var sd = Sqrt(sumOfSquaresOfDifferences / percentChanges.Count);

                Mean = mean;
                StandardDeviation = sd;
            }

            public List<decimal> PercentChange(List<decimal> closes)
            {
                var percentChanges = new List<decimal>();

                for (int i = 1; i < closes.Count; i++)
                {
                    percentChanges.Add((closes[i] - closes[i - 1]) / closes[i - 1]);
                }

                return percentChanges;
            }

            public static decimal Sqrt(decimal x, decimal? guess = null)
            {
                var ourGuess = guess.GetValueOrDefault(x / 2m);
                var result = x / ourGuess;
                var average = (ourGuess + result) / 2m;

                return average == ourGuess ? average : Sqrt(x, average);
            }
        }
    }
}