using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    public class IchimokuKinkoHyoAlgorithm : QCAlgorithm
    {
        private const int NumberOfSymbols = 10;
        private const decimal FractionOfPortfolio = 1.0m;

        private readonly ConcurrentDictionary<Symbol, TrendSelectionData> _selectionDatas =
            new ConcurrentDictionary<Symbol, TrendSelectionData>();

        private static readonly List<string> RotatingSymbols = new List<string>
        {
            "SPY",
        };

        private static readonly List<string> HedgeSymbols = new List<string>
        {
            //"EDV",
            //"SPLV"
        };

        private readonly IEnumerable<string> _symbols = RotatingSymbols.Union(HedgeSymbols);

        private string _bench = "SPY";

        private Chart _chart;

        public override void Initialize()
        {
            SetCash(10000);
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);
            //SetBenchmark("SPY");

            SetWarmup(44);

            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Daily);
                _selectionDatas.AddOrUpdate(symbol, new TrendSelectionData(symbol));
            }

            Schedule.On(DateRules.On(new DateTime(2000, 7, 1), new DateTime(2007, 10, 1)), TimeRules.BeforeMarketClose("SPY"), () =>
            {
                Log("Sell >> " + Securities["SPY"].Price + " Symbol " + "SPY");
                SetHoldings("SPY", -FractionOfPortfolio);
            });

            _chart = new Chart("ICH", ChartType.Overlay);
            _chart.AddSeries(new Series("Price", SeriesType.Line));
            _chart.AddSeries(new Series("Tenkan", SeriesType.Line));
            _chart.AddSeries(new Series("Kijun", SeriesType.Line));
            _chart.AddSeries(new Series("SenkouA", SeriesType.Line));
            _chart.AddSeries(new Series("SenkouB", SeriesType.Line));
            _chart.AddSeries(new Series("ADX", SeriesType.Line));
            _chart.AddSeries(new Series("ADX.NegativeDirectionalIndex", SeriesType.Line));
            _chart.AddSeries(new Series("ADX.PositiveDirectionalIndex", SeriesType.Line));
        }

        public void OnData(TradeBars data)
        {
            foreach (var symbol in _symbols.Where(s => data.ContainsKey(s)))
            {
                _selectionDatas[symbol].Update(data[symbol], Portfolio[symbol].Quantity);
                Securities[symbol].IsTradable = true;
            }

            /*foreach (var symbol in _hedgeSymbols.Where(s => data.ContainsKey(s) && !Portfolio[s].Invested))
            {
                Log("BUY >> " + symbol);
                SetHoldings(symbol, FractionOfPortfolio);
            }*/

            if (IsWarmingUp)
            {
                return;
            }

            foreach (var symbol in _symbols.Where(s => Portfolio[s].Invested))
            {
                var bullishToBearish = Portfolio[symbol].IsLong
                    && _selectionDatas[symbol].TrendDirection == TrendSelectionData.Direction.Bearish;
                var bearishToBullish = Portfolio[symbol].IsShort
                    && _selectionDatas[symbol].TrendDirection == TrendSelectionData.Direction.Bullish;
                var takeProfit = Portfolio[symbol].UnrealizedProfitPercent >= 0.3m;
                var stopLoss = Portfolio[symbol].UnrealizedProfitPercent < -0.1m;
                if (bullishToBearish || bearishToBullish || takeProfit || stopLoss)
                {
                    Log("Flat >> " + Securities[symbol].Price + " Symbol " + symbol + " Profit: " + Portfolio[symbol].UnrealizedProfitPercent);
                    //Liquidate(symbol);
                    LimitOrder(symbol, -Portfolio[symbol].Quantity, data[symbol].Price);
                    Securities[symbol].IsTradable = false;
                }
            }

            var trending = _selectionDatas
                .Where(kvp => kvp.Value.IsReady && Securities[kvp.Key].IsTradable)
                .Select(kvp => kvp)
                .ToList();
            trending.Sort((a, b) => a.Value.CompareTo(b.Value));

            var topTrending = trending.Take(NumberOfSymbols - Portfolio.Count);

            foreach (var security in topTrending)
            {
                if (security.Value.TrendDirection == TrendSelectionData.Direction.Bullish)
                {
                    Log("Buy >> " + Securities[security.Key].Price + " Symbol " + security + " Profit: " + Portfolio[security.Key].UnrealizedProfitPercent);
                    //SetHoldings(security.Key, FractionOfPortfolio);
                    LimitOrder(security.Key, (int)(Portfolio.Cash / data[security.Key].Price), data[security.Key].Price);
                }
                else if (security.Value.TrendDirection == TrendSelectionData.Direction.Bearish)
                {
                    Log("Sell >> " + Securities[security.Key].Price + " Symbol " + security + " Profit: " + Portfolio[security.Key].UnrealizedProfitPercent);
                    //SetHoldings(security.Key, -FractionOfPortfolio);
                    LimitOrder(security.Key, -(int)(Portfolio.Cash / data[security.Key].Price), data[security.Key].Price);
                }
            }

            Plot("ICH", "Price", data[_bench].Price);
            Plot("ICH", "SenkouA", _selectionDatas[_bench]._ich.SenkouA);
            Plot("ICH", "SenkouB", _selectionDatas[_bench]._ich.SenkouB);

            Plot("ICH", "ADX", _selectionDatas[_bench]._adx);
            Plot("ICH", "ADX.NegativeDirectionalIndex", _selectionDatas[_bench]._adx.NegativeDirectionalIndex);
            Plot("ICH", "ADX.PositiveDirectionalIndex", _selectionDatas[_bench]._adx.PositiveDirectionalIndex);
        }
    }
}