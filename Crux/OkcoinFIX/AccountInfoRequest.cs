using QuickFix;
namespace Crux.OkcoinFIX
{
    class AccountInfoRequest : Message
    {
        public static string MSGTYPE = "Z1000";

        public AccountInfoRequest()
        {
            Header.SetField(new QuickFix.Fields.MsgType(MSGTYPE));
        }

        public void set(QuickFix.Fields.Account field)
        {
            SetField(field);
        }

        public void set(AccReqID field)
        {
            SetField(field);
        }
    }
}