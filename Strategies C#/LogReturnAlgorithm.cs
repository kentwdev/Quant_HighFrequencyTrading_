using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies
{
    /*
    *   QuantConnect University: Full Basic Template:
    *
    *   The underlying QCAlgorithm class is full of helper methods which enable you to use QuantConnect.
    *   We have explained some of these here, but the full algorithm can be found at:
    *   https://github.com/QuantConnect/QCAlgorithm/blob/master/QuantConnect.Algorithm/QCAlgorithm.cs
    */
    public class LogReturnAlgorithm : QCAlgorithm
    {
        string symbol = "SPY";

        private LogReturn logr_14;
        private LogReturn logr_30;

        private LeastSquaresMovingAverage logr14;
        private LeastSquaresMovingAverage logr30;

        //RegisterIndicator(symbol, logr, null);

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {
            //Start and End Date range for the backtest:
            SetStartDate(2009, 3, 1);
            SetEndDate(DateTime.Now);

            //Cash allocation
            SetCash(25000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            AddSecurity(SecurityType.Equity, symbol, Resolution.Hour);

            logr_14 = LOGR(symbol, 14, Resolution.Hour);
            logr_30 = LOGR(symbol, 2, Resolution.Hour);

            logr14 = new LeastSquaresMovingAverage(14).Of(logr_14);
            logr30 = new LeastSquaresMovingAverage(30).Of(logr_30);
        }

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        // "TradeBars" object holds many "TradeBar" objects: it is a dictionary indexed by the symbol:
        // 
        //  e.g.  data["MSFT"] data["GOOG"]
        public void OnData(TradeBars data)
        {
            // update Indicators
            decimal price = data[symbol].Close;
            IndicatorDataPoint datum = new IndicatorDataPoint(data[symbol].Time, price);
            logr14.Update(datum);
            logr30.Update(datum);

            // wait for the indicators to fully initialize
            if (!logr30.IsReady) return;

            // stock is moving in an upwards trend
            if (logr30.Current.Value > logr_30.Current.Value * 1.0m)
            {
                if (!Portfolio.HoldStock)
                {
                    SetHoldings(symbol, 1m);
                }
            }
            // stock is moving in a downwards trend
            else if (logr_30.Current.Value > logr30.Current.Value * 1.0m)
            {
                Liquidate();
            }

            Plot(symbol, logr14);
            Plot(symbol, logr30);
        }
    }
}