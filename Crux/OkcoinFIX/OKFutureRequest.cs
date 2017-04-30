using QuickFix;
using QuickFix.Fields;

namespace Crux.OkcoinFIX
{
    class OKFutureRequest
    {
        public static Message CreateOrderBookRequest(decimal price, char side, bool open)
        {
            QuickFix.FIX44.NewOrderSingle newOrderSingleRequest = new QuickFix.FIX44.NewOrderSingle();
            newOrderSingleRequest.Set(AccountUtil.Account);
            newOrderSingleRequest.Set(new ClOrdID("111"));
            newOrderSingleRequest.Set(new OrderQty(1));
            newOrderSingleRequest.Set(new OrdType('2'));
            newOrderSingleRequest.Set(new Price(price));
            newOrderSingleRequest.Set(new Side(side));
            newOrderSingleRequest.Set(new PositionEffect(open ? 'O' : 'C'));
            newOrderSingleRequest.Set(new Symbol("quarter"));
            newOrderSingleRequest.Set(new Currency("btc_usd"));
            newOrderSingleRequest.Set(new SecurityType(SecurityType.FUTURE));
            newOrderSingleRequest.Set(new MarginRatio(20));
            newOrderSingleRequest.Set(new TransactTime());
            return newOrderSingleRequest;
        }

        public static Message CreateOrderCancelRequest(int orderID)
        {
            QuickFix.FIX44.OrderCancelRequest orderCancelRequest = new QuickFix.FIX44.OrderCancelRequest();
            orderCancelRequest.Set(new ClOrdID("123"));

            return orderCancelRequest;
        }

        public static Message createFutureLiveDataRequest(string type)
        {
            FutureLiveDataRequest futureLiveDataRequest = new FutureLiveDataRequest();
            futureLiveDataRequest.SetField(new NewsCategory());
            futureLiveDataRequest.SetField(new StringField(8216, type));
            return futureLiveDataRequest;
        }

        public static Message CreateSubLivePositionDataRequest()
        {
            return createFutureLiveDataRequest("P");
        }

        public static Message CreateSubLiveOrderDataRequest()
        {
            return createFutureLiveDataRequest("O");
        }
    }

    class FutureLiveDataRequest : Message
    {
        public static string MSGTYPE = "Z2001";

        public FutureLiveDataRequest()
        {
            Header.SetField(new QuickFix.Fields.MsgType(MSGTYPE));
        }
    }
}
