using Notary.Contract;
using Notary.Interface.Service;

using Org.BouncyCastle.Crypto;

using System.Threading.Tasks;

namespace Notary.Interface.Service
{
    public interface IAsymmetricKeyService : IEntityService<AsymmetricKey>
    {
        /// <summary>
        /// Get a public/private key combination
        /// </summary>
        /// <param name="slug">The key slub</param>
        /// <returns></returns>
        Task<AsymmetricCipherKeyPair> GetKeyPairAsync(string slug);

        /// <summary>
        /// Get a public key from the private key
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<byte[]> GetPublicKey(string slug);

        /// <summary>
        /// Re-encrypt every stored private key that is plaintext or uses an older encryption format. Keys already
        /// in the current format are skipped, so this is safe to run on every start. A key that cannot be migrated
        /// is left unchanged and counted as failed.
        /// </summary>
        /// <returns>How many keys were migrated, already current, and failed</returns>
        Task<KeyMigrationResult> MigrateStoredKeysAsync();
    }
}