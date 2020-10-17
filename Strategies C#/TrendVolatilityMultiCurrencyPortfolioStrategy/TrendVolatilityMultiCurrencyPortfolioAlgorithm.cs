using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NodaTime;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Statistics;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class TrendVolatilityMultiCurrencyPortfolioAlgorithm : QCAlgorithm, IRequiredOrderMethods
    {
        //Configure which securities you'd like to use:
        private readonly string[] _symbols = { "EURUSD", "GBPUSD", "NZDUSD", "AUDUSD" };

        //Risk in dollars per trade ($ or the quote currency of the assets)
        public decimal RiskPerTrade = 40;

        //Sets the profit to loss ratio we want to hit before we exit
        public decimal TargetProfitLossRatio = 0.1m;

        //Cap the investment maximum size ($).
        public decimal MaximumTradeSize = 10000;

        private Resolution _dataResolution = Resolution.Minute;
        private Dictionary<Symbol, TradingAsset> _tradingAssets
            = new Dictionary<Symbol, TradingAsset>();

        //List to store the RSI value for each asset
        private Dictionary<string, VolumeWeightedAveragePriceIndicator> _vwaps
            = new Dictionary<string, VolumeWeightedAveragePriceIndicator>();

        public override void Initialize()
        {
            SetStartDate(2007, 4, 1);
            SetEndDate(DateTime.Now);
            SetCash(2000);

            SetBenchmark(SecurityType.Forex, "EURUSD");

            //Add as many securities as you like. All the data will be passed into the event handler:
            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Forex, symbol, _dataResolution);
                //Securities[symbol].FeeModel = new ConstantFeeModel(0.04m);
                Securities[symbol].FeeModel = new FxcmFeeModel();
                Securities[symbol].SlippageModel = new SpreadSlippageModel();
                Securities[symbol].TransactionModel = new FxcmTransactionModel();
                //Securities[symbol].SetLeverage(50.0m);
                SetBrokerageModel(BrokerageName.FxcmBrokerage);

                Schedule.On(DateRules.Every(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday),
                    TimeRules.AfterMarketOpen(symbol, -60d), () =>
                    {
                        Securities[symbol].IsTradable = false;
                        // make untradeable
                    });

                Schedule.On(DateRules.Every(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday),
                    TimeRules.AfterMarketOpen(symbol, 120d), () =>
                {
                    // make tradeable
                });

                Schedule.On(DateRules.Every(DayOfWeek.Friday), TimeRules.BeforeMarketClose(symbol, -60d), () =>
                {
                    Liquidate();
                    // make untradeable
                });

                _vwaps.Add(symbol, VWAP(symbol, 30));

                var rsi = RSI(symbol, 30, MovingAverageType.Exponential, _dataResolution);

                var momersion = MOMERSION(symbol, 10, 30, _dataResolution);

                var williams = WILR(symbol, 60, _dataResolution);

                var ind = SMA(symbol, 10, _dataResolution, x => rsi);

                var adx = ADX(symbol, 30, _dataResolution);

                var psar = PSAR(symbol, 0.02M, 0.02M, 0.2M, _dataResolution);

                var fisher = new FisherTransform(symbol, 30);

                var stoch1 = STO(symbol, 30, 30, 60);
                var stoch2 = new Stochastic(symbol, 30, 30, 60);

                var rocp = ROCP(symbol, 30, _dataResolution);

                var tradeBarHistory = History(symbol, TimeSpan.FromDays(2), _dataResolution);

                foreach (var tradeBar in tradeBarHistory)
                {
                    _vwaps[symbol].Update(tradeBar);
                }

                Securities[symbol].VolatilityModel = new ThreeSigmaVolatilityModel(STD(symbol, 390, _dataResolution));
                _tradingAssets.Add(symbol,
                    new TradingAsset(Securities[symbol],
                        new OneShotTrigger(new VwapSignal(_vwaps[symbol], Portfolio[symbol])),
                        new ProfitTargetSignalExit(null, TargetProfitLossRatio),
                        RiskPerTrade,
                        MaximumTradeSize,
                        this
                    ));
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        public void OnData(TradeBars data)
        {
            foreach (var symbol in _symbols.Where(s => data.ContainsKey(s)))
            {
                //Create a trading asset package for each symbol
                _vwaps[symbol].Update(data[symbol]);
                //_tradingAssets[symbol].Scan(data[symbol]);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            double scale = Math.Pow(10, (int)Math.Log10(orderEvent.FillQuantity));
            orderEvent.FillQuantity = (int)(Math.Ceiling(orderEvent.FillQuantity / scale) * scale);

            var rollOverTime = DateTime.Today.AddHours(17.0).ToUniversalTime();

            if (orderEvent.UtcTime > rollOverTime.AddMinutes(-15)
                && orderEvent.UtcTime < rollOverTime.AddMinutes(15))
            {
                if (!Portfolio[orderEvent.Symbol].Invested)
                {
                    orderEvent.Status = OrderStatus.Canceled;
                }

                Liquidate(orderEvent.Symbol);
            }

            base.OnOrderEvent(orderEvent);
        }

        /*public override void OnEndOfDay()
        {
            foreach (var symbol in Symbols)
            {
                Plot("Charts", "Holdings", Portfolio[symbol].Quantity);
                Plot("Charts", "RSI", _vwap);
            }
        }*/
    }

    /// <summary>
    /// Interface for the two types of orders required to make the trade
    /// </summary>
    public interface IRequiredOrderMethods
    {
        OrderTicket StopMarketOrder(Symbol symbol, int quantity, decimal stopPrice, string tag = "");
        OrderTicket MarketOrder(Symbol symbol, int quantity, bool asynchronous = false, string tag = "");
        OrderTicket StopLimitOrder(Symbol symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "");
        OrderTicket LimitOrder(Symbol symbol, int quantity, decimal limitPrice, string tag = "");
    }
}