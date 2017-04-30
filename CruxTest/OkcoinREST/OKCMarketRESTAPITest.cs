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
    }
}
