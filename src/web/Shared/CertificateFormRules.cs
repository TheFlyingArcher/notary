using System.Text.RegularExpressions;
using Notary.Contract;

namespace Notary.Web.Shared;

/// <summary>
/// Field validation shared by the certificate creation forms.
/// </summary>
public static class CertificateFormRules
{
    public static IEnumerable<string> ValidateCommonName(string commonName)
    {
        var problem = DistinguishedName.CheckCommonName(commonName);
        if (problem != null)
            yield return problem;
    }

    public static IEnumerable<string> ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            yield return "Please enter a certificate name";
        }

        var regex = new Regex("[a-zA-Z0-9\\-]");
        if (!regex.IsMatch(name))
        {
            yield return "Name can only contain alphanumerics and dashes";
        }
    }
}
