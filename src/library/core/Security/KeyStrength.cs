namespace Notary.Security;

/// <summary>
/// The parts of a key that decide its strength
/// </summary>
/// <param name="Algorithm">The key algorithm</param>
/// <param name="Curve">The curve; required for Elliptic Curve keys</param>
/// <param name="Length">The size in bits; required for RSA keys</param>
public sealed record KeySpec(Algorithm Algorithm, EllipticCurve? Curve, int? Length);

/// <summary>
/// Compares keys across RSA and Elliptic Curve using NIST SP 800-57 security bits, so a certificate can be
/// required to be no weaker than the CA that issues it
/// </summary>
public static class KeyStrength
{
    /// <summary>
    /// The security strength of a key in bits. An RSA size between two NIST rows takes the row below it.
    /// </summary>
    public static Result<int> GetSecurityBits(KeySpec key)
    {
        if (key == null)
            return Result<int>.Fail(ResultStatus.Invalid, "A key is required to determine its strength.");

        switch (key.Algorithm)
        {
            case Algorithm.EllipticCurve:
                if (!key.Curve.HasValue)
                    return Result<int>.Fail(ResultStatus.Invalid, "An Elliptic Curve key needs a curve to determine its strength.");

                return Result<int>.Ok(key.Curve.Value switch
                {
                    EllipticCurve.P256 => 128,
                    EllipticCurve.P384 => 192,
                    _ => 256
                });

            case Algorithm.RSA:
                if (!key.Length.HasValue || key.Length.Value <= 0)
                    return Result<int>.Fail(ResultStatus.Invalid, "An RSA key needs a length to determine its strength.");

                return Result<int>.Ok(key.Length.Value switch
                {
                    < 2048 => 80,
                    < 3072 => 112,
                    < 7680 => 128,
                    < 15360 => 192,
                    _ => 256
                });

            default:
                return Result<int>.Fail(ResultStatus.Invalid, $"The key algorithm {key.Algorithm} has no known strength.");
        }
    }

    /// <summary>
    /// Fails when the subject key is weaker than the key of the CA that issues it. Equal strength passes, whatever
    /// the key types, so an RSA-3072 CA may issue a P-256 key and the reverse.
    /// </summary>
    public static Result CheckNotWeaker(KeySpec issuer, KeySpec subject)
    {
        var issuerBits = GetSecurityBits(issuer);
        if (!issuerBits.IsSuccess)
            return Result.Fail(issuerBits.Status, $"The issuing CA key: {issuerBits.Error}");

        var subjectBits = GetSecurityBits(subject);
        if (!subjectBits.IsSuccess)
            return Result.Fail(subjectBits.Status, subjectBits.Error);

        if (subjectBits.Value < issuerBits.Value)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                $"A {subjectBits.Value}-bit key cannot be issued under a {issuerBits.Value}-bit CA; it must be at least as strong as its issuer.");
        }

        return Result.Ok();
    }

    /// <summary>
    /// True when the candidate key is at least as strong as the issuer's. A key whose strength cannot be read on
    /// either side is not offered.
    /// </summary>
    public static bool MeetsFloor(KeySpec issuer, KeySpec candidate) => CheckNotWeaker(issuer, candidate).IsSuccess;
}
