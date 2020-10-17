using System;
using System.Globalization;
using QuantConnect;
using QuantConnect.Data;

namespace Strategies.RotatingInverslyCorrelatedAssetsStrategy
{
    public class USDINR : BaseData
    {
        public decimal Open;
        public decimal High;
        public decimal Low;
        public decimal Close;
        public decimal Volume;
        public decimal AdjustedClose;

        public USDINR()
        {
            Symbol = "USDINR";
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var url = "https://in.finance.yahoo.com/q/hp?s=^NSEI&a=00&b=1&c=1998&d=06&e=4&f=2016&g=d";
            return new SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New Nifty object
            USDINR bar = new USDINR();

            try
            {
                var data = line.Split(',');
                //Required.
                bar.Symbol = config.Symbol;
                bar.Time = DateTime.ParseExact(data[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);

                //User configured / optional data on each bar:
                bar.Open = Convert.ToDecimal(data[1]);
                bar.High = Convert.ToDecimal(data[2]);
                bar.Low = Convert.ToDecimal(data[3]);
                bar.Close = Convert.ToDecimal(data[4]);
                bar.Volume = Convert.ToDecimal(data[5]);
                bar.AdjustedClose = Convert.ToDecimal(data[6]);

                //This is the value the engine uses for portfolio calculations
                bar.Value = bar.AdjustedClose;
            }
            catch
            {
            }

            return bar;
        }
    }
}