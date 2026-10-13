using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace Notary.Contract
{
    /// <summary>
    /// Represents a X509.3 Distinguished Name
    /// </summary>
    public class DistinguishedName
    {
        // Upper bounds from RFC 5280 Appendix A (X.520): common name, organization and organizational unit are 64
        // characters; locality and state are 128
        private const int NameMaxLength = 64;
        private const int PlaceMaxLength = 128;

        // Exactly two ASCII letters per ISO 3166-1 alpha-2
        private static readonly Regex CountryCodeRegex = new Regex(
            @"^[A-Za-z]{2}$",
            RegexOptions.Compiled);

        [JsonProperty("cn", Required = Required.Always), Required]
        public string CommonName
        {
            get; set;
        }


        [JsonProperty("c", Required = Required.AllowNull)]
        public string Country
        {
            get; set;
        }

        [JsonProperty("l", Required = Required.AllowNull)]
        public string Locale
        {
            get; set;
        }

        [JsonProperty("o", Required = Required.AllowNull)]
        public string Organization
        {
            get; set;
        }

        [JsonProperty("ou", Required = Required.AllowNull)]
        public string OrganizationalUnit
        {
            get; set;
        }

        [JsonProperty("s", Required = Required.AllowNull)]
        public string StateProvince
        {
            get; set;
        }

        /// <summary>
        /// Checks all DN components. CommonName is required; all other fields are optional but checked if present.
        /// Values may contain any text, including punctuation, because they are escaped when the name is built;
        /// only length limits (RFC 5280 Appendix A) and control, invisible or malformed characters are refused.
        /// </summary>
        /// <returns>Success, or an Invalid result describing the first problem found</returns>
        public Result Check()
        {
            var problem = FindProblem();
            return problem is null ? Result.Ok() : Result.Fail(ResultStatus.Invalid, problem.Value.Message);
        }

        /// <summary>
        /// Checks a Common Name on its own, for showing a message next to the input
        /// </summary>
        /// <param name="commonName">The value to check</param>
        /// <returns>A message describing the problem, or null if the value is acceptable</returns>
        public static string CheckCommonName(string commonName)
        {
            return new DistinguishedName { CommonName = commonName }.FindProblem()?.Message;
        }

        /// <summary>
        /// Checks all DN components like <see cref="Check"/>, but throws instead of returning a result.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when a component contains invalid data.</exception>
        public void Validate()
        {
            var problem = FindProblem();
            if (problem is not null)
                throw new ArgumentException(problem.Value.Message, problem.Value.Field);
        }

        private (string Field, string Message)? FindProblem()
        {
            if (string.IsNullOrWhiteSpace(CommonName))
                return (nameof(CommonName), "Common Name is required and cannot be empty.");

            var problem = ComponentProblem(CommonName, nameof(CommonName), "Common Name", NameMaxLength);
            if (problem is not null)
                return problem;

            if (!string.IsNullOrEmpty(Country) && !CountryCodeRegex.IsMatch(Country))
                return (nameof(Country), "Country must be a two-letter ISO 3166-1 alpha-2 code (e.g. 'US', 'GB').");

            (string Value, string Field, string Label, int Max)[] optional =
            [
                (Locale, nameof(Locale), "Locale", PlaceMaxLength),
                (Organization, nameof(Organization), "Organization", NameMaxLength),
                (OrganizationalUnit, nameof(OrganizationalUnit), "Organizational Unit", NameMaxLength),
                (StateProvince, nameof(StateProvince), "State", PlaceMaxLength)
            ];

            foreach (var component in optional)
            {
                if (string.IsNullOrEmpty(component.Value))
                    continue;

                problem = ComponentProblem(component.Value, component.Field, component.Label, component.Max);
                if (problem is not null)
                    return problem;
            }

            return null;
        }

        private static (string Field, string Message)? ComponentProblem(string value, string field, string label, int maxLength)
        {
            // Directory matching ignores leading and trailing spaces, and they do not survive being written out and
            // read back, so refuse them instead of silently changing the name
            if (value.Length != value.Trim().Length)
                return (field, $"{label} cannot start or end with a space.");

            int length = 0;
            int index = 0;
            while (index < value.Length)
            {
                // A lone surrogate is not text and cannot be encoded
                if (System.Buffers.OperationStatus.Done != Rune.DecodeFromUtf16(value.AsSpan(index), out Rune rune, out int consumed))
                    return (field, $"{label} contains a character that is not valid text.");

                switch (Rune.GetUnicodeCategory(rune))
                {
                    // Control characters (including NUL, tab and line breaks), invisible formatting such as zero-width
                    // and right-to-left override characters, and line or paragraph separators can disguise a name
                    case UnicodeCategory.Control:
                    case UnicodeCategory.Format:
                    case UnicodeCategory.LineSeparator:
                    case UnicodeCategory.ParagraphSeparator:
                        return (field, $"{label} contains control or invisible formatting characters.");
                }

                length++;
                index += consumed;
            }

            if (length > maxLength)
                return (field, $"{label} cannot be longer than {maxLength} characters.");

            return null;
        }

        /// <summary>
        /// Escape a value for use in a distinguished name string (RFC 4514 section 2.4), so that punctuation such as
        /// commas, plus signs and equals signs is read as part of the value and cannot add or change other attributes.
        /// </summary>
        /// <param name="value">The attribute value</param>
        public static string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var sb = new StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool escape = c is '"' or '+' or ',' or ';' or '<' or '>' or '\\' or '='
                    || (i == 0 && (c == ' ' || c == '#'))
                    || (i == value.Length - 1 && c == ' ');

                if (escape)
                    sb.Append('\\');

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get the name's attributes in certificate order as object identifier and value pairs. Certificates are built
        /// from these directly, because writing a name out as text and parsing it again can alter some values.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<(string Oid, string Value)> GetAttributes()
        {
            var attributes = new System.Collections.Generic.List<(string Oid, string Value)>(6);
            if (!string.IsNullOrWhiteSpace(CommonName))
                attributes.Add(("2.5.4.3", CommonName));
            if (!string.IsNullOrWhiteSpace(Country))
                attributes.Add(("2.5.4.6", Country));
            if (!string.IsNullOrWhiteSpace(Locale))
                attributes.Add(("2.5.4.7", Locale));
            if (!string.IsNullOrWhiteSpace(Organization))
                attributes.Add(("2.5.4.10", Organization));
            if (!string.IsNullOrWhiteSpace(OrganizationalUnit))
                attributes.Add(("2.5.4.11", OrganizationalUnit));
            if (!string.IsNullOrWhiteSpace(StateProvince))
                attributes.Add(("2.5.4.8", StateProvince));

            return attributes;
        }

        /// <summary>
        /// Generate a distinguished name string for display. Values are escaped (RFC 4514), so punctuation cannot
        /// be mistaken for another attribute. Certificates are built from <see cref="GetAttributes"/>, not from this text.
        /// </summary>
        /// <param name="issuer"></param>
        /// <returns></returns>
        public static string BuildDistinguishedName(DistinguishedName issuer)
        {
            var parts = new System.Collections.Generic.List<string>(6);
            if (!string.IsNullOrWhiteSpace(issuer.CommonName))
                parts.Add($"CN={EscapeValue(issuer.CommonName)}");
            if (!string.IsNullOrWhiteSpace(issuer.Country))
                parts.Add($"C={EscapeValue(issuer.Country)}");
            if (!string.IsNullOrWhiteSpace(issuer.Locale))
                parts.Add($"L={EscapeValue(issuer.Locale)}");
            if (!string.IsNullOrWhiteSpace(issuer.Organization))
                parts.Add($"O={EscapeValue(issuer.Organization)}");
            if (!string.IsNullOrWhiteSpace(issuer.OrganizationalUnit))
                parts.Add($"OU={EscapeValue(issuer.OrganizationalUnit)}");
            if (!string.IsNullOrWhiteSpace(issuer.StateProvince))
                parts.Add($"ST={EscapeValue(issuer.StateProvince)}");

            // Joined rather than trimmed: an escaped value may legitimately end in a comma
            return string.Join(",", parts);
        }


        /// <summary>
        /// Makes a DN string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return BuildDistinguishedName(this);
        }
    }
}