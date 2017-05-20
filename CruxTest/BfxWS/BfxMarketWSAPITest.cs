﻿using Crux.BfxWS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickFix.Fields;
using System.Diagnostics;
using System.Threading;

namespace CruxTest.BfxWS
{
    [TestClass]
    public class BfxMarketWSAPITest
    {
        [TestMethod]
        public void Connection()
        {
            BfxMarketWSAPI api = new BfxMarketWSAPI("../../../Keys/bfx.txt", "BTC");
            Thread.Sleep(1000);
            var fiatBalance = api.GetBalanceFiat();
            var securityBalance = api.GetBalanceSecurity();
            var lastPrice = api.GetLastPrice();

            Assert.IsTrue(fiatBalance >= 0, "Bad fiat balance");
            Assert.IsTrue(securityBalance >= 0, "Bad security balance");
            Assert.IsTrue(lastPrice > 0, "Price is not larger than zero");

            Trace.WriteLine($"Fiat balance: {api.GetBalanceFiat()}");
            Trace.WriteLine($"Security balance: {api.GetBalanceSecurity()}");
            Trace.WriteLine($"Last price: {lastPrice}");
            api.Close();
            Thread.Sleep(500);
        }

        [TestMethod]
        public void NewOrder()
        {
            BfxMarketWSAPI api = new BfxMarketWSAPI("../../../Keys/bfx.txt", "BTC");
            Thread.Sleep(1000);
            api.SubmitOrder(1000, 0.01, Side.BUY, OrdType.LIMIT);

            Thread.Sleep(1000);
            api.Close();
            Thread.Sleep(500);
        }

        [TestMethod]
        public void QueryOrders()
        {
            BfxMarketWSAPI api = new BfxMarketWSAPI("../../../Keys/bfx.txt", "BTC");
            Thread.Sleep(1000);
            var orders = api.GetActiveOrders();

            foreach (var o in orders)
            {
                Trace.WriteLine($"{o.ClientOrderID} | {o.Price} | {o.Volume}");
            }

            api.Close();
            Thread.Sleep(500);
        }

        [TestMethod]
        public void AddCancelOrders()
        {
            BfxMarketWSAPI api = new BfxMarketWSAPI("../../../Keys/bfx.txt", "BTC");
            while (!api.IsReady()) ;

            var order = api.SubmitOrder(1000, 0.01, Side.BUY, OrdType.LIMIT);
            Thread.Sleep(2000);
            api.CancelOrder(order);
        }
    }
}
