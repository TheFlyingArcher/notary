using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Generators;

namespace Notary.Test;

/// <summary>
/// In-memory stand-ins for storage and key services, shared by tests that issue real certificates.
/// </summary>

internal class FakeRepository<T> : IRepository<T> where T : Entity
{
    protected readonly Dictionary<string, T> Items = [];

    public Task DeleteAsync(string slug, string updatedBySlug)
    {
        if (Items.TryGetValue(slug, out var item))
            item.Active = false;
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(string id, string updatedBySlug) => DeleteAsync(id, updatedBySlug);

    public Task<T> GetAsync(string slug) => Task.FromResult(Items.TryGetValue(slug, out var item) ? Clone(item) : null);

    public Task<List<T>> GetAllAsync() => Task.FromResult(Items.Values.Select(Clone).ToList());

    public Task<T> GetByIdAsync(string id) => GetAsync(id);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task SaveAsync(T entity)
    {
        if (string.IsNullOrEmpty(entity.Slug))
            entity.Slug = entity.Slugify();

        Items[entity.Slug] = Clone(entity);
        return Task.CompletedTask;
    }

    // Mimic a database: callers get copies, so changes only persist through SaveAsync
    protected static T Clone(T item) => (T)typeof(object)
        .GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .Invoke(item, null)!;
}

internal class FakeCertificateRepository : FakeRepository<Certificate>, ICertificateRepository
{
    public Task<List<Certificate>> GetCertificatesByCaAsync(string caSlug) =>
        Task.FromResult(Items.Values.Where(c => c.IssuingSlug == caSlug).Select(Clone).ToList());

    public Task<List<Certificate>> GetSelfSignedAsync() =>
        Task.FromResult(Items.Values.Where(c => !c.IsCaCertificate && string.IsNullOrEmpty(c.IssuingSlug)).Select(Clone).ToList());
}

internal class FakeCrlRepository : FakeRepository<CrlRecord>, ICrlRecordRepository
{
}

internal class FakeRevocationRepository : FakeRepository<Revocation>, IRevocationRepository
{
    public Task<List<Revocation>> GetActiveByIssuerAsync(string issuerSlug) =>
        Task.FromResult(Items.Values.Where(r => r.Active && r.IssuerSlug == issuerSlug).Select(Clone).ToList());

    public Task<Revocation> GetActiveByCertificateAsync(string certificateSlug) =>
        Task.FromResult(Items.Values.Where(r => r.Active && r.CertificateSlug == certificateSlug).Select(Clone).FirstOrDefault());
}

internal class FakeKeyService : IAsymmetricKeyService
{
    private readonly Dictionary<string, (AsymmetricKey Key, AsymmetricCipherKeyPair Pair)> _keys = [];

    public Task SaveAsync(AsymmetricKey entity, string updatedBySlug)
    {
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom()));

        entity.Slug = Guid.NewGuid().ToString("N");
        _keys[entity.Slug] = (entity, generator.GenerateKeyPair());
        return Task.CompletedTask;
    }

    public Task<AsymmetricKey> GetAsync(string slug) => Task.FromResult(_keys.TryGetValue(slug, out var k) ? k.Key : null);

    public Task<AsymmetricCipherKeyPair> GetKeyPairAsync(string slug) => Task.FromResult(_keys.TryGetValue(slug, out var k) ? k.Pair : null);

    public Task<byte[]> GetPublicKey(string slug) => throw new NotSupportedException();

    public Task<KeyMigrationResult> MigrateStoredKeysAsync() => throw new NotSupportedException();

    public Task DeleteAsync(string slug, string updatedBySlug) => throw new NotSupportedException();

    public Task<List<AsymmetricKey>> GetAllAsync() => throw new NotSupportedException();

    public Task InitializeAsync() => Task.CompletedTask;
}


internal class FakeCertificateAuthorityRepository : FakeRepository<CertificateAuthority>, ICertificateAuthorityRepository
{
    public Task<CertificateAuthority> GetByCertificateSlugAsync(string certificateSlug) =>
        Task.FromResult(Items.Values.Where(c => c.CertificateSlug == certificateSlug).Select(Clone).FirstOrDefault());
}
