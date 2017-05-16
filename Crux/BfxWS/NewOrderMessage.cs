using QuickFix.Fields;
using System;
using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    [DataContract]
    class NewOrderMessage : WebsocketMessage
    {
        [DataMember(Name = "gid")]
        public uint GroupID { get; set; }

        [DataMember(Name = "cid")]
        public long ClientID { get; set; }

        [DataMember(Name = "type")]
        public string OrderType { get; set; }

        [DataMember(Name = "amount")]
        public string Volume { get; set; }

        [DataMember(Name = "price")]
        public string Price { get; set; }

        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }

        [DataMember(Name = "hidden")]
        public int Hidden { get; set; }

        public NewOrderMessage(string symbol, double price, double volume, char side, char type)
        {
            GroupID = 1;
            ClientID = DateTime.Now.UnixTimestamp();
            if (type == OrdType.LIMIT)
            {
                OrderType = "EXCHANGE LIMIT";
            }
            else
            {
                OrderType = "EXCHANGE MARKET";
            }

            Volume = side == Side.BUY ? volume.ToString() : (-volume).ToString();
            Price = price.ToString();
            Symbol = symbol;
            Hidden = 0;
        }
    }
}
