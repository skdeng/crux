using QuickFix.Fields;
using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class NewOrderMessage : PrivateSubMessage
    {
        public NewOrderMessage(string symbol, double price, double volume, char side, char type)
        {
            Channel = "ok_spotusd_trade";
            Parameters["api_key"] = APIKey;
            Parameters["symbol"] = symbol.Contains("LTC") ? "ltc_usd" : "btc_usd";
            Parameters["type"] = side == Side.BUY ? "buy" : "sell";
            if (type == OrdType.MARKET)
            {
                Parameters["type"] += "_market";
            }
            Parameters["price"] = price.ToString();
            Parameters["amount"] = volume.ToString();
            Parameters["sign"] = Sign;
        }
    }
}
