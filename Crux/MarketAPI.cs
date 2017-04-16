using System.Collections.Generic;

namespace Crux
{
    public interface MarketAPI
    {
        void CancelAllOrders();

        void CancelOrder(Order order);

        List<Order> GetActiveOrders();

        double GetBalanceFiat();

        double GetBalanceSecurity();

        double GetLastPrice();

        OrderBook GetOrderBook();

        Order SubmitOrder(double price, double volume, char side, char type);

        bool Tick();
    }
}
