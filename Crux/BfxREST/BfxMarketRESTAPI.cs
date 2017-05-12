using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Crux.BfxREST
{
    public class BfxMarketRESTAPI : IMarketAPI
    {
        private string Symbol { get; set; }
        private string TradeSymbol { get; set; }
        private string TradeSymbolV2 { get; set; }

        private double BalanceFiat;
        private double BalanceSecurity;
        private DateTime BalanceLastUpdate;
        private TimeSpan BalanceCacheTime = TimeSpan.FromSeconds(5);

        private string APIKey { get; set; }
        private string SecretKey { get; set; }

        public BfxMarketRESTAPI(string keyFile, string symbol)
        {
            var lines = File.ReadAllLines(keyFile);
            APIKey = lines[0];
            SecretKey = lines[1];

            Symbol = symbol;
            TradeSymbol = $"{symbol.ToLower()}usd";
            TradeSymbolV2 = $"t{symbol.ToUpper()}USD";
        }

        public void CancelAllOrders()
        {
            string endPoint = "https://api.bitfinex.com/v1/order/cancel/all";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            string[] headers;
            BuildSignedMessage("/v1/order/cancel/all", new string[0], out headers);

            try
            {
                var json = client.MakeRequest("", headers, false);
            }
            catch
            {
            }
        }

        public void CancelOrder(Order order, OrderOperationCallback callback = null)
        {
            string endPoint = "https://api.bitfinex.com/v1/order/cancel";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            string[] headers;
            string[] param = new string[]
            {
                $"\"order_id\":\"{order.OrderID}\""
            };
            BuildSignedMessage("/v1/order/cancel", param, out headers);

            try
            {
                var json = client.MakeRequest("", headers, false);
                var response = (JObject)JsonConvert.DeserializeObject(json);
                Log.Write($"Cancelled {order}", 2);
                callback?.Invoke(false);
            }
            catch (Exception e)
            {
                Log.Write($"Failed to cancel {order}", 0);
                Log.Write($"Exception: {e}", 0);
                callback?.Invoke(true);
            }
        }

        public List<Order> GetActiveOrders(Order queryOrder = null)
        {
            string endPoint;
            string[] param;
            if (queryOrder == null)
            {
                endPoint = "/v1/orders";
                param = new string[0];
            }
            else
            {
                endPoint = "/v1/order/status";
                param = new string[] { $"\"order_id\":{queryOrder.OrderID}" };
            }
            string url = "https://api.bitfinex.com";
            var client = new RestClient(url + endPoint, RestClient.HttpVerb.POST);

            string[] headers;
            BuildSignedMessage(endPoint, param, out headers);

            try
            {
                var json = client.MakeRequest("", headers, false);

                if (queryOrder == null)
                {
                    var response = (JArray)JsonConvert.DeserializeObject(json);
                    return response.Select(order => new Order()
                    {
                        OrderID = order["id"].ToString(),
                        ClientOrderID = (long)order["id"],
                        Price = (double)order["price"],
                        Side = order["side"].ToString().Equals("buy", StringComparison.OrdinalIgnoreCase) ? Side.BUY : Side.SELL,
                        OrderType = order["type"].ToString().Contains("limit") ? OrdType.LIMIT : OrdType.MARKET,
                        Volume = (double)order["original_amount"],
                        FilledVolume = (double)order["executed_amount"],
                        Time = new DateTime(1970, 1, 1).AddMilliseconds((double)order["timestamp"])
                    }).ToList();
                }
                else
                {
                    var order = (JObject)JsonConvert.DeserializeObject(json);
                    return new List<Order>() { new Order()
                    {
                        OrderID = order["id"].ToString(),
                        ClientOrderID = (long)order["id"],
                        Price = (double)order["price"],
                        Side = order["side"].ToString().Equals("buy", StringComparison.OrdinalIgnoreCase) ? Side.BUY : Side.SELL,
                        OrderType = order["type"].ToString().Contains("limit") ? OrdType.LIMIT : OrdType.MARKET,
                        Volume = (double)order["original_amount"],
                        FilledVolume = (double)order["executed_amount"],
                        Time = new DateTime(1970, 1, 1).AddMilliseconds((double)order["timestamp"])
                    } };
                }
            }
            catch (Exception e)
            {
                Log.Write($"Failed to get active orders", 0);
                Log.Write($"Exception: {e}", 0);
                return new List<Order>();
            }
        }

        public double GetBalanceFiat()
        {
            if ((DateTime.Now - BalanceLastUpdate) > BalanceCacheTime)
            {
                GetBalance();
            }
            return BalanceFiat;
        }

        public double GetBalanceSecurity()
        {
            if ((DateTime.Now - BalanceLastUpdate) > BalanceCacheTime)
            {
                GetBalance();
            }
            return BalanceSecurity;
        }

        private void GetBalance()
        {
            string endPoint = $"https://api.bitfinex.com/v1/balances";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            string[] headers;
            BuildSignedMessage("/v1/balances", new string[0], out headers);

            try
            {
                var json = client.MakeRequest("", headers);

                var response = (JArray)JsonConvert.DeserializeObject(json);

                foreach (var data in response)
                {
                    if (data["type"].ToString().Equals("exchange", StringComparison.OrdinalIgnoreCase))
                    {
                        string currency = data["currency"].ToString();
                        if (currency.Equals("usd", StringComparison.OrdinalIgnoreCase))
                        {
                            BalanceFiat = (double)data["amount"];
                        }
                        else if (currency.Equals(Symbol, StringComparison.OrdinalIgnoreCase))
                        {
                            BalanceSecurity = (double)data["amount"];
                        }
                    }
                }

                BalanceLastUpdate = DateTime.Now;
            }
            catch
            {
                Log.Write($"Error in GetBalance", 0);
            }
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
            string endPoint = $"https://api.bitfinex.com/v2/candles/trade:{timeFrameString}:{TradeSymbolV2}/hist";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);
            var json = client.MakeRequest($"?limit={numPeriods}");
            var response = (JArray)JsonConvert.DeserializeObject(json);
            if (response.Count != numPeriods)
            {
                Log.Write($"Got the wrong number of historical prices. Asked: {numPeriods} | Received: {response.Count}", 0);
            }

            return response.Select(d => new Candle((double)d[1], (double)d[2], (double)d[3], (double)d[4], timespan));
        }

        public double GetLastPrice()
        {
            string endPoint = $"https://api.bitfinex.com/v1/trades/{TradeSymbol}";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);

            var json = client.MakeRequest("?limit_trades=1", null, false);
            var response = (JArray)JsonConvert.DeserializeObject(json);
            return (double)response[0]["price"];
        }

        public OrderBook GetOrderBook()
        {
            var endPoint = $"https://api.bitfinex.com/v1/book/{TradeSymbol}";
            var client = new RestClient(endPoint, RestClient.HttpVerb.GET);

            var json = client.MakeRequest("?limit_bids=2&limit_asks=2&group=1", null, false);
            var response = (JObject)JsonConvert.DeserializeObject(json);

            OrderBook orderBook = new OrderBook();
            foreach (var data in response["bids"])
            {
                orderBook.AddOrder((double)data["price"], (double)data["amount"], MDEntryType.BID);
            }
            foreach (var data in response["asks"])
            {
                orderBook.AddOrder((double)data["price"], (double)data["amount"], MDEntryType.OFFER);
            }
            return orderBook;
        }

        public Order SubmitOrder(double price, double volume, char side, char type, OrderOperationCallback callback = null)
        {
            string endPoint = "https://api.bitfinex.com/v1/order/new";
            var client = new RestClient(endPoint, RestClient.HttpVerb.POST);

            string[] headers;
            string[] param = new string[]
            {
                $"\"symbol\":\"{TradeSymbol}\"",
                $"\"amount\":\"{volume}\"",
                $"\"price\":\"{price}\"",
                $"\"side\":\"{(side.Equals(Side.BUY) ? "buy" : "sell")}\"",
                $"\"type\":\"exchange {(type.Equals(OrdType.LIMIT) ? "limit" : "market")}\"",
                "\"is_hidden\":false",
                "\"is_postonly\":false",
                "\"use_all_available\":false",
                "\"ocoorder\":false",
                "\"buy_price_oco\":\"0\"",
                "\"sell_price_oco\":\"0\"",
            };
            BuildSignedMessage("/v1/order/new", param, out headers);

            try
            {
                var json = client.MakeRequest("", headers, false);
                var response = (JObject)JsonConvert.DeserializeObject(json);

                Order order = new Order()
                {
                    OrderID = response["id"].ToString(),
                    ClientOrderID = (long)response["id"],
                    Price = price,
                    Volume = volume,
                    Side = side,
                    OrderType = type,
                    Time = new DateTime(1970, 1, 1).AddMilliseconds((double)response["timestamp"])
                };
                Log.Write($"Submitted {order}", 2);

                callback?.Invoke(false);
                return order;
            }
            catch (Exception e)
            {
                Log.Write($"Failed to submit order", 0);
                Log.Write($"Exception: {e}", 0);
                callback?.Invoke(true);
                return null;
            }
        }

        public void BuildSignedMessage(string endPoint, string[] parameters, out string[] headers)
        {
            var nonce = ((uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
            var paramString = $"{{\"request\":\"{endPoint}\",\"nonce\":\"{nonce}\"";
            foreach (var p in parameters)
            {
                paramString += $",{p}";
            }
            paramString += "}";

            Log.Write($"Param string: {paramString}", 3);

            var paramData = Convert.ToBase64String(Encoding.Default.GetBytes(paramString));

            var signer = new HMACSHA384(Encoding.Default.GetBytes(SecretKey));
            var signature = BitConverter.ToString(signer.ComputeHash(Encoding.Default.GetBytes(paramData))).Replace("-", string.Empty).ToLower();
            headers = new string[] { $"X-BFX-APIKEY:{APIKey}", $"X-BFX-PAYLOAD:{paramData}", $"X-BFX-SIGNATURE:{signature}" };
        }
    }
}
