using System;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies
{
    public class VolatilityAdjustedMovingAverage : Indicator
    {
        private RollingWindow<TradeBar> _window = new RollingWindow<TradeBar>(75);
        private RollingWindow<TradeBar> _windowH = new RollingWindow<TradeBar>(2000);

        private RollingWindow<decimal> _mW = new RollingWindow<decimal>(45);
        private RollingWindow<decimal> _mHW = new RollingWindow<decimal>(1200);

        private RollingWindow<decimal> _vamaW = new RollingWindow<decimal>(40);
        private RollingWindow<decimal> _residW = new RollingWindow<decimal>(40);
        private RollingWindow<decimal> _vamaHW = new RollingWindow<decimal>(1200);
        private RollingWindow<decimal> _residHW = new RollingWindow<decimal>(1200);

        //indicators
        private SimpleMovingAverage _sma;
        private RelativeStrengthIndex _rsi;
        private StandardDeviation _sd;

        private decimal _vamaHour;
        private decimal _vamaDaily;
        private decimal _prevamaHour;
        private decimal _prevamaDaily;
        private decimal _m;
        private decimal _mL;
        private decimal _mHL;
        private decimal _residual;
        private decimal _residualH;
        private decimal _residualSm;
        private decimal _residualHSm;
        private decimal _prersi;

        //parameters
        private int _volSmooth = 4;
        private decimal _fast = 4;
        private decimal _slow = 4;

        private TickConsolidator _hourTradeBar;

        public decimal Current;

        public override bool IsReady { get; }

        public VolatilityAdjustedMovingAverage(string name) : base(name)
        {
        }

        public VolatilityAdjustedMovingAverage(string name, int period) : base(name)
        {
            _sma = new SimpleMovingAverage(400);
            _rsi = new RelativeStrengthIndex(6, MovingAverageType.Simple);
            _sd = new StandardDeviation(100);
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (input.Price > _vamaHour && input.Price > _vamaW[0] && _residualSm > 0 && _residualHSm > 0 && _rsi > 40 && _prersi < 40 && _sd > 0.005m)
            {
                Current = 1m;
            }
            else if (input.Price < _vamaHour && input.Price < _vamaW[0] && _residualSm < 0 && _residualHSm < 0 &&
                     _rsi < 60 && _prersi > 60 && _sd > 0.005m)
            {
                Current = -1m;
            }
            else
            {
                Current = 0m;
            }

            return Current;
        }

        public void OnDaily(object sender, TradeBar data)
        {
            _window.Add(data);
            if (!_window.IsReady) return;

            _m = Math.Max(Math.Max(_window[0].High - _window[1].Close, _window[0].High - _window[0].Low), Math.Max(_window[0].Low - _window[1].Close, 0));

            _mW.Add(_m);
            if (!_mW.IsReady) return;

            _mL = LSMA(_mW, _volSmooth);

            if (_prevamaDaily != 0)
            {
                _vamaDaily = _prevamaDaily + _mL * _slow * (_window[0].Close - _prevamaDaily);
            }
            else
            {
                _vamaDaily = _window[0].Close;
            }

            _vamaW.Add(_vamaDaily);
            if (!_vamaW.IsReady) return;

            _residual = _window[0].Close - LSMA(_vamaW, 40);

            _residW.Add(_residual);
            if (!_residW.IsReady) return;

            _residualSm = LSMA(_residW, 40);

            _prevamaDaily = _vamaDaily;
        }

        public void OnHour(object sender, TradeBar data)
        {
            if (!_rsi.IsReady) return;
            if (!_sd.IsReady) return;

            _windowH.Add(data);
            if (!_windowH.IsReady) return;

            var _mH = Math.Max(Math.Max(_windowH[0].High - _windowH[1].Close, _windowH[0].High - _windowH[0].Low), Math.Max(_windowH[0].Low - _windowH[1].Close, 0));

            _mHW.Add(_m);
            if (!_mHW.IsReady) return;

            _mHL = LSMA(_mW, _volSmooth);

            if (_prevamaHour != 0)
            {
                _vamaHour = _prevamaHour + _mL * _fast * (_windowH[0].Close - _prevamaHour);
            }
            else
            {
                _vamaHour = _windowH[0].Close;
            }

            _vamaHW.Add(_vamaHour);
            if (!_vamaHW.IsReady) return;

            _residualH = _windowH[0].Close - LSMA(_vamaHW, 40);

            _residHW.Add(_residualH);
            if (!_residHW.IsReady) return;

            _residualHSm = LSMA(_residHW, 40);

            _sma.Update(data.Time, _sd);
            if (!_sma.IsReady) return;

            _prevamaHour = _vamaHour;
            _prersi = _rsi;
        }

        private decimal LSMA(RollingWindow<decimal> dataW, int lsmaPeriod)
        {
            decimal delta = 0;
            for (int i = lsmaPeriod; i >= 1; i--)
            {
                delta += (i - (lsmaPeriod + 1) / 3.0m) * dataW[lsmaPeriod - i];
            }

            var lsma = delta * 6 / (lsmaPeriod * (lsmaPeriod + 1));
            return lsma;
        }
    }
}