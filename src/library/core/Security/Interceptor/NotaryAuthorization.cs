using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Notary.Security.Interceptor;

/// <summary>
/// Denies a service call unless the current user holds a role that is granted the method's
/// <see cref="NotaryOperation"/>. Calls one service makes to another while serving an authorized call are trusted,
/// so a role needs only the operation of the call it makes, not of every collaborator behind it.
/// </summary>
public class NotaryAuthorization(INotaryUserContext userContext) : IInterceptor
{
    // True while an authorized call is in progress on this logical flow of execution
    private static readonly AsyncLocal<bool> s_inAuthorizedCall = new();

    private static readonly MethodInfo s_fromException = typeof(Task)
        .GetMethod(nameof(Task.FromException), 1, [typeof(Exception)])!;

    public void Intercept(IInvocation invocation)
    {
        if (s_inAuthorizedCall.Value)
        {
            invocation.Proceed();
            return;
        }

        var denial = Authorize(invocation);
        if (denial is not null)
        {
            Deny(invocation, denial);
            return;
        }

        s_inAuthorizedCall.Value = true;
        try
        {
            // An async method captures the flag as it starts, so it stays set for the rest of that call
            invocation.Proceed();
        }
        finally
        {
            s_inAuthorizedCall.Value = false;
        }
    }

    private NotaryAuthorizationException Authorize(IInvocation invocation)
    {
        var (operation, allowAnonymous) = Resolve(invocation);
        if (allowAnonymous)
        {
            return null;
        }

        var method = $"{invocation.Method.DeclaringType?.Name}.{invocation.Method.Name}";
        if (operation == NotaryOperation.None)
        {
            return new NotaryAuthorizationException(operation, $"{method} does not declare an operation, so it is denied");
        }

        return NotaryRolePermissions.IsAllowed(userContext.User, operation)
            ? null
            : new NotaryAuthorizationException(operation, $"Not authorized to perform {operation} ({method})");
    }

    // The operation is declared on the service class method
    private static (NotaryOperation Operation, bool AllowAnonymous) Resolve(IInvocation invocation)
    {
        var attribute = invocation.MethodInvocationTarget?.GetCustomAttribute<NotaryAuthorizeAttribute>()
            ?? invocation.Method.GetCustomAttribute<NotaryAuthorizeAttribute>();

        return attribute is null
            ? (NotaryOperation.None, false)
            : (attribute.Operation, attribute.AllowAnonymous);
    }

    // An async method reports the denial through its task, as it would any other failure
    private static void Deny(IInvocation invocation, NotaryAuthorizationException denial)
    {
        var returnType = invocation.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            invocation.ReturnValue = Task.FromException(denial);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            invocation.ReturnValue = s_fromException
                .MakeGenericMethod(returnType.GetGenericArguments()[0])
                .Invoke(null, [denial]);
        }
        else
        {
            throw denial;
        }
    }
}
