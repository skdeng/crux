using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crux.Okcoin
{
    class OKCMarketAPI : MessageCracker, IApplication, MarketAPI
    {
        private Session CurrentSession;

        private readonly List<Order> CurrentOrders = new List<Order>();

        private double BalanceBTC;

        private double BalanceLTC;

        private double BalanceUSD;

        private OrderBook CurrentOrderBook = new OrderBook();

        private Symbol TradeSymbol;

        private Trade LastTrade;

        private bool TrackOrderBook;
        private bool TrackLiveTrade;

        public OKCMarketAPI(string keyfile, string symbol, bool orderbook, bool livetrade)
        {
            AccountUtil.ReadKeyFile(keyfile);
            TradeSymbol = new Symbol(symbol);
            OKTradingRequest.TradeSymbol = TradeSymbol;
            OKMarketDataRequest.TradeSymbol = TradeSymbol;

            LastTrade = null;
            TrackOrderBook = orderbook;
            TrackLiveTrade = livetrade;
        }

        public void CancelAllOrders()
        {
            throw new NotImplementedException();
        }

        public void CancelOrder(Order order)
        {
            var request = OKTradingRequest.CreateOrderCancelRequest(order);
            CurrentSession.Send(request);
        }

        public List<Order> GetActiveOrders()
        {
            throw new NotImplementedException();
        }

        public double GetBalanceFiat()
        {
            return BalanceUSD;
        }

        public double GetBalanceSecurity()
        {
            var symbol = TradeSymbol.getValue();
            if (symbol.Contains("BTC"))
            {
                return BalanceBTC;
            }
            else if (symbol.Contains("LTC"))
            {
                return BalanceLTC;
            }
            else
            {
                Log.Write($"Broken symbol: {symbol}", 0);
                return 0;
            }
        }

        public double GetLastPrice()
        {
            return LastTrade == null ? 0 : LastTrade.Price;
        }

        public OrderBook GetOrderBook()
        {
            throw new NotImplementedException();
        }

        public Order SubmitOrder(double price, double volume, char side, char type)
        {
            var request = OKTradingRequest.CreateNewOrderRequest(volume, price, side, type);
            CurrentSession.Send(request);
            return null;
        }

        public bool Tick()
        {
            throw new NotImplementedException();
        }

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
                var entry = msg.GetGroup(i, Tags.NoMDEntries);
                var entryType = entry.GetChar(Tags.MDEntryType);
                if (entryType.Equals(MDEntryType.BID) || entryType.Equals(MDEntryType.OFFER))
                {
                    CurrentOrderBook.AddOrder((double)entry.GetDecimal(Tags.MDEntryPx), (double)entry.GetDecimal(Tags.MDEntrySize), entry.GetChar(Tags.MDEntryType));
                }
                else if (entryType.Equals(MDEntryType.TRADE))
                {
                    LastTrade = new Trade()
                    {
                        Price = (double)entry.GetDecimal(Tags.MDEntryPx),
                        Volume = (double)entry.GetDecimal(Tags.MDEntrySize)
                    };
                }
                else
                {
                    Log.Write($"Unknown entry type {entryType}", 0);
                }
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
                var entry = msg.GetGroup(i, Tags.NoMDEntries);
                var entryType = entry.GetChar(Tags.MDEntryType);
                if (entryType.Equals(MDEntryType.BID) || entryType.Equals(MDEntryType.OFFER))
                {
                    var action = entry.GetChar(Tags.MDUpdateAction);
                    switch (action)
                    {
                        case MDUpdateAction.CHANGE:
                            CurrentOrderBook.ChangeOrder((double)entry.GetDecimal(Tags.MDEntryPx), (double)entry.GetDecimal(Tags.MDEntrySize), entry.GetChar(Tags.MDEntryType));
                            break;
                        case MDUpdateAction.DELETE:
                            CurrentOrderBook.RemoveOrder((double)entry.GetDecimal(Tags.MDEntryPx), entry.GetChar(Tags.MDEntryType), (double)entry.GetDecimal(Tags.MDEntrySize));
                            break;
                        default:
                            Log.Write($"Unknown market data update action {action}", 0);
                            break;
                    }
                }
                else if (entryType.Equals(MDEntryType.TRADE))
                {
                    LastTrade = new Trade()
                    {
                        Price = (double)entry.GetDecimal(Tags.MDEntryPx),
                        Volume = (double)entry.GetDecimal(Tags.MDEntrySize)
                    };
                }
            }
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
                                CurrentOrders.First(o => o.ClientOrderID == clientOrderID).Volume = remainingQty;
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
            BalanceBTC = (double)msg.GetDecimal(8101);
            BalanceLTC = (double)msg.GetDecimal(8102);
            BalanceUSD = (double)msg.GetDecimal(8103);
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

            while (CurrentSession == null)
            {
                CurrentSession = Session.LookupSession(sessionID);
            }

            if (TrackOrderBook)
            {
                CurrentSession.Send(OKMarketDataRequest.CreateOrderBookRequest(10));
            }
            if (TrackLiveTrade)
            {
                CurrentSession.Send(OKMarketDataRequest.CreateLiveTradesRequest());
            }

            // Fetch user info
            CurrentSession.Send(OKTradingRequest.CreateUserAccountRequest());
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
