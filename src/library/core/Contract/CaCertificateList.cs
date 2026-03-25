using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Notary.Contract;

[DataContract]
public class CaCertificateList
{
    public CaCertificateList()
    {
        CertificateCollection = new List<Certificate>();
    }

    [DataMember] public List<Certificate> CertificateCollection { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public string ParentCaSlug { get; set; }

    [DataMember] public string Slug { get; set; }
}