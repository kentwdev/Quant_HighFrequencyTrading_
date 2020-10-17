using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class VwapSignal : ISignal
    {
        private readonly VolumeWeightedAveragePriceIndicator _vwap;
        private readonly SecurityHolding _securityHolding;

        public VwapSignal(VolumeWeightedAveragePriceIndicator vwap, SecurityHolding securityHolding)
        {
            _vwap = vwap;
            _securityHolding = securityHolding;
        }

        public void Scan(TradeBar data)
        {
            _vwap.Update(data);

            if (_vwap > data.Price && !_securityHolding.Invested)
            {
                Signal = SignalType.Long;
            }
            else if (_vwap < data.Price && !_securityHolding.Invested)
            {
                Signal = SignalType.Short;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }
        }

        public SignalType Signal { get; private set; }
    }
}