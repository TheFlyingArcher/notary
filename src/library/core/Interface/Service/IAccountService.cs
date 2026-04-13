
using System.Threading.Tasks;
using Notary.Contract;
namespace Notary.Interface.Service;

public interface IAccountService : IEntityService<Account>
{
    /// <summary>
    /// Authenticate the user against the configured provider
    /// </summary>
    /// <param name="username">The username of the account</param>
    /// <param name="password">The password of the account</param>
    /// <returns>An account matching credentials or null if no account found</returns>
    Task<Account> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Get a particular account by its username
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    Task<Account> GetByUsernameAsync(string username);
}