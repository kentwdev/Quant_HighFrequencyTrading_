using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class FuzzySignal : ISignal
    {
        private RelativeStrengthIndex _rsi;
        private SecurityHolding _securityHolding;

        private FuzzyEngine _engine;

        public FuzzySignal(RelativeStrengthIndex rsi, SecurityHolding securityHolding)
        {
            _rsi = rsi;
            _securityHolding = securityHolding;

            _engine = new FuzzyEngine();
        }

        public void Scan(QuoteBar data)
        {
            var filter = !_securityHolding.Invested;
            double signal = _engine.DoInference((float)_rsi.Current.Value);

            if (signal > 20 && filter)
            {
                Signal = SignalType.Short;
            }
            else if (signal < -20 && filter)
            {
                Signal = SignalType.Long;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }
        }

        public SignalType Signal { get; private set; }
    }
}