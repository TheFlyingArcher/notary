namespace Notary.Validity;

/// <summary>
/// The kind of certificate a validity period is being chosen for
/// </summary>
public enum CertificateRole
{
    RootCa,
    IntermediateCa,
    Leaf
}
