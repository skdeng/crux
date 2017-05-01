using System;
using System.Linq;
using System.Threading;

namespace Crux
{
    public abstract class TradeStrategy
    {
        public Statistics StrategyStatistics { get; set; }

        public TimeSpan TimeUntilNextTrade { get; protected set; }

        public double PortfolioValue { get { return MarketTerminal.GetBalanceFiat() + MarketTerminal.GetBalanceSecurity() * MarketTerminal.GetLastPrice(); } }

        protected MarketAPI MarketTerminal;

        protected bool Trading;

        private Thread TradeThread;

        public TradeStrategy(MarketAPI api, Statistics stats)
        {
            MarketTerminal = api;
            StrategyStatistics = stats;
        }

        public void Start(bool async)
        {
            Trading = true;
            if (async)
            {
                if (TradeThread == null)
                {
                    TradeThread = new Thread(new ThreadStart(_Trade));
                    TradeThread.Priority = ThreadPriority.Highest;
                }
                TradeThread.Start();
            }
            else
            {
                _Trade();
            }
        }

        public void Stop()
        {
            Trading = false;
            if (TradeThread != null)
            {
                TradeThread.Join();
            }
        }

        private void _Trade()
        {
            while (Trading)
            {
                Trade();
                StrategyStatistics.Snapshot(MarketTerminal.GetBalanceFiat(), MarketTerminal.GetBalanceSecurity(), MarketTerminal.GetLastPrice());
                Log.Write($"USD: {StrategyStatistics.Snapshots.Last().Fiat} | Asset: {StrategyStatistics.Snapshots.Last().Security}", 1);
                Log.Write($"Period PL: {StrategyStatistics.Snapshots.Last().PL} | Cumulative PL: {StrategyStatistics.Snapshots.Last().CumulativePL}", 1);
            }
        }

        protected abstract void Trade();
    }
}
