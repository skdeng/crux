using System;
using System.Collections.Generic;

namespace Crux.Poloniex
{
    class PoloniexMarketAPI : IMarketAPI
    {
        public void CancelAllOrders()
        {
            throw new NotImplementedException();
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            throw new NotImplementedException();
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            throw new NotImplementedException();
        }

        public double GetBalanceFiat()
        {
            throw new NotImplementedException();
        }

        public double GetBalanceSecurity()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            throw new NotImplementedException();
        }

        public double GetLastPrice()
        {
            throw new NotImplementedException();
        }

        public OrderBook GetOrderBook()
        {
            throw new NotImplementedException();
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            throw new NotImplementedException();
        }

        public bool Tick()
        {
            throw new NotImplementedException();
        }
    }
}
