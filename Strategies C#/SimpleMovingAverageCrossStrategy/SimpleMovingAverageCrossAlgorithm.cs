using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace Strategies.SimpleMovingAverageCrossStrategy
{
    public class SimpleMovingAverageCrossAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";

        private SimpleMovingAverage _fast;
        private SimpleMovingAverage _slow;
        private AverageTrueRange _atr;

        const decimal tolerance = 0.00025m;

        private Chart _chart;

        private OrderTicket _openStopMarketOrder;

        private readonly RollingWindow<decimal> _volume = new RollingWindow<decimal>(250);

        private readonly RollingWindow<decimal> _price = new RollingWindow<decimal>(1);

        public override void Initialize()
        {
            SetCash(1000);
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _fast = SMA(Symbol, 100, Resolution.Daily);
            _slow = SMA(Symbol, 400, Resolution.Daily);
            _atr = new AverageTrueRange(Symbol, 14, MovingAverageType.Simple);

            _chart = new Chart("SMA Cross", ChartType.Overlay);
            _chart.AddSeries(new Series("100", SeriesType.Line));
            _chart.AddSeries(new Series("400", SeriesType.Line));
            _chart.AddSeries(new Series("Price", SeriesType.Line));
        }

        public void OnData(TradeBars data)
        {
            if (!_slow.IsReady)
            {
                _volume.Add(Securities[Symbol].Volume);
                _price.Add(Securities[Symbol].Price);
                return;
            }

            var averageVolume = _volume.Sum() / _volume.Count;
            var volumeSignal = Securities[Symbol].Volume > averageVolume
                || _volume.Skip(_volume.Count - 7).Any(av => av > averageVolume);

            var holdings = Portfolio[Symbol].Quantity;

            if (_openStopMarketOrder != null && _openStopMarketOrder.Status != OrderStatus.Filled)
            {
                if (_openStopMarketOrder.Tag.Equals("long") && _price[0] < Securities[Symbol].Price)
                {
                    var newLongLimit = Securities[Symbol].Price * (1 - 0.05m);

                    _openStopMarketOrder.Update(new UpdateOrderFields
                    {
                        StopPrice = newLongLimit
                    });
                }
                else if (_openStopMarketOrder.Tag.Equals("short") && _price[0] > Securities[Symbol].Price)
                {
                    var newShortLimit = Securities[Symbol].Price * 1.05m;

                    _openStopMarketOrder.Update(new UpdateOrderFields
                    {
                        StopPrice = newShortLimit
                    });
                }
            }
            else if (holdings <= 0 && _fast > _slow * (1 + tolerance) && volumeSignal)
            {
                Log("BUY  >> " + Securities[Symbol].Price);
                //SetHoldings(Symbol, 1.0);

                var orderTicket = StopMarketOrder(Symbol, (int)(Portfolio.Cash / Securities[Symbol].Price), Securities[Symbol].Price * (1 - 0.05m), "long");

                _openStopMarketOrder?.Cancel();
                _openStopMarketOrder = orderTicket;
            }
            else if (holdings >= 0 && _fast < _slow * (1 - tolerance) && volumeSignal)
            {
                Log("SELL >> " + Securities[Symbol].Price);
                //SetHoldings(Symbol, -1.0);

                var shortAmount = Portfolio[Symbol].HoldingsValue == 0 ? Portfolio.Cash : Portfolio[Symbol].HoldingsValue;
                var orderTicket = StopMarketOrder(Symbol, -(int)(shortAmount / Securities[Symbol].Price), Securities[Symbol].Price * 1.05m, "short");

                _openStopMarketOrder?.Cancel();
                _openStopMarketOrder = orderTicket;
            }

            _volume.Add(Securities[Symbol].Volume);
            _price.Add(Securities[Symbol].Price);

            Plot("SMA Cross", "400", _slow.RollingSum / _slow.Period);
            Plot("SMA Cross", "100", _fast.RollingSum / _fast.Period);
            Plot("SMA Cross", "Price", Securities[Symbol].Price);
        }
    }
}