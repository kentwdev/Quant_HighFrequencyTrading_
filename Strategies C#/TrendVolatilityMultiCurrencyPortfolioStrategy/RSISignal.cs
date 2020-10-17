using System;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class RsiSignal : ISignal
    {
        private readonly RelativeStrengthIndex _rsi;
        private readonly SecurityHolding _securityHolding;

        private RollingWindow<decimal> _volume;

        private TickConsolidator _ticks;

        public RsiSignal(RelativeStrengthIndex rsi, SecurityHolding securityHolding)
        {
            _rsi = rsi;
            _securityHolding = securityHolding;
        }

        public void Scan(TradeBar data)
        {
            var isDecreasingVolume = _volume.Skip(27).Average() < _volume.Average();

            var tb = new TradeBar
            {
                EndTime = data.EndTime,
                Close = data.Close
            };

            if (_rsi > 70 && !_securityHolding.Invested)
            {
                Signal = SignalType.Short;
            }
            else if (_rsi < 30 && !_securityHolding.Invested)
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