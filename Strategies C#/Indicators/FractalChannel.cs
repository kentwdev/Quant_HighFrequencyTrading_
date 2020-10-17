using Accord.Extensions;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class WilliamsFractals : TradeBarIndicator
    {
        private readonly RollingWindow<TradeBar> _fractal;
        private readonly int _fractalMidIndex;

        private decimal _barryUp;
        private decimal _barryDown;

        public decimal BarryUp => _barryUp;
        public decimal BarryDown => _barryDown;
        public decimal MidPoint => (_barryUp - _barryDown) / 2m;

        public override bool IsReady => _fractal.IsReady;

        public WilliamsFractals(int fractalLength = 5) : this("WilliamsFractals" + fractalLength, fractalLength)
        {
        }

        public WilliamsFractals(string name, int fractalLength = 5) : base(name)
        {
            _fractal = new RollingWindow<TradeBar>(fractalLength);
            _fractalMidIndex = fractalLength / 2 - (fractalLength % 2 == 0 ? 1 : 0);
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _fractal.Add(input);

            if (!_fractal.IsReady) return MidPoint;

            if (_fractal.IndexOfMax((bar, index) => bar.High) == _fractalMidIndex)
            {
                _barryUp = input.High;
            }

            if (_fractal.IndexOfMin((bar, index) => bar.Low) == _fractalMidIndex)
            {
                _barryDown = input.Low;
            }

            return MidPoint;
        }
    }
}