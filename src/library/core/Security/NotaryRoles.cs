namespace Notary.Security;

/// <summary>
/// The role names a user can hold. These match the roles used by the <c>Authorize</c> attributes on the pages.
/// </summary>
public static class NotaryRoles
{
    public const string Admin = "NotaryAdmin";
    public const string Writer = "NotaryWriter";
    public const string User = "NotaryUser";
}
