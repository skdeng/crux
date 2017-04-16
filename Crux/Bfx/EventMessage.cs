using System.Runtime.Serialization;

namespace Crux.Bfx
{
    abstract class EventMessage : BaseMessage
    {
        [DataMember(Name = "event")]
        public string Event { get; protected set; }
    }
}
