using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;

namespace Strategies.MundoStrategy
{
    // See: http://www.talaikis.com/forex-index-portfolio-mundo-index-pseudo-beta-sample-trading-strategy/
    // See: https://www.quantconnect.com/forum/discussion/824/forex-pseudo-beta-index-mundo-strategy/p1
    // See: https://www.quantconnect.com/forum/discussion/1206/my-attempt-at-a-tweaked-version-of-the-mundo-strategy/p1
    public class MundoAlgorithm : QCAlgorithm
    {
        //List of Currencies you would like to trade and get the indexes for 
        private readonly string[] _symbols = { "EURUSD", "GBPUSD", "AUDUSD", "NZDUSD" };

        private readonly TradeBars _bars = new TradeBars();
        private readonly Dictionary<string, SymbolData> _symbolData = new Dictionary<string, SymbolData>();
        private readonly TimeSpan _barPeriod = TimeSpan.FromDays(1);

        //parameters
        private const int NormPeriod = 60;
        private const int Period = 20;
        private const int RollingWindowSize = 2;

        private decimal PortfolioSize => _symbols.Length;

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {
            SetStartDate(2007, 4, 1);
            SetEndDate(DateTime.Now);
            SetCash(3000);

            // initialize data for all the symbols
            foreach (var symbol in _symbols)
            {
                _symbolData.Add(symbol, new SymbolData(symbol, SecurityType.Forex, _barPeriod, RollingWindowSize));
            }

            //Set forex securities and Consolidate all the data
            foreach (var symbolData in _symbolData.Select(kvp => kvp.Value))
            {
                //AddSecurity(symbolData.SecurityType, symbolData.Symbol, Resolution.Hour);
                AddForex(symbolData.Symbol, Resolution.Hour, Market.Oanda);
                Securities[symbolData.Symbol].FeeModel = new ConstantFeeModel(0m);

                // define a consolidator to consolidate data for this symbol on the requested period
                var consolidator = new TradeBarConsolidator(_barPeriod);

                // define indicators
                symbolData.StdDev = new StandardDeviation(Period);
                symbolData.Min = new Minimum(NormPeriod);
                symbolData.Max = new Maximum(NormPeriod);

                //update indicators
                consolidator.DataConsolidated += (sender, bar) =>
                {
                    // 'bar' here is our newly consolidated data
                    symbolData.Min.Update(bar.Time, symbolData.Portfolio);
                    symbolData.Max.Update(bar.Time, symbolData.Portfolio);
                    symbolData.StdDev.Update(bar.Time, bar.Close);

                    // we're also going to add this bar to our rolling window so we have access to it later
                    symbolData.Bars.Add(bar);
                };

                // we need to add this consolidator so it gets auto updates
                SubscriptionManager.AddConsolidator(symbolData.Symbol, consolidator);
            }
        }

        public void OnData(TradeBars data)
        {
            UpdateBars(data);
            if (_bars.Count != _symbols.Length) return;

            decimal totalSd = 0;
            decimal beta = 0;

            // Calculate total SD
            foreach (var symbolData in _symbolData.Values)
            {
                if (!symbolData.StdDev.IsReady) return;

                totalSd += symbolData.StdDev;
                beta += symbolData.StdDev;
            }

            var prt = _symbolData.Values
                .Where(symbolData => beta != decimal.Zero)
                .Sum(symbolData => _bars[symbolData.Symbol].Close * symbolData.StdDev / (beta / 4m));

            foreach (var symbolData in _symbolData.Values)
            {
                symbolData.Portfolio = prt;

                // Do a basic 0-1 Min-Max Normalization to normalize all the values
                if (symbolData.Max - symbolData.Min != decimal.Zero)
                {
                    symbolData.Norm = (prt - symbolData.Min) / (symbolData.Max - symbolData.Min);
                }

                if (Portfolio[symbolData.Symbol].IsLong && symbolData.Norm > decimal.One)
                {
                    //Liquidate();
                    MarketOrder(symbolData.Symbol, -Portfolio[symbolData.Symbol].Quantity);
                }

                if (Portfolio[symbolData.Symbol].IsShort && symbolData.Norm < decimal.Zero)
                {
                    //Liquidate();
                    MarketOrder(symbolData.Symbol, -Portfolio[symbolData.Symbol].Quantity);
                }

                if (!Portfolio[symbolData.Symbol].Invested && symbolData.Norm > decimal.Zero && symbolData.Prenorm < decimal.Zero)
                {
                    if (beta / 4m != decimal.Zero && symbolData.StdDev != decimal.Zero)
                    {
                        //SetHoldings(symbolData.Symbol, 0.4m / (symbolData.StdDev / (beta / 4m)));
                        var target = 0.4m / (symbolData.StdDev / (beta / 4m));
                        MarketOrder(symbolData.Symbol, (int)(target * Portfolio.TotalPortfolioValue / data[symbolData.Symbol].Price));
                    }
                }

                if (!Portfolio[symbolData.Symbol].Invested && symbolData.Norm < decimal.One && symbolData.Prenorm > decimal.One)
                {
                    if (beta / 4m != decimal.Zero && symbolData.StdDev != decimal.Zero)
                    {
                        //SetHoldings(symbolData.Symbol, -0.6m / (symbolData.StdDev / (beta / 4m)));
                        var target = -0.6m / (symbolData.StdDev / (beta / 4m));
                        MarketOrder(symbolData.Symbol, (int)(target * Portfolio.TotalPortfolioValue / data[symbolData.Symbol].Price));
                    }
                }

                symbolData.Prenorm = symbolData.Norm; //Keep track of the previous normalized values
            }
        }

        private void UpdateBars(TradeBars data)
        {
            foreach (var bar in data.Values)
            {
                if (!_bars.ContainsKey(bar.Symbol))
                {
                    _bars.Add(bar.Symbol, bar);
                }

                _bars[bar.Symbol] = bar;
            }
        }
    }
}