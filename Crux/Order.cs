using System;

namespace Crux
{
    public class Order
    {
        public int ClientOrderID { get; set; }
        public int OrderID { get; set; }
        public double Price { get; set; }
        public char Side { get; set; }
        public double Volume { get; set; }
        public char OrderType { get; set; }
        public DateTime Time { get; set; }
    }
}
