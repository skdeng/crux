using QuickFix.Fields;
using System.Threading;

namespace Crux
{
    public class OrderBook
    {
        public BookOrder BestBid { get; private set; } = null;
        public BookOrder BestOffer { get; private set; } = null;

        public double WorstBidPrice { get; private set; }
        public double WorstOfferPrice { get; private set; }

        public int NumBids { get; private set; } = 0;
        public int NumOffers { get; private set; } = 0;

        public Mutex BidLock = new Mutex();
        public Mutex OfferLock = new Mutex();

        public BookOrder GetBook(char side) { return side.Equals(MDEntryType.BID) ? BestBid : BestOffer; }

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

        public void RemoveOrder(double price, char side, double vol)
        {
            if (vol == 0)
            {
                return;
            }
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
                        //OfferLock.ReleaseMutex();
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
                current.Vol = vol;
            }
        }

        public double PriceDepth(double vol, char side)
        {
            double cumulativeVol = 0;
            BookOrder Start = GetBook(side);
            double prevPrice = side.Equals(MDEntryType.BID) ? BestBid.Price : BestOffer.Price;
            for (var current = Start; current != null; current = current.Next)
            {
                cumulativeVol += current.Vol;
                prevPrice = current.Price;
                if (cumulativeVol >= vol)
                {
                    break;
                }
            }
            return prevPrice;
        }

        public double CumulativeVolume(double price, char side)
        {
            double cumulativeVol = 0;
            if (side.Equals(MDEntryType.BID))
            {
                for (var current = BestBid; current != null && current.Price >= price; current = current.Next)
                {
                    cumulativeVol += current.Vol;
                }
            }
            else
            {
                for (var current = BestOffer; current != null && current.Price <= price; current = current.Next)
                {
                    cumulativeVol += current.Vol;
                }
            }
            return cumulativeVol;
        }
    }

    public class BookOrder
    {
        public BookOrder Next;
        public double Price;
        public double Vol;

        public BookOrder(double price, double vol, BookOrder next = null)
        {
            Price = price;
            Vol = vol;
            Next = next;
        }
    }
}
