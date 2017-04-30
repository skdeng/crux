using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class LoginMessage : PrivateMessage
    {
        public LoginMessage() : base()
        {
            Event = "login";
            Parameters["api_key"] = APIKey;
            Parameters["sign"] = Sign;
        }
    }
}
