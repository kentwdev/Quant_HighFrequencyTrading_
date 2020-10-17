using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class CCISignal : ISignal
    {
        private SecurityHolding _securityHolding;

        private CommodityChannelIndex _cci;
        private decimal previousCci;

        private bool above;
        private bool below;

        public CCISignal(CommodityChannelIndex cci, SecurityHolding securityHolding)
        {
            _cci = cci;
            _securityHolding = securityHolding;
        }

        public void Scan(QuoteBar data)
        {
            var filter = !_securityHolding.Invested;

            bool enterLongSignal, enterShortSignal, exitLongSignal, exitShortSignal;

            enterLongSignal = (filter && _cci > 0m && previousCci < 0m);

            enterShortSignal = (filter && _cci < 0m && previousCci > 0m);

            exitLongSignal = _cci < 150m && previousCci > 150m;
            exitShortSignal = _cci > -150m && previousCci < -150m;

            if (enterShortSignal)
            {
                Signal = SignalType.Short;
            }
            else if (enterLongSignal)
            {
                Signal = SignalType.Long;
            }
            else if ((exitLongSignal) && _securityHolding.IsLong)
            {
                // exit long due to bb switching
                Signal = SignalType.Exit;
            }
            else if ((exitShortSignal) && _securityHolding.IsShort)
            {
                // exit short due to bb switching
                Signal = SignalType.Exit;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }

            previousCci = _cci;
        }

        public SignalType Signal { get; private set; }
    }
}