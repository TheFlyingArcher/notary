using System;

namespace Notary.Contract
{
    /// <summary>
    /// The persisted state of the most recent CRL issued by a certificate authority. Holds the CRL number counter.
    /// </summary>
    public class CrlRecord : Entity
    {
        /// <summary>
        /// Build the slug for the CRL record of a CA
        /// </summary>
        public static string SlugFor(string caSlug) => $"crl-{caSlug}";

        /// <summary>Get or set the slug of the issuing CA certificate</summary>
        public string CaSlug { get; set; }

        /// <summary>Get or set the CRL number of the stored CRL</summary>
        public long Number { get; set; }

        public DateTime ThisUpdate { get; set; }

        public DateTime NextUpdate { get; set; }

        /// <summary>Get or set the number of revoked certificates in the CRL</summary>
        public int EntryCount { get; set; }

        /// <summary>Get or set the base64 encoded DER CRL</summary>
        public string Data { get; set; }

        public override string[] SlugProperties() => [SlugFor(CaSlug)];
    }
}
