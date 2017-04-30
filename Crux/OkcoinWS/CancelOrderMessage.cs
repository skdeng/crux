using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class CancelOrderMessage : PrivateSubMessage
    {
        public CancelOrderMessage(string symbol, string orderID)
        {
            Channel = "ok_spotusd_cancel_order";
            Parameters["api_key"] = APIKey;
            Parameters["sign"] = Sign;
            Parameters["symbol"] = symbol.Contains("LTC") ? "ltc_usd" : "btc_usd";
            Parameters["order_id"] = orderID.ToString();
        }
    }
}
