using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    [DataContract]
    class CancelOrderMessage : WebsocketMessage
    {
        [DataMember(Name = "id")]
        public long ID { get; set; }

        [DataMember(Name = "cid_date")]
        public string Date { get; set; }

        public CancelOrderMessage(Order order)
        {
            ID = order.ClientOrderID;
            Date = order.Time.Date.ToString("yyyy-MM-dd");
        }
    }
}
