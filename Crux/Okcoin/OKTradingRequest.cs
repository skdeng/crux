using QuickFix;
using QuickFix.Fields;
using System;

namespace Crux.Okcoin
{
    class OKTradingRequest
    {
        private static uint FreeID = 0x00000000;
        private static string GetFreeID { get { return FreeID++.ToString(); } }
        public static Symbol TradeSymbol = new Symbol("BTC/USD");

        /// <summary>
        /// Create FIX request for new limit order
        /// </summary>
        /// <param name="vol"></param>
        /// <param name="price"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        public static Message CreateNewOrderRequest(double vol, double price, char side, char type)
        {

            QuickFix.FIX44.NewOrderSingle newOrderSingleRequest = new QuickFix.FIX44.NewOrderSingle();
            newOrderSingleRequest.Set(new Account(AccountUtil.APIKey + "," + AccountUtil.SecretKey));
            newOrderSingleRequest.Set(new ClOrdID(GetFreeID));
            newOrderSingleRequest.Set(new OrderQty(new decimal(vol)));
            newOrderSingleRequest.Set(new OrdType(type));
            newOrderSingleRequest.Set(new Price(new decimal(price)));
            newOrderSingleRequest.Set(new Side(side));
            newOrderSingleRequest.Set(TradeSymbol);
            newOrderSingleRequest.Set(new TransactTime(DateTime.Now));
            return newOrderSingleRequest;
        }

        /// <summary>
        /// Create request for order cancellation
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public static Message CreateOrderCancelRequest(Order order)
        {
            QuickFix.FIX44.OrderCancelRequest OrderCancelRequest = new QuickFix.FIX44.OrderCancelRequest();
            OrderCancelRequest.Set(new ClOrdID(GetFreeID));
            OrderCancelRequest.Set(new OrigClOrdID(order.ClientOrderID.ToString()));
            OrderCancelRequest.Set(new Side(order.Side));
            OrderCancelRequest.Set(TradeSymbol);
            OrderCancelRequest.Set(new TransactTime(DateTime.Now));
            return OrderCancelRequest;
        }

        /// <summary>
        /// Create request for order status
        /// </summary>
        /// <param name="order">Request information for a given order, if null, request information for all orders</param>
        /// <returns></returns>
        public static Message CreateOrderMassStatusRequest(Order order = null)
        {
            QuickFix.FIX44.OrderMassStatusRequest orderMassStatusRequest = new QuickFix.FIX44.OrderMassStatusRequest();
            if (order == null)
            {
                orderMassStatusRequest.Set(new MassStatusReqType(MassStatusReqType.STATUS_FOR_ALL_ORDERS));
                orderMassStatusRequest.Set(new MassStatusReqID(GetFreeID));
            }
            else
            {
                orderMassStatusRequest.Set(new MassStatusReqType(MassStatusReqType.STATUS_FOR_ORDERS_FOR_A_SECURITY));
                orderMassStatusRequest.Set(new MassStatusReqID(order.ClientOrderID.ToString()));
            }
            return orderMassStatusRequest;
        }

        /// <summary>
        /// Create request for user account information
        /// </summary>
        /// <returns></returns>
        public static Message CreateUserAccountRequest()
        {
            AccountInfoRequest accountInfoRequest = new AccountInfoRequest();
            accountInfoRequest.set(AccountUtil.Account);
            accountInfoRequest.set(new AccReqID("123"));
            return accountInfoRequest;
        }

        /// <summary>
        /// Create request for trade history
        /// </summary>
        /// <returns></returns>
        public static Message CreateTradeHistoryRequest()
        {
            QuickFix.FIX44.TradeCaptureReportRequest tradeCaptureReportRequest = new QuickFix.FIX44.TradeCaptureReportRequest();
            tradeCaptureReportRequest.Set(TradeSymbol);
            tradeCaptureReportRequest.Set(new TradeRequestID("order_id"));
            tradeCaptureReportRequest.Set(new TradeRequestType(1)); // must be 1
            return tradeCaptureReportRequest;
        }


        public static Message CreateTradeOrdersBySomeID()
        {
            OrdersAfterSomeIDRequest request = new OrdersAfterSomeIDRequest();
            request.set(new OrderID("1"));
            request.set(TradeSymbol);
            request.set(new OrdStatus('1'));
            request.set(new TradeRequestID("liushuihao_001"));
            //		request.Set(new TradeRequestType(1));
            return request;
        }

    }
}
