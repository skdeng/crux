using Crux.BfxREST;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CruxTest.BfxREST
{
    [TestClass]
    public class BfxMarketRESTAPITest
    {
        [TestMethod]
        public void GetBalanceFiatTest()
        {
            BfxMarketRESTAPI api = new BfxMarketRESTAPI("../../../Keys/bfx.txt", "xmr");
            api.GetBalanceFiat();
        }

        [TestMethod]
        public void CancelOrderTest()
        {
            BfxMarketRESTAPI api = new BfxMarketRESTAPI("../../../Keys/bfx.txt", "xmr");
            api.CancelOrder(new Crux.Order() { OrderID = "446915287" });
        }
    }
}
