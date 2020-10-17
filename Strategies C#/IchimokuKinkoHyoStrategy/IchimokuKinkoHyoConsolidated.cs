using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using static Strategies.IchimokuKinkoHyoStrategy.IchimokuKinkoHyoSignal;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    public class IchimokuKinkoHyoConsolidated
    {
        public enum Direction
        {
            Bearish, Bullish
        }

        private TradeBar _previousData;
        private IchimokuKinkoHyo _previousIndicator;

        private TradeBar _data;
        private IchimokuKinkoHyo _indicator;

        public bool IsReady => IsConsolidated && _previousIndicator.IsReady && _indicator.IsReady;

        public bool IsInitialized => _previousIndicator != null;

        public bool IsConsolidated => _previousIndicator != null && _indicator != null;

        public Direction PriceDirection => _previousData.Price < _data.Price ? Direction.Bullish : Direction.Bearish;

        public IchimokuKinkoHyoConsolidated(TradeBar data, IchimokuKinkoHyo indicator)
        {
            _previousData = data;
            _previousIndicator = indicator;
        }

        public void Consolidate(TradeBar data, IchimokuKinkoHyo indicator)
        {
            _data = data;
            _indicator = indicator;
        }

        public void RollConsolidationWindow()
        {
            _previousData = _data;
            _previousIndicator = _indicator;
        }

        public SignalStrength BullishTenkanKijunCross()
        {
            return IchimokuKinkoHyoSignal.BullishTenkanKijunCross(_previousIndicator, _indicator);
        }

        public SignalStrength BearishTenkanKijunCross()
        {
            return IchimokuKinkoHyoSignal.BearishTenkanKijunCross(_previousIndicator, _indicator);
        }

        public SignalStrength BullishKijunCross()
        {
            return IchimokuKinkoHyoSignal.BullishKijunCross(_previousIndicator, _indicator, _previousData.Price, _data.Price);
        }

        public SignalStrength BearishKijunCross()
        {
            return IchimokuKinkoHyoSignal.BearishKijunCross(_previousIndicator, _indicator, _previousData.Price, _data.Price);
        }

        public SignalStrength BullishKumoBreakout()
        {
            return IchimokuKinkoHyoSignal.BullishKumoBreakout(_previousIndicator, _indicator, _previousData.Price, _data.Price);
        }

        public SignalStrength BearishKumoBreakout()
        {
            return IchimokuKinkoHyoSignal.BearishKumoBreakout(_previousIndicator, _indicator, _previousData.Price, _data.Price);
        }

        public SignalStrength BullishSenkouCross()
        {
            return IchimokuKinkoHyoSignal.BullishSenkouCross(_previousIndicator, _indicator, _data.Price);
        }

        public SignalStrength BearishSenkouCross()
        {
            return IchimokuKinkoHyoSignal.BearishSenkouCross(_previousIndicator, _indicator, _data.Price);
        }
    }
}