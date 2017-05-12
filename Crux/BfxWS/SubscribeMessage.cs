﻿using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    [DataContract]
    class SubscribeMessage : EventMessage
    {
        [DataMember(Name = "channel")]
        public string Channel { get; set; }

        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }

        public SubscribeMessage()
        {
            Event = "subscribe";
        }
    }
}
