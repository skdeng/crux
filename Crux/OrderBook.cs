using QuickFix.Fields;
using System.Threading;

namespace Crux
{
    /// <summary>
    /// Limite orderbook
    /// </summary>
    public class OrderBook
    {
        /// <summary>
        /// Current best bid, must exist if the order book has 1 or more bids
        /// </summary>
        public BookOrder BestBid { get; private set; } = null;
        /// <summary>
        /// Current best offer, must exist if the order book has 1 or more offers
        /// </summary>
        public BookOrder BestOffer { get; private set; } = null;

        /// <summary>
        /// Price of the worst bid (lowest bid price)
        /// </summary>
        public double WorstBidPrice { get; private set; } = 0;
        /// <summary>
        /// Price of the worst offer (highest offer price)
        /// </summary>
        public double WorstOfferPrice { get; private set; } = 0;

        /// <summary>
        /// Current number of bids
        /// </summary>
        public int NumBids { get; private set; } = 0;
        /// <summary>
        /// Current number of offers
        /// </summary>
        public int NumOffers { get; private set; } = 0;

        /// <summary>
        /// Mutex for concurrent modification of the bid side
        /// </summary>
        public Mutex BidLock = new Mutex();
        /// <summary>
        /// Mutex for concurrent modification of the offer side
        /// </summary>
        public Mutex OfferLock = new Mutex();

        public BookOrder GetBook(char side) { return side.Equals(MDEntryType.BID) ? BestBid : BestOffer; }

        /// <summary>
        /// Add volume to the order book at the given price level
        /// </summary>
        /// <param name="price">Price level</param>
        /// <param name="vol">Volume at given price level</param>
        /// <param name="side">Side of price level</param>
        /// <remarks>No verification is made to check if the price and side are possible</remarks>
        public void AddOrder(double price, double vol, char side)
        {
            if (side.Equals(MDEntryType.BID))
            {
                BidLock.WaitOne();
                NumBids += 1;
                if (BestBid == null)
                {
                    BestBid = new BookOrder(price, vol);
                }
                else
                {
                    if (price > BestBid.Price)
                    {
                        BookOrder newBestBid = new BookOrder(price, vol, BestBid);
                        BestBid = newBestBid;
                    }
                    else
                    {
                        BookOrder current;
                        BookOrder prev = BestBid;
                        for (current = BestBid; current != null && current.Price > price; prev = current, current = current.Next) ;
                        BookOrder newOrder = new BookOrder(price, vol, prev.Next);
                        prev.Next = newOrder;
                    }
                }
                if (WorstBidPrice == 0 || WorstBidPrice > price)
                {
                    WorstBidPrice = price;
                }
                BidLock.ReleaseMutex();
            }
            else if (side.Equals(MDEntryType.OFFER))
            {
                OfferLock.WaitOne();
                NumOffers += 1;
                if (BestOffer == null)
                {
                    BestOffer = new BookOrder(price, vol);
                }
                else
                {
                    if (price < BestOffer.Price)
                    {
                        BookOrder newBestOffer = new BookOrder(price, vol, BestOffer);
                        BestOffer = newBestOffer;
                    }
                    else
                    {
                        BookOrder current;
                        BookOrder prev = BestOffer;
                        for (current = BestOffer; current != null && current.Price < price; prev = current, current = current.Next) ;
                        BookOrder newOrder = new BookOrder(price, vol, prev.Next);
                        prev.Next = newOrder;
                    }
                }
                OfferLock.ReleaseMutex();
                if (WorstOfferPrice == 0 || WorstOfferPrice < price)
                {
                    WorstOfferPrice = price;
                }
            }
            else
            {
                Log.Write($"Unknown MDEntryType: {side}", 0);
                return;
            }
        }

