using System;
using System.Collections.Generic;
using System.Linq;

using Notary.Contract;

namespace Notary.Profiles;

/// <summary>
/// The CA purposes offered, their profiles, and the rules for how a CA relates to its parent
/// </summary>
public static class CaProfileCatalog
{
    private static readonly CertificateKeyUsage[] _caKeyUsages =
    [
        CertificateKeyUsage.KeyCertSign,
        CertificateKeyUsage.CrlSign
    ];

    private static readonly CertificateKeyUsage[] _leafSigningAndEncryption =
    [
        CertificateKeyUsage.DigitalSignature,
        CertificateKeyUsage.KeyEncipherment
    ];

    private static readonly CaProfile[] _profiles =
    [
        new(
            CaPurpose.Root,
            "Root",
            "Signs intermediate CAs only. Cannot be created under another CA and never issues certificates directly.",
            false,
            _caKeyUsages,
            [],
            true,
            false,
            true,
            [],
            []),
        new(
            CaPurpose.TlsSsl,
            "TLS/SSL",
            "Web server certificates (HTTPS).",
            false,
            _caKeyUsages,
            [KeyPurposeOids.ServerAuthentication],
            true,
            true,
            true,
            _leafSigningAndEncryption,
            [KeyPurposeOids.ServerAuthentication]),
        new(
            CaPurpose.UserLogin,
            "User login",
            "Sign-in certificates for people (smart card, hardware token).",
            false,
            _caKeyUsages,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.SmartCardLogin],
            true,
            true,
            true,
            _leafSigningAndEncryption,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.SmartCardLogin]),
        new(
            CaPurpose.Smime,
            "S/MIME",
            "E-mail signing and encryption.",
            false,
            _caKeyUsages,
            [KeyPurposeOids.EmailProtection],
            true,
            true,
            true,
            [
                CertificateKeyUsage.DigitalSignature,
                CertificateKeyUsage.NonRepudiation,
                CertificateKeyUsage.KeyEncipherment
            ],
            [KeyPurposeOids.EmailProtection]),
        new(
            CaPurpose.IoT,
            "IoT devices",
            "Certificates for devices that authenticate to services and may offer their own.",
            false,
            _caKeyUsages,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication],
            true,
            true,
            true,
            _leafSigningAndEncryption,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication]),
        new(
            CaPurpose.Dot1xRadius,
            "802.1x/RADIUS",
            "Network access certificates for supplicants and RADIUS servers.",
            false,
            _caKeyUsages,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication],
            true,
            true,
            true,
            _leafSigningAndEncryption,
            [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication]),
        new(
            CaPurpose.Custom,
            "Custom",
            "Choose key usages yourself. Discouraged: a mistake can weaken the CA or break trust chains.",
            true,
            _caKeyUsages,
            [],
            true,
            true,
            false,
            [],
            [])
    ];

    /// <summary>
    /// Get all profiles in the order shown to users
    /// </summary>
    public static IReadOnlyList<CaProfile> GetAll()
    {
        return _profiles;
    }

    /// <summary>
    /// Get the profile for a purpose. A missing purpose, as on CAs created before purposes existed, is Custom.
    /// </summary>
    public static CaProfile Get(CaPurpose? purpose)
    {
        return _profiles.First(p => p.Purpose == (purpose ?? CaPurpose.Custom));
    }

    /// <summary>
    /// Get the profile that applies to a stored CA: its purpose's profile, a Custom profile built from the usages
    /// chosen for it, or the unrestricted Custom profile when it has no purpose or nothing was chosen.
    /// </summary>
    public static CaProfile Resolve(CertificateAuthority ca)
    {
        ArgumentNullException.ThrowIfNull(ca);

        bool hasCustomUsages = ca.CustomKeyUsages is { Count: > 0 } || ca.CustomExtendedKeyUsages is { Count: > 0 };

        return ca.Purpose is CaPurpose.Custom && hasCustomUsages
            ? CreateCustom(ca.CustomKeyUsages ?? [], ca.CustomExtendedKeyUsages ?? [])
            : Get(ca.Purpose);
    }

    /// <summary>
    /// Check that a CA may issue the certificate of a CA with the given profile. A purpose CA that itself has a
    /// parent has a path length of 0 and issues end-entity certificates only.
    /// </summary>
    /// <param name="parent">The CA that would issue the new CA</param>
    /// <param name="child">The profile of the new CA</param>
    public static Result CheckIssuer(CertificateAuthority parent, CaProfile child)
    {
        ArgumentNullException.ThrowIfNull(parent);

        Result canIssue = CheckCanIssueCas(parent);
        return canIssue.IsSuccess ? CheckChild(Resolve(parent), child) : canIssue;
    }

    /// <summary>
    /// Check that a CA is allowed to issue other CAs at all, i.e. its own path length is not 0
    /// </summary>
    public static Result CheckCanIssueCas(CertificateAuthority ca)
    {
        ArgumentNullException.ThrowIfNull(ca);

        if (GetPathLength(ca.Purpose, !string.IsNullOrEmpty(ca.ParentCaSlug)) == 0)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                $"The {Get(ca.Purpose).DisplayName} CA '{ca.Name}' can only issue end-entity certificates, not other CAs.");
        }

        return Result.Ok();
    }

    /// <summary>
    /// Check that a leaf certificate with the given usages may be issued by a CA with the given profile
    /// </summary>
    /// <param name="issuer">The issuing CA's profile</param>
    /// <param name="keyUsages">The key usages requested for the leaf</param>
    /// <param name="extendedKeyUsages">The extended key usage OIDs requested for the leaf</param>
    /// <returns>Success, or an Invalid result naming what is not permitted</returns>
    public static Result CheckLeaf(CaProfile issuer, IEnumerable<CertificateKeyUsage> keyUsages, IEnumerable<string> extendedKeyUsages)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        CertificateKeyUsage[] requestedKeyUsages = [.. (keyUsages ?? []).Distinct()];
        string[] requestedExtendedKeyUsages = [.. (extendedKeyUsages ?? []).Distinct()];

        if (!issuer.IssuesLeafCertificates)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                $"A {issuer.DisplayName} CA never issues certificates directly. Create an intermediate CA and issue from that.");
        }

        if (issuer.RestrictsLeafUsages)
        {
            if (requestedKeyUsages.Length == 0)
            {
                return Result.Fail(ResultStatus.Invalid, $"Certificates from a {issuer.DisplayName} CA need at least one key usage.");
            }

            if (requestedExtendedKeyUsages.Length == 0)
            {
                return Result.Fail(ResultStatus.Invalid, $"Certificates from a {issuer.DisplayName} CA need at least one extended key usage.");
            }

            CertificateKeyUsage[] outsideKeyUsages = [.. requestedKeyUsages.Except(issuer.LeafKeyUsages)];
            if (outsideKeyUsages.Length > 0)
            {
                return Result.Fail(
                    ResultStatus.Invalid,
                    $"{string.Join(", ", outsideKeyUsages.Select(DescribeKeyUsage))} not permitted by the {issuer.DisplayName} CA.");
            }

            return CheckExtendedKeyUsages(issuer, requestedExtendedKeyUsages, issuer.LeafExtendedKeyUsages);
        }

        // A Custom CA that chose extended key usages carries them on its certificate, so a leaf cannot go beyond them
        return issuer.RestrictsExtendedKeyUsages
            ? CheckExtendedKeyUsages(issuer, requestedExtendedKeyUsages, issuer.CaExtendedKeyUsages)
            : Result.Ok();
    }

    private static Result CheckExtendedKeyUsages(CaProfile issuer, string[] requested, IReadOnlyList<string> permitted)
    {
        string[] outside = [.. requested.Except(permitted)];

        return outside.Length > 0
            ? Result.Fail(
                ResultStatus.Invalid,
                $"{string.Join(", ", outside.Select(KeyPurposeOids.GetName))} not permitted by the {issuer.DisplayName} CA.")
            : Result.Ok();
    }

    /// <summary>
    /// Get a readable name for a key usage, matching the names used on the certificate form
    /// </summary>
    public static string DescribeKeyUsage(CertificateKeyUsage usage)
    {
        return usage switch
        {
            CertificateKeyUsage.KeyCertSign => "Certificate Key Sign",
            CertificateKeyUsage.CrlSign => "CRL Sign",
            // "DigitalSignature" becomes "Digital Signature"
            _ => System.Text.RegularExpressions.Regex.Replace(usage.ToString(), "(?<!^)([A-Z])", " $1")
        };
    }

    /// <summary>
    /// Get every purpose with whether a CA of that purpose can be created under the given parent
    /// </summary>
    /// <param name="parent">The parent CA, or null for a self-signed CA</param>
    public static IReadOnlyList<CaPurposeOption> EvaluateForParent(CertificateAuthority parent)
    {
        return [.. _profiles.Select(profile =>
        {
            if (parent == null)
            {
                return new CaPurposeOption(profile, true, null);
            }

            // Custom is chosen together with explicit usages, which the page requires under a restricted parent and
            // CheckIssuer enforces on save, so only whether the parent can issue CAs decides if it is offered
            Result check = profile.Purpose == CaPurpose.Custom ? CheckCanIssueCas(parent) : CheckIssuer(parent, profile);
            return new CaPurposeOption(profile, check.IsSuccess, check.Error);
        })];
    }

    /// <summary>
    /// Build a Custom profile from the usages the user chose. Certificate signing and CRL signing are always
    /// included, because a CA cannot sign certificates or publish revocations without them.
    /// </summary>
    /// <param name="keyUsages">The chosen key usages</param>
    /// <param name="extendedKeyUsages">The chosen extended key usage OIDs</param>
    public static CaProfile CreateCustom(IEnumerable<CertificateKeyUsage> keyUsages, IEnumerable<string> extendedKeyUsages)
    {
        ArgumentNullException.ThrowIfNull(keyUsages);
        ArgumentNullException.ThrowIfNull(extendedKeyUsages);

        CertificateKeyUsage[] allKeyUsages = [.. keyUsages.Concat(_caKeyUsages).Distinct()];
        string[] allExtendedKeyUsages = [.. extendedKeyUsages.Distinct()];

        return Get(CaPurpose.Custom) with
        {
            CaKeyUsages = allKeyUsages,
            CaExtendedKeyUsages = allExtendedKeyUsages
        };
    }

    /// <summary>
    /// Get the basicConstraints path length for a CA certificate. A purpose CA that has a parent issues
    /// leaf certificates only (0); every other CA is unconstrained (null).
    /// </summary>
    /// <param name="purpose">The CA purpose; null is treated as Custom</param>
    /// <param name="hasParent">True if another CA issues this CA's certificate</param>
    public static int? GetPathLength(CaPurpose? purpose, bool hasParent)
    {
        CaPurpose resolved = purpose ?? CaPurpose.Custom;

        if (resolved is CaPurpose.Root or CaPurpose.Custom)
        {
            return null;
        }

        return hasParent ? 0 : null;
    }

    /// <summary>
    /// Check that a CA with the child profile may be issued by a CA with the parent profile
    /// </summary>
    /// <param name="parent">The issuing CA's profile</param>
    /// <param name="child">The new CA's profile</param>
    /// <returns>Success, or an Invalid result describing why the child is not allowed</returns>
    public static Result CheckChild(CaProfile parent, CaProfile child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        if (child.Purpose == CaPurpose.Root)
        {
            return Result.Fail(ResultStatus.Invalid, "A Root CA cannot be issued by another CA.");
        }

        if (!parent.IssuesCaCertificates)
        {
            return Result.Fail(ResultStatus.Invalid, $"A {parent.DisplayName} CA cannot issue CA certificates.");
        }

        if (!parent.RestrictsExtendedKeyUsages)
        {
            return Result.Ok();
        }

        if (!child.RestrictsExtendedKeyUsages)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                $"A {child.DisplayName} CA under a {parent.DisplayName} CA must list explicit usages within the {parent.DisplayName} CA's usages.");
        }

        string[] outside = [.. child.CaExtendedKeyUsages.Except(parent.CaExtendedKeyUsages)];
        if (outside.Length > 0)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                $"A {child.DisplayName} CA cannot be issued by a {parent.DisplayName} CA: {string.Join(", ", outside.Select(KeyPurposeOids.GetName))} not permitted by the parent.");
        }

        return Result.Ok();
    }
}
