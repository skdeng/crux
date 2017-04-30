using System.Runtime.Serialization;

namespace Crux.OkcoinWS
{
    [DataContract]
    abstract class PublicSubMessage
    {
        [DataMember(Name = "event")]
        public string Event { get; set; }

        [DataMember(Name = "channel")]
        public string Channel { get; set; }

        public PublicSubMessage()
        {
            Event = "addChannel";
        }
    }
}