        /// <summary>
        /// Remove all volume from the order book at a given price level
        /// </summary>
        /// <param name="price">Price level</param>
        /// <param name="side">Side of the price level</param>
        public void RemoveOrder(double price, char side)
        {
            if (side.Equals(MDEntryType.BID))
            {
                BidLock.WaitOne();
                if (BestBid.Price == price) // remove first
                {
                    BestBid = BestBid.Next;
                }
                else if (price < WorstBidPrice)
                {
                    Log.Write("Not removing price outside orderbook", 3);
                    BidLock.ReleaseMutex();
                    return;
                }
                else // if not first
                {
                    BookOrder current;
                    BookOrder prev = BestBid;
                    for (current = BestBid; current != null && current.Price > price; prev = current, current = current.Next) ;
                    if (current == null || current.Price != price)
                    {
                        Log.Write("Error: Trying to remove inexistant book order", 3);
                        BidLock.ReleaseMutex();
                        return;
                    }
                    prev.Next = current.Next;
                    current = null;
                }
                NumBids -= 1;
                BidLock.ReleaseMutex();
            }
            else if (side.Equals(MDEntryType.OFFER))
            {
                OfferLock.WaitOne();
                if (BestOffer.Price == price) // remove first
                {
                    BestOffer = BestOffer.Next;
                }
                else if (price > WorstOfferPrice)
                {
                    Log.Write("Not removing price outside orderbook", 3);
                    OfferLock.ReleaseMutex();
                    return;
                }
                else // if not first
                {
                    BookOrder current;
                    BookOrder prev = BestOffer;
                    for (current = BestOffer; current != null && current.Price < price; prev = current, current = current.Next) ;
                    if (current == null || current.Price != price)
                    {
                        Log.Write("Error: Trying to remove inexistant book order", 3);
                        OfferLock.ReleaseMutex();
                        return;
                    }
                    prev.Next = current.Next;
                    current = null;
                }
                NumOffers -= 1;
                OfferLock.ReleaseMutex();
            }
            else
            {
                Log.Write($"Unknown MDEntryType: {side}", 0);
                return;
            }
        }

        /// <summary>
        /// Change the size of depth at a certain price. If there is no volume at a certain price, the depth is added
        /// </summary>
        /// <param name="price">Prive level to change</param>
        /// <param name="vol">New volume at the given price level</param>
        /// <param name="side">Side of the price level</param>
        public void ChangeOrder(double price, double vol, char side)
        {
            BookOrder current = null;
            BookOrder prev = null;
            Mutex currentLock = null;
            if (side.Equals(MDEntryType.BID))
            {
                currentLock = BidLock;
                currentLock.WaitOne();
                for (current = BestBid, prev = BestBid; current != null && price < current.Price; prev = current, current = current.Next) ;
            }
            else
            {
                currentLock = OfferLock;
                currentLock.WaitOne();
                for (current = BestOffer, prev = BestOffer; current != null && price > current.Price; prev = current, current = current.Next) ;
            }
            currentLock.ReleaseMutex();

            if (current == null || current.Price != price)
            {
                AddOrder(price, vol, side);
            }
            else
            {
                current.Volume = vol;
            }
        }

        /// <summary>
        /// Get price depth if a certain volume is traded on one side
        /// i.e. What would be the worst price if the desired volume is traded on the given side
        /// </summary>
        /// <param name="vol">Volume traded</param>
        /// <param name="side">Side of the trade</param>
        /// <returns>Most extreme price traded</returns>
        /// <remarks>This function does not return the best bid/offer after the trade but the price of the last trade. This is significant in cases where the last trade exhausts all volume at a certain price level and best bid/offer falls to the next price level</remarks>
        public double PriceDepth(double vol, char side)
        {
            double cumulativeVol = 0;
            BookOrder Start = GetBook(side);
            double prevPrice = side.Equals(MDEntryType.BID) ? BestBid.Price : BestOffer.Price;
            for (var current = Start; current != null; current = current.Next)
            {
                cumulativeVol += current.Volume;
                prevPrice = current.Price;
                if (cumulativeVol >= vol)
                {
                    break;
                }
            }
            return prevPrice;
        }

        /// <summary>
        /// Cumulative volume at a certain price level
        /// i.e. How much volume has to be traded to reach the given price on the given side
        /// </summary>
        /// <param name="price">Desired price level</param>
        /// <param name="side">Side of the trade</param>
        /// <returns>Cumulative volume at the given price level</returns>
        public double CumulativeVolume(double price, char side)
        {
            double cumulativeVol = 0;
            if (side.Equals(MDEntryType.BID))
            {
                for (var current = BestBid; current != null && current.Price >= price; current = current.Next)
                {
                    cumulativeVol += current.Volume;
                }
            }
            else
            {
                for (var current = BestOffer; current != null && current.Price <= price; current = current.Next)
                {
                    cumulativeVol += current.Volume;
                }
            }
            return cumulativeVol;
        }
    }

    /// <summary>
    /// Represent a single price level in the order book
    /// </summary>
    public class BookOrder
    {
        /// <summary>
        /// Next best order this side of the order book
        /// </summary>
        public BookOrder Next;
        /// <summary>
        /// Price level
        /// </summary>
        public double Price;
        /// <summary>
        /// Volume at this price level
        /// </summary>
        public double Volume;

        public BookOrder(double price, double vol, BookOrder next = null)
        {
            Price = price;
            Volume = vol;
            Next = next;
        }
    }
}
