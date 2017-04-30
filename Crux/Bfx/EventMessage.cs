using System.Runtime.Serialization;

namespace Crux.Bfx
{
    abstract class EventMessage : WebsocketMessage
    {
        [DataMember(Name = "event")]
        public string Event { get; protected set; }
    }
}
