namespace Crux
{
    class Order
    {
        public string ClientOrderID { get; set; }
        public string OrderID { get; set; }
        public float Price { get; set; }
        public char Side { get; set; }
        public float Vol { get; set; }
    }
}
