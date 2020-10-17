using System;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using RDotNet;

namespace Trading
{
    public class ArimaGarchAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";

        private const int _windowSize = 500;

        private RollingWindow<double> _window = new RollingWindow<double>(_windowSize);

        private readonly REngine _engine = REngine.GetInstance();

        public override void Initialize()
        {
            SetCash(1000);
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            SetWarmup(500);
        }

        public void OnData(TradeBars data)
        {
            if (IsWarmingUp) return;

            var logData = _window.Select(closingPrice => Math.Log10(closingPrice));
            var diff = logData.Zip(logData.Skip(1), (x, y) => y - x);

            var returns = _engine.CreateNumericVector(diff);
            _engine.SetSymbol("roll.returns", returns);
            _engine.Evaluate(@"source('C:\Users\M\Documents\Visual Studio 2015\Projects\Trading\Trading\arima_garch.r')");
            var direction = _engine.GetSymbol("directions").AsInteger()[0];

            var holdings = Portfolio[Symbol].Quantity;

            if (holdings <= 0 && direction == 1)
            {
                Log("BUY  >> " + Securities[Symbol].Price);
                SetHoldings(Symbol, 1.0);
            }
            else if (holdings >= 0 && direction == -1)
            {
                Log("SELL  >> " + Securities[Symbol].Price);
                SetHoldings(Symbol, -1.0);
            }
        }
    }
}