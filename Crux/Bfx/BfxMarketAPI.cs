using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebSocket4Net;

namespace Crux.Bfx
{
    public class BfxMarketAPI : MarketAPI
    {
        private static string ConnectionString = "wss://api.bitfinex.com/ws/2";

        private static string KeyFileName;

        private WebSocket SocketTerminal;

        private Dictionary<string, int> ChannelID;

        private string Symbol;

        private string TradeSymbol;

        private double BalanceFiat = -1;

        private double BalanceSecurity = -1;

        private Trade LastTrade;

        private List<Order> CurrentOrders;

        private OrderOperationCallback OrderSubmitCallback;
        private OrderOperationCallback OrderCancelCallback;

        public BfxMarketAPI(string keyFile, string symbol)
        {
            KeyFileName = keyFile;
            ChannelID = new Dictionary<string, int>();
            ChannelID.Add("auth", 0);
            CurrentOrders = new List<Order>();

            Symbol = symbol;
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
        }

        public void CancelAllOrders()
        {
            var subCancelGroupOrder = new CancelGroupOrderMessage();
            var cancelGroupOrderMessage = BuildRequestMsg(subCancelGroupOrder, "oc_multi");
            SocketTerminal.Send(cancelGroupOrderMessage);
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
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
            throw new NotImplementedException();
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
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
                ClientOrderID = (int)subOrderMsg.ClientID
            };
            return order;
        }

        public bool Tick()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            throw new NotImplementedException();
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
                    if (msgType.Equals("ws"))   // wallet info
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
                    else if (msgType.Equals("on"))  // new order confirmation
                    {
                        var order = new Order()
                        {
                            OrderID = dataArray[0].ToString(),
                            ClientOrderID = (int)dataArray[2],
                            Time = (DateTime)dataArray[4],
                            Volume = Math.Abs((double)dataArray[6]),

                        };
                        OrderSubmitCallback?.Invoke(false);
                        OrderSubmitCallback = null;
                    }
                    else if (msgType.Equals("oc")) // cancel order confirmation
                    {
                        OrderCancelCallback?.Invoke(false);
                        OrderCancelCallback = null;
                    }
                }
                else if (channelID == ChannelID["trades"])
                {
                    var msgType = dataArray[1].ToString();
                    if (msgType.Equals("te"))       // update
                    {
                        LastTrade = new Trade()
                        {
                            Price = (double)dataArray[2][3],
                            Volume = (double)dataArray[2][2]
                        };
                    }
                    else if (!msgType.Equals("tu")) //snapshot
                    {
                        var trades = dataArray[1];
                        var lastTrade = trades.First;
                        LastTrade = new Trade()
                        {
                            Price = (double)lastTrade[3],
                            Volume = (double)lastTrade[2]
                        };
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
            var authMsg = new AuthMessage(lines[0], lines[1]);
            var authMsgStr = JsonConvert.SerializeObject(authMsg);
            SocketTerminal.Send(authMsgStr);

            var subMsg = new SubscribeMessage() { Symbol = TradeSymbol, Channel = "trades" };
            var subMsgStr = JsonConvert.SerializeObject(subMsg);
            SocketTerminal.Send(subMsgStr);
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
