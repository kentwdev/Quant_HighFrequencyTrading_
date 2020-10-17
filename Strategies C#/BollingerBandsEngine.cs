using QuantConnect.Indicators;

namespace Strategies
{
    public class BollingerBandsEngine
    {
        public enum SignalType
        {
            Squeeze, BreakOut, BullishContinuation, BearishContinuation, BullishReversal, BearishReversal
        }

        public enum ZoneType
        {
            Buy, Sell, Indeterminate
        }

        public enum BollingerBandsDirection
        {
            Flat, Upward, Downward, Diverging, Converging
        }

        private BollingerBands _bb;

        public BollingerBandsEngine(BollingerBands bb)
        {
            _bb = bb;
        }

        public ZoneType CurrentZoneType(decimal price)
        {
            return price > _bb.UpperBand - (_bb.UpperBand - _bb.MiddleBand) / 2m
                ? ZoneType.Buy
                : price < _bb.LowerBand + (_bb.MiddleBand - _bb.LowerBand) / 2m ? ZoneType.Sell : ZoneType.Indeterminate;
        }

        public BollingerBandsDirection CurrentBollingerBandsDirection(decimal price)
        {
            return BollingerBandsDirection.Flat;
        }
    }
}