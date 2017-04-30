using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class OrderInfoMessage : PrivateSubMessage
    {
        public OrderInfoMessage(string symbol, string orderID)
        {
            Channel = "ok_spotusd_orderinfo";
            Parameters["api_key"] = APIKey;
            Parameters["sign"] = Sign;
            Parameters["symbol"] = symbol.Contains("LTC") ? "ltc_usd" : "btc_usd";
            Parameters["order_id"] = orderID;
        }
    }
}
