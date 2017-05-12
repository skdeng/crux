using System;

namespace Crux
{
    public class Order : IEquatable<Order>
    {
        public long ClientOrderID { get; set; }
        public string OrderID { get; set; }
        public double Price { get; set; }
        public char Side { get; set; }
        public double Volume { get; set; }
        public double FilledVolume { get; set; }
        public char OrderType { get; set; }
        public DateTime Time { get; set; }

        public bool Equals(Order other)
        {
            return (ClientOrderID == other.ClientOrderID || other.OrderID == other.OrderID) &&
                    Price == other.Price &&
                    Volume == other.Volume &&
                    Side == other.Side;
        }

        public override string ToString()
        {
            return $"Order ({OrderID}|{ClientOrderID}): {(Side.Equals(QuickFix.Fields.Side.BUY) ? "BUY" : "SELL")} {FilledVolume.ToString("N4")}/{Volume.ToString("N4")} at {Price.ToString("N3")}$";
        }
    }
}
