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
            HistoricalDataAPI historicalAPI = new HistoricalDataAPI("../../../data/eth_hourly.csv", 100, 1);
            TradeStrategy meanReversal = new MeanReversalStrategy(historicalAPI, 0, 24);
            meanReversal.Start(false);
            Benchmark strategyBenchmark = new Benchmark();
            strategyBenchmark.PL = meanReversal.PL;
            strategyBenchmark.Prices = historicalAPI.HistoricalPrices;

            double cumulativePL = meanReversal.PortfolioValue / meanReversal.StartingPortfolioValue;
            double assetPL = historicalAPI.HistoricalPrices.Last() / historicalAPI.HistoricalPrices.First();
            Console.WriteLine($"Sharpe ratio: {strategyBenchmark.SharpeRatio(8760)}");
            Console.WriteLine($"Cumulative PL: {cumulativePL}");
            Console.WriteLine($"Asset PL: {assetPL}");
            Console.ReadKey();
        }
    }
}
