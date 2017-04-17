using System.Collections.Generic;
using System.Threading;

namespace Crux
{
    public abstract class TradeStrategy
    {
        public readonly List<double> PL;

        public double CumulativePL { get { return PortfolioValue / StartingPortfolioValue; } }

        protected int CurrentTick;

        public double StartingPortfolioValue;

        public double PortfolioValue { get { return MarketTerminal.GetBalanceFiat() + MarketTerminal.GetBalanceSecurity() * MarketTerminal.GetLastPrice(); } }

        protected MarketAPI MarketTerminal;

        protected bool Trading;

        private Thread TradeThread;

        public TradeStrategy(MarketAPI api)
        {
            PL = new List<double>();
            CurrentTick = 0;
            MarketTerminal = api;
            StartingPortfolioValue = PortfolioValue;
        }

        public void Start(bool async)
        {
            Trading = true;
            if (async)
            {
                if (TradeThread == null)
                {
                    TradeThread = new Thread(new ThreadStart(_Trade));
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
                var beforeValue = PortfolioValue;
                Trade();

                Log.Write($"USD: {MarketTerminal.GetBalanceFiat()} | Asset: {MarketTerminal.GetBalanceSecurity()}", 1);

                var periodPL = PortfolioValue / beforeValue;
                PL.Add(periodPL);
                Log.Write($"Period PL: {periodPL} | Cumulative PL: {CumulativePL}", 1);
            }
        }

        protected abstract void Trade();
    }
}
