using QuantConnect.Data.Market;

namespace Strategies.VixIntradayStrategy
{
    public class TrailingStop
    {
        public decimal TrailingStopValue { get; set; }
        private decimal TrailingStopPercent { get; }
        private bool IsLong { get; }

        public TrailingStop(decimal stopValue, decimal stopPercent, bool isLongEntry)
        {
            TrailingStopValue = stopValue;
            TrailingStopPercent = stopPercent;
            IsLong = isLongEntry;
        }

        public bool IsTrailingExit(TradeBar b, out decimal exitPrice)
        {
            exitPrice = 0;

            if (IsLong)
            {
                if (b.Close / TrailingStopValue < 1 - TrailingStopPercent / 100)
                {
                    exitPrice = b.Close;
                    return true;
                }
            }
            else
            {
                if (b.Close / TrailingStopValue > 1 + TrailingStopPercent / 100)
                {
                    exitPrice = b.Close;
                    return true;
                }
            }

            // update Trailing stop if needed

            if (IsLong && b.Close > TrailingStopValue)
            {
                TrailingStopValue = b.Close;
            }
            else if (!IsLong && b.Close < TrailingStopValue)
            {
                TrailingStopValue = b.Close;
            }

            return false;
        }
    }
}