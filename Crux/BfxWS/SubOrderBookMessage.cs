using System.Runtime.Serialization;

namespace Crux.BfxWS
{
    [DataContract]
    class SubOrderBookMessage : SubscribeMessage
    {
        /// <summary>
        /// Level of price aggregation
        /// </summary>
        [DataMember(Name = "prec")]
        public string Precision { get; set; }

        /// <summary>
        /// Frequency of updates
        /// F0 - realtime
        /// F1 = 2sec
        /// F2 = 5sec
        /// F3 = 10sec
        /// </summary>
        [DataMember(Name = "freq")]
        public string Frequency { get; set; }

        /// <summary>
        /// Number of price points
        /// </summary>
        [DataMember(Name = "len")]
        public string Length { get; set; }

        public SubOrderBookMessage()
        {
            Channel = "book";
            Frequency = "F0";
            Precision = "P0";
        }
    }
}
