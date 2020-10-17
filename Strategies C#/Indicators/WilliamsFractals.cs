using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class WilliamsFractals : TradeBarIndicator
    {
        private RollingWindow<TradeBar> _previousBars = new RollingWindow<TradeBar>(5);

        private decimal _barryUp;
        private decimal _barryDown;

        public decimal BarryUp => _barryUp;
        public decimal BarryDown => _barryDown;
        public decimal MidPoint => (_barryUp - _barryDown) / 2m;

        public override bool IsReady => _previousBars.IsReady;

        public WilliamsFractals() : base("WilliamsFractals")
        {
        }

        public WilliamsFractals(string name) : base(name)
        {
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _previousBars.Add(input);

            if (_previousBars.IsReady)
            {
                if (_previousBars[2].High > _previousBars[0].High
                    && _previousBars[2].High > _previousBars[1].High
                    && _previousBars[2].High > _previousBars[3].High
                    && _previousBars[2].High > _previousBars[4].High)
                {
                    _barryUp = input.High;
                }

                if (_previousBars[2].Low > _previousBars[0].Low
                    && _previousBars[2].Low > _previousBars[1].Low
                    && _previousBars[2].Low > _previousBars[3].Low
                    && _previousBars[2].Low > _previousBars[4].Low)
                {
                    _barryDown = input.Low;
                }
            }

            return MidPoint;
        }
    }
}