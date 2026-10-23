using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Notary.Security;

/// <summary>
/// Maps each role to the operations it may perform. A user holding several roles may perform the union.
/// </summary>
public static class NotaryRolePermissions
{
    private static readonly FrozenDictionary<string, FrozenSet<NotaryOperation>> s_operationsByRole = BuildMap();

    /// <summary>
    /// The operations granted to a role
    /// </summary>
    /// <returns>The role's operations, or an empty set for an unknown role</returns>
    public static IReadOnlySet<NotaryOperation> GetOperations(string role) =>
        s_operationsByRole.TryGetValue(role, out var operations) ? operations : FrozenSet<NotaryOperation>.Empty;

    /// <summary>
    /// Whether any of the user's roles grants the operation
    /// </summary>
    public static bool IsAllowed(ClaimsPrincipal user, NotaryOperation operation)
    {
        if (user?.Identity is not { IsAuthenticated: true } || operation == NotaryOperation.None)
        {
            return false;
        }

        foreach (var (role, operations) in s_operationsByRole)
        {
            if (operations.Contains(operation) && user.IsInRole(role))
            {
                return true;
            }
        }

        return false;
    }

    private static FrozenDictionary<string, FrozenSet<NotaryOperation>> BuildMap()
    {
        NotaryOperation[] userOperations =
        [
            NotaryOperation.CertificateList,
            NotaryOperation.CertificateGet,
            NotaryOperation.CertificateDownload,
            NotaryOperation.CertificateAuthorityList,
            NotaryOperation.CertificateAuthorityGet,
            NotaryOperation.CertificateAuthorityDownload,
            // The certificate detail page shows the key behind a certificate
            NotaryOperation.AsymmetricKeyGet
        ];

        NotaryOperation[] writerOperations =
        [
            .. userOperations,
            NotaryOperation.CertificateCreate,
            NotaryOperation.CertificateRevoke
        ];

        // Only an administrator manages certificate authorities and keys directly. The keys that issuing a
        // certificate needs are reached through the already-authorized service call.
        var adminOperations = Enum.GetValues<NotaryOperation>().Where(o => o != NotaryOperation.None);

        return new Dictionary<string, FrozenSet<NotaryOperation>>
        {
            [NotaryRoles.User] = userOperations.ToFrozenSet(),
            [NotaryRoles.Writer] = writerOperations.ToFrozenSet(),
            [NotaryRoles.Admin] = adminOperations.ToFrozenSet()
        }.ToFrozenDictionary();
    }
}
