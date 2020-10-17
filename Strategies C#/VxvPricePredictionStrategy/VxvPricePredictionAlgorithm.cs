using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Equity;

namespace Strategies.VxvPricePredictionStrategy
{
    public class VxvPricePredictionAlgorithm : QCAlgorithm
    {
        private string _vix = "CBOE/VIX";
        private string _vxv = "CBOE/VXV";

        private string _vxx = "VXX";
        private string _xiv = "XIV";
        private string _spy = "SPY";

        private Resolution _dataResolution = Resolution.Daily;

        private YangZhang _realizedVolatility;

        private List<Tuple<double[], int>> _trainingData;

        private KNeighborsRegressor _predictor;

        private int _lookAhead = 1;
        private int _trainingDataSize = 900;

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2016, 8, 8);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, _vxx, _dataResolution);
            AddSecurity(SecurityType.Equity, _xiv, _dataResolution);
            AddSecurity(SecurityType.Equity, _spy, _dataResolution);

            AddData<QuandlVixContract>(_vix, _dataResolution);
            AddData<Quandl>(_vxv, _dataResolution);

            Securities[_vix].Exchange = new EquityExchange();
            Securities[_vxv].Exchange = new EquityExchange();

            _realizedVolatility = new YangZhang(10);

            _trainingData = new List<Tuple<double[], int>>(_trainingDataSize + _lookAhead);
            var history = History(TimeSpan.FromDays(_trainingDataSize + 10 + _lookAhead));

            foreach (var slice in history.Take(10))
            {
                _realizedVolatility.Update(slice[_spy]);
            }

            foreach (var slice in history.Skip(10))
            {
                _realizedVolatility.Update(slice[_spy]);
                var features = new double[] { CalculateVixDelta(slice[_vix].Price, slice[_vxv].Price), CalculateVixPremium(slice[_vix].Price) };
                var outcome = slice[_vxx].Close < slice[_vxx].Open ? -1 : slice[_vxx].Close > slice[_vxx].Open ? 1 : 0;
                _trainingData.Add(new Tuple<double[], int>(features, outcome));
            }

            // Realign the training data so outcome is outcome of _lookAhead trading days in the future

            _predictor = new KNeighborsRegressor(
                80,
                3,
                _trainingData.Select(tuple => tuple.Item1).Take(_trainingData.Count - _lookAhead).ToArray(),
                _trainingData.Select(tuple => tuple.Item2).Skip(_lookAhead).ToArray()
            );

            Securities[_vxx].FeeModel = new ConstantFeeModel(0m);
            Securities[_xiv].FeeModel = new ConstantFeeModel(0m);
        }

        public override void OnData(Slice data)
        {
            _realizedVolatility.Update(data[_spy]);

            var prediction = _predictor?.Predict(new double[] { CalculateVixDelta(), CalculateVixPremium() });
            if (prediction > 0.0)
            {
                SetHoldings(_vxx, 5000m / Portfolio.TotalPortfolioValue);
            }
            else if (prediction < 0.0)
            {
                SetHoldings(_vxx, -1m);
            }
            else
            {
                //Liquidate();
            }
        }

        private double CalculateVixDelta()
        {
            return CalculateVixDelta(Securities[_vix].Price, Securities[_vxv].Price);
        }

        public double CalculateVixDelta(decimal vixPrice, decimal vxvPrice)
        {
            return (double)(vixPrice - vxvPrice);
        }

        // Yang-Zhang
        private double CalculateVixPremium()
        {
            return CalculateVixPremium(Securities[_vix].Price);
        }

        public double CalculateVixPremium(decimal vixPrice)
        {
            return (double)(vixPrice - _realizedVolatility);
        }

        public override void OnEndOfDay()
        {
            Plot("Charts", "Direction", Portfolio[_xiv].Invested ? 1 : Portfolio[_vxx].Invested ? -1 : 0);
            Plot("Charts", "Return", Portfolio.TotalProfit / 10000);
        }
    }
}