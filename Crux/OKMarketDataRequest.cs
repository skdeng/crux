using QuickFix;
using QuickFix.Fields;
namespace Crux
{
    class OKMarketDataRequest
    {
        private static uint FreeID = 0x00000000;
        private static string GetFreeID { get { return FreeID++.ToString(); } }

        private static readonly Symbol TradeSymbol = new Symbol("BTC/USD");
        public static Message createOrderBookRequest()
        {
            QuickFix.FIX44.MarketDataRequest orderBookRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
            noRelatedSym.Set(TradeSymbol);
            orderBookRequest.AddGroup(noRelatedSym);

            orderBookRequest.Set(new MDReqID(GetFreeID));
            orderBookRequest.Set(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));
            orderBookRequest.Set(new MDUpdateType(1));//0全部  。1增量
            orderBookRequest.Set(new MarketDepth(0));

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup bid = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            bid.Set(new MDEntryType(MDEntryType.BID));
            orderBookRequest.AddGroup(bid);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup offer = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            offer.Set(new MDEntryType(MDEntryType.OFFER));
            orderBookRequest.AddGroup(offer);

            //QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup vol = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            //vol.Set(new MDEntryType(MDEntryType.TRADE_VOLUME));
            //orderBookRequest.AddGroup(vol);

            return orderBookRequest;
        }

        /**
         * 现场交易请求
         * @return
         */
        public static Message createLiveTradesRequest()
        {
            QuickFix.FIX44.MarketDataRequest liveTradesRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();

            //		noRelatedSym.Set(new Symbol("LTC/USD"));
            noRelatedSym.Set(new Symbol("LTC/CNY"));
            liveTradesRequest.AddGroup(noRelatedSym);
            liveTradesRequest.Set(new MDReqID("123"));
            liveTradesRequest.Set(new SubscriptionRequestType('1'));
            liveTradesRequest.Set(new MarketDepth(0));
            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group.Set(new MDEntryType('2'));
            liveTradesRequest.AddGroup(group);
            return liveTradesRequest;
        }

        /**
         * 24h行情请求
         * @return
         */
        public static Message create24HTickerRequest()
        {
            QuickFix.FIX44.MarketDataRequest tickerRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();

            //noRelatedSym.Set(new Symbol("BTC/CNY"));
            noRelatedSym.Set(new Symbol("BTC/CNY"));

            tickerRequest.AddGroup(noRelatedSym);

            tickerRequest.Set(new MDReqID("123"));
            tickerRequest.Set(new SubscriptionRequestType('1'));
            tickerRequest.Set(new MarketDepth(0));

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group1 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group1.Set(new MDEntryType('4'));
            tickerRequest.AddGroup(group1);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group2 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group2.Set(new MDEntryType('5'));
            tickerRequest.AddGroup(group2);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group3 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group3.Set(new MDEntryType('7'));
            tickerRequest.AddGroup(group3);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group4 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group4.Set(new MDEntryType('8'));
            tickerRequest.AddGroup(group4);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group5 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group5.Set(new MDEntryType('9'));
            tickerRequest.AddGroup(group5);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group6 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group6.Set(new MDEntryType('B'));
            tickerRequest.AddGroup(group6);

            return tickerRequest;
        }
    }
}