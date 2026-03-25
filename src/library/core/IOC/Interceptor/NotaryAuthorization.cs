using Castle.DynamicProxy;

namespace Notary.IOC.Interceptor;

public class NotaryAuthorization : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
    }
}