using System.Text;

using Notary.Configuration;
using Notary.Contract;
using Notary.Service;

using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Pkcs;

namespace Notary.Test;

[TestFixture]
public class AsymmetricKeyMigrationTest
{
    private const string AppKey = "unit-test-application-key-0123456789";
    private const string Pem = "-----BEGIN ENCRYPTED PRIVATE KEY-----";

    private FakeKeyRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new FakeKeyRepository();
    }

    private AsymmetricKeyService CreateService(string? applicationKey = AppKey) =>
        new(_repository, Mock.Of<ILog>(), new NotaryConfiguration { ApplicationKey = applicationKey });

    private static AsymmetricCipherKeyPair NewKeyPair(Algorithm algorithm)
    {
        if (algorithm == Algorithm.RSA)
        {
            var rsa = new RsaKeyPairGenerator();
            rsa.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            return rsa.GenerateKeyPair();
        }

        var ec = new ECKeyPairGenerator();
        ec.Init(new ECKeyGenerationParameters(NistNamedCurves.GetOid("P-256"), new SecureRandom()));
        return ec.GenerateKeyPair();
    }

    private static byte[] ToPem(Pkcs8Generator generator)
    {
        using var writer = new StringWriter();
        new PemWriter(writer).WriteObject(generator);
        return Encoding.ASCII.GetBytes(writer.ToString());
    }

    private static byte[] PlaintextPem(AsymmetricKeyParameter key) => ToPem(new Pkcs8Generator(key));

    // The format used before the plaintext defect: PKCS #8 with PBES1 SHA-1 / 3DES
    private static byte[] LegacyPem(AsymmetricKeyParameter key, string password) =>
        ToPem(new Pkcs8Generator(key, Pkcs8Generator.PbeSha1_3DES)
        {
            Password = password.ToCharArray(),
            SecureRandom = new SecureRandom(),
            IterationCount = 2048
        });

    private async Task<AsymmetricKey> AddRowAsync(byte[] storedPem, Algorithm algorithm, string name, bool active = true)
    {
        var key = new AsymmetricKey
        {
            Active = active,
            KeyAlgorithm = algorithm,
            Name = name,
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddDays(1),
            EncryptedPrivateKey = storedPem
        };
        await _repository.SaveAsync(key);
        return key;
    }

    private static string AsText(AsymmetricKey key) => Encoding.ASCII.GetString(key.EncryptedPrivateKey);

    private static void AssertSignsWith(AsymmetricCipherKeyPair loaded, AsymmetricCipherKeyPair original, Algorithm algorithm)
    {
        var signatureAlgorithm = algorithm == Algorithm.RSA ? "SHA256withRSA" : "SHA256withECDSA";
        var message = Encoding.UTF8.GetBytes("notary");
        var signer = SignerUtilities.GetSigner(signatureAlgorithm);
        signer.Init(true, loaded.Private);
        signer.BlockUpdate(message, 0, message.Length);
        var signature = signer.GenerateSignature();

        var verifier = SignerUtilities.GetSigner(signatureAlgorithm);
        verifier.Init(false, original.Public);
        verifier.BlockUpdate(message, 0, message.Length);
        Assert.That(verifier.VerifySignature(signature), Is.True);
    }

    [TestCase(Algorithm.RSA)]
    [TestCase(Algorithm.EllipticCurve)]
    public async Task Migrate_PlaintextRow_IsEncryptedAndStillSignsWithTheSameKey(Algorithm algorithm)
    {
        var original = NewKeyPair(algorithm);
        var row = await AddRowAsync(PlaintextPem(original.Private), algorithm, "plain");
        var service = CreateService();

        var result = await service.MigrateStoredKeysAsync();

        Assert.That(result, Is.EqualTo(new KeyMigrationResult(1, 0, 0)));
        var stored = await _repository.GetAsync(row.Slug);
        Assert.That(AsText(stored), Does.StartWith(Pem));
        AssertSignsWith((await service.GetKeyPairAsync(row.Slug))!, original, algorithm);
        Assert.That(stored.UpdatedBySlug, Is.EqualTo("key-migration"));
    }

    [Test]
    public async Task Migrate_LegacyTripleDesRow_IsUpgradedToPbes2()
    {
        var original = NewKeyPair(Algorithm.EllipticCurve);
        var row = await AddRowAsync(LegacyPem(original.Private, AppKey), Algorithm.EllipticCurve, "legacy");
        var service = CreateService();

        var result = await service.MigrateStoredKeysAsync();

        Assert.That(result, Is.EqualTo(new KeyMigrationResult(1, 0, 0)));
        var stored = await _repository.GetAsync(row.Slug);
        var base64 = string.Concat(AsText(stored)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !l.StartsWith("-----")));
        var info = EncryptedPrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(Convert.FromBase64String(base64)));
        Assert.That(info.EncryptionAlgorithm.Algorithm, Is.EqualTo(PkcsObjectIdentifiers.IdPbeS2));
        AssertSignsWith((await service.GetKeyPairAsync(row.Slug))!, original, Algorithm.EllipticCurve);
    }

    [Test]
    public async Task Migrate_CurrentFormatRow_IsLeftUntouched()
    {
        var service = CreateService();
        var current = new AsymmetricKey
        {
            Active = true,
            KeyAlgorithm = Algorithm.EllipticCurve,
            KeyCurve = EllipticCurve.P256,
            Name = "current",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddDays(1)
        };
        await service.SaveAsync(current, "tester");
        var before = (byte[])current.EncryptedPrivateKey.Clone();

        var result = await service.MigrateStoredKeysAsync();

        Assert.That(result, Is.EqualTo(new KeyMigrationResult(0, 1, 0)));
        Assert.That((await _repository.GetAsync(current.Slug)).EncryptedPrivateKey, Is.EqualTo(before));
    }

    [Test]
    public async Task Migrate_SecondRun_ChangesNothing()
    {
        var service = CreateService();
        var plain = await AddRowAsync(PlaintextPem(NewKeyPair(Algorithm.EllipticCurve).Private), Algorithm.EllipticCurve, "plain");
        await AddRowAsync(LegacyPem(NewKeyPair(Algorithm.RSA).Private, AppKey), Algorithm.RSA, "legacy");

        var first = await service.MigrateStoredKeysAsync();
        var afterFirst = (byte[])(await _repository.GetAsync(plain.Slug)).EncryptedPrivateKey.Clone();
        var second = await service.MigrateStoredKeysAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new KeyMigrationResult(2, 0, 0)));
            Assert.That(second, Is.EqualTo(new KeyMigrationResult(0, 2, 0)));
        });
        Assert.That((await _repository.GetAsync(plain.Slug)).EncryptedPrivateKey, Is.EqualTo(afterFirst));
    }

    [Test]
    public async Task Migrate_BadRows_AreCountedLeftUnchangedAndDoNotStopTheRest()
    {
        var service = CreateService();
        var garbage = await AddRowAsync(Encoding.ASCII.GetBytes("not a key"), Algorithm.RSA, "garbage");
        var empty = await AddRowAsync([], Algorithm.RSA, "empty");
        var otherPassphrase = await AddRowAsync(
            LegacyPem(NewKeyPair(Algorithm.EllipticCurve).Private, "some other passphrase"), Algorithm.EllipticCurve, "other");
        var good = await AddRowAsync(PlaintextPem(NewKeyPair(Algorithm.EllipticCurve).Private), Algorithm.EllipticCurve, "good");
        var garbageBefore = (byte[])garbage.EncryptedPrivateKey.Clone();
        var otherBefore = (byte[])otherPassphrase.EncryptedPrivateKey.Clone();

        var result = await service.MigrateStoredKeysAsync();

        Assert.That(result, Is.EqualTo(new KeyMigrationResult(1, 0, 3)));
        Assert.That((await _repository.GetAsync(garbage.Slug)).EncryptedPrivateKey, Is.EqualTo(garbageBefore));
        Assert.That((await _repository.GetAsync(empty.Slug)).EncryptedPrivateKey, Is.Empty);
        Assert.That((await _repository.GetAsync(otherPassphrase.Slug)).EncryptedPrivateKey, Is.EqualTo(otherBefore));
        Assert.That(AsText(await _repository.GetAsync(good.Slug)), Does.StartWith(Pem));
    }

    [Test]
    public async Task Migrate_InactiveRow_IsAlsoMigrated()
    {
        var row = await AddRowAsync(
            PlaintextPem(NewKeyPair(Algorithm.EllipticCurve).Private), Algorithm.EllipticCurve, "retired", active: false);

        var result = await CreateService().MigrateStoredKeysAsync();

        Assert.That(result, Is.EqualTo(new KeyMigrationResult(1, 0, 0)));
        Assert.That(AsText(await _repository.GetAsync(row.Slug)), Does.StartWith(Pem));
    }

    [Test]
    public void Migrate_WithoutApplicationKey_Throws()
    {
        Assert.That(async () => await CreateService(null).MigrateStoredKeysAsync(), Throws.InvalidOperationException);
    }

    [Test]
    public async Task Load_BeforeMigration_RefusesPlaintextRatherThanUsingItUnprotected()
    {
        var row = await AddRowAsync(PlaintextPem(NewKeyPair(Algorithm.EllipticCurve).Private), Algorithm.EllipticCurve, "plain");

        Assert.That(async () => await CreateService().GetKeyPairAsync(row.Slug), Throws.InvalidOperationException);
    }
}
