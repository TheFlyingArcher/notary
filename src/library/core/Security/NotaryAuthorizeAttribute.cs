using System;

namespace Notary.Security;

/// <summary>
/// Declares the operation a service method performs. The caller is denied unless one of their roles is granted
/// the operation. A service method with no declared operation is denied to everyone.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotaryAuthorizeAttribute(NotaryOperation operation, bool allowAnonymous = false) : Attribute
{
    public NotaryOperation Operation { get; } = operation;

    /// <summary>
    /// When true, the method can be called with no user (for example the published CRL or startup work)
    /// </summary>
    public bool AllowAnonymous { get; } = allowAnonymous;
}
