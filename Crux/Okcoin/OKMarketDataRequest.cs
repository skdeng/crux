using QuickFix;
using QuickFix.Fields;

namespace Crux.Okcoin
{
    class OKMarketDataRequest
    {
        private static uint FreeID = 0x00000000;
        private static string GetFreeID { get { return FreeID++.ToString(); } }

        private static readonly Symbol TradeSymbol = new Symbol("BTC/USD");

        public static Message createOrderBookRequest(int depth = 0)
        {
            QuickFix.FIX44.MarketDataRequest orderBookRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
            noRelatedSym.Set(TradeSymbol);
            orderBookRequest.AddGroup(noRelatedSym);

            orderBookRequest.Set(new MDReqID(GetFreeID));
            orderBookRequest.Set(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));
            orderBookRequest.Set(new MDUpdateType(MDUpdateType.INCREMENTAL_REFRESH));
            orderBookRequest.Set(new MarketDepth(depth));   // 0 means full book

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup bid = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            bid.Set(new MDEntryType(MDEntryType.BID));
            orderBookRequest.AddGroup(bid);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup offer = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            offer.Set(new MDEntryType(MDEntryType.OFFER));
            orderBookRequest.AddGroup(offer);

            return orderBookRequest;
        }

        public static Message createLiveTradesRequest()
        {
            QuickFix.FIX44.MarketDataRequest liveTradesRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();

            noRelatedSym.Set(TradeSymbol);
            liveTradesRequest.AddGroup(noRelatedSym);
            liveTradesRequest.Set(new MDReqID(GetFreeID));
            liveTradesRequest.Set(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));
            liveTradesRequest.Set(new MarketDepth(0));
            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group.Set(new MDEntryType(MDEntryType.TRADE));
            liveTradesRequest.AddGroup(group);
            return liveTradesRequest;
        }

        public static Message create24HTickerRequest()
        {
            QuickFix.FIX44.MarketDataRequest tickerRequest = new QuickFix.FIX44.MarketDataRequest();
            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup noRelatedSym = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();

            noRelatedSym.Set(TradeSymbol);

            tickerRequest.AddGroup(noRelatedSym);

            tickerRequest.Set(new MDReqID(GetFreeID));
            tickerRequest.Set(new SubscriptionRequestType('1'));
            tickerRequest.Set(new MarketDepth(0));

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group1 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group1.Set(new MDEntryType(MDEntryType.OPENING_PRICE));
            tickerRequest.AddGroup(group1);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group2 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group2.Set(new MDEntryType(MDEntryType.CLOSING_PRICE));
            tickerRequest.AddGroup(group2);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group3 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group3.Set(new MDEntryType(MDEntryType.TRADING_SESSION_HIGH_PRICE));
            tickerRequest.AddGroup(group3);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group4 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group4.Set(new MDEntryType(MDEntryType.TRADING_SESSION_LOW_PRICE));
            tickerRequest.AddGroup(group4);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group5 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group5.Set(new MDEntryType(MDEntryType.TRADING_SESSION_VWAP_PRICE));
            tickerRequest.AddGroup(group5);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group6 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            group6.Set(new MDEntryType(MDEntryType.TRADE_VOLUME));
            tickerRequest.AddGroup(group6);

            return tickerRequest;
        }
    }
}