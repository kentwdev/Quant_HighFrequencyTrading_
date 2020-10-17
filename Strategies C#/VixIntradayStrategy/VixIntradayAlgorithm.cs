using QuantConnect.Algorithm;
using System;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace Strategies.VixIntradayStrategy
{
    public class VixIntradayAlgorithm : QCAlgorithm
    {
        private const string Symbol = "VXX";
        private const int RollingWindowSize = 2;

        private readonly RollingWindow<TradeBar> _history = new RollingWindow<TradeBar>(RollingWindowSize);

        private TradeBar _firstBar;

        private TrailingStop _trlStop;

        private bool _enableEntry = true;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2009, 3, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Minute);

        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            try
            {
                TradeBar b;
                if (!data.TryGetValue(Symbol, out b) || b == null)
                {
                    return;
                }

                if (_history.IsReady)
                {
                    if (IsFirstTradingMin(b))
                    {
                        _firstBar = b;

                        // enable entry on the first bar if not invested yet
                        if (!Portfolio.Invested)
                        {
                            _enableEntry = true;
                        }
                    }

                    decimal price = 0;
                    if (IsExit(b, out price))
                    {
                        Liquidate(Symbol);
                        Log(">>Close>> " + b.Time + " " + Symbol + " @" + price);
                    }
                    else
                    {
                        bool isLong = true;
                        if (IsEntry(b, out price, out isLong))
                        {
                            int qnt = (int)(Portfolio.Cash / price);
                            if (isLong)
                            {
                                SetHoldings(Symbol, 1.0);
                            }
                            else
                            {
                                qnt = -qnt;
                                SetHoldings(Symbol, -1.0);
                            }

                            _trlStop = new TrailingStop(price, 2, isLong);
                            _enableEntry = false;

                            Log(">>BUY/sell>> " + b.Time + " " + qnt + " " + Symbol + " @" + price);
                        }
                    }
                }

                if (IsLastTradingMin(b))
                {
                    _history.Add(b);
                }
            }
            catch (Exception ex)
            {
                Error("OnData: " + ex.Message + "\r\n\r\n" + ex.StackTrace);
            }
        }

        private static bool IsLastTradingMin(IBaseData b)
        {
            return b.Time.Hour == 15 && b.Time.Minute == 59;
        }

        private static bool IsFirstTradingMin(IBaseData b)
        {
            return b.Time.Hour == 9 && b.Time.Minute == 31;
        }

        private bool IsEntry(TradeBar b, out decimal entryPrice, out bool isLong)
        {
            entryPrice = 0;
            isLong = true;
            var isEntry = false;

            // check for Long entry VXX
            if (!Portfolio.Invested
                && (b.Time.Date.DayOfWeek == DayOfWeek.Monday
                        // || b.Time.Date.DayOfWeek == DayOfWeek.Tuesday
                        || b.Time.Date.DayOfWeek == DayOfWeek.Wednesday
                        || b.Time.Date.DayOfWeek == DayOfWeek.Thursday
                        || b.Time.Date.DayOfWeek == DayOfWeek.Friday
                    )
                 && _enableEntry
                 && (b.Time.Hour == 9)
                 && _history[0].Close > _history[1].Close // vxx
                 && b.Close < _history[0].Close)  // vxx open below the Close
            {
                entryPrice = b.Close;
                isLong = true;
                isEntry = true;
            }

            // check for Sort entry VXX
            if (!Portfolio.Invested
                && _enableEntry
                && _history[0].Close < _history[1].Close
                && _firstBar.Close < _history[0].Close * 0.99m)
            {
                entryPrice = b.Close;
                isLong = false;
                isEntry = true;
            }

            return isEntry;
        }

        private bool IsExit(TradeBar b, out decimal exitPrice)
        {
            exitPrice = 0;
            bool rtn = false;
            if (Portfolio.Invested && Portfolio[Symbol] != null)
            {
                if ( // for Long exit
                     Portfolio[Symbol].IsLong && b.Time.Hour == 10 && b.Time.Minute == 45
                     ||
                     // for short exit
                     Portfolio[Symbol].IsShort
                     && _trlStop.IsTrailingExit(b, out exitPrice)
                    )
                {
                    rtn = true;
                    exitPrice = b.Close;
                }

            }
            return rtn;
        }

        private decimal GettPercentGain(TradeBar b, string ticker)
        {
            decimal rtn = 0;

            if (Portfolio[ticker] != null)
            {
                rtn = (Portfolio[ticker].Price - Portfolio[ticker].AveragePrice) / Portfolio[ticker].AveragePrice * 100;
                if (Portfolio[ticker].IsShort)
                {
                    rtn = -rtn;
                }
            }
            return rtn;
        }
    }
}