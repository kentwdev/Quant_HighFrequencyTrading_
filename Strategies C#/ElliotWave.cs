namespace QuantConnect.Indicators
{
    public class ElliotWave : Indicator
    {
        private SimpleMovingAverage _fastSma;
        private SimpleMovingAverage _slowSma;
        private SimpleMovingAverage _sma100;
        private SimpleMovingAverage _sma200;
        private SimpleMovingAverage _sma20;

        private decimal _d;
        private bool _upTrend;
        private bool _neutral;

        public int FastPeriod { get; set; }

        public int SlowPeriod { get; set; }

        public override bool IsReady => _sma200.IsReady;

        public ElliotWave(int fastPeriod = 5, int slowPeriod = 35) : this(string.Format("ElliotWave({0},{1})", fastPeriod, slowPeriod))
        {
        }

        public ElliotWave(string name, int fastPeriod = 5, int slowPeriod = 35) : base(name)
        {
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _fastSma.Update(input);
            _slowSma.Update(input);
            _sma200.Update(input);
            _sma100.Update(input);
            _sma20.Update(input);

            return 0;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _fastSma.Reset();
            _slowSma.Reset();
            _sma200.Reset();
            _sma100.Reset();
            _sma20.Reset();

            base.Reset();
        }
    }
}