using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crux.Okcoin
{
    class OKCMarketAPI : MessageCracker, IApplication
    {
        private Session CurrentSession;

        private readonly List<Order> CurrentOrders = new List<Order>();

        private double BalanceBTC;

        private double BalanceUSD;

        public OrderBook CurrentOrderBook = new OrderBook();

        /// <summary>
        /// Initial market data snapshot
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void OnMessage(QuickFix.FIX44.MarketDataSnapshotFullRefresh msg, SessionID sessionID)
        {
            var numMDEntries = msg.GetInt(Tags.NoMDEntries);
            for (int i = 1; i <= numMDEntries; i++)
            {
                var order = msg.GetGroup(i, Tags.NoMDEntries);
                CurrentOrderBook.AddOrder((double)order.GetDecimal(Tags.MDEntryPx), (double)order.GetDecimal(Tags.MDEntrySize), order.GetChar(Tags.MDEntryType));
            }
        }

        /// <summary>
        /// Incremental market data refresh
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void OnMessage(QuickFix.FIX44.MarketDataIncrementalRefresh msg, SessionID sessionID)
        {
            var numMDEntries = msg.GetInt(Tags.NoMDEntries);
            for (int i = 1; i <= numMDEntries; i++)
            {
                var order = msg.GetGroup(i, Tags.NoMDEntries);
                var action = order.GetChar(Tags.MDUpdateAction);
                switch (action)
                {
                    case MDUpdateAction.CHANGE:
                        CurrentOrderBook.ChangeOrder((double)order.GetDecimal(Tags.MDEntryPx), (double)order.GetDecimal(Tags.MDEntrySize), order.GetChar(Tags.MDEntryType));
                        break;
                    case MDUpdateAction.DELETE:
                        CurrentOrderBook.RemoveOrder((double)order.GetDecimal(Tags.MDEntryPx), order.GetChar(Tags.MDEntryType), (double)order.GetDecimal(Tags.MDEntrySize));
                        break;
                    default:
                        Log.Write($"Unknown market data update action {action}", 0);
                        break;
                }
            }
            Console.Title = $"Project Crux Bid: {CurrentOrderBook.BestBid.Price} Ask: {CurrentOrderBook.BestOffer.Price} NumBids: {CurrentOrderBook.NumBids} NumAsks: {CurrentOrderBook.NumOffers}";
        }

        public void OnMessage(QuickFix.FIX44.MarketDataRequestReject msg, SessionID sessionID)
        {
            Log.Write($"Market data request refused: {msg.GetChar(Tags.MDReqRejReason)}", 0);
        }

        /// <summary>
        /// Report after an inquiry about orders is made
        /// Possible cause: 
        /// - New order
        /// - Cancel order
        /// - Rejected order
        /// - Requested order info
        /// - Order filled or partially filled
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void OnMessage(QuickFix.FIX44.ExecutionReport msg, SessionID sessionID)
        {
            char executionReportType = msg.GetChar(Tags.ExecType);
            int clientOrderID = msg.GetInt(Tags.ClOrdID);
            switch (executionReportType)
            {
                case '0':
                    {
                        // New order execution report
                        string avgPrice = msg.GetString(Tags.AvgPx);
                        char side = msg.GetChar(Tags.Side);
                        CurrentOrders.Add(new Order()
                        {
                            OrderID = msg.GetInt(Tags.ExecID),
                            ClientOrderID = clientOrderID,
                            Price = double.Parse(avgPrice),
                            Side = side
                        });
                        Log.Write($"New {(side.Equals(Side.BUY) ? "BUY" : "SELL")} order at {avgPrice}", 1);
                        break;
                    }
                case '4':
                    {
                        // Order cancel report
                        string orderID = msg.GetString(Tags.ExecID);
                        CurrentOrders.RemoveAll(o => o.ClientOrderID == clientOrderID);
                        break;
                    }
                case '8':
                    {
                        // Rejected order execution report
                        string orderStatus = msg.GetString(Tags.OrdStatus);
                        string text = msg.GetString(Tags.Text);
                        Log.Write($"Order rejected | status: {orderStatus} | reason: {text}", 1);
                        break;
                    }
                case 'A':
                    {
                        // Requested Order info
                        int numReports = msg.GetInt(Tags.TotNumReports);
                        if (!CurrentOrders.Exists(o => o.ClientOrderID.Equals(clientOrderID)))
                        {
                            CurrentOrders.Add(new Order()
                            {
                                OrderID = msg.GetInt(Tags.OrderID),
                                ClientOrderID = clientOrderID,
                                Price = (double)msg.GetDecimal(Tags.AvgPx),
                                Side = msg.GetChar(Tags.Side)
                            });
                        }
                        break;
                    }
                case 'F':
                    {
                        // Order executed
                        double executionPrice = (double)msg.GetDecimal(Tags.AvgPx);
                        int orderStatus = msg.GetInt(Tags.OrdStatus);
                        switch (orderStatus)
                        {
                            case 1:
                                // partial fill
                                int remainingQty = msg.GetInt(Tags.LeavesQty);
                                CurrentOrders.First(o => o.ClientOrderID == clientOrderID).Vol = remainingQty;
                                break;
                            case 2:
                                // full fill
                                CurrentOrders.RemoveAll(o => o.ClientOrderID == clientOrderID);
                                break;
                        }
                        break;
                    }
                default:
                    Log.Write($"Unknown execution report type {executionReportType}", 0);
                    break;
            }
        }

        /// <summary>
        /// Report about account information
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void OnAccountInfoResponse(Message msg, SessionID sessionID)
        {
            BalanceBTC = double.Parse(msg.GetString(8101));
            BalanceUSD = double.Parse(msg.GetString(8103));
        }

        public void OnExchangeErrorMessage(Message msg, SessionID sessionID)
        {
            Log.Write("Exchange Error:", 0);
            Log.Write(msg.GetString(Tags.Text), 0);
        }

        /// <summary>
        /// CALLBACK: Inbound admin level messages
        /// e.g. heartbeat, logons, logouts
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void FromAdmin(Message msg, SessionID sessionID)
        {
            Log.Write("===== Admin Message Received =====", 3);
            Log.Write($"{sessionID} : {msg.ToString()}", 3);
        }

        /// <summary>
        /// CALLBACK: Inbound application level messages
        /// e.g. Orders, executions, security definitions, market data
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void FromApp(Message msg, SessionID sessionID)
        {
            Log.Write("===== Application Message Received =====", 3);
            string msgType = msg.Header.GetString(Tags.MsgType);
            if (msgType.Equals("Z1001"))
            {
                OnAccountInfoResponse(msg, sessionID);
            }
            else if (msgType.Equals("E1000"))
            {
                OnExchangeErrorMessage(msg, sessionID);
            }
            else
            {
                Crack(msg, sessionID);
            }

        }

        /// <summary>
        /// CALLBACK: Creation of new sessions
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnCreate(SessionID sessionID)
        {
            Log.Write($"===== Client {sessionID} created =====", 3);
        }

        /// <summary>
        /// CALLBACK: successful logon
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogon(SessionID sessionID)
        {
            Log.Write("===== Client logon =====", 3);

            CurrentSession = Session.LookupSession(sessionID);
            CurrentSession.Send(OKMarketDataRequest.createOrderBookRequest(10));

            // Fetch user info
            CurrentSession.Send(OKTradingRequest.createUserAccountRequest());
        }

        /// <summary>
        /// CALLBACK: when sessions become offline, either from logout message or network connection loss
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogout(SessionID sessionID)
        {
            Log.Write($"===== Client {sessionID} Logout =====", 3);
        }

        /// <summary>
        /// CALLBACK: Before outbound admin level messages are sent
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void ToAdmin(Message msg, SessionID sessionID)
        {
            msg.SetField(new StringField(553, AccountUtil.APIKey));
            msg.SetField(new StringField(554, AccountUtil.SecretKey));

        }

        /// <summary>
        /// CALLBACK: Before outbound application level messages
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void ToApp(Message msg, SessionID sessionID)
        {
            Log.Write("===== Application Message Sent =====", 3);
            Log.Write($"{sessionID} : {msg.ToString()}", 3);
        }

    }
}
