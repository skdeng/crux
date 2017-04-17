using System.Collections.Generic;

namespace Crux
{
    /// <summary>
    /// Values correspond to the number of minutes per period
    /// </summary>
    public enum TimePeriod
    {
        ONE_MIN = 1,
        ONE_HOUR = 60,
        ONE_DAY = 1440
    }

    public interface MarketAPI
    {
        void CancelAllOrders();

        void CancelOrder(Order order, OperationCallback callback = null);

        List<Order> GetActiveOrders();

        double GetBalanceFiat();

        double GetBalanceSecurity();

        double GetLastPrice();

        OrderBook GetOrderBook();

        Order SubmitOrder(double price, double volume, char side, char type, OperationCallback callback = null);

        bool Tick();

        IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods);
    }

    public delegate void MarketAPIReadyCallback();
    public delegate void OperationCallback(bool rejected);
}
