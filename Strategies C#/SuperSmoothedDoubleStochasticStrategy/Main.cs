using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;

namespace Strategies.SuperSmoothedDoubleStochasticStrategy
{
    /*
    *   QuantConnect University: Full Basic Template:
    *
    *   The underlying QCAlgorithm class is full of helper methods which enable you to use QuantConnect.
    *   We have explained some of these here, but the full algorithm can be found at:
    *   https://github.com/QuantConnect/QCAlgorithm/blob/master/QuantConnect.Algorithm/QCAlgorithm.cs
    *   
    *   See: https://cssanalytics.wordpress.com/2009/09/11/calculation-dv-super-smoothed-double-stochastic-oscillator/
    */
    public class BasicTemplateAlgorithm : QCAlgorithm
    {
        private SuperSmoothedDoubleStochastic _sss;

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {

            //Start and End Date range for the backtest:
            SetStartDate(2013, 1, 1);
            SetEndDate(DateTime.Now);

            //Cash allocation
            SetCash(25000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);

            _sss = new SuperSmoothedDoubleStochastic("SPY", 10, 10, 30);
        }

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public void OnData(TradeBars data)
        {
            _sss.Update(data["SPY"]);

            if (!_sss.IsReady) return;

            if (_sss > 80)
            {
                SetHoldings("SPY", -1m);
            }
            else if (_sss < 20)
            {
                SetHoldings("SPY", 1m);
            }
            else if (Portfolio["SPY"].UnrealizedProfitPercent > 0.05m)
            {
                Liquidate("SPY");
            }
        }

        public override void OnEndOfDay()
        {
            Plot("Chart", "Price", Securities["SPY"].Price);
            Plot("Chart", "SSO", _sss);
        }
    }
}