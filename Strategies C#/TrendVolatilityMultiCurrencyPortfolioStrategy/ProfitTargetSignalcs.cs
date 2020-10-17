using QuantConnect.Data.Market;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class ProfitTargetSignalExit : IExitSignal
    {
        private readonly TradeProfile _tradeProfile;
        private readonly decimal _targetProfitLossRatio;

        public ProfitTargetSignalExit() { }

        public ProfitTargetSignalExit(TradeProfile tradeProfile, decimal targetProfitLossRatio)
        {
            _tradeProfile = tradeProfile;
            _targetProfitLossRatio = targetProfitLossRatio;
        }

        public void Scan(TradeBar data)
        {
            Signal = _tradeProfile.ProfitLossRatio > _targetProfitLossRatio ? SignalType.Exit : SignalType.NoSignal;
        }

        public SignalType Signal { get; private set; }

        public ISignal ExitSignalFactory(TradeProfile tradeProfile)
        {
            return new ProfitTargetSignalExit(tradeProfile, _targetProfitLossRatio);
        }
    }
}