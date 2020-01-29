using System.Runtime.Serialization;

namespace jll.emea.crm.DocGeneration
{
    [DataContract]
    public class CropRectangle
    {
        [DataMember]
        public decimal? top;
        [DataMember]
        public decimal? left;
        [DataMember]
        public decimal? right;
        [DataMember]
        public decimal? bottom;
        public decimal? width;
        public decimal? height;
    }
}
