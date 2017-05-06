using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    abstract class EventMessage : WebsocketMessage
    {
        [DataMember(Name = "event")]
        public string Event { get; protected set; }
    }
}
