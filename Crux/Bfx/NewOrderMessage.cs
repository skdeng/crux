using QuickFix.Fields;
using System;
using System.Runtime.Serialization;

namespace Crux.Bfx
{
    [DataContract]
    class NewOrderMessage : WebsocketMessage
    {
        private static uint _OrderID = 0;
        private static uint FreeOrderID { get { return _OrderID++; } }

        [DataMember(Name = "gid")]
        public uint GroupID { get; set; }

        [DataMember(Name = "cid")]
        public uint ClientID { get; set; }

        [DataMember(Name = "type")]
        public string OrderType { get; set; }

        [DataMember(Name = "amount")]
        public double Volume { get; set; }

        [DataMember(Name = "price")]
        public double Price { get; set; }

        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }

        [DataMember(Name = "hidden")]
        public int Hidden { get; set; }

        public NewOrderMessage(string symbol, double price, double volume, char side, char type)
        {
            GroupID = 1;
            ClientID = FreeOrderID;
            if (type == OrdType.LIMIT)
            {
                OrderType = "LIMIT";
            }
            else if (type == OrdType.MARKET)
            {
                OrderType = "MARKET";
            }
            else
            {
                Log.Write($"Unknown order type {type}", 0);
                throw new Exception();
            }

            Volume = side == Side.BUY ? volume : -volume;
            Price = price;
            Symbol = symbol;
            Hidden = 0;
        }
    }
}
