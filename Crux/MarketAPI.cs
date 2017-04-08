using System.Collections.Generic;

namespace Crux
{
    public interface MarketAPI
    {
        void CancelAllOrders();

        void CancelOrder(int orderID);

        List<Order> GetActiveOrders();

        double GetBalanceFiat();

        double GetBalanceSecurity();

        double GetLastPrice();

        OrderBook GetOrderBook();

        int SubmitOrder(double price, double volume, char side, char type);

        bool Tick();
    }
}
