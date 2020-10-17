using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.PivotPointsStrategy
{
    public class PivotPoints : TradeBarIndicator
    {
        private TradeBar _previousTradeBar;

        public override bool IsReady => _previousTradeBar != null;

        public decimal PivotPoint;
        public decimal S1;
        public decimal S2;
        public decimal S3;
        public decimal R1;
        public decimal R2;
        public decimal R3;

        public PivotPoints(string name) : base(name)
        {
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (IsReady)
            {
                PivotPoint = (_previousTradeBar.High + _previousTradeBar.Low + _previousTradeBar.Close) / 3;
                S1 = PivotPoint * 2 - _previousTradeBar.High;
                S2 = PivotPoint - _previousTradeBar.High + _previousTradeBar.Low;
                S3 = S2 - _previousTradeBar.High + _previousTradeBar.Low;
                R1 = PivotPoint * 2 - _previousTradeBar.Low;
                R2 = PivotPoint + _previousTradeBar.High - _previousTradeBar.Low;
                R3 = R2 + _previousTradeBar.High - _previousTradeBar.Low;
            }

            _previousTradeBar = input;
            return PivotPoint;
        }
    }
}