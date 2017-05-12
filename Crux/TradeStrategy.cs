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

        public bool Trading { get; protected set; }

        protected IMarketAPI MarketTerminal { get; set; }

        private Thread TradeThread { get; set; }

        private Thread LogThread { get; set; }

        private DateTime LastStatTime { get; set; }

        public TradeStrategy(IMarketAPI api, Statistics stats)
        {
            MarketTerminal = api;
            StrategyStatistics = stats;
            LastStatTime = StrategyStatistics.Snapshots.LastOrDefault()?.Time ?? new DateTime();

            LogThread = new Thread(new ThreadStart(_LogStats));
            LogThread.Name = "LogThread";
        }

        public void Start(bool async)
        {
            Trading = true;
            LogThread.Start();
            if (async)
            {
                if (TradeThread == null)
                {
                    TradeThread = new Thread(new ThreadStart(_Trade));
                    TradeThread.Name = "TradeThread";
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
            if (TradeThread?.ThreadState == ThreadState.Running)
            {
                TradeThread.Join();
            }
            if (LogThread.ThreadState == ThreadState.WaitSleepJoin)
            {
                LogThread.Abort();
            }
            else if (LogThread.ThreadState == ThreadState.Running)
            {
                LogThread.Join();
            }
        }

        private void _Trade()
        {
            while (Trading)
            {
                Trade();
                //try
                //{
                //    Trade();
                //}
                //catch (Exception e)
                //{
                //    Log.Write($"General exception: {e}", 0);
                //}
            }
        }

        private void _LogStats()
        {
            while (Trading)
            {
                var waitTime = LastStatTime.AddMinutes(15) - DateTime.Now;
                if (waitTime.TotalMilliseconds > 0)
                {
                    Thread.Sleep(waitTime);
                }
                StrategyStatistics.Snapshot(MarketTerminal.GetBalanceFiat(), MarketTerminal.GetBalanceSecurity(), MarketTerminal.GetLastPrice());
                Log.Write($"USD: {StrategyStatistics.Snapshots.Last().Fiat.ToString("N5")} | Asset: {StrategyStatistics.Snapshots.Last().Security}", 1);
                Log.Write($"Period PL: {StrategyStatistics.Snapshots.Last().PL.ToString("N5")} | Cumulative PL: {StrategyStatistics.Snapshots.Last().CumulativePL.ToString("N5")}", 1);
                LastStatTime = DateTime.Now;
            }

        }

        protected abstract void Trade();
    }
}
