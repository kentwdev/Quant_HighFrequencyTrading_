namespace QuantConnect.Indicators
{
    public class InstantTrend : IndicatorBase<IndicatorDataPoint>
    {
        public RollingWindow<IndicatorDataPoint> Price { get; private set; }

        public RollingWindow<IndicatorDataPoint> Trend { get; private set; }

        private readonly decimal a = .05m;

        public InstantTrend(string name, int period)
            : base(name)
        {
            Price = new RollingWindow<IndicatorDataPoint>(period * 2);
            Trend = new RollingWindow<IndicatorDataPoint>(period);
        }

        public InstantTrend(int period)
            : this("InstantTrend" + period, period)
        {
        }

        public override bool IsReady
        {
            get { return Trend.IsReady; }
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            Price.Add(input);

            if (Price.Samples > 2)
            {
                // From Ehlers page 16 equation 2.9
                var it = (a - ((a / 2) * (a / 2))) * Price[0].Value + ((a * a) / 2) * Price[1].Value
                         - (a - (3 * (a * a) / 4)) * Price[2].Value + 2 * (1 - a) * Trend[0].Value
                         - ((1 - a) * (1 - a)) * Trend[1].Value;
                Trend.Add(new IndicatorDataPoint(input.Time, it));
            }
            else
            {
                Trend.Add(new IndicatorDataPoint(input.Time, Price[0].Value));
            }

            return Trend[0].Value;
        }

        public override void Reset()
        {
            Price.Reset();
            Trend.Reset();
            base.Reset();
        }
    }
}