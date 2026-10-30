using MudBlazor;

using Notary.Profiles;

namespace Notary.Web.ViewModels;

/// <summary>
/// The icons that denote what a certificate authority is for
/// </summary>
public static class CaPurposeIcons
{
    /// <summary>
    /// The icon for a root CA
    /// </summary>
    public const string Root = Icons.Material.Filled.Shield;

    /// <summary>
    /// The generic icon for a certificate that is not a CA
    /// </summary>
    public const string Certificate = Icons.Material.Filled.CardMembership;

    /// <summary>
    /// The icon for a CA below a root. A CA created before purposes existed counts as Custom.
    /// </summary>
    public static string ForSubordinate(CaPurpose? purpose) => purpose switch
    {
        CaPurpose.Root => Root,
        CaPurpose.TlsSsl => Icons.Material.Filled.Http,
        CaPurpose.UserLogin => Icons.Material.Filled.AssignmentInd,
        CaPurpose.Smime => Icons.Material.Filled.Email,
        CaPurpose.IoT => Icons.Material.Filled.Devices,
        CaPurpose.Dot1xRadius => Icons.Material.Filled.WifiPassword,
        _ => Icons.Material.Filled.Tune
    };
}
