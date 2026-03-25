using Autofac;
using Notary.Data;

namespace Notary.Service;

public static class RegisterModules
{
    public static void Register(ContainerBuilder builder)
    {
        builder.RegisterModule(new IocRegistration());
        builder.RegisterModule(new IocRegistrations());
    }
}