using Autofac;
using Autofac.Extras.DynamicProxy;

using Notary.Interface.Service;
using Notary.IOC;
using Notary.Security;
using Notary.Security.Interceptor;

namespace Notary.Service
{
    public class IocRegistrations : Module, IRegister
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Hosts with a signed-in user register their own INotaryUserContext after this module, which replaces
            // this one. Without one, every call that requires a role is denied.
            builder.RegisterType<NoUserContext>()
                .As<INotaryUserContext>()
                .SingleInstance();

            builder.RegisterType<NotaryAuthorization>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AsymmetricKeyService>()
                .As<IAsymmetricKeyService>()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(NotaryAuthorization));

            builder.RegisterType<CertificateAuthorityService>()
                .As<ICertificateAuthorityService>()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(NotaryAuthorization));

            builder.RegisterType<CertificateRevokeService>()
                .As<ICertificateRevokeService>()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(NotaryAuthorization));

            builder.RegisterType<CertificateService>()
                .As<ICertificateService>()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(NotaryAuthorization));
        }
    }
}
