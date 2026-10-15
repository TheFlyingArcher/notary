namespace Notary.Contract;

/// <summary>
/// A certificate authority positioned in the CA hierarchy.
/// </summary>
/// <param name="Authority">The certificate authority</param>
/// <param name="Depth">Zero for a root, one more than the issuing CA's depth otherwise</param>
public record CaHierarchyRow(CertificateAuthority Authority, int Depth);
