using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class TradingAsset
    {
        public IExitSignal ExitSignal;
        public ISignal EnterSignal;

        private decimal _risk;
        private Symbol _symbol;
        private Security _security;
        private decimal _maximumTradeSize;
        private List<TradeProfile> _tradeProfiles;
        private IRequiredOrderMethods _orderMethods;
        // private decimal _targetProfitLossRatio;

        /// <summary>
        /// Initializes each Trading Asset
        /// </summary>
        /// <param name="security"></param>
        /// <param name="enterSignal"></param>
        /// <param name="exitSignal"></param>
        /// <param name="risk"></param>
        /// <param name="maximumTradeSize"></param>
        /// <param name="orderMethods"></param>
        public TradingAsset(Security security, ISignal enterSignal, IExitSignal exitSignal, decimal risk, decimal maximumTradeSize, IRequiredOrderMethods orderMethods)
        {
            _security = security;
            _symbol = _security.Symbol;
            EnterSignal = enterSignal;
            ExitSignal = exitSignal;
            _risk = risk;
            _maximumTradeSize = maximumTradeSize;
            _orderMethods = orderMethods;
            _tradeProfiles = new List<TradeProfile>();
        }

        /// <summary>
        /// Scan
        /// </summary>
        /// <param name="data"></param>
        public void Scan(TradeBar data, Security symbol)
        {
            foreach (var tradeProfile in _tradeProfiles)
            {
                tradeProfile.UpdateStopLoss(data.Close);
                tradeProfile.CurrentPrice = data.Close;
            }
            MarkStopTicketsFilled();
            EnterTradeSignal(data, symbol);
            ExitTradeSignal(data);
            RemoveAllFinishedTrades();
        }

        /// <summary>
        /// Executes all the logic when the Enter Signal is triggered
        /// </summary>
        /// <param name="data"></param>
        public void EnterTradeSignal(TradeBar data, Security symbol)
        {
            EnterSignal.Scan(data);
            if (symbol.IsTradable && EnterSignal.Signal == SignalType.Long || EnterSignal.Signal == SignalType.Short)
            {
                //Creates a new trade profile once it enters a trade
                var profile = new TradeProfile(_symbol, _security.VolatilityModel.Volatility, _risk, data.Close, _maximumTradeSize);

                profile.ExitSignal = ExitSignal.ExitSignalFactory(profile);

                var profileQuantity = profile.Quantity;
                if (profileQuantity > 0)
                {
                    profile.OpenTicket = _orderMethods.MarketOrder(_symbol, (int)EnterSignal.Signal * profile.Quantity);
                    profile.StopTicket = _orderMethods.StopMarketOrder(_symbol, -(int)EnterSignal.Signal * profile.Quantity,
                        profile.OpenTicket.AverageFillPrice - (int)EnterSignal.Signal * profile.DeltaStopLoss);

                    _tradeProfiles.Add(profile);
                }
            }
        }

        /// <summary>
        /// Executes all the logic when the Exit Signal is triggered
        /// </summary>
        /// <param name="data"></param>
        public void ExitTradeSignal(TradeBar data)
        {
            foreach (var tradeProfile in _tradeProfiles.Where(x => x.IsTradeFinished == false))
            {
                tradeProfile.ExitSignal.Scan(data);

                if (tradeProfile.ExitSignal.Signal == SignalType.Exit)
                {
                    if (tradeProfile.StopTicket.Status != OrderStatus.Filled)
                    {
                        tradeProfile.ExitTicket = _orderMethods.MarketOrder(_symbol, -(int)tradeProfile.OpenTicket.QuantityFilled);
                        tradeProfile.StopTicket.Cancel();

                        tradeProfile.IsTradeFinished = true;
                    }
                }
            }
        }

        /// <summary>
        /// Marks all the trades as finished which are completed due to hitting the stop loss
        /// </summary>
        public void MarkStopTicketsFilled()
        {
            foreach (var tradeProfile in _tradeProfiles.Where(tradeProfile => tradeProfile.StopTicket.Status == OrderStatus.Filled))
            {
                tradeProfile.IsTradeFinished = true;
            }
        }

        /// <summary>
        /// Removes all the completed trades from the trade profile list
        /// </summary>
        public void RemoveAllFinishedTrades()
        {
            _tradeProfiles = _tradeProfiles.Where(x => !x.IsTradeFinished).ToList();
        }
    }
}