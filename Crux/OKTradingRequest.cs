using QuickFix;
using QuickFix.Fields;
using System;
namespace Crux
{
    class OKTradingRequest
    {
        private static uint FreeID = 0x00000000;
        private static string GetFreeID { get { return FreeID++.ToString(); } }
        private static readonly string OrderSymbol = "BTC/USD";
        /**
         * new Order request
         * @throws IOException 
         */
        public static Message createOrderBookRequest(double vol, decimal price, char side)
        {

            QuickFix.FIX44.NewOrderSingle newOrderSingleRequest = new QuickFix.FIX44.NewOrderSingle();
            newOrderSingleRequest.Set(new Account(AccountUtil.apiKey + "," + AccountUtil.sercretKey));
            newOrderSingleRequest.Set(new ClOrdID(GetFreeID));
            newOrderSingleRequest.Set(new OrderQty(new decimal(vol)));
            newOrderSingleRequest.Set(new OrdType(OrdType.LIMIT));
            newOrderSingleRequest.Set(new Price(price));
            newOrderSingleRequest.Set(new Side(side));
            newOrderSingleRequest.Set(new Symbol(OrderSymbol));
            newOrderSingleRequest.Set(new TransactTime(DateTime.Now));
            return newOrderSingleRequest;
        }

        /**
         * 取消订单请求
         */
        public static Message createOrderCancelRequest(Order order)
        {
            QuickFix.FIX44.OrderCancelRequest OrderCancelRequest = new QuickFix.FIX44.OrderCancelRequest();
            OrderCancelRequest.Set(new ClOrdID(GetFreeID));
            OrderCancelRequest.Set(new OrigClOrdID(order.ClientOrderID));
            OrderCancelRequest.Set(new Side(order.Side));
            OrderCancelRequest.Set(new Symbol(OrderSymbol));
            OrderCancelRequest.Set(new TransactTime(DateTime.Now));
            return OrderCancelRequest;
        }

        public static Message createOrderMassStatusRequest(Order order = null)
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
                orderMassStatusRequest.Set(new MassStatusReqID(order.ClientOrderID));
            }
            return orderMassStatusRequest;
        }

        /**
         * 订单状态请求
         */
        public static Message createOrderStatusRequest()
        {
            QuickFix.FIX44.OrderMassStatusRequest orderMassStatusRequest = new QuickFix.FIX44.OrderMassStatusRequest();
            orderMassStatusRequest.Set(new MassStatusReqID("2123413"));//查询的订单ID
            orderMassStatusRequest.Set(new MassStatusReqType(MassStatusReqType.STATUS_FOR_ALL_ORDERS));
            return orderMassStatusRequest;
        }

        /**
         * 账户信息请求 
         */
        public static Message createUserAccountRequest()
        {
            AccountInfoRequest accountInfoRequest = new AccountInfoRequest();
            accountInfoRequest.set(new Account(AccountUtil.apiKey + "," + AccountUtil.sercretKey));//这里可以设置可以省略
            accountInfoRequest.set(new AccReqID("123"));
            return accountInfoRequest;
        }

        /**
         * 请求历史交易
         */
        public static Message createTradeHistoryRequest()
        {
            QuickFix.FIX44.TradeCaptureReportRequest tradeCaptureReportRequest = new QuickFix.FIX44.TradeCaptureReportRequest();
            tradeCaptureReportRequest.Set(new Symbol("BTC/CNY"));
            tradeCaptureReportRequest.Set(new TradeRequestID("order_id"));
            tradeCaptureReportRequest.Set(new TradeRequestType(1));//这里必须是1
            return tradeCaptureReportRequest;
        }

        public static Message createTradeOrdersBySomeID()
        {
            OrdersAfterSomeIDRequest request = new OrdersAfterSomeIDRequest();
            request.set(new OrderID("1"));
            request.set(new Symbol("BTC/USD"));
            request.set(new OrdStatus('1'));
            request.set(new TradeRequestID("liushuihao_001"));
            //		request.Set(new TradeRequestType(1));
            return request;
        }

    }
}
