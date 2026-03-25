using System.Collections.Generic;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Service;

public interface ICertificateAuthorityService : IEntityService<CertificateAuthority>
{
    /// <summary>
    ///     Get a brief list of certificate authorities
    /// </summary>
    /// <returns>A brief list of certificate authorities</returns>
    Task<List<CaBrief>> GetCaListBrief();
}