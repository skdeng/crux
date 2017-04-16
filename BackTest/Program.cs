using Crux;
using Crux.BasicStrategy;
using System;
using System.Linq;

namespace BackTest
{
    class Program
    {
        static void Main(string[] args)
        {
            HistoricalDataAPI historicalAPI = new HistoricalDataAPI("../../../data/polo_LTC_USD1hour.csv", 100, 1, 0.005);
            TradeStrategy meanReversal = new MeanReversalStrategy(historicalAPI, 0, 72);
            meanReversal.Start(false);

            double assetPL = historicalAPI.HistoricalPrices.Last() / historicalAPI.HistoricalPrices.First();
            Log.Write($"Sharpe ratio: {Benchmark.SharpeRatio(8760, meanReversal.PL, historicalAPI.HistoricalPrices)}", 1);
            Log.Write($"Cumulative PL: {meanReversal.CumulativePL}", 1);
            Log.Write($"Asset PL: {assetPL}", 1);

            Log.Write($"PL avg: {meanReversal.PL.Average()}", 2);
            Log.Write($"PL max: {meanReversal.PL.Max()} | PL min: {meanReversal.PL.Min()}", 2);
            Console.ReadKey();
        }
    }
}
