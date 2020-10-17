using QuantConnect.Data.Market;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    /// <summary>
    /// Base Signal Interface
    /// </summary>
    public interface ISignal
    {
        void Scan(TradeBar data);

        SignalType Signal { get; }
    }

    public interface IExitSignal : ISignal
    {
        ISignal ExitSignalFactory(TradeProfile tradeProfile);
    }

    public enum SignalType
    {
        Long = 1,
        Short = -1,
        Exit = 2,
        NoSignal = 0
    }
}