using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Notary.Contract;

namespace Notary.Service
{
    /// <summary>
    /// Finds the issuing chain of a certificate by following the issuer links up to the root
    /// </summary>
    internal static class CertificateChainResolver
    {
        // Far deeper than any chain Notary issues; stops a corrupt cycle from walking forever
        private const int MaxChainLength = 10;

        /// <summary>
        /// Get a certificate followed by each of its issuers, ending with the root
        /// </summary>
        /// <param name="certificate">The certificate to start from</param>
        /// <param name="getBySlugAsync">Loads a certificate by slug, or returns null when there is none</param>
        /// <returns>The certificate first, then its issuer, then that issuer's issuer, and so on. A self-signed certificate is returned alone.</returns>
        /// <exception cref="InvalidOperationException">An issuer is missing, or the issuer links loop or run too deep</exception>
        public static async Task<List<Certificate>> ResolveAsync(Certificate certificate, Func<string, Task<Certificate>> getBySlugAsync)
        {
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentNullException.ThrowIfNull(getBySlugAsync);

            List<Certificate> chain = [certificate];
            HashSet<string> seen = [certificate.Slug];
            Certificate current = certificate;

            while (!string.IsNullOrEmpty(current.IssuingSlug))
            {
                if (!seen.Add(current.IssuingSlug))
                {
                    throw new InvalidOperationException($"The issuing chain of certificate '{certificate.Slug}' loops back on itself.");
                }

                if (chain.Count >= MaxChainLength)
                {
                    throw new InvalidOperationException($"The issuing chain of certificate '{certificate.Slug}' is longer than {MaxChainLength} certificates.");
                }

                Certificate issuer = await getBySlugAsync(current.IssuingSlug).ConfigureAwait(false);
                if (issuer == null)
                {
                    throw new InvalidOperationException(
                        $"The issuer '{current.IssuingSlug}' of certificate '{current.Slug}' was not found, so the chain cannot be exported.");
                }

                chain.Add(issuer);
                current = issuer;
            }

            return chain;
        }
    }
}
