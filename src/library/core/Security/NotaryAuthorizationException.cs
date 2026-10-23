namespace Notary.Security;

/// <summary>
/// The caller is not permitted to perform an operation
/// </summary>
public class NotaryAuthorizationException(NotaryOperation operation, string message) : NotaryException(message)
{
    /// <summary>
    /// The HTTP status code to report for this failure
    /// </summary>
    public int StatusCode => 401;

    public NotaryOperation Operation { get; } = operation;
}
