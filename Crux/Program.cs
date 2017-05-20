using Crux.BasicStrategy;
using Crux.OkcoinFIX;
using System;

namespace Crux
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LogStream = new ConsoleLogStream();
            Log.LogLevel = 3;
            var okcAPI = new OKCMarketFIXAPI("../../../Keys/okc.txt", "LTC/USD", true, true);
            //Statistics stats = new Statistics();
            //ModifiedMR meanReversalStrategy = new ModifiedMR(okcAPI, TimeSpan.FromMinutes(15), TimePeriod.ONE_HOUR, 12, stats);
            //meanReversalStrategy.Start(false);
        }
    }
}
