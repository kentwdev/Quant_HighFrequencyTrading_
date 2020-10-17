using QuantConnect.Data.Market;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class OneShotTrigger : ISignal
    {
        private ISignal _signal;
        private SignalType _previousSignalType;
        public OneShotTrigger(ISignal signal)
        {
            _signal = signal;
            Signal = SignalType.NoSignal;
            _previousSignalType = SignalType.NoSignal;
        }

        public void Scan(TradeBar data)
        {
            _signal.Scan(data);

            Signal = _signal.Signal != _previousSignalType ? _signal.Signal : SignalType.NoSignal;
            _previousSignalType = _signal.Signal;
        }

        public SignalType Signal { get; private set; }
    }
}