using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Crux.OkcoinFIX
{
    class OKCMarketFIXAPI : MessageCracker, IApplication, MarketAPI
    {
        private Session CurrentSession;

        private List<Order> CurrentOrders;
        private OrderBook CurrentOrderBook;
        private Trade LastTrade;

        private double BalanceBTC;
        private double BalanceLTC;
        private double BalanceUSD;

        private string TradeSymbolString;
        private Symbol TradeSymbol;

        private bool TrackOrderBook;
        private bool TrackLiveTrade;

        private QuickFix.Transport.SocketInitiator Initiator;

        private OrderOperationCallback OrderCancelCallback;
        private OrderOperationCallback OrderSubmitCallback;

        public OKCMarketFIXAPI(string keyfile, string symbol, bool orderbook, bool livetrade)
        {
            SessionSettings settings = new SessionSettings("config/quickfix-client.cfg");
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new ScreenLogFactory(settings);
            Initiator = new QuickFix.Transport.SocketInitiator(this, storeFactory, settings, logFactory);
            Initiator.Start();

            OrderCancelCallback = null;
            OrderSubmitCallback = null;

            AccountUtil.ReadKeyFile(keyfile);
            TradeSymbolString = symbol;
            TradeSymbol = new Symbol(symbol);
            OKTradingRequest.TradeSymbol = TradeSymbol;
            OKMarketDataRequest.TradeSymbol = TradeSymbol;
            CurrentOrders = new List<Order>();
            if (orderbook)
            {
                CurrentOrderBook = new OrderBook();
            }

            LastTrade = null;
            TrackOrderBook = orderbook;
            TrackLiveTrade = livetrade;
        }

        ~OKCMarketFIXAPI()
        {
            Initiator.Stop();
            Initiator.Dispose();
        }

        public void CancelAllOrders()
        {
            CurrentSession.Send(OKTradingRequest.CreateOrderMassStatusRequest());
            Thread.Sleep(500);
            foreach (var order in CurrentOrders)
            {
                CancelOrder(order);
            }
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            OrderCancelCallback = callback;
            var request = OKTradingRequest.CreateOrderCancelRequest(order);
            CurrentSession.Send(request);
            Log.Write($"Cancel order {order.Volume} at {order.Price.ToString("N3")}", 1);
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            if (queryOrder != null)
            {
                return CurrentOrders.Where(o => o.ClientOrderID == queryOrder.ClientOrderID).ToList();
            }
            else
            {
                return CurrentOrders;
            }
        }

        public double GetBalanceFiat()
        {
            return BalanceUSD;
        }

        public double GetBalanceSecurity()
        {
            if (TradeSymbolString.Contains("BTC"))
            {
                return BalanceBTC;
            }
            else if (TradeSymbolString.Contains("LTC"))
            {
                return BalanceLTC;
            }
            else
            {
                Log.Write($"Broken symbol: {TradeSymbolString}", 0);
                return 0;
            }
        }

        public double GetLastPrice()
        {
            return LastTrade == null ? 0 : LastTrade.Price;
        }

        public OrderBook GetOrderBook()
        {
            return CurrentOrderBook;
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            OrderSubmitCallback = callback;
            var request = OKTradingRequest.CreateNewOrderRequest(volume, price, side, type);
            CurrentSession.Send(request);
            Log.Write($"Submit {(side == Side.BUY ? "BUY" : "SELL")} order: {volume} at {price.ToString("N3")}$", 1);
            return new Order()
            {
                Price = price,
                Volume = volume,
                Side = side,
                OrderType = type,
                ClientOrderID = request.GetInt(Tags.ClOrdID),
                Time = request.GetDateTime(Tags.TransactTime)
            };
        }

        public bool Tick()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            string endPoint = "https://www.okcoin.com/api/v1/kline.do";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);
            string symbol = TradeSymbolString.Contains("LTC") ? "ltc_usd" : "btc_usd";
            string type;
            switch (timespan)
            {
                case TimePeriod.ONE_MIN:
                    type = "1min";
                    break;
                default:
                case TimePeriod.ONE_HOUR:
                    type = "1hour";
                    break;
                case TimePeriod.ONE_DAY:
                    type = "1day";
                    break;
            }
            var json = client.MakeRequest($"?symbol={symbol}&type={type}&size={numPeriods}");
            var data = (JArray)JsonConvert.DeserializeObject(json);
            if (data.Count != numPeriods)
            {
                Log.Write($"Got the wrong number of historical prices. Asked: {numPeriods} | Received: {data.Count}", 0);
            }

            return data.Select(d => new Candle((double)d[1], (double)d[4], (double)d[2], (double)d[3]));
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
                            CurrentOrderBook.RemoveOrder((double)entry.GetDecimal(Tags.MDEntryPx), entry.GetChar(Tags.MDEntryType));
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
        /// Report after a request about orders is made
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
                case ExecType.NEW:
                    {
                        // New order execution report
                        string avgPrice = msg.GetString(Tags.AvgPx);
                        char side = msg.GetChar(Tags.Side);
                        CurrentOrders.Add(new Order()
                        {
                            OrderID = msg.GetString(Tags.ExecID),
                            ClientOrderID = clientOrderID,
                            Price = double.Parse(avgPrice),
                            Side = side
                        });
                        OrderSubmitCallback?.Invoke(false);
                        OrderSubmitCallback = null;
                        Log.Write($"New {(side.Equals(Side.BUY) ? "BUY" : "SELL")} order placed", 1);
                        break;
                    }
                case ExecType.PARTIAL_FILL:
                    {
                        // Order executed
                        double executionPrice = (double)msg.GetDecimal(Tags.AvgPx);
                        int orderStatus = msg.GetInt(Tags.OrdStatus);
                        char side = msg.GetChar(Tags.Side);

                        double remainingQty = (double)msg.GetDecimal(Tags.LeavesQty);
                        CurrentOrders.First(o => o.ClientOrderID == clientOrderID).Volume = remainingQty;
                        Log.Write($"{(side.Equals(Side.BUY) ? "BUY" : "SELL")} order partially filled ({(double)msg.GetDecimal(Tags.CumQty)}/{remainingQty}) at {executionPrice}", 1);
                        break;
                    }
                case ExecType.FILL:
                    {
                        // Order executed
                        double executionPrice = (double)msg.GetDecimal(Tags.AvgPx);
                        int orderStatus = msg.GetInt(Tags.OrdStatus);
                        char side = msg.GetChar(Tags.Side);
                        CurrentOrders.RemoveAll(o => o.ClientOrderID == clientOrderID);
                        Log.Write($"{(side.Equals(Side.BUY) ? "BUY" : "SELL")} order filled {msg.GetDecimal(Tags.CumQty)} at {executionPrice}", 1);
                        break;
                    }

                case ExecType.CANCELED:
                    {
                        // Order cancel report
                        string orderID = msg.GetString(Tags.ExecID);
                        CurrentOrders.RemoveAll(o => o.ClientOrderID == clientOrderID);
                        OrderCancelCallback?.Invoke(false);
                        OrderCancelCallback = null;
                        break;
                    }
                case ExecType.REJECTED:
                    {
                        // Rejected order execution report
                        OrderSubmitCallback?.Invoke(true);
                        OrderSubmitCallback = null;
                        string orderStatus = msg.GetString(Tags.OrdStatus);
                        string text = msg.GetString(Tags.Text);
                        Log.Write($"Order rejected | status: {orderStatus} | reason: {text}", 1);
                        break;
                    }
                case ExecType.PENDING_NEW:
                    {
                        // Requested Order info
                        int numReports = msg.GetInt(Tags.TotNumReports);
                        if (!CurrentOrders.Exists(o => o.ClientOrderID.Equals(clientOrderID)))
                        {
                            CurrentOrders.Add(new Order()
                            {
                                OrderID = msg.GetString(Tags.OrderID),
                                ClientOrderID = clientOrderID,
                                Price = (double)msg.GetDecimal(Tags.AvgPx),
                                Side = msg.GetChar(Tags.Side)
                            });
                        }
                        break;
                    }
                default:
                    Log.Write($"Unknown execution report type {executionReportType}", 0);
                    break;
            }
        }

        public void OnMessage(QuickFix.FIX44.OrderCancelReject msg, SessionID sessionID)
        {
            OrderCancelCallback?.Invoke(true);
            OrderCancelCallback = null;
            Log.Write($"Order cancel error: {msg.GetString(Tags.Text)}", 0);
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
