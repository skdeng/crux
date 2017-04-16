using Crux.Okcoin;
using System;

namespace Crux
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                QuickFix.SessionSettings settings = new QuickFix.SessionSettings("config/quickfix-client.cfg");
                OKCMarketAPI application = new OKCMarketAPI("../../../Keys/okc.txt", "BTC/USD", false, true);
                QuickFix.IMessageStoreFactory storeFactory = new QuickFix.FileStoreFactory(settings);
                QuickFix.ILogFactory logFactory = new QuickFix.ScreenLogFactory(settings);
                QuickFix.Transport.SocketInitiator initiator = new QuickFix.Transport.SocketInitiator(application, storeFactory, settings, logFactory);
                initiator.Start();
                Console.ReadKey();
                initiator.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ReadKey();
            }
        }
    }
}
