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
                MarketMaker application = new MarketMaker();
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
            Environment.Exit(1);
        }
    }
}
