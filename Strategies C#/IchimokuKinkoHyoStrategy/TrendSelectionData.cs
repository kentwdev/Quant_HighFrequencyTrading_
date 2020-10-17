using System;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    public class TrendSelectionData : IComparable
    {
        public enum Direction
        {
            Bullish, Bearish, None
        }

        private const decimal AdxLowerThreshold = 20.0m;
        private const decimal AdxUpperThreshold = 50.0m;
        private const int VolumeLookbackPeriod = 5;
        private const decimal Tolerance = 0.015m;

        private Symbol _symbol;

        private TradeBar _data;
        private decimal _holdings;

        public IchimokuKinkoHyo _ich;
        public AverageDirectionalIndex _adx;
        public VolumeWeightedAveragePriceIndicator _vwap;

        private IchimokuKinkoHyoConsolidated _consolidation;

        //private readonly RollingWindow<decimal> _volume = new RollingWindow<decimal>(250);

        public Direction TrendDirection { get; private set; }

        public bool IsReady => _ich.IsReady && _adx.IsReady && _vwap.IsReady && _consolidation.IsReady;

        public TrendSelectionData(Symbol symbol)
        {
            _symbol = symbol;

            _ich = new IchimokuKinkoHyo(symbol, 7, 22, 22, 44, 22, 22);

            _adx = new AverageDirectionalIndex(symbol, 22);

            _vwap = new VolumeWeightedAveragePriceIndicator(symbol, 22);

            _consolidation = new IchimokuKinkoHyoConsolidated(null, null);
        }

        public bool Update(TradeBar data, decimal holdings)
        {
            _data = data;
            _holdings = holdings;

            var isReady = _ich.Update(data) && _adx.Update(data) && _vwap.Update(data);

            _consolidation.RollConsolidationWindow();
            _consolidation.Consolidate(data, _ich);

            //_volume.Add(data.Volume);

            TrendDirection = IsBullishTrend()
                ? Direction.Bullish
                : IsBearishTrend() ? Direction.Bearish : Direction.None;

            return isReady;
        }

        public int CompareTo(object obj)
        {
            var otherTrendSelectionData = obj as TrendSelectionData;

            var isThisTrending = IsBullishTrend() || IsBearishTrend();

            var isOtherTrending = otherTrendSelectionData != null
                && (otherTrendSelectionData.IsBullishTrend()
                || otherTrendSelectionData.IsBearishTrend());

            return isThisTrending && isOtherTrending ? 0 : isThisTrending ? 1 : -1;
        }

        private bool IsBullishTrend()
        {
            if (_holdings > 0 || !IsReady)
            {
                return false;
            }

            var isBullishTrend = false;

            var tenkanKijunSignal = _consolidation.BullishTenkanKijunCross();
            var kijunSignal = _consolidation.BullishKijunCross();
            var kumoSignal = _data.Price > Math.Max(_ich.SenkouA, _ich.SenkouB);
            var senkouSignal = _consolidation.BullishSenkouCross();

            /*var averageVolume = _volume.Sum() / _volume.Count;
            var volumeSignal = _data.Volume > averageVolume
                || _volume.Skip(_volume.Count - VolumeLookbackPeriod).Any(av => av > averageVolume);*/

            var strongSignal = tenkanKijunSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong
                || kijunSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong
                || senkouSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong;

            var adxSignal = _adx > AdxLowerThreshold
                && _adx < AdxUpperThreshold
                && _adx.NegativeDirectionalIndex < _adx.PositiveDirectionalIndex * (1.0m - Tolerance);

            var vwapSignal = _data.Price < _vwap;

            if (strongSignal && kumoSignal && adxSignal && vwapSignal)
            {
                isBullishTrend = true;
            }

            return isBullishTrend;
        }

        private bool IsBearishTrend()
        {
            if (_holdings < 0 || !IsReady)
            {
                return false;
            }

            var isBearishTrend = false;

            var tenkanKijunSignal = _consolidation.BearishTenkanKijunCross();
            var kijunSignal = _consolidation.BearishKijunCross();
            var kumoSignal = _data.Price < Math.Min(_ich.SenkouA, _ich.SenkouB);
            var senkouSignal = _consolidation.BearishSenkouCross();

            /*var averageVolume = _volume.Sum() / _volume.Count;
            var volumeSignal = _data.Volume > averageVolume
                || _volume.Skip(_volume.Count - VolumeLookbackPeriod).Any(av => av > averageVolume);*/

            var strongSignal = tenkanKijunSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong
                || kijunSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong
                || senkouSignal == IchimokuKinkoHyoSignal.SignalStrength.Strong;

            var adxSignal = _adx > AdxLowerThreshold
                && _adx < AdxUpperThreshold 
                && _adx.NegativeDirectionalIndex > _adx.PositiveDirectionalIndex * (1.0m + Tolerance);

            var vwapSignal = _data.Price > _vwap;

            if (strongSignal && kumoSignal && adxSignal && vwapSignal)
            {
                isBearishTrend = true;
            }

            return isBearishTrend;
        }
    }
}