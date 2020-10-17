using System;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace Strategies.CointegrationStrategy
{
    public class CointegrationAlgorithm : QCAlgorithm
    {
        public string[] Symbols = { "EURUSD", "GBPUSD", "NZDUSD", "AUDUSD" };

        public override void Initialize()
        {
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            SetCash(10000);
        }

        public override void OnData(Slice data)
        {
            if (/* z > 2.1 */true)
            {
                
            }
            else if (/* z < -2.1 */false)
            {
                // Take short
            }
        }
    }
}