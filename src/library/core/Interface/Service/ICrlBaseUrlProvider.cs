namespace Notary.Interface.Service
{
    /// <summary>
    /// Supplies the absolute base URL under which CRLs are published (e.g. https://pki.example.com/api/crl)
    /// </summary>
    public interface ICrlBaseUrlProvider
    {
        /// <summary>
        /// Get the CRL base URL, without a trailing slash
        /// </summary>
        /// <returns>The absolute http(s) base URL, or null if it cannot be determined</returns>
        string GetCrlBaseUrl();
    }
}
