using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Repository;

/// <summary>
/// Repository for all account related transactions
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Find a particular account by username
    /// </summary>
    /// <param name="username">The username to lookup</param>
    /// <returns>The account matching username or null if not found</returns>
    Task<Account> FindByUsernameAsync(string username);
}