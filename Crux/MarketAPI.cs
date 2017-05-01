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
        /// <summary>
        /// Cancel all active orders
        /// </summary>
        void CancelAllOrders();

        /// <summary>
        /// Cancel a given order, does nothing if the order is already cancelled or filled or does not exist
        /// Operation does not have to be async but callback must execute after the cancellation has been confirmed or rejected by the exchange
        /// </summary>
        /// <param name="order">Order to be cancelled</param>
        /// <param name="callback">Optional callback to execute after the order has been cancelled</param>
        void CancelOrder(Order order, OrderOperationCallback callback = null);

        /// <summary>
        /// Get a list of active orders
        /// Alternatively, query the information about one single order
        /// </summary>
        /// <param name="queryOrder">If not null, return a list with a single order</param>
        /// <returns></returns>
        List<Order> GetActiveOrders(Order queryOrder = null);

        /// <summary>
        /// Get the latest fiat balance
        /// </summary>
        /// <returns></returns>
        double GetBalanceFiat();

        /// <summary>
        /// Get the latest security balance
        /// </summary>
        /// <returns></returns>
        double GetBalanceSecurity();

        /// <summary>
        /// Get the price of the last trade
        /// </summary>
        /// <returns></returns>
        double GetLastPrice();

        /// <summary>
        /// Get the lastest order book
        /// </summary>
        /// <returns>Latest order book</returns>
        OrderBook GetOrderBook();

        /// <summary>
        /// Submit a new order
        /// Operation does not have to be async but callback must execute after the submission is confirmed or rejected by the exchange
        /// </summary>
        /// <param name="price">Price of the order</param>
        /// <param name="volume">Amount of security to trade</param>
        /// <param name="side">Side of the trade, uses QuickFIX constants Side.BUY and Side.SELL</param>
        /// <param name="type">Limit order or market order, uses QuickFIX constants Type.LIMIT and Type.MARKET</param>
        /// <param name="callback">Callback to be executed after the order has been submitted</param>
        /// <returns></returns>
        Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null);

        /// <summary>
        /// Get historical prices
        /// </summary>
        /// <param name="timespan">Time spam of either period</param>
        /// <param name="numPeriods">Number of periods</param>
        /// <returns></returns>
        IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods);
    }

    /// <summary>
    /// Callback to be executed after an order operation has been executed
    /// </summary>
    /// <param name="rejected">Whether the operation was rejected</param>
    public delegate void OrderOperationCallback(bool rejected);
}
