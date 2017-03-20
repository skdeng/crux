using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Crux
{
    class MarketMaker : MessageCracker, IApplication
    {
        private Session CurrentSession;

        private readonly List<Order> CurrentOrders = new List<Order>();

        private float BalanceBTC;

        private float BalanceUSD;

        private bool Running;

        private Thread MarketMakerThread;

        private float BuySpread;

        private float SellSpread;

        private float BuyAdjust;

        private float SellAdjust;

        private List<Order> BuyBook = new List<Order>();
        private List<Order> SellBook = new List<Order>();

        public void StartMarketMake()
        {
            Running = true;
            MarketMakerThread = new Thread(() => MarketMake());
            MarketMakerThread.Start();
        }

        private void MarketMake()
        {
            while (Running)
            {
                CurrentOrders.Clear();
                FetchAllOrders();
                FetchUserInfo();
                Thread.Sleep(200);

                // If no order exists
                if (!CurrentOrders.Exists(o => true))
                {

                }
            }
        }

        public void StopMarketMake()
        {
            Running = false;
            MarketMakerThread.Join();
        }

        private void FetchAllOrders()
        {
            CurrentSession.Send(OKTradingRequest.createOrderMassStatusRequest());
        }

        private void FetchUserInfo()
        {
            CurrentSession.Send(OKTradingRequest.createUserAccountRequest());
        }

        private void ComputeSpread()
        {

        }

        public void OnMessage(QuickFix.FIX44.MarketDataSnapshotFullRefresh msg, SessionID sessionID)
        {
            var numMDEntries = msg.GetInt(Tags.NoMDEntries);
            for (int i = 1; i <= numMDEntries; i++)
            {
                var order = msg.GetGroup(i, Tags.NoMDEntries);
                if (order.GetInt(Tags.MDEntryType) == 0)
                {
                    BuyBook.Add(new Order()
                    {
                        Price = (float)order.GetDecimal(Tags.MDEntryPx),
                        Side = order.GetChar(Tags.Side),
                        Vol = (float)order.GetDecimal(Tags.MDEntrySize)
                    });
                }
                else
                {
                    SellBook.Add(new Order()
                    {
                        Price = (float)order.GetDecimal(Tags.MDEntryPx),
                        Side = order.GetChar(Tags.Side),
                        Vol = (float)order.GetDecimal(Tags.MDEntrySize)
                    });
                }
            }
        }

        public void OnMessage(QuickFix.FIX44.MarketDataIncrementalRefresh msg, SessionID sessionID)
        {
            var numMDEntries = msg.GetInt(Tags.NoMDEntries);
            var g = msg.GetGroup(1, 268);
            Console.WriteLine("Got Market Data");
        }

        public void OnMessage(QuickFix.FIX44.ExecutionReport msg, SessionID sessionID)
        {
            string executionReportType = msg.GetString(Tags.ExecType);
            if (executionReportType.Equals("0"))
            {
                // New order execution report
                string avgPrice = msg.GetString(Tags.AvgPx);
                string orderID = msg.GetString(Tags.ExecID);
                string clientOrderID = msg.GetString(Tags.ClOrdID);
                char side = msg.GetChar(Tags.Side);
                CurrentOrders.Add(new Order()
                {
                    OrderID = orderID,
                    ClientOrderID = clientOrderID,
                    Price = float.Parse(avgPrice),
                    Side = side
                });
                Console.WriteLine("New order executed");
            }
            else if (executionReportType.Equals("4"))
            {
                // Order cancel report
            }
            else if (executionReportType.Equals("8"))
            {
                // Rejected order execution report
                Console.WriteLine("Order rejected");
                string orderStatus = msg.GetString(Tags.OrdStatus);
                string text = msg.GetString(Tags.Text);
                Console.Write($"Order status: {orderStatus}\nReason: {text}");
            }
            else if (executionReportType.Equals("A"))
            {
                string clientOrderID = msg.GetString(Tags.ClOrdID);
                // Requested Order info
                if (!CurrentOrders.Exists(o => o.ClientOrderID.Equals(clientOrderID)))
                {
                    CurrentOrders.Add(new Order()
                    {
                        OrderID = msg.GetString(Tags.OrderID),
                        ClientOrderID = msg.GetString(Tags.ClOrdID),
                        Price = (float)msg.GetDecimal(Tags.AvgPx),
                        Side = msg.GetChar(Tags.Side)
                    });
                }
            }
        }

        public void OnAccountInfoResponse(Message msg, SessionID sessionID)
        {
            BalanceBTC = float.Parse(msg.GetString(8101));
            BalanceUSD = float.Parse(msg.GetString(8103));
        }

        public void OnErrorMessage(Message msg, SessionID sessionID)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error message: ");
            Console.WriteLine(msg.GetString(Tags.Text));
            Console.ForegroundColor = prevColor;
        }

        /// <summary>
        /// CALLBACK: Inbound admin level messages
        /// e.g. heartbeat, logons, logouts
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void FromAdmin(Message msg, SessionID sessionID)
        {
            Console.WriteLine("===== Message From Admin =====");
            Console.WriteLine($"{sessionID} : {msg.ToString()}");
        }

        /// <summary>
        /// CALLBACK: Inbound application level messages
        /// e.g. Orders, executions, security definitions, market data
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void FromApp(Message msg, SessionID sessionID)
        {
            Console.WriteLine("===== Message Received =====");
            string msgType = msg.Header.GetString(Tags.MsgType);
            if (msgType.Equals("Z1001"))
            {
                OnAccountInfoResponse(msg, sessionID);
            }
            else if (msgType.Equals("E1000"))
            {
                OnErrorMessage(msg, sessionID);
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
            Console.WriteLine("===== Client created =====");
            Console.WriteLine(sessionID);
        }

        /// <summary>
        /// CALLBACK: successful logon
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine("===== Client logon =====");

            CurrentSession = Session.LookupSession(sessionID);
            CurrentSession.Send(OKMarketDataRequest.createOrderBookRequest());
        }

        /// <summary>
        /// CALLBACK: when sessions become offline, either from logout message or network connection loss
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine("===== Client Logout =====");
            Console.WriteLine(sessionID);
        }

        /// <summary>
        /// CALLBACK: Before outbound admin level messages are sent
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void ToAdmin(Message msg, SessionID sessionID)
        {
            msg.SetField(new StringField(553, AccountUtil.apiKey));
            msg.SetField(new StringField(554, AccountUtil.sercretKey));

        }

        /// <summary>
        /// CALLBACK: Before outbound application level messages
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sessionID"></param>
        public void ToApp(Message msg, SessionID sessionID)
        {
            Console.WriteLine("===== Message Sent =====");
            Console.WriteLine($"{sessionID} : {msg.ToString()}");
        }

    }
}
