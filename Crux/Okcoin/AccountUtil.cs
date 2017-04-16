using System.IO;

namespace Crux.Okcoin
{
    public class AccountUtil
    {
        public static void ReadKeyFile(string filename)
        {
            var lines = File.ReadAllLines(filename);
            APIKey = lines[0];
            SecretKey = lines[1];
            Account = new QuickFix.Fields.Account($"{APIKey},{SecretKey}");
        }

        public static string APIKey;
        public static string SecretKey;
        public static QuickFix.Fields.Account Account;
    }
}