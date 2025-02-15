using System.Runtime.Serialization;

namespace Notary.Contract
{
    [DataContract]
    public class CaBrief
    {
        [DataMember]
        public string Slug { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ParentName { get; set; }

        [DataMember]
        public string CreatedOn { get; set; }

        [DataMember]
        public string CreatedBy { get; set; }

        [DataMember]
        public int Certificates { get; set; }
    }
}
