using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    [DataContract]
    class CancelOrderMessage : WebsocketMessage
    {
        [DataMember(Name = "id")]
        public long ID { get; set; }

        public CancelOrderMessage(Order order)
        {
            ID = long.Parse(order.OrderID);
        }
    }
}
