using System;
using QuantConnect;
using QuantConnect.Orders;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class TradeProfile
    {

        //Ticket tracking the open order
        public OrderTicket OpenTicket, StopTicket, ExitTicket;
        //Keeps track of the current price and the direction of the trade
        public decimal CurrentPrice;
        public int TradeDirection => OpenTicket.Quantity > 0 ? 1 : OpenTicket.Quantity < 0 ? -1 : 0;
        public Symbol TradeSymbol;

        private decimal _risk;
        private int _maximumTradeQuantity;
        protected decimal _volatility;

        public int Quantity
        {
            get
            {
                if (_volatility == 0) return 0;
                long quantity = (long)(_risk / _volatility);
                //Check if the value for the maximum trade quantity is less than zero to avoid placing 0 value trade
                if (quantity > _maximumTradeQuantity)
                {
                    return _maximumTradeQuantity < 1000 ? 1000 : _maximumTradeQuantity;
                }

                return (int)quantity < 1000 ? 1000 : (int)quantity;
            }
        }

        public decimal DeltaStopLoss
        {
            get
            {
                if (Quantity == 0) return 0m;
                return _risk / Quantity;
            }
        }

        public decimal ProfitLossRatio
        {
            get
            {
                if (OpenTicket != null)
                {
                    return OpenTicket.Quantity * (CurrentPrice - OpenTicket.AverageFillPrice) / _risk;
                }
                return 0m;
            }
        }

        public void UpdateStopLoss(decimal latestPrice)
        {
            if ((latestPrice > CurrentPrice && TradeDirection > 0)
                || (latestPrice < CurrentPrice && TradeDirection < 0))
            {
                StopTicket.Update(
                    new UpdateOrderFields
                    {
                        StopPrice = StopTicket.Get(OrderField.StopPrice) + TradeDirection * Math.Abs(latestPrice - CurrentPrice)
                    });
            }
        }

        public ISignal ExitSignal
        {
            get;
            set;
        }

        public bool IsTradeFinished { get; set; }

        public TradeProfile(Symbol symbol, decimal volatility, decimal risk, decimal currentPrice, decimal maximumTradeSize)
        {
            TradeSymbol = symbol;
            _volatility = volatility;
            _risk = risk;
            CurrentPrice = currentPrice;
            _maximumTradeQuantity = (int)(maximumTradeSize / CurrentPrice);
        }
    }
}