using Crux.BasicStrategy;
using Crux.OkcoinREST;
using System;

namespace Crux
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Log.LogExportFile = $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt";
                Log.LogLevel = 2;
                var okcAPI = new OKCMarketRESTAPI("../../../Keys/okc.txt", "LTC/USD");
                Statistics stats = new Statistics();
                MeanReversalStrategy meanReversalStrategy = new MeanReversalStrategy(okcAPI, TimeSpan.FromMinutes(15), TimePeriod.ONE_HOUR, 12, stats);
                meanReversalStrategy.Start(false);
            }
        }
    }
}
