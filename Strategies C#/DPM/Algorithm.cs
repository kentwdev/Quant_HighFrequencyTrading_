using System;
using System.Collections.Generic;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace Strategies.DPM
{
    public class Algorithm : QCAlgorithm
    {
        private List<Symbol> _securities = new List<Symbol>(new Symbol[] { "XLY", "XLF", "XLK", "XLE", "XLV", "XLI", "XLP", "XLB", "XLU" });
        private Symbol _rfAsset = "SHY";
        private decimal _baseWeight;
        private int _lookback = 252;

        private Dictionary<Symbol, DownsideProtectionModel> _downsideProtectionModels; 

        public override void Initialize()
        {
            SetStartDate(2010, 1, 1);
            SetEndDate(DateTime.Now.Date);
            SetCash(25000);

            _baseWeight = 1m / _securities.Count;
            _downsideProtectionModels = new Dictionary<Symbol, DownsideProtectionModel>();

            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);
            AddSecurity(SecurityType.Equity, _rfAsset, Resolution.Daily);

            foreach (var security in _securities)
            {
                AddSecurity(SecurityType.Equity, security, Resolution.Daily);
                var dpm = new DownsideProtectionModel(security.Value);
                RegisterIndicator(security, dpm, Resolution.Daily);
                _downsideProtectionModels[security] = dpm;
            }

            var history = History(TimeSpan.FromDays(252), Resolution.Daily);
            foreach (var tb in history)
            {
                foreach (var security in _securities)
                {
                    _downsideProtectionModels[security].Update(tb[security]);
                }
            }

            Schedule.On(DateRules.MonthStart(), TimeRules.At(12, 0), () =>
            {
                var rfAssetTarget = 0m;
                foreach (var dpm in _downsideProtectionModels)
                {
                    SetHoldings(dpm.Key, dpm.Value.Exposure * _baseWeight);
                    rfAssetTarget += dpm.Value.NonAssetExposure;
                }

                SetHoldings(_rfAsset, rfAssetTarget * _baseWeight);
            });
        }

        public void OnData(Slice data)
        {
        }
    }
}