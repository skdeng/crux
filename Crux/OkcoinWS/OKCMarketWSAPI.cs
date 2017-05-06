using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocket4Net;

namespace Crux.OkcoinWS
{
    public class OKCMarketWSAPI : IMarketAPI
    {
        private string TradeSymbol;

        private readonly List<Order> CurrentOrders;
        private readonly OrderBook CurrentOrderBook;
        private Trade LastTrade;

        private double BalanceBTC;
        private double BalanceLTC;
        private double BalanceUSD;

        private WebSocket SocketTerminal;
        private string ConnectionString = "wss://real.okcoin.com:10440/websocket/okcoinapi";

        private bool TrackOrderBook;
        private bool TrackLiveTrade;

        private OrderOperationCallback OrderCancelCallback;
        private OrderOperationCallback OrderSubmitCallback;
        private AutoResetEvent NewOrderWaitHandle;
        private int NewOrderID;

        public OKCMarketWSAPI(string keyfile, string symbol, bool orderbook, bool livetrade)
        {
            PrivateMessage.LoadKeyfile(keyfile);

            TradeSymbol = symbol;
            CurrentOrders = new List<Order>();
            if (orderbook)
            {
                CurrentOrderBook = new OrderBook();
            }

            SocketTerminal = new WebSocket(ConnectionString);
            SocketTerminal.Opened += SocketTerminal_Opened;
            SocketTerminal.Closed += SocketTerminal_Closed;
            SocketTerminal.MessageReceived += SocketTerminal_MessageReceived;
            SocketTerminal.Error += SocketTerminal_Error;
            SocketTerminal.Open();

            NewOrderWaitHandle = new AutoResetEvent(false);
            OrderCancelCallback = null;
            OrderSubmitCallback = null;

            TrackOrderBook = orderbook;
            TrackLiveTrade = livetrade;
        }

        public void CancelAllOrders()
        {
            Log.Write("Cancelling all orders", 2);
            foreach (var order in CurrentOrders)
            {
                CancelOrder(order);
            }
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            OrderCancelCallback = callback;
            var msg = JsonConvert.SerializeObject(new CancelOrderMessage(TradeSymbol, order.OrderID));
            SocketTerminal.Send(msg);
            Log.Write($"Cancel {order}", 2);
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            return CurrentOrders;
        }

        public double GetBalanceFiat()
        {
            return BalanceUSD;
        }

        public double GetBalanceSecurity()
        {
            if (TradeSymbol.Contains("BTC"))
            {
                return BalanceBTC;
            }
            else if (TradeSymbol.Contains("LTC"))
            {
                return BalanceLTC;
            }
            else
            {
                Log.Write($"Broken symbol: {TradeSymbol}", 0);
                return 0;
            }
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            string endPoint = "https://www.okcoin.com/api/v1/kline.do";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);
            string symbol = TradeSymbol.Contains("LTC") ? "ltc_usd" : "btc_usd";
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

            return data.Select(d => new Candle((double)d[1], (double)d[4], (double)d[2], (double)d[3], timespan));
        }

        public double GetLastPrice()
        {
            return LastTrade.Price;
        }

        public OrderBook GetOrderBook()
        {
            return CurrentOrderBook;
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            OrderSubmitCallback = callback;
            var msg = new NewOrderMessage(TradeSymbol, price, volume, side, type);
            SocketTerminal.Send(JsonConvert.SerializeObject(msg));
            NewOrderWaitHandle.WaitOne();
            if (NewOrderID > 0)
            {
                var order = new Order()
                {
                    Price = price,
                    Volume = volume,
                    Side = side,
                    OrderType = type,
                    Time = DateTime.Now
                };
                order.ClientOrderID = NewOrderID;
                order.OrderID = NewOrderID.ToString();
                CurrentOrders.Add(order);
                Log.Write($"Submit {order}$", 2);
                return order;
            }
            else
            {
                return null;
            }
        }

