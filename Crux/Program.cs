using Crux.BasicStrategy;
using Crux.Okcoin;

namespace Crux
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LogExportFile = "log.txt";
            Log.LogLevel = 3;
            OKCMarketAPI okcAPI = null;
            okcAPI = new OKCMarketAPI("../../../Keys/okc.txt", "LTC/USD", true, true, new MarketAPIReadyCallback(() =>
            {
                MeanReversalStrategy meanReversalStrategy = new MeanReversalStrategy(okcAPI, 3600000, 24);
                meanReversalStrategy.Start(false);
            }));
            //MeanReversalStrategy meanReversalStrategy = new MeanReversalStrategy(okcAPI, 3600000, 24);
            //meanReversalStrategy.Start(false);
        }
    }
}
