using System.Collections.Generic;
using System.Linq;

namespace Notary.Profiles;

/// <summary>
/// Extended key usage OIDs used by the CA profiles
/// </summary>
public static class KeyPurposeOids
{
    public const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

    public const string ClientAuthentication = "1.3.6.1.5.5.7.3.2";

    public const string CodeSigning = "1.3.6.1.5.5.7.3.3";

    public const string EmailProtection = "1.3.6.1.5.5.7.3.4";

    public const string SmartCardLogin = "1.3.6.1.4.1.311.20.2.2";

    /// <summary>
    /// Every extended key usage the application knows by name, in the order they are listed to users
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> Known { get; } =
    [
        new(ServerAuthentication, "Server Authentication"),
        new(ClientAuthentication, "Client Authentication"),
        new(CodeSigning, "Code Signing"),
        new(EmailProtection, "E-mail Protection"),
        new("1.3.6.1.5.5.7.3.5", "IPSEC End System"),
        new("1.3.6.1.5.5.7.3.6", "IPSEC Tunnel"),
        new("1.3.6.1.5.5.7.3.7", "IPSEC User"),
        new("1.3.6.1.5.5.7.3.8", "Time Stamping"),
        new("1.3.6.1.5.5.7.3.9", "OCSP Signing"),
        new(SmartCardLogin, "Smart Card Login"),
        new("1.3.6.1.1.1.1.22", "MAC Address")
    ];

    /// <summary>
    /// Get a readable name for an extended key usage, or the OID itself when it is not known
    /// </summary>
    public static string GetName(string oid)
    {
        var known = Known.FirstOrDefault(k => k.Key == oid);
        return known.Key == null ? oid : known.Value;
    }
}
