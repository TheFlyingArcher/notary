using System;
using Notary.Security;

namespace Notary.IOC;

[AttributeUsage(AttributeTargets.Method)]
public class NotaryAuthorizationAttribute : Attribute
{
    public NotaryAuthorizationAttribute(NotaryOperation op)
    {
    }

    public NotaryAuthorizationAttribute(NotaryOperation nop, bool allowAnon)
    {
    }

    public NotaryOperation Operation { get; }

    public bool AllowAnonymous { get; }
}