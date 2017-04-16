using Crux;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BackTest
{
    class HistoricalDataAPI : MarketAPI
    {
        private int CurrentTick;

        public List<double> HistoricalPrices { get; private set; }

        private List<Order> CurrentOrders;

        private double BalanceFiat;

        private double BalanceSecurity;

        private double TransactionCost;

        private static int FreeID = 0x00000000;
        private static int GetFreeID { get { return FreeID++; } }

        private Mutex OrderLock { get; set; }

        public HistoricalDataAPI(string historicalDataFile, double startingUSD, double startingBTC, double transactionCost)
        {
            HistoricalPrices = new List<double>();
            CurrentTick = 0;
            CurrentOrders = new List<Order>();
            BalanceFiat = startingUSD;
            BalanceSecurity = startingBTC;
            TransactionCost = transactionCost;

            ReadDateFile(historicalDataFile);
            OrderLock = new Mutex();
        }

        public void CancelAllOrders()
        {
            CurrentOrders.RemoveRange(0, CurrentOrders.Count);
        }

        public void CancelOrder(Order order)
        {
            OrderLock.WaitOne();
            CurrentOrders.RemoveAll(o => o.ClientOrderID == order.ClientOrderID || o.OrderID == order.OrderID);
            OrderLock.ReleaseMutex();
        }

        public List<Order> GetActiveOrders()
        {
            return CurrentOrders;
        }

        public double GetBalanceFiat()
        {
            return BalanceFiat;
        }

        public double GetBalanceSecurity()
        {
            return BalanceSecurity;
        }

        public double GetLastPrice()
        {
            return HistoricalPrices[CurrentTick];
        }

        public OrderBook GetOrderBook()
        {
            return null;
        }

        public Order SubmitOrder(double price, double volume, char side, char type)
        {
            switch (side)
            {
                case Side.BUY:
                    if (price * volume > BalanceFiat)
                    {
                        return null;
                    }
                    break;
                case Side.SELL:
                    if (volume > BalanceSecurity)
                    {
                        return null;
                    }
                    break;
                default:
                    break;
            }

            if (type.Equals(OrdType.MARKET))
            {
                ExecuteOrder(volume, side);
                return new Order() { Price = price, Volume = volume, Side = side, OrderType = type, OrderID = GetFreeID };
            }
            var newOrder = new Order() { Price = price, Volume = volume, Side = side, OrderType = type, OrderID = GetFreeID };

            OrderLock.WaitOne();
            CurrentOrders.Add(newOrder);
            OrderLock.ReleaseMutex();

            return newOrder;
        }

        public void StartTick()
        {
            while (CurrentTick < HistoricalPrices.Count)
            {
                Tick();
                Thread.Sleep(10);
            }
        }

        public bool Tick()
        {
            OrderLock.WaitOne();
            foreach (var order in CurrentOrders)
            {
                switch (order.Side)
                {
                    case Side.BUY:
                        if (order.Price >= HistoricalPrices[CurrentTick])
                        {
                            ExecuteOrder(order.Volume, order.Side);
                        }
                        break;
                    case Side.SELL:
                        if (order.Price <= HistoricalPrices[CurrentTick])
                        {
                            ExecuteOrder(order.Volume, order.Side);
                        }
                        break;
                    default:
                        break;
                }
            }
            OrderLock.ReleaseMutex();

            CurrentTick++;
            if (CurrentTick == HistoricalPrices.Count)
            {
                CurrentTick--;
                return false;
            }
            else
            {
                return true;
            }
        }

        private void ExecuteOrder(double volume, char side)
        {
            var price = HistoricalPrices[CurrentTick];
            switch (side)
            {
                case Side.BUY:
                    BalanceFiat -= price * volume;
                    BalanceSecurity += volume * (1 - TransactionCost);
                    break;
                case Side.SELL:
                    BalanceFiat += price * volume * (1 - TransactionCost);
                    BalanceSecurity -= volume;
                    break;
                default:
                    break;
            }
        }

        private void ReadDateFile(string historicalDataFile)
        {
            var lines = File.ReadAllLines(historicalDataFile);
            foreach (var line in lines)
            {
                var tokens = line.Split(',');
                HistoricalPrices.Add(double.Parse(tokens[1]));
            }
        }

        private DateTime ParseTimestamp(int timestamp)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(timestamp);
        }
    }
}
