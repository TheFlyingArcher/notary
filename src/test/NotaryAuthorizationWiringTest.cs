using System.Security.Claims;
using Autofac;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Security;
using Notary.Service;

namespace Notary.Test;

/// <summary>
/// Checks that the container hands out services that enforce authorization, not just that the interceptor works
/// </summary>
public class NotaryAuthorizationWiringTest
{
    private sealed class FakeUserContext(ClaimsPrincipal user) : INotaryUserContext
    {
        public ClaimsPrincipal User { get; } = user;
    }

    private static IContainer BuildContainer(ClaimsPrincipal user, Mock<ICertificateRepository> certificates)
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(new NotaryConfiguration());
        builder.RegisterInstance(new Mock<ILog>().Object);
        builder.RegisterInstance(certificates.Object);
        builder.RegisterInstance(new Mock<ICertificateAuthorityRepository>().Object);
        builder.RegisterInstance(new Mock<IAsymmetricKeyRepository>().Object);
        builder.RegisterInstance(new Mock<IRevocationRepository>().Object);
        builder.RegisterInstance(new Mock<ICrlRecordRepository>().Object);
        builder.RegisterInstance(new Mock<ICrlBaseUrlProvider>().Object);
        builder.RegisterModule(new IocRegistrations());

        if (user is not null)
        {
            // Registered after the module, as a host does, so it replaces the module's default
            builder.RegisterInstance(new FakeUserContext(user)).As<INotaryUserContext>();
        }

        return builder.Build();
    }

    private static ClaimsPrincipal UserWithRole(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

    [Test]
    public async Task ResolvedService_AuthorizedUser_ReachesTheRepository()
    {
        var certificates = new Mock<ICertificateRepository>();
        certificates.Setup(r => r.GetAllAsync()).ReturnsAsync([new Certificate()]);
        using var container = BuildContainer(UserWithRole(NotaryRoles.User), certificates);

        var result = await container.Resolve<ICertificateService>().GetAllAsync();

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResolvedService_UnauthorizedUser_IsDenied()
    {
        var certificates = new Mock<ICertificateRepository>();
        using var container = BuildContainer(UserWithRole(NotaryRoles.User), certificates);

        Assert.ThrowsAsync<NotaryAuthorizationException>(
            async () => await container.Resolve<ICertificateService>().SaveAsync(new Certificate(), null));
        certificates.Verify(r => r.SaveAsync(It.IsAny<Certificate>()), Times.Never);
    }

    [Test]
    public void ResolvedService_HostWithNoUser_IsDeniedExceptAnonymousMethods()
    {
        using var container = BuildContainer(null, new Mock<ICertificateRepository>());

        Assert.ThrowsAsync<NotaryAuthorizationException>(
            async () => await container.Resolve<ICertificateService>().GetAllAsync());
        Assert.DoesNotThrowAsync(async () => await container.Resolve<ICertificateService>().InitializeAsync());
        Assert.DoesNotThrowAsync(async () => await container.Resolve<ICertificateRevokeService>().GetCrlAsync(" "));
    }
}
