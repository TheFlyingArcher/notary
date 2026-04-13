using System;
using System.Runtime.Serialization;

namespace Notary.Contract;

[DataContract]
public class Token : Entity
{
    [DataMember]
    public string AccessToken { get; set; }
    
    [DataMember]
    public string AccountSlug { get; set; }
    
    [DataMember]
    public DateTime Expires { get; set; }
    
    public override string[] SlugProperties()
    {
        return [Guid.NewGuid().ToString()];
    }
}