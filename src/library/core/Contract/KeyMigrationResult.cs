namespace Notary.Contract
{
    /// <summary>
    /// The outcome of bringing stored private keys up to the current encryption format
    /// </summary>
    /// <param name="Migrated">Keys that were re-encrypted</param>
    /// <param name="AlreadyEncrypted">Keys already in the current format, left untouched</param>
    /// <param name="Failed">Keys that could not be migrated and were left unchanged</param>
    public sealed record KeyMigrationResult(int Migrated, int AlreadyEncrypted, int Failed);
}
