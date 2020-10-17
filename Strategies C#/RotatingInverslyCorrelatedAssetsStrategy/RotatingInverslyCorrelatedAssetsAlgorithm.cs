using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Orders.Fees;
using Strategies.Data.Quandl;

namespace Strategies.RotatingInverslyCorrelatedAssetsStrategy
{
    public class RotatingInverslyCorrelatedAssetsAlgorithm : QCAlgorithm
    {
        private CorrelationPair _today;
        private readonly List<CorrelationPair> _prices = new List<CorrelationPair>();
        private const int MinimumCorrelationHistory = 11;

        // Codes from Quandl
        private const string Usdinr = "FRED/DEXINUS";
        private const string Nifty = "NSE/NIFTY_50";

        public override void Initialize()
        {
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            SetCash(10000);

            AddData<Quandl>(Nifty, Resolution.Daily);
            AddData<QuandlFx>(Usdinr, Resolution.Daily);

            Securities[Usdinr].FeeModel = new ConstantFeeModel(1.0m);
            Securities[Nifty].FeeModel = new ConstantFeeModel(1.0m);
        }

        public override void OnData(Slice data)
        {
            if (!data.ContainsKey(Usdinr) || !data.ContainsKey(Nifty)) return;

            _today = new CorrelationPair(data.Time);
            _today.Add(Usdinr, data[Usdinr].Price);

            try
            {
                _today.Add(Nifty, data[Nifty].Close);
                if (_today.Date == data.Time)
                {
                    Log("Date: " + data.Time + " Price: " + data[Nifty].Price);
                    _prices.Add(_today);

                    if (_prices.Count > MinimumCorrelationHistory)
                    {
                        _prices.RemoveAt(0);
                    }
                }

                if (_prices.Count < 2)
                {
                    return;
                }

                var maxAsset = string.Empty;
                var maxGain = double.MinValue;

                foreach (var symbol in _today.Prices.Keys)
                {
                    var last = (from pair in _prices select pair.Prices[symbol]).Last();
                    var first = (from pair in _prices select pair.Prices[symbol]).First();
                    var gain = (last - first) / first;

                    if (!(gain > maxGain)) continue;

                    maxAsset = symbol;
                    maxGain = gain;
                }

                if (!maxAsset.Equals(string.Empty))
                {
                    CustomSetHoldings(maxAsset, 1, true);
                }
            }
            catch (Exception ex)
            {
                Debug("Exception: " + ex.Message);
            }
        }

        public override void OnEndOfDay()
        {
            if (!_today.Prices.ContainsKey(Nifty)) return;

            if (_today.Prices[Nifty] != 0 && _today.Date.DayOfWeek == DayOfWeek.Wednesday)
            {
                Plot("NIFTY", _today.Prices[Nifty]);
            }
            if (_today.Prices[Usdinr] != 0 && _today.Date.DayOfWeek == DayOfWeek.Wednesday)
            {
                Plot("USDINR", _today.Prices[Usdinr]);
            }
        }

        public void CustomSetHoldings(string symbol, decimal percentage, bool liquidateExistingHoldings = false)
        {
            // TODO Use cash to determine position size?
            decimal cash = Portfolio.Cash;
            decimal currentHoldingQuantity = Portfolio[symbol].Quantity;

            //Range check values:
            if (percentage > 1)
            {
                percentage = 1;
            }
            else if (percentage < -1)
            {
                percentage = -1;
            }

            if (liquidateExistingHoldings)
            {
                foreach (var holdingSymbol in Portfolio.Keys.Where(holdingSymbol => holdingSymbol != symbol))
                {
                    //Go through all existing holdings, market order the inverse quantity
                    Order(holdingSymbol, -Portfolio[holdingSymbol].Quantity);
                }
            }

            //Now rebalance the symbol requested:
            var targetHoldingQuantity = Math.Floor(percentage * Portfolio.TotalPortfolioValue / Securities[symbol].Price);

            var netHoldingQuantity = targetHoldingQuantity - currentHoldingQuantity;
            if (Math.Abs(netHoldingQuantity) > 0)
            {
                Order(symbol, (int) netHoldingQuantity);
            }
        }
    }
}