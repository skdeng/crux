using System.Runtime.Serialization;

namespace Crux.Bfx
{
    [DataContract]
    class CancelGroupOrderMessage : WebsocketMessage
    {
        [DataMember(Name = "gid")]
        public int[] GroupID { get; set; }

        public CancelGroupOrderMessage()
        {
            GroupID = new int[1];
            GroupID[0] = 1;
        }
    }
}
