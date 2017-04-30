using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    class PrivateSubMessage : PrivateMessage
    {
        [DataMember(Name = "channel")]
        public string Channel { get; set; }

        public PrivateSubMessage() : base()
        {
            Event = "addChannel";
        }
    }
}
