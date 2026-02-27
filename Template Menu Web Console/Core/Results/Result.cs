using System;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Represents the outcome of an operation that either succeeds or fails with an <see cref="AppError"/>. Use <see cref="Result{T}"/> when the operation also returns a value.
    /// </summary>
    public class Result
    {
        /// <summary>Gets a value indicating whether the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>Gets the error when <see cref="IsSuccess"/> is <c>false</c>; otherwise <c>null</c>.</summary>
        public AppError? Error { get; }

        /// <summary>Initialises the result. Use the static factory methods instead of calling this directly.</summary>
        protected Result(bool isSuccess, AppError? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>Creates a successful <see cref="Result"/>.</summary>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed <see cref="Result"/> with the given error.
        /// </summary>
        /// <param name="error">The error describing the failure. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <c>null</c>.</exception>
        public static Result Failure(AppError error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));
    }

    /// <summary>
    /// Represents the outcome of an operation that either succeeds with a value of type <typeparamref name="T"/>, or fails with an <see cref="AppError"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public sealed class Result<T> : Result
    {
        /// <summary>Gets the value when <see cref="Result.IsSuccess"/> is <c>true</c>; otherwise <c>default</c>.</summary>
        public T? Value { get; }

        /// <summary>Initialises the result. Use the static factory methods instead of calling this directly.</summary>
        private Result(bool isSuccess, T? value, AppError? error)
            : base(isSuccess, error)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a successful <see cref="Result{T}"/> carrying the given value.
        /// </summary>
        /// <param name="value">The result value. May be <c>null</c> for reference and nullable types.</param>
        public static Result<T> Success(T? value) => new(true, value, null);

        /// <summary>
        /// Creates a failed <see cref="Result{T}"/> with the given error.
        /// </summary>
        /// <param name="error">The error describing the failure. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <c>null</c>.</exception>
        public static new Result<T> Failure(AppError error) => new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    }
}
