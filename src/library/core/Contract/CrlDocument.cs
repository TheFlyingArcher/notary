using System;

namespace Notary.Contract
{
    /// <summary>
    /// A signed, DER encoded certificate revocation list along with its publication metadata
    /// </summary>
    /// <param name="Data">The DER encoded CRL</param>
    /// <param name="Number">The monotonically increasing CRL number</param>
    /// <param name="ThisUpdate">When the CRL was issued (UTC)</param>
    /// <param name="NextUpdate">When the next CRL will be issued at the latest (UTC)</param>
    public sealed record CrlDocument(byte[] Data, long Number, DateTime ThisUpdate, DateTime NextUpdate);
}
