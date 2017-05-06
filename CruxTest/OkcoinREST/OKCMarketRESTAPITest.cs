using Crux.OkcoinREST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace CruxTest.OkcoinREST
{
    [TestClass]
    public class OKCMarketRESTAPITest
    {
        [TestMethod]
        public void GetActiveOrdersTest()
        {
            OKCMarketRESTAPI api = new OKCMarketRESTAPI("../../../Keys/okc.txt", "LTC/USD");
            var orders = api.GetActiveOrders();
            foreach (var o in orders)
            {
                Trace.WriteLine(o);
            }
        }

        [TestMethod]
        public void GetOrderBookTest()
        {
            OKCMarketRESTAPI api = new OKCMarketRESTAPI("../../../Keys/okc.txt", "LTC/USD");
            var orderbook = api.GetOrderBook();
            Trace.WriteLine($"{orderbook.BestBid.Price} {orderbook.BestBid.Volume}");
            Trace.WriteLine($"{orderbook.BestOffer.Price} {orderbook.BestOffer.Volume}");
        }
    }
}
