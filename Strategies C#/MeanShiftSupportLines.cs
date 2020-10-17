using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Math.Distances;
using System.Linq;

namespace Strategies
{
    public class MeanShiftSupportLines : QCAlgorithm
    {
        public string symbol = "SPY";

        public double[][] result;

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2016, 7, 1);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, symbol, Resolution.Hour);

            var tradeBarHistory = History<TradeBar>(symbol, TimeSpan.FromDays(7), Resolution.Hour);

            // we can loop over the return value from these functions and we get TradeBars
            // we can use these TradeBars to initialize indicators or perform other math
            var closes = new double[][] { tradeBarHistory.Select((tb) => tb.Close).ToDoubleArray() };

            IRadiallySymmetricKernel kernel = new GaussianKernel(1);

            var meanShift = new MeanShift(kernel, 1)
            {
                //Tolerance = 0.05,
                //MaxIterations = 10
            };


            // Compute the mean-shift algorithm until the difference 
            // in shift vectors between two iterations is below 0.05

            int[] idx = meanShift.Learn(closes).Decide(closes);


            // Replace every pixel with its corresponding centroid
            result = closes.Apply((x, i) => meanShift.Clusters.Modes[idx[i]], result: closes);

            foreach (var rr in result)
            {
                foreach (var r in rr)
                {
                    Debug("" + r);
                }
            }
        }

        public override void OnData(Slice slice)
        {
            Plot("Charts", "Price", Securities[symbol].Close);

            foreach (var r in result)
            {
                Plot("Charts", "MS", r[0]);
            }
        }
    }
}