using QuantConnect.Indicators;
using System;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    public static class IchimokuKinkoHyoSignal
    {
        public enum SignalStrength
        {
            None, Weak, Neutral, Strong
        }

        // Signals as defined in http://www.ichimokutrader.com/signals.html

        public static SignalStrength BullishTenkanKijunCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator)
        {
            var signalStrength = SignalStrength.None;

            // Tenkan Sen has not crossed Kijun Sen, no signal
            var cross = previousIndicator.Tenkan < previousIndicator.Kijun && indicator.Tenkan > indicator.Kijun;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            // Cross occured below the Kumo (cloud)
            if (indicator.Kijun < senkouLow)
            {
                signalStrength = SignalStrength.Weak;
            }
            // Cross occured inside the Kumo (cloud)
            else if (indicator.Kijun < senkouHigh && indicator.Kijun > senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            // Cross occured above the Kumo (cloud)
            else if (indicator.Kijun > senkouHigh)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        public static SignalStrength BearishTenkanKijunCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator)
        {
            var signalStrength = SignalStrength.None;

            var cross = previousIndicator.Tenkan > previousIndicator.Kijun && indicator.Tenkan < indicator.Kijun;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            if (indicator.Kijun > senkouHigh)
            {
                signalStrength = SignalStrength.Weak;
            }
            else if (indicator.Kijun <= senkouHigh && indicator.Kijun >= senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            else if (indicator.Kijun < senkouLow)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        public static SignalStrength BullishKijunCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal previousPrice,
            decimal price)
        {
            var signalStrength = SignalStrength.None;

            var cross = previousPrice < previousIndicator.Kijun && price > indicator.Kijun;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            if (indicator.Kijun < senkouLow)
            {
                signalStrength = SignalStrength.Weak;
            }
            else if (indicator.Kijun <= senkouHigh && indicator.Kijun >= senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            else if (indicator.Kijun > senkouHigh)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        public static SignalStrength BearishKijunCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal previousPrice,
            decimal price)
        {
            var signalStrength = SignalStrength.None;

            var cross = previousPrice > previousIndicator.Kijun && price < indicator.Kijun;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            if (indicator.Kijun > senkouHigh)
            {
                signalStrength = SignalStrength.Weak;
            }
            else if (indicator.Kijun <= senkouHigh && indicator.Kijun >= senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            else if (indicator.Kijun < senkouLow)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        public static SignalStrength BullishKumoBreakout(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal previousPrice,
            decimal price)
        {
            var previousSenkouHigh = Math.Max(previousIndicator.SenkouA, previousIndicator.SenkouB);
            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);

            var cross = previousPrice < previousSenkouHigh && price > senkouHigh;

            return cross ? SignalStrength.Neutral : SignalStrength.None;
        }

        public static SignalStrength BearishKumoBreakout(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal previousPrice,
            decimal price)
        {
                var previousSenkouLow = Math.Min(previousIndicator.SenkouA, previousIndicator.SenkouB);
                var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

                var cross = previousPrice > previousSenkouLow && price < senkouLow;

                return cross ? SignalStrength.Neutral : SignalStrength.None;
            }

        public static SignalStrength BullishSenkouCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal price)
        {
            var signalStrength = SignalStrength.None;

            var cross = previousIndicator.SenkouA < previousIndicator.SenkouB && indicator.SenkouA > indicator.SenkouB;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            if (price < senkouLow)
            {
                signalStrength = SignalStrength.Weak;
            }
            else if (price <= senkouHigh && price >= senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            else if (price > senkouHigh)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        public static SignalStrength BearishSenkouCross(
            IchimokuKinkoHyo previousIndicator,
            IchimokuKinkoHyo indicator,
            decimal price)
        {
            var signalStrength = SignalStrength.None;

            var cross = previousIndicator.SenkouA > previousIndicator.SenkouB && indicator.SenkouA < indicator.SenkouB;
            if (!cross)
            {
                return signalStrength;
            }

            var senkouHigh = Math.Max(indicator.SenkouA, indicator.SenkouB);
            var senkouLow = Math.Min(indicator.SenkouA, indicator.SenkouB);

            if (price > senkouHigh)
            {
                signalStrength = SignalStrength.Weak;
            }
            else if (price <= senkouHigh && price >= senkouLow)
            {
                signalStrength = SignalStrength.Neutral;
            }
            else if (price < senkouLow)
            {
                signalStrength = SignalStrength.Strong;
            }

            return signalStrength;
        }

        /*private SignalStrength BullishChikouCross(TradeBars data)
        {
            indicator.
        }

        private SignalStrength BearishChikouCross(TradeBars data)
        {

        }*/
    }
}