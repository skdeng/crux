using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Crux.OkcoinREST
{
    public class OKCMarketRESTAPI : IMarketAPI
    {
        private string TradeSymbol;
        private string TradeSymbolString;

        private string APIKey;
        private string SecretKey;
        private static char[] HEX_DIGITS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private double BalanceUSD;
        private double BalanceBTC;
        private double BalanceLTC;
        private DateTime BalanceLastUpdate;
        private TimeSpan BalanceCacheTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyfile">A file with API key on the first line and secret key on the second line</param>
        /// <param name="symbol">Must contain the asset symbol in uppercase letters</param>
        public OKCMarketRESTAPI(string keyfile, string symbol)
        {
            var lines = File.ReadAllLines(keyfile);
            APIKey = lines[0];
            SecretKey = lines[1];

            TradeSymbol = symbol;
            TradeSymbolString = TradeSymbol.ToLower().Contains("ltc") ? "ltc_usd" : "btc_usd";
        }

        public void CancelAllOrders()
        {
            foreach (var order in GetActiveOrders())
            {
                CancelOrder(order);
            }
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            string endPoint = "https://www.okcoin.com/api/v1/cancel_order.do";
            RestClient client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["api_key"] = APIKey;
            param["symbol"] = TradeSymbolString;
            param["order_id"] = order.OrderID;

            var reqMsg = BuildSignedMessage(param);
            var json = client.MakeRequest(reqMsg);

            var response = (JObject)JsonConvert.DeserializeObject(json);
            if ((bool)response["result"])
            {
                Log.Write($"Cancel {order}", 2);
            }
            else
            {
                Log.Write($"Cancel order error: {response["error_code"]}", 0);
            }
            callback?.Invoke(!(bool)response["result"]);
        }

        public void Close()
        {
            // Nothing needs to be done
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            string endPoint = "https://www.okcoin.com/api/v1/order_info.do";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["api_key"] = APIKey;
            param["order_id"] = queryOrder?.OrderID ?? "-1";
            param["symbol"] = TradeSymbolString;

            var reqMsg = BuildSignedMessage(param);

            var json = client.MakeRequest(reqMsg);
            var data = (JObject)JsonConvert.DeserializeObject(json);

            List<Order> openOrders = new List<Order>();
            if ((bool)data["result"])
            {
                var orders = (JArray)data["orders"];
                foreach (var order in orders)
                {
                    DateTime orderTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    orderTime = orderTime.AddMilliseconds((long)order["create_date"]).ToLocalTime();
                    openOrders.Add(new Order
                    {
                        OrderID = order["order_id"].ToString(),
                        ClientOrderID = (int)order["order_id"],
                        Price = (double)order["price"],
                        Volume = (double)order["amount"],
                        FilledVolume = (double)order["deal_amount"],
                        Time = orderTime,
                        Side = order["type"].ToString().Contains("buy") ? Side.BUY : Side.SELL,
                        OrderType = order["type"].ToString().Contains("market") ? OrdType.MARKET : OrdType.LIMIT
                    });
                }
            }

            return openOrders;
        }

        public double GetBalanceFiat()
        {
            if ((DateTime.Now - BalanceLastUpdate) > BalanceCacheTime)
            {
                GetBalance();
            }
            return BalanceUSD;
        }

        public double GetBalanceSecurity()
        {
            if ((DateTime.Now - BalanceLastUpdate) > BalanceCacheTime)
            {
                GetBalance();
            }
            return TradeSymbolString.Equals("ltc_usd") ? BalanceLTC : BalanceBTC;
        }

        private void GetBalance()
        {
            string endPoint = "https://www.okcoin.com/api/v1/userinfo.do";
            RestClient client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["api_key"] = APIKey;

            var reqMsg = BuildSignedMessage(param);
            var json = client.MakeRequest(reqMsg);

            var response = (JObject)JsonConvert.DeserializeObject(json);
            BalanceUSD = (double)response["info"]["funds"]["free"]["usd"] + (double)response["info"]["funds"]["freezed"]["usd"];
            BalanceBTC = (double)response["info"]["funds"]["free"]["btc"] + (double)response["info"]["funds"]["freezed"]["btc"]; ;
            BalanceLTC = (double)response["info"]["funds"]["free"]["ltc"] + (double)response["info"]["funds"]["freezed"]["ltc"]; ;
            BalanceLastUpdate = DateTime.Now;
        }

        public IEnumerable<Candle> GetHistoricalPrices(TimePeriod timespan, int numPeriods)
        {
            string endPoint = "https://www.okcoin.com/api/v1/kline.do";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);
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
            var json = client.MakeRequest($"?symbol={TradeSymbolString}&type={type}&size={numPeriods}");
            var data = (JArray)JsonConvert.DeserializeObject(json);
            if (data.Count != numPeriods)
            {
                Log.Write($"Got the wrong number of historical prices. Asked: {numPeriods} | Received: {data.Count}", 0);
            }

            return data.Select(d => new Candle((double)d[1], (double)d[4], (double)d[2], (double)d[3], timespan));
        }

        public double GetLastPrice()
        {
            string endPoint = "https://www.okcoin.com/api/v1/ticker.do";
            RestClient client = new RestClient(endPoint, RestClient.HttpVerb.GET);

            string reqMsg = $"?symbol={TradeSymbolString}";
            var json = client.MakeRequest(reqMsg);

            var response = (JObject)JsonConvert.DeserializeObject(json);
            return (double)response["ticker"]["last"];
        }

        public OrderBook GetOrderBook()
        {
            string endPoint = "https://www.okcoin.com/api/v1/depth.do";
            RestClient client = new RestClient(endPoint, RestClient.HttpVerb.GET);

            string reqMsg = $"?symbol={TradeSymbolString}&size=5";
            var json = client.MakeRequest(reqMsg);

            var response = (JObject)JsonConvert.DeserializeObject(json);
            var asks = (JArray)response["asks"];
            var bids = (JArray)response["bids"];

            OrderBook orderBook = new OrderBook();
            foreach (var ask in asks)
            {
                orderBook.AddOrder((double)ask[0], (double)ask[1], MDEntryType.OFFER);
            }
            foreach (var bid in bids)
            {
                orderBook.AddOrder((double)bid[0], (double)bid[1], MDEntryType.BID);
            }

            return orderBook;
        }

        public bool IsReady()
        {
            return true;
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            string endPoint = "https://www.okcoin.com/api/v1/trade.do";
            RestClient client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            Dictionary<string, string> param = new Dictionary<string, string>();
            param["api_key"] = APIKey;
            param["symbol"] = TradeSymbolString;
            param["type"] = side == Side.BUY ? "buy" : "sell";
            if (type == OrdType.MARKET)
            {
                param["type"] += "_market";
            }
            param["price"] = price.ToString();
            param["amount"] = volume.ToString();

            var reqMsg = BuildSignedMessage(param);
            var json = client.MakeRequest(reqMsg);

            var response = (JObject)JsonConvert.DeserializeObject(json);
            var result = (bool)response["result"];

            Order order = null;
            if (result)
            {
                order = new Order()
                {
                    Price = price,
                    Volume = volume,
                    Time = DateTime.Now,
                    Side = side,
                    OrderType = type,
                    OrderID = response["order_id"].ToString(),
                    ClientOrderID = (long)response["order_id"]
                };
                Log.Write($"Submit {order}", 2);
            }
            else
            {
                Log.Write($"Submit order error: {response["errorcode"]}", 0);
            }
            callback?.Invoke(!result);

            return order;
        }

        private string BuildSignedMessage(Dictionary<string, string> param)
        {
            if (param.Count == 0)
            {
                return "";
            }

            var keys = new List<string>(param.Keys);
            keys.Sort();
            string paramString = "";
            for (int i = 0; i < keys.Count; i++)
            {
                paramString += $"{keys[i]}={param[keys[i]]}&";
            }

            var preMsg = paramString + $"secret_key={SecretKey}";

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            var hash = md5.ComputeHash(Encoding.Default.GetBytes(preMsg));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append($"{HEX_DIGITS[(hash[i] & 0xf0) >> 4]}{HEX_DIGITS[hash[i] & 0xf]}");
            }
            return $"?{paramString}&sign={sb.ToString()}";
        }
    }
}
