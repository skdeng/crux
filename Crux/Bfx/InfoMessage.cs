using System.Runtime.Serialization;

namespace Crux.Bfx
{
    [DataContract]
    class InfoMessage : EventMessage
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "msg")]
        public string Msg { get; set; }

        public InfoMessage()
        {
            Event = "info";
        }
    }
}
