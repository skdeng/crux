using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class SubLiveTradeMessage : PublicSubMessage
    {
        public SubLiveTradeMessage(string symbol)
        {
            Channel = $"ok_sub_spotusd_{(symbol.Contains("LTC") ? "ltc" : "btc")}_trades";
        }
    }
}
