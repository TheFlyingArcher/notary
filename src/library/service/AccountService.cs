using System;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Logging;

namespace Notary.Service;

public class AccountService(IAccountRepository repository, ILog log, NotaryConfiguration configuration)
    : EntityService<Account>(repository, log), IAccountService
{
    public async Task<Account> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new ArgumentException("One or more credentials are empty");

        var authProvider = NotaryConfiguration.Authentication;
        
        Account account = null;
        switch (authProvider)
        {
            case AuthenticationProvider.ActiveDirectory:
                account = await AuthenticateLdap(username, password);
                break;
            case AuthenticationProvider.System:
                account = await AuthenticateSystem(username, password);
                break;
            default:
                throw new NotImplementedException("OpenID or other provider is not implemented");
        }

        return account;
    }

    public async Task<Account> GetByUsernameAsync(string username)
    {
        var repository = Repository as IAccountRepository;
        if (repository == null) throw new NotImplementedException("Check your DI");

        return await repository.FindByUsernameAsync(username);
    }
    
    private async Task<Account> AuthenticateSystem(string username, string password)
    {
        throw new NotImplementedException();
    }

    private async Task<Account> AuthenticateLdap(string username, string password)
    {
        var ldapCredentials = new NetworkCredential(
            username,
            password
        );
        var ldapId = new LdapDirectoryIdentifier(NotaryConfiguration.ActiveDirectory.ServerName);
        using var connection = new LdapConnection(ldapId, ldapCredentials);
        try
        {
            connection.Bind();
            
            var account = await GetByUsernameAsync(username) ??
                          await RegisterAsync(connection, username);

            return account;
        }
        catch (LdapException ex)
        {
            ex.IfNotLoggedThenLog(Logger);
            return null;
        }
    }

    private async Task<Account> RegisterAsync(LdapConnection connection, string username)
    {
        var account = await GetByUsernameAsync(username);
        if (account != null)
            throw new NotaryException("Account already exists");

        var searchBase = $"DC={NotaryConfiguration.ActiveDirectory.Domain},DC=lan";
        var filter = "(sAMAccountName=username)";
        string[] attributesToReturn = ["displayName", "mail", "userPrincipalName", "memberOf"];

        SearchRequest request = new SearchRequest(
            searchBase, 
            filter, 
            SearchScope.Subtree, 
            attributesToReturn
        );

        var results = connection.SendRequest(request) as SearchResponse;
        if (results == null)
            throw new NotaryException("No results returned");

        account = new Account();
        
        await SaveAsync(account, null);
        return account;
    }

    protected NotaryConfiguration NotaryConfiguration { get; } = configuration;
}