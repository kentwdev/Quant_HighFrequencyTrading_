using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class WFSignal : ISignal
    {
        private WilliamsFractals _wf;
        private SecurityHolding _securityHolding;

        private decimal _previousPrice;

        // TODO: Stop loss based on EMA
        // TODO: Adaptable parameters according to price
        public WFSignal(WilliamsFractals wf, SecurityHolding securityHolding)
        {
            _wf = wf;
            _securityHolding = securityHolding;
        }

        public void Scan(QuoteBar data)
        {
            var filter = !_securityHolding.Invested;

            if (data.Price >= _wf.BarryUp)
            {
                Signal = SignalType.Short;
            }
            else if (data.Price <= _wf.BarryDown)
            {
                Signal = SignalType.Long;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }

            _previousPrice = data.Price;
        }

        public SignalType Signal { get; private set; }
    }
}