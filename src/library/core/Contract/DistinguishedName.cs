using System;
using System.ComponentModel.DataAnnotations;
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
        // Alphanumeric, space, hyphen, underscore, period, and @ — max 64 chars
        private static readonly Regex SafeComponentRegex = new Regex(
            @"^[a-zA-Z0-9 \-_.@]{1,64}$",
            RegexOptions.Compiled);

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
        /// Validates all DN components against RFC 4514 character constraints.
        /// CommonName is required; all other fields are optional but validated if present.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when a component contains invalid data.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(CommonName))
                throw new ArgumentException("CommonName is required and cannot be empty.", nameof(CommonName));

            ValidateComponent(CommonName, nameof(CommonName));

            if (!string.IsNullOrEmpty(Country) && !CountryCodeRegex.IsMatch(Country))
                throw new ArgumentException(
                    "Country must be a two-letter ISO 3166-1 alpha-2 code (e.g. 'US', 'GB').",
                    nameof(Country));

            if (!string.IsNullOrEmpty(Locale))
                ValidateComponent(Locale, nameof(Locale));

            if (!string.IsNullOrEmpty(Organization))
                ValidateComponent(Organization, nameof(Organization));

            if (!string.IsNullOrEmpty(OrganizationalUnit))
                ValidateComponent(OrganizationalUnit, nameof(OrganizationalUnit));

            if (!string.IsNullOrEmpty(StateProvince))
                ValidateComponent(StateProvince, nameof(StateProvince));
        }

        private static void ValidateComponent(string value, string fieldName)
        {
            if (value.Contains('\0'))
                throw new ArgumentException($"{fieldName} contains null bytes.", fieldName);

            if (!SafeComponentRegex.IsMatch(value))
                throw new ArgumentException(
                    $"{fieldName} contains invalid characters or exceeds 64 characters. " +
                    "Only alphanumeric, space, hyphen, underscore, period, and @ are allowed.",
                    fieldName);
        }

        /// <summary>
        /// Generate a distinguished name string from parameters
        /// </summary>
        /// <param name="issuer"></param>
        /// <returns></returns>
        public static string BuildDistinguishedName(DistinguishedName issuer)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(issuer.CommonName))
                sb.AppendFormat("CN={0},", issuer.CommonName);
            if (!string.IsNullOrWhiteSpace(issuer.Country))
                sb.AppendFormat("C={0},", issuer.Country);
            if (!string.IsNullOrWhiteSpace(issuer.Locale))
                sb.AppendFormat("L={0},", issuer.Locale);
            if (!string.IsNullOrWhiteSpace(issuer.Organization))
                sb.AppendFormat("O={0},", issuer.Organization);
            if (!string.IsNullOrWhiteSpace(issuer.OrganizationalUnit))
                sb.AppendFormat("OU={0},", issuer.OrganizationalUnit);
            if (!string.IsNullOrWhiteSpace(issuer.StateProvince))
                sb.AppendFormat("ST={0},", issuer.StateProvince);

            return sb.ToString().Trim(',');
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