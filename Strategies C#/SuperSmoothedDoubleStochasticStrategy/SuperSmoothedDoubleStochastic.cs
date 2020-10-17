/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.SuperSmoothedDoubleStochasticStrategy
{
    public class SuperSmoothedDoubleStochastic : TradeBarIndicator
    {
        private readonly IndicatorBase<IndicatorDataPoint> _maximum;
        private readonly IndicatorBase<IndicatorDataPoint> _mininum;

        private readonly SimpleMovingAverage _firstSmoothing;

        private decimal _prevStochastic;
        private readonly Stochastic _stochastic;

        public IndicatorBase<TradeBar> Smoothed { get; private set; }

        public SuperSmoothedDoubleStochastic(string name, int period, int kPeriod, int dPeriod)
            : base(name)
        {
            _maximum = new Maximum(name + "_Max", period);
            _mininum = new Minimum(name + "_Min", period);
            _stochastic = new Stochastic(name + "_Stoch", period, kPeriod, dPeriod);
            _firstSmoothing = new SimpleMovingAverage(3);

            Smoothed = new FunctionalIndicator<TradeBar>(name + "_Smoothed",
                input => ComputeSmoothed(period, input),
                smoothed => _maximum.IsReady,
                () => _maximum.Reset()
                );
        }

        public SuperSmoothedDoubleStochastic(int period, int kPeriod, int dPeriod)
            : this("STO" + period, period, kPeriod, dPeriod)
        {
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _stochastic.Update(input);
            _maximum.Update(input.Time, _stochastic);
            _mininum.Update(input.Time, _stochastic);

            if (IsReady)
            {
                Smoothed.Update(input);
            }

            _prevStochastic = Smoothed;

            return Smoothed * 100;
        }

        private decimal ComputeSmoothed(int period, TradeBar input)
        {
            var denominator = _maximum - _mininum;
            var numerator = _stochastic - _mininum;
            decimal smoothed;
            if (denominator == 0m)
            {
                // if there's no range, just return constant zero
                smoothed = 0m;
            }
            else
            {
                smoothed = _maximum.Samples >= period ? numerator / denominator : new decimal(0.0);
            }

            _firstSmoothing.Update(input.Time, smoothed);

            if (_firstSmoothing.IsReady)
            {
                smoothed = 0.85m * _firstSmoothing + 0.15m * _prevStochastic;
            }

            return smoothed;
        }

        public override void Reset()
        {
            Smoothed.Reset();
            base.Reset();
        }

        public override bool IsReady => _stochastic.IsReady;
    }
}