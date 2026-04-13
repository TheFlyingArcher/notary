using System.Threading.Tasks;
using Notary.Contract;
using Notary.Interface.Service;

namespace Notary.Interface.Repository;

public interface ITokenService : IEntityService<Token>
{
    /// <summary>
    /// Create a JWT from an account
    /// </summary>
    /// <param name="account">The authenticated account from which to create the JWT</param>
    /// <returns>The base-64 encoded JWT</returns>
    Task<string> CreateToken(Account account);
}