using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Crux.BfxWS
{
    [DataContract]
    class AuthMessage : EventMessage
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { set; get; }

        [DataMember(Name = "authSig")]
        public string AuthenticationSig { get; set; }

        [DataMember(Name = "authPayload")]
        public string AuthenticationPayload { get; set; }

        [DataMember(Name = "authNonce")]
        public string Nonce { get; private set; }

        public AuthMessage(string apiKey, string secretKey)
        {
            Event = "auth";
            Nonce = DateTime.Now.UnixTimestamp().ToString();
            AuthenticationPayload = "AUTH" + Nonce;
            var sig = new HMACSHA384(Encoding.Default.GetBytes(secretKey));
            AuthenticationSig = BitConverter.ToString(sig.ComputeHash(Encoding.Default.GetBytes(AuthenticationPayload))).Replace("-", string.Empty).ToLower();
            ApiKey = apiKey;
        }
    }
}
