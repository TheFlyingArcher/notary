namespace Notary
{
    /// <summary>
    /// The category of a failed operation
    /// </summary>
    public enum ResultStatus
    {
        Success = 0,
        NotFound,
        Invalid,
        Conflict,
        Failed
    }

    /// <summary>
    /// The outcome of an operation that returns no value
    /// </summary>
    public sealed record Result(ResultStatus Status, string Error)
    {
        public bool IsSuccess => Status == ResultStatus.Success;

        public static Result Ok() => new(ResultStatus.Success, null);

        public static Result Fail(ResultStatus status, string error) => new(status, error);
    }

    /// <summary>
    /// The outcome of an operation that returns a value. Used for expected business failures instead of exceptions.
    /// </summary>
    public sealed record Result<T>(ResultStatus Status, T Value, string Error)
    {
        public bool IsSuccess => Status == ResultStatus.Success;

        public static Result<T> Ok(T value) => new(ResultStatus.Success, value, null);

        public static Result<T> Fail(ResultStatus status, string error) => new(status, default, error);
    }
}
