using System;

namespace Crux
{
    public class Order : IEquatable<Order>
    {
        public int ClientOrderID { get; set; }
        public string OrderID { get; set; }
        public double Price { get; set; }
        public char Side { get; set; }
        public double Volume { get; set; }
        public char OrderType { get; set; }
        public DateTime Time { get; set; }

        public bool Equals(Order other)
        {
            return (ClientOrderID == other.ClientOrderID || other.OrderID == other.OrderID) &&
                    Price == other.Price &&
                    Volume == other.Volume &&
                    Side == other.Side &&
                    Time == other.Time;
        }
    }
}
