using Crux;
using Crux.BasicStrategy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackTest
{
    class Program
    {
        static void Main(string[] args)
        {
            HistoricalDataAPI historicalAPI = new HistoricalDataAPI("../../../data/polo_LTC_USD1hour.csv", 100, 1, 0.005);
            Statistics stats = new Statistics();
            TradeStrategy meanReversal = new MeanReversalStrategy(historicalAPI, TimeSpan.FromSeconds(0), TimePeriod.ONE_HOUR, 72, stats);
            meanReversal.Start(false);

            double assetPL = historicalAPI.HistoricalPrices.Last() / historicalAPI.HistoricalPrices.First();
            List<double> benchmarkPL = new List<double>();
            benchmarkPL.Add(0);
            for (int i = 1; i < historicalAPI.HistoricalPrices.Count; i++)
            {
                benchmarkPL.Add(historicalAPI.HistoricalPrices[i] / historicalAPI.HistoricalPrices[i - 1] - 1);
            }
            Log.Write($"Sharpe ratio: {meanReversal.StrategyStatistics.SharpeRatio()}", 1);
            Log.Write($"Cumulative PL: {meanReversal.StrategyStatistics.CumulativePL()}", 1);
            Log.Write($"Asset PL: {assetPL}", 1);

            Console.ReadKey();
        }
    }
}
