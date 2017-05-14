using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebSocket4Net;

namespace Crux.BfxWS
{
    public class BfxMarketWSAPI : IMarketAPI
    {
        private static string ConnectionString = "wss://api.bitfinex.com/ws/2";

        private static string KeyFileName;

        private WebSocket SocketTerminal;

        private Dictionary<string, int> ChannelID;

        private string Symbol;

        private string TradeSymbol;

        private double BalanceFiat = 0;

        private double BalanceSecurity = 0;

        private Trade LastTrade;

        private List<Order> CurrentOrders;
        private OrderBook CurrentOrderBook;

        private OrderOperationCallback OrderSubmitCallback;
        private OrderOperationCallback OrderCancelCallback;

        public BfxMarketWSAPI(string keyFile, string symbol)
        {
            KeyFileName = keyFile;
            ChannelID = new Dictionary<string, int>();
            ChannelID.Add("auth", 0);
            CurrentOrders = new List<Order>();
            CurrentOrderBook = new OrderBook();

            Symbol = symbol.ToUpper();
            TradeSymbol = $"t{Symbol}USD";

            SocketTerminal = new WebSocket(ConnectionString);
            SocketTerminal.Opened += SocketTerminal_OnOpen;
            SocketTerminal.Closed += SocketTerminal_OnClose;
            SocketTerminal.Error += SocketTerminal_OnError;
            SocketTerminal.MessageReceived += SocketTerminal_OnMessageReceived;
            SocketTerminal.Open();
        }

        public void Close()
        {
            SocketTerminal.Close();
            SocketTerminal.Dispose();
        }

        public void CancelAllOrders()
        {
            //var cancelGroupOrder = new CancelGroupOrderMessage();
            //var cancelGroupOrderMsg = BuildRequestMsg(cancelGroupOrder, "oc_multi");
            //Log.Write($"Cancel group order msg: {cancelGroupOrderMsg}", 3);
            //SocketTerminal.Send(cancelGroupOrderMsg);

            foreach (var order in CurrentOrders)
            {
                CancelOrder(order);
            }
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            OrderCancelCallback = callback;
            var subCancelOrder = new CancelOrderMessage(order);
            var cancelOrderMessage = BuildRequestMsg(subCancelOrder, "oc");
            SocketTerminal.Send(cancelOrderMessage);
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            if (queryOrder != null)
            {
                return CurrentOrders.Where(o => o.Equals(queryOrder)).ToList();
            }
            else
            {
                return CurrentOrders;
            }
        }

        public double GetBalanceFiat()
        {
            return BalanceFiat;
        }

        public double GetBalanceSecurity()
        {
            return BalanceSecurity;
        }

        public double GetLastPrice()
        {
            return LastTrade.Price;
        }

        public OrderBook GetOrderBook()
        {
            return CurrentOrderBook;
        }

        public bool IsReady()
        {
            return LastTrade != null &&
                   CurrentOrderBook.BestBid != null &&
                   CurrentOrderBook.BestOffer != null;
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            OrderSubmitCallback = callback;

            var subOrderMsg = new NewOrderMessage(TradeSymbol, price, volume, side, type);
            var orderMsg = BuildRequestMsg(subOrderMsg, "on");

            Log.Write($"Submitted order msg: {orderMsg.ToString()}", 3);

            SocketTerminal.Send(orderMsg.ToString());
            var order = new Order()
            {
                Price = price,
                Volume = volume,
                Side = side,
                OrderType = type,
                Time = DateTime.Now,
                ClientOrderID = subOrderMsg.ClientID
            };
            return order;
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            string timeFrameString;
            switch (timespan)
            {
                case TimePeriod.ONE_MIN:
                    timeFrameString = "1m";
                    break;
                default:
                case TimePeriod.ONE_HOUR:
                    timeFrameString = "1h";
                    break;
                case TimePeriod.ONE_DAY:
                    timeFrameString = "1D";
                    break;
            }
            string endPoint = $"https://api.bitfinex.com/v2/candles/trade:{timeFrameString}:{TradeSymbol}/hist";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);
            var json = client.MakeRequest($"?limit={numPeriods}");
            var data = (JArray)JsonConvert.DeserializeObject(json);
            if (data.Count != numPeriods)
            {
                Log.Write($"Got the wrong number of historical prices. Asked: {numPeriods} | Received: {data.Count}", 0);
            }

            return data.Reverse().Select(d => new Candle((double)d[1], (double)d[2], (double)d[3], (double)d[4], timespan));
        }

        private void SocketTerminal_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Log.Write($"Message received: {e.Message}", 3);