        private void SocketTerminal_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Log.Write($"Websocket connection error: {e.Exception}", 0);
        }

        private void SocketTerminal_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var dataArray = (JArray)JsonConvert.DeserializeObject(e.Message);
            for (int i = 0; i < dataArray.Count; i++)
            {
                Log.Write($"Message received: {dataArray[i]}\n", 3);

                var channel = dataArray[i]["channel"].ToString();

                // wallet info
                if (channel.Equals("ok_sub_spotusd_userinfo"))
                {
                    BalanceUSD = (double)dataArray[i]["data"]["info"]["free"]["usd"];
                    BalanceBTC = (double)dataArray[i]["data"]["info"]["free"]["btc"];
                    BalanceLTC = (double)dataArray[i]["data"]["info"]["free"]["ltc"];
                }
                // wallet info
                else if (channel.Equals("ok_spotusd_userinfo"))
                {
                    BalanceUSD = (double)dataArray[i]["data"]["info"]["funds"]["free"]["usd"];
                    BalanceBTC = (double)dataArray[i]["data"]["info"]["funds"]["free"]["btc"];
                    BalanceLTC = (double)dataArray[i]["data"]["info"]["funds"]["free"]["ltc"];
                }
                // new order response
                else if (channel.Equals("ok_spotusd_trade"))
                {
                    var result = (bool)dataArray[i]["data"]["result"];
                    if (result)
                    {
                        NewOrderID = (int)dataArray[i]["data"]["order_id"];
                    }
                    else
                    {
                        NewOrderID = -1;
                        Log.Write($"Submit order error: {dataArray[i]["data"]["error_code"]}", 0);
                    }
                    NewOrderWaitHandle.Set();
                    OrderSubmitCallback?.Invoke(!result);
                    OrderSubmitCallback = null;
                }
                //cancel order response
                else if (channel.Equals("ok_spotusd_cancel_order"))
                {
                    var result = (bool)dataArray[i]["data"]["result"];
                    if (result)
                    {
                        CurrentOrders.RemoveAll(o => o.OrderID.Equals(dataArray[i]["data"]["order_id"].ToString()));
                    }
                    else
                    {
                        Log.Write($"Cancel order error: {dataArray[i]["data"]["error_code"]}", 0);
                    }
                    NewOrderWaitHandle.Set();
                    OrderCancelCallback?.Invoke(!result);
                    OrderSubmitCallback = null;
                }
                // order book data
                else if (channel.Equals("ok_sub_spot_btc_depth") || channel.Equals("ok_sub_spot_ltc_depth"))
                {
                    var bids = (JArray)dataArray[i]["data"]["bids"];
                    foreach (JArray bid in bids)
                    {
                        if ((double)bid[1] > 0.0001)
                        {
                            CurrentOrderBook.ChangeOrder((double)bid[0], (double)bid[1], MDEntryType.BID);
                        }
                        else
                        {
                            CurrentOrderBook.RemoveOrder((double)bid[0], MDEntryType.BID);
                        }
                    }
                    var asks = (JArray)dataArray[i]["data"]["asks"];
                    foreach (JArray ask in asks)
                    {
                        if ((double)ask[1] > 0.0001)
                        {
                            CurrentOrderBook.ChangeOrder((double)ask[0], (double)ask[1], MDEntryType.OFFER);
                        }
                        else
                        {
                            CurrentOrderBook.RemoveOrder((double)ask[0], MDEntryType.OFFER);
                        }
                    }
                }
                // live trade data
                else if (channel.Equals("ok_sub_spotusd_btc_trades") || channel.Equals("ok_sub_spotusd_ltc_trades"))
                {
                    var trade = (JArray)dataArray[i]["data"];
                    LastTrade = new Trade()
                    {
                        Price = (double)trade.Last[1],
                        Volume = (double)trade.Last[2]
                    };
                }
                // order info
                else if (channel.Equals("ok_spotusd_orderinfo"))
                {
                    if ((bool)dataArray[i]["data"]["result"])
                    {
                        var orders = (JArray)dataArray[i]["data"]["orders"];
                        foreach (var order in orders)
                        {
                            // add to existing order if it's not already there
                            if (!CurrentOrders.Exists(o => o.OrderID.Equals(order["order_id"].ToString())) && ((int)order["status"] == 0) || (int)order["status"] == 1)
                            {
                                CurrentOrders.Add(new Order()
                                {
                                    OrderID = order["order_id"].ToString(),
                                    Time = (DateTime)order["create_date"],
                                    Price = (double)order["price"],
                                    Volume = (double)order["amount"],
                                    Side = order["type"].ToString().Contains("buy") ? Side.BUY : Side.SELL,
                                    OrderType = order["type"].ToString().Contains("market") ? OrdType.MARKET : OrdType.LIMIT
                                });
                            }
                        }
                    }
                    else
                    {
                        Log.Write($"Error in requesting order info: {dataArray[i]["data"]}", 0);
                    }
                }
                // sub order/trade info
                else if (channel.Equals("ok_sub_spotusd_trades"))
                {
                    var status = (int)dataArray[i]["data"]["status"];
                    switch (status)
                    {
                        case -1:    // cancelled
                        case 2:     // filled
                            CurrentOrders.RemoveAll(o => o.OrderID.Equals(dataArray[i]["data"]["orderId"]));
                            break;
                        case 1:     // partial fill
                            CurrentOrders.First(o => o.OrderID.Equals(dataArray[i]["data"]["orderId"])).FilledVolume = (double)dataArray[i]["data"]["completedTradeAmount"];
                            break;
                    }
                }
            }
        }

        private void SocketTerminal_Closed(object sender, EventArgs e)
        {
            Log.Write($"Websocket connection closed", 1);
        }

        private void SocketTerminal_Opened(object sender, EventArgs e)
        {
            Log.Write($"Websocket connection opened", 3);

            // Login
            var loginMsg = new LoginMessage();
            SocketTerminal.Send(JsonConvert.SerializeObject(loginMsg));

            // Sub to user info
            var subUserInfoMsg = new SubUserInfoMessage();
            SocketTerminal.Send(JsonConvert.SerializeObject(subUserInfoMsg));

            // Sub to order book
            if (TrackOrderBook)
            {
                var subMarketDepthMsg = new SubMarketDepthMessage(TradeSymbol);
                var reqStr = JsonConvert.SerializeObject(subMarketDepthMsg);
                SocketTerminal.Send(JsonConvert.SerializeObject(subMarketDepthMsg));
            }

            // Sub to live trades
            if (TrackLiveTrade)
            {
                var subLiveTradeMsg = new SubLiveTradeMessage(TradeSymbol);
                SocketTerminal.Send(JsonConvert.SerializeObject(subLiveTradeMsg));
            }

            GetOpenOrders();
        }

        private void GetOpenOrders()
        {
            string endPoint = "https://www.okcoin.com/api/v1/order_history.do";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            var preMsg = $"api_key={PrivateMessage.APIKey}&current_page=0&page_length=200&status=0&symbol={(TradeSymbol.Contains("LTC") ? "ltc_usd" : "btc_usd")}";
            var toSign = preMsg + $"&secret_key={PrivateMessage.SecretKey}";
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            var hash = md5.ComputeHash(Encoding.Default.GetBytes(toSign));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append($"{PrivateMessage.HEX_DIGITS[(hash[i] & 0xf0) >> 4]}{PrivateMessage.HEX_DIGITS[hash[i] & 0xf]}");
            }
            var reqMsg = $"?{preMsg}&sign={sb.ToString()}";

            var json = client.MakeRequest(reqMsg);
            var data = (JObject)JsonConvert.DeserializeObject(json);

            if ((bool)data["result"])
            {
                var orders = (JArray)data["orders"];
                foreach (var order in orders)
                {
                    DateTime orderTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    orderTime = orderTime.AddMilliseconds((long)order["create_date"]).ToLocalTime();
                    CurrentOrders.Add(new Order
                    {
                        OrderID = order["order_id"].ToString(),
                        ClientOrderID = (int)order["order_id"],
                        Price = (double)order["price"],
                        Volume = (double)order["amount"],
                        FilledVolume = (double)order["deal_amount"],
                        Time = orderTime,
                        Side = order["type"].Contains("buy") ? Side.BUY : Side.SELL,
                        OrderType = order["type"].Contains("market") ? OrdType.MARKET : OrdType.LIMIT
                    });
                }
            }
        }
    }
}
