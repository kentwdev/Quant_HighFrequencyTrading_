using System;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.MundoStrategy
{
    public class SymbolData
    {
        public readonly string Symbol;
        private readonly SecurityType _securityType;
        public readonly RollingWindow<TradeBar> Bars;
        private readonly TimeSpan _barPeriod;

        public StandardDeviation StdDev;
        public Minimum Min;
        public Maximum Max;

        public decimal Portfolio;
        public decimal Norm;
        public decimal Prenorm;

        public SymbolData(string symbol, SecurityType securityType, TimeSpan barPeriod, int windowSize)
        {
            Symbol = symbol;
            _securityType = securityType;
            _barPeriod = barPeriod;
            Bars = new RollingWindow<TradeBar>(windowSize);
        }

        public bool IsReady => Bars.IsReady
                               && StdDev.IsReady
                               && Min.IsReady
                               && Max.IsReady;

        //   public bool WasJustUpdated(DateTime current)
        //  {
        //     return Bars.Count > 0 && Bars[0].Time == current - BarPeriod;
        //}
    }
}