            var data = JsonConvert.DeserializeObject(e.Message);
            if (data is JObject)
            {
                var dataObject = (JObject)data;
                var eventType = dataObject["event"].ToString();
                if (eventType.Equals("subscribed"))
                {
                    ChannelID.Add(dataObject["channel"].ToString(), (int)dataObject["chanId"]);
                }
                else if (eventType.Equals("auth"))
                {
                    var errorCode = dataObject["code"];
                    if (errorCode != null)
                    {
                        Log.Write($"Authentication failure with code {errorCode} | msg: {dataObject["msg"]}", 0);
                    }
                }
                else if (eventType.Equals("error"))
                {
                    Log.Write($"Error code: {dataObject["code"]} | msg: {dataObject["msg"]}", 0);
                }
            }
            else if (data is JArray)
            {
                var dataArray = (JArray)data;
                var channelID = (int)dataArray[0];
                if (channelID == ChannelID["auth"])
                {
                    var msgType = dataArray[1].ToString();
                    if (msgType.Equals("ws"))   // wallet snapshot
                    {
                        var wallets = dataArray[2];
                        for (var wal = wallets.First; wal != null; wal = wal.Next)
                        {
                            if (wal[0].ToString().Equals("exchange"))
                            {
                                var currency = wal[1].ToString();
                                if (currency.Equals("USD"))
                                {
                                    BalanceFiat = (double)wal[2];
                                }
                                else if (currency.Equals(Symbol))
                                {
                                    BalanceSecurity = (double)wal[2];
                                }
                            }
                        }
                    }
                    else if (msgType.Equals("wu"))  // wallet update
                    {
                        // Only consider exchange wallets
                        if (dataArray[2][0].ToString().Equals("exchange"))
                        {
                            var currency = dataArray[2][1].ToString();
                            if (currency.Equals("USD"))
                            {
                                BalanceFiat = (double)dataArray[2][2];
                            }
                            else if (currency.Equals(Symbol))
                            {
                                BalanceSecurity = (double)dataArray[2][2];
                            }
                        }
                    }
                    else if (msgType.Equals("on"))  // new order confirmation
                    {
                        var order = new Order()
                        {
                            OrderID = dataArray[2][0].ToString(),
                            ClientOrderID = (long)dataArray[2][2],
                            Time = new DateTime(1970, 1, 1).AddMilliseconds((double)dataArray[2][4]),
                            Volume = Math.Abs((double)dataArray[2][7]),
                            FilledVolume = Math.Abs((double)dataArray[2][6] - (double)dataArray[2][7]),
                            Side = (double)dataArray[2][6] > 0 ? Side.BUY : Side.SELL,
                            OrderType = dataArray[2][8].ToString().Contains("LIMIT") ? OrdType.LIMIT : OrdType.MARKET
                        };
                        CurrentOrders.Add(order);
                        OrderSubmitCallback?.Invoke(false);
                        OrderSubmitCallback = null;
                        Log.Write($"Submitted {order}", 2);
                    }
                    else if (msgType.Equals("oc"))  // cancel order confirmation
                    {
                        string orderId = dataArray[2][0].ToString();
                        var order = CurrentOrders.FirstOrDefault(o => o.OrderID == orderId);
                        if (order != null)
                        {
                            CurrentOrders.Remove(order);
                            OrderCancelCallback?.Invoke(false);
                            OrderCancelCallback = null;

                            Log.Write($"Cancelled {order}", 2);
                        }
                        else
                        {
                            Log.Write($"Received cancellation message about unregistered {order}", 0);
                        }
                    }
                    else if (msgType.Equals("os"))  // order info snapshot
                    {
                        var orders = dataArray[2];
                        foreach (var order in orders)
                        {
                            string orderId = order[0].ToString();
                            // If order is not yet registered locally
                            if (!CurrentOrders.Any(o => o.OrderID.Equals(orderId)))
                            {
                                CurrentOrders.Add(new Order()
                                {
                                    OrderID = orderId,
                                    ClientOrderID = (long)order[2],
                                    Price = (double)order[12],
                                    Volume = Math.Abs((double)order[7]),
                                    FilledVolume = Math.Abs((double)order[6] - (double)order[7]),
                                    Side = (double)order[6] > 0 ? Side.BUY : Side.SELL,
                                    OrderType = order[8].ToString().Equals("LIMIT", StringComparison.OrdinalIgnoreCase) ? OrdType.LIMIT : OrdType.MARKET,
                                    Time = new DateTime(1970, 1, 1).AddMilliseconds((double)order[4]),
                                });
                            }
                        }
                    }
                    else if (msgType.Equals("ou"))  // order info update
                    {
                        string orderId = dataArray[2][0].ToString();
                        if (!CurrentOrders.Any(o => o.OrderID.Equals(orderId)))
                        {
                            var order = new Order()
                            {
                                OrderID = dataArray[2][0].ToString(),
                                ClientOrderID = (int)dataArray[2][2],
                                Time = new DateTime(1970, 1, 1).AddMilliseconds((double)dataArray[2][4]),
                                Volume = Math.Abs((double)dataArray[2][7]),
                                FilledVolume = Math.Abs((double)dataArray[2][6] - (double)dataArray[2][7]),
                                Side = (double)dataArray[2][6] > 0 ? Side.BUY : Side.SELL,
                                OrderType = dataArray[2][8].ToString().Equals("LIMIT", StringComparison.OrdinalIgnoreCase) ? OrdType.LIMIT : OrdType.MARKET
                            };
                        }
                    }
                    else if (msgType.Equals("tu"))  // trades
                    {
                        string orderId = dataArray[2][3].ToString();
                        var order = CurrentOrders.FirstOrDefault(o => o.OrderID.Equals(orderId));
                        if (order != null)
                        {
                            order.FilledVolume = (double)dataArray[2][4];
                        }
                        else
                        {
                            Log.Write($"Trade info about unregistered order", 1);
                        }
                    }
                    else if (msgType.Equals("n"))   // notification
                    {
                        string status = dataArray[2][6].ToString();
                        if (status.Equals("ERROR"))
                        {
                            Log.Write($"Error: {dataArray[2][7].ToString()}", 0);
                            string req = dataArray[2][1].ToString();
                            if (req.Equals("on-req"))   // new order error
                            {
                                OrderSubmitCallback?.Invoke(true);
                            }
                            else if (req.Equals("oc-req"))  // cancel order error
                            {
                                OrderCancelCallback?.Invoke(true);
                            }
                        }
                        else if (status.Equals("FAILURE"))
                        {
                            Log.Write($"Failure: {dataArray[2][7].ToString()}", 0);
                        }
                    }
                }
                else if (ChannelID.ContainsKey("trades") && channelID == ChannelID["trades"])
                {
                    if (dataArray[1] is JArray) // snapshot
                    {
                        var trades = dataArray[1];
                        var lastTrade = trades.First;
                        LastTrade = new Trade()
                        {
                            Price = (double)lastTrade[3],
                            Volume = (double)lastTrade[2]
                        };
                    }
                    else // update
                    {
                        if (dataArray[1].ToString().Equals("te"))
                        {
                            LastTrade = new Trade()
                            {
                                Price = (double)dataArray[2][3],
                                Volume = (double)dataArray[2][2]
                            };
                        }
                    }
                }
                else if (ChannelID.ContainsKey("book") && channelID == ChannelID["book"])
                {
                    // heartbeat
                    if (dataArray[1].ToString().Equals("hb"))
                    {

                    }
                    // Snapshot
                    else if (dataArray[1][0] is JArray)
                    {
                        foreach (var entry in dataArray[1])
                        {
                            int count = (int)entry[1];
                            if (count == 0)
                            {
                                Log.Write("Error: zero count in order book snapshot", 0);
                            }
                            else
                            {
                                double volume = (double)entry[2];
                                CurrentOrderBook.AddOrder((double)entry[0], volume, volume > 0 ? MDEntryType.BID : MDEntryType.OFFER);
                            }
                        }
                    }
                    // Update
                    else
                    {
                        int count = (int)dataArray[1][1];
                        if (count > 0)  // change bid
                        {
                            CurrentOrderBook.ChangeOrder((double)dataArray[1][0], (double)dataArray[1][2], MDEntryType.BID);
                        }
                        else // remove
                        {
                            CurrentOrderBook.RemoveOrder((double)dataArray[1][0], (double)dataArray[1][2] > 0 ? MDEntryType.BID : MDEntryType.OFFER);
                        }
                    }
                }
            }
            else
            {
                Log.Write($"Unknown message json type: {data.GetType()}", 0);
            }
        }

        private void SocketTerminal_OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Log.Write($"Connection error: {e.Exception.Message}", 0);
        }

        private void SocketTerminal_OnClose(object sender, EventArgs e)
        {
            Log.Write("Bitfinex connection closed", 1);
        }

        private void SocketTerminal_OnOpen(object sender, EventArgs e)
        {
            Log.Write("Bitfinex connection opened", 1);
            var lines = File.ReadAllLines(KeyFileName);

            // Subscribe to authenticated channel
            var authMsg = new AuthMessage(lines[0], lines[1]);
            var authMsgStr = JsonConvert.SerializeObject(authMsg);
            SocketTerminal.Send(authMsgStr);


            // Subscribe to the trade channel
            var subMsg = new SubscribeMessage() { Symbol = TradeSymbol, Channel = "trades" };
            var subMsgStr = JsonConvert.SerializeObject(subMsg);
            SocketTerminal.Send(subMsgStr);

            // Subscribe to the order book channel
            var subBookMsg = new SubOrderBookMessage() { Symbol = TradeSymbol, Length = "25" };
            var subBookMsgStr = JsonConvert.SerializeObject(subBookMsg);
            SocketTerminal.Send(subBookMsgStr);
        }

        private string BuildRequestMsg(WebsocketMessage subMsg, string msgType)
        {
            var reqMsg = new JArray();
            reqMsg.Add(new JValue(0));
            reqMsg.Add(new JValue(msgType));
            reqMsg.Add(null);
            reqMsg.Add(JObject.FromObject(subMsg));

            return reqMsg.ToString();
        }
    }
}
