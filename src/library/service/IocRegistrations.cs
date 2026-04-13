using Autofac;
using Notary.Interface.Service;
using Notary.IOC;

namespace Notary.Service;

public class IocRegistrations : Module, IRegister
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AccountService>()
            .As<IAccountService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<AsymmetricKeyService>()
            .As<IAsymmetricKeyService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CertificateAuthorityService>()
            .As<ICertificateAuthorityService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CertificateRevokeService>()
            .As<ICertificateRevokeService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CertificateService>()
            .As<ICertificateService>()
            .InstancePerLifetimeScope();
    }
}