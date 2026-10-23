using System.Reflection;
using System.Security.Claims;
using Castle.DynamicProxy;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Service;
using Notary.Security;
using Notary.Security.Interceptor;

namespace Notary.Test;

public class NotaryAuthorizationTest
{
    private static readonly ProxyGenerator s_generator = new();

    private sealed class FakeUserContext(ClaimsPrincipal user) : INotaryUserContext
    {
        public ClaimsPrincipal User { get; set; } = user;
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"));

    [TestCase(NotaryRoles.User, NotaryOperation.CertificateList, true)]
    [TestCase(NotaryRoles.User, NotaryOperation.CertificateDownload, true)]
    [TestCase(NotaryRoles.User, NotaryOperation.CertificateCreate, false)]
    [TestCase(NotaryRoles.User, NotaryOperation.CertificateRevoke, false)]
    [TestCase(NotaryRoles.User, NotaryOperation.CertificateAuthorityCreate, false)]
    [TestCase(NotaryRoles.User, NotaryOperation.AsymmetricKeyList, false)]
    [TestCase(NotaryRoles.Writer, NotaryOperation.CertificateCreate, true)]
    [TestCase(NotaryRoles.Writer, NotaryOperation.CertificateRevoke, true)]
    [TestCase(NotaryRoles.Writer, NotaryOperation.CertificateAuthorityCreate, false)]
    [TestCase(NotaryRoles.Writer, NotaryOperation.AsymmetricKeyDelete, false)]
    [TestCase(NotaryRoles.Admin, NotaryOperation.CertificateAuthorityCreate, true)]
    [TestCase(NotaryRoles.Admin, NotaryOperation.AsymmetricKeyDelete, true)]
    [TestCase("SomeOtherRole", NotaryOperation.CertificateList, false)]
    public void IsAllowed_ByRole(string role, NotaryOperation operation, bool expected)
    {
        Assert.That(NotaryRolePermissions.IsAllowed(UserWithRoles(role), operation), Is.EqualTo(expected));
    }

    [Test]
    public void IsAllowed_AdminHoldsEveryOperation()
    {
        var admin = UserWithRoles(NotaryRoles.Admin);

        foreach (var operation in Enum.GetValues<NotaryOperation>().Where(o => o != NotaryOperation.None))
        {
            Assert.That(NotaryRolePermissions.IsAllowed(admin, operation), Is.True, operation.ToString());
        }
    }

    [Test]
    public void IsAllowed_NoOperationOrUnauthenticatedUser_IsDenied()
    {
        Assert.That(NotaryRolePermissions.IsAllowed(UserWithRoles(NotaryRoles.Admin), NotaryOperation.None), Is.False);
        Assert.That(NotaryRolePermissions.IsAllowed(null, NotaryOperation.CertificateList), Is.False);

        // Carries the role claim but was never authenticated
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, NotaryRoles.Admin)]));
        Assert.That(NotaryRolePermissions.IsAllowed(unauthenticated, NotaryOperation.CertificateList), Is.False);
    }

    [Test]
    public void IsAllowed_MultipleRoles_GrantsTheUnion()
    {
        var user = UserWithRoles(NotaryRoles.User, NotaryRoles.Writer);

        Assert.That(NotaryRolePermissions.IsAllowed(user, NotaryOperation.CertificateCreate), Is.True);
        Assert.That(NotaryRolePermissions.IsAllowed(user, NotaryOperation.CertificateAuthorityCreate), Is.False);
    }

    public interface IWidgetService
    {
        Task<List<string>> ListAsync();
        Task SaveAsync();
        Task InitializeAsync();
        Task UndeclaredAsync();
        Task<byte[]> DownloadAsync();
    }

    public class WidgetService : IWidgetService
    {
        public int Calls { get; private set; }

        public Func<Task> Nested { get; set; }

        [NotaryAuthorize(NotaryOperation.CertificateList)]
        public virtual async Task<List<string>> ListAsync()
        {
            Calls++;
            if (Nested is not null)
            {
                await Task.Yield();
                await Nested();
            }
            return [];
        }

        [NotaryAuthorize(NotaryOperation.CertificateCreate)]
        public virtual Task SaveAsync()
        {
            Calls++;
            return Task.CompletedTask;
        }

        [NotaryAuthorize(NotaryOperation.None, allowAnonymous: true)]
        public virtual Task InitializeAsync() => Task.CompletedTask;

        public virtual Task UndeclaredAsync() => Task.CompletedTask;

        [NotaryAuthorize(NotaryOperation.CertificateDownload)]
        public virtual Task<byte[]> DownloadAsync() => Task.FromResult<byte[]>([1]);
    }

    private static (IWidgetService Proxy, WidgetService Target) Widget(ClaimsPrincipal user, NotaryAuthorization interceptor = null)
    {
        var target = new WidgetService();
        var proxy = s_generator.CreateInterfaceProxyWithTarget<IWidgetService>(
            target, interceptor ?? new NotaryAuthorization(new FakeUserContext(user)));
        return (proxy, target);
    }

    [Test]
    public async Task Intercept_AllowedRole_CallsTheService()
    {
        var (service, target) = Widget(UserWithRoles(NotaryRoles.User));

        await service.ListAsync();

        Assert.That(target.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Intercept_RoleWithoutOperation_IsDeniedAndTheServiceIsNotCalled()
    {
        var (service, target) = Widget(UserWithRoles(NotaryRoles.User));

        var denied = Assert.ThrowsAsync<NotaryAuthorizationException>(async () => await service.SaveAsync());

        Assert.That(denied.StatusCode, Is.EqualTo(401));
        Assert.That(denied.Operation, Is.EqualTo(NotaryOperation.CertificateCreate));
        Assert.That(target.Calls, Is.EqualTo(0));
    }

    [Test]
    public void Intercept_DenialIsReportedThroughTheTask()
    {
        var (service, _) = Widget(UserWithRoles());

        // The call itself does not throw; awaiting it does
        Task<byte[]> pending = null;
        Assert.DoesNotThrow(() => pending = service.DownloadAsync());
        Assert.That(pending.IsFaulted, Is.True);
    }

    [Test]
    public void Intercept_NoUser_IsDenied()
    {
        var (service, _) = Widget(null);

        Assert.ThrowsAsync<NotaryAuthorizationException>(async () => await service.ListAsync());
    }

    [Test]
    public void Intercept_AllowAnonymousMethod_RunsWithNoUser()
    {
        var (service, _) = Widget(null);

        Assert.DoesNotThrowAsync(async () => await service.InitializeAsync());
    }

    [Test]
    public void Intercept_MethodWithNoOperation_IsDeniedEvenToAdmin()
    {
        var (service, _) = Widget(UserWithRoles(NotaryRoles.Admin));

        Assert.ThrowsAsync<NotaryAuthorizationException>(async () => await service.UndeclaredAsync());
    }

    [Test]
    public async Task Intercept_CallsMadeWhileServingAnAuthorizedCall_AreTrusted()
    {
        // The user may list but not save
        var interceptor = new NotaryAuthorization(new FakeUserContext(UserWithRoles(NotaryRoles.User)));
        var (inner, innerTarget) = Widget(null, interceptor);
        var (outer, outerTarget) = Widget(null, interceptor);

        // A collaborator the listing calls is trusted, even after the call has gone async
        outerTarget.Nested = () => inner.SaveAsync();

        await outer.ListAsync();
        Assert.That(innerTarget.Calls, Is.EqualTo(1));

        // and the trust ends with the call: the same user is denied a direct save
        Assert.ThrowsAsync<NotaryAuthorizationException>(async () => await inner.SaveAsync());
    }

    [Test]
    public void ServiceClasses_EveryInterfaceMethodDeclaresAnOperation()
    {
        (Type Interface, Type Class)[] services =
        [
            (typeof(ICertificateService), typeof(CertificateService)),
            (typeof(ICertificateAuthorityService), typeof(CertificateAuthorityService)),
            (typeof(ICertificateRevokeService), typeof(CertificateRevokeService)),
            (typeof(IAsymmetricKeyService), typeof(AsymmetricKeyService))
        ];

        foreach (var (serviceInterface, serviceClass) in services)
        {
            var undeclared = new List<string>();

            foreach (var i in serviceInterface.GetInterfaces().Prepend(serviceInterface))
            {
                var map = serviceClass.GetInterfaceMap(i);
                foreach (var target in map.TargetMethods)
                {
                    if (target.GetCustomAttribute<NotaryAuthorizeAttribute>() is null)
                    {
                        undeclared.Add($"{serviceClass.Name}.{target.Name}");
                    }
                }
            }

            Assert.That(undeclared, Is.Empty, $"{serviceClass.Name} has methods that no role could call");
        }
    }
}
