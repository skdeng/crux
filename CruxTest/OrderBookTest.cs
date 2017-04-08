using Crux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickFix.Fields;

namespace CruxTest
{
    [TestClass]
    public class OrderBookTest
    {
        [TestMethod]
        public void AddBids()
        {
            OrderBook book = new OrderBook();
            book.AddOrder(10, 1, MDEntryType.BID);
            book.AddOrder(11, 2, MDEntryType.BID);
            book.AddOrder(12, 3, MDEntryType.BID);

            Assert.AreEqual(12, book.BestBid.Price);
            Assert.AreEqual(3, book.BestBid.Vol);
            Assert.AreEqual(11, book.BestBid.Next.Price);
            Assert.AreEqual(2, book.BestBid.Next.Vol);

            Assert.AreEqual(3, book.NumBids);

            Assert.AreEqual(3, book.CumulativeVolume(12, MDEntryType.BID));
            Assert.AreEqual(6, book.CumulativeVolume(10, MDEntryType.BID));

            Assert.AreEqual(11, book.PriceDepth(4, MDEntryType.BID));
            Assert.AreEqual(12, book.PriceDepth(3, MDEntryType.BID));

            Assert.IsNull(book.BestOffer);
        }

        [TestMethod]
        public void AddOffers()
        {
            OrderBook book = new OrderBook();
            book.AddOrder(10, 1, MDEntryType.OFFER);
            book.AddOrder(11, 2, MDEntryType.OFFER);
            book.AddOrder(12, 3, MDEntryType.OFFER);

            Assert.AreEqual(10, book.BestOffer.Price);
            Assert.AreEqual(1, book.BestOffer.Vol);
            Assert.AreEqual(11, book.BestOffer.Next.Price);
            Assert.AreEqual(2, book.BestOffer.Next.Vol);

            Assert.AreEqual(3, book.NumOffers);

            Assert.AreEqual(6, book.CumulativeVolume(12, MDEntryType.OFFER));
            Assert.AreEqual(1, book.CumulativeVolume(10, MDEntryType.OFFER));

            Assert.AreEqual(11, book.PriceDepth(2, MDEntryType.OFFER));
            Assert.AreEqual(12, book.PriceDepth(5, MDEntryType.OFFER));

            Assert.IsNull(book.BestBid);
        }

        [TestMethod]
        public void RemoveBid()
        {
            OrderBook book = new OrderBook();
            book.AddOrder(10, 1, MDEntryType.BID);
            book.AddOrder(11, 2, MDEntryType.BID);
            book.AddOrder(12, 3, MDEntryType.BID);

            book.RemoveOrder(12, MDEntryType.BID, 3);

            Assert.AreEqual(11, book.BestBid.Price);
            Assert.AreEqual(3, book.CumulativeVolume(10, MDEntryType.BID));
        }

        [TestMethod]
        public void RemoveOffer()
        {
            OrderBook book = new OrderBook();
            book.AddOrder(10, 1, MDEntryType.OFFER);
            book.AddOrder(11, 2, MDEntryType.OFFER);
            book.AddOrder(12, 3, MDEntryType.OFFER);

            book.RemoveOrder(10, MDEntryType.OFFER, 1);

            Assert.AreEqual(11, book.BestOffer.Price);
            Assert.AreEqual(5, book.CumulativeVolume(12, MDEntryType.OFFER));
        }

        [TestMethod]
        public void ChangeBid()
        {
            OrderBook book = new OrderBook();
            book.AddOrder(10, 1, MDEntryType.BID);
            book.AddOrder(11, 2, MDEntryType.BID);
            book.AddOrder(12, 3, MDEntryType.BID);

            book.ChangeOrder(12.5, 4, MDEntryType.BID);

            Assert.AreEqual(12.5, book.BestBid.Price);
            Assert.AreEqual(4, book.BestBid.Vol);
            Assert.AreEqual(12, book.BestBid.Next.Price);
            Assert.AreEqual(3, book.BestBid.Next.Vol);

            Assert.AreEqual(4, book.NumBids);

            Assert.AreEqual(7, book.CumulativeVolume(11.2, MDEntryType.BID));
            Assert.AreEqual(10, book.CumulativeVolume(10, MDEntryType.BID));

            Assert.IsNull(book.BestOffer);
        }

        [TestMethod]
        public void ChangeOffer()
        {
            OrderBook book = new OrderBook();
            book.ChangeOrder(10, 1, MDEntryType.BID);
        }
    }
}
