using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class SubMarketDepthMessage : PublicSubMessage
    {
        public SubMarketDepthMessage(string symbol)
        {
            Channel = $"ok_sub_spot_{(symbol.Contains("LTC") ? "ltc" : "btc")}_depth";
        }
    }
}
