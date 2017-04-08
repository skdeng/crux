namespace Crux
{
    public class AccountUtil
    {
        public static string APIKey = "c35dd7ab-d556-41c4-9c25-aa376bfb20b5";
        public static string SecretKey = "2D8B66B54E28DDE64DE85751CA20C769";
        public static QuickFix.Fields.Account Account = new QuickFix.Fields.Account($"{APIKey},{SecretKey}");
    }
}
