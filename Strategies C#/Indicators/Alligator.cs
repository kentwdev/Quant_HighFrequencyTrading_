using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class AlligatorIndicator : TradeBarIndicator
    {
        private WindowIndicator<IndicatorDataPoint> _jaw;
        private WindowIndicator<IndicatorDataPoint> _teeth;
        private WindowIndicator<IndicatorDataPoint> _lips;

        private Delay _jawDelay;
        private Delay _teethDelay;
        private Delay _lipsDelay;

        public override bool IsReady => _jaw.IsReady && _teeth.IsReady && _lips.IsReady;

        public decimal Jaw => _jaw;
        public decimal Teeth => _teeth;
        public decimal Lips => _lips;

        public AlligatorIndicator(string name) : this(name, 13, 8, 5, 8, 5, 3, MovingAverageType.Simple)
        {
        }

        public AlligatorIndicator(string name, int jawPeriod, int teethPeriod, int lipsPeriod, int jawDelay, int teethDelay, int lipsDelay, MovingAverageType movingAverageType) : base(name)
        {
            switch (movingAverageType)
            {
                case MovingAverageType.Simple:
                    _jaw = new SimpleMovingAverage(name + "_JAW", jawPeriod);
                    _teeth = new SimpleMovingAverage(name + "_TEETH", teethPeriod);
                    _lips = new SimpleMovingAverage(name + "_LIPS", lipsPeriod);
                    break;
                case MovingAverageType.Exponential:
                    break;
            }

            _jawDelay = new Delay(name + "_JAW_DELAY", jawDelay);
            _teethDelay = new Delay(name + "_TEETH_DELAY", teethDelay);
            _lipsDelay = new Delay(name + "_LIPS_DELAY", lipsDelay);
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            var mean = (input.High + input.Low) / 2m;

            _jawDelay.Update(input.EndTime, mean);
            _teethDelay.Update(input.EndTime, mean);
            _lipsDelay.Update(input.EndTime, mean);

            if (!_lipsDelay.IsReady)
            {
                return 0m;
            }
            _lips.Update(input.EndTime, _lipsDelay);

            if (!_teethDelay.IsReady)
            {
                return 0m;
            }
            _teeth.Update(input.EndTime, _teethDelay);

            if (!_jawDelay.IsReady)
            {
                return 0m;
            }
            _jaw.Update(input.EndTime, _jawDelay);

            return 0m;
        }
    }
}