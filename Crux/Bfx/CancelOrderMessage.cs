using System.Runtime.Serialization;

namespace Crux.Bfx
{
    [DataContract]
    class CancelOrderMessage : BaseMessage
    {
        [DataMember(Name = "id")]
        public int ID { get; set; }

        [DataMember(Name = "cid_date")]
        public string Date { get; set; }

        public CancelOrderMessage(Order order)
        {
            ID = order.ClientOrderID;
            Date = order.Time.Date.ToString("yyyy-MM-dd");
        }
    }
}
