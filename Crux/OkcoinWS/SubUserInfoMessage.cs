using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class SubUserInfoMessage : PrivateSubMessage
    {
        public SubUserInfoMessage() : base()
        {
            Channel = "ok_spotusd_userinfo";
            Parameters["api_key"] = APIKey;
            Parameters["sign"] = Sign;
        }
    }
}
