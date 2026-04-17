using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents a successful result containing a value of type <typeparamref name="T"/>.
/// </summary>
public sealed record Success<T>(T Value)
{
    public override string ToString() =>
        $"Success({Value})";
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value of type <typeparamref name="T"/> or fail with an <see cref="common.Error"/>.
/// </summary>
public readonly union Result<T>(Success<T>, Error) : IEquatable<Result<T>>
{
    public bool IsSuccess => this is Success<T>;

    public bool IsError => this is Error;

    public static implicit operator Result<T>(Error error) =>
        new(error);

    public override string ToString() =>
        this switch
        {
            null => "<null>",
            Success<T> { Value: var value } => $"Success: {value}",
            Error error => $"Error: {error}",
        };

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Result<T> other && Equals(other);

    public bool Equals(Result<T> other) =>
        (this, other) switch
        {
            (Error, Error) => true,
            (Success<T> success1, Success<T> success2) => success1.Equals(success2),
            _ => false
        };

    public override int GetHashCode() =>
        this switch
        {
            Error => 0,
            Success<T> success => success.GetHashCode(),
            _ => 0
        };

    public static bool operator ==(Result<T> left, Result<T> right) =>
        left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) =>
        !left.Equals(right);
}

public static class Result
{
    /// <summary>
    /// Wraps <paramref name="value"/> in a successful <see cref="Result{T}"/>.
    /// </summary>
    public static Result<T> Success<T>(T value) =>
        new(new Success<T>(value));

    /// <summary>
    /// Wraps <paramref name="error"/> in a failed <see cref="Result{T}"/>.
    /// </summary>
    public static Result<T> Error<T>(Error error) =>
        new(error);

    /// <summary>
    /// Applies <paramref name="f"/> to the success value.
    /// </summary>
    /// <returns><c>Success(f(value))</c> if successful, otherwise the original error.</returns>
    public static Result<T2> Map<T, T2>(this Result<T> result, Func<T, T2> f) =>
        result switch
        {
            Success<T> { Value: var t } => Success(f(t)),
            Error error => Error<T2>(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Asynchronously applies <paramref name="f"/> to the success value.
    /// </summary>
    /// <returns><c>Success(await f(value))</c> if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> MapTask<T, T2>(this Result<T> result, Func<T, ValueTask<T2>> f) =>
        result switch
        {
            Success<T> { Value: var t } => Success(await f(t)),
            Error error => Error<T2>(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Applies <paramref name="f"/> to the error.
    /// </summary>
    /// <returns>The original success, or <c>Error(f(error))</c> if error.</returns>
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> f) =>
        result switch
        {
            Success<T> => result,
            Error error => Error<T>(f(error)),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>f(value)</c> if successful, otherwise the original error.</returns>
    public static Result<T2> Bind<T, T2>(this Result<T> result, Func<T, Result<T2>> f) =>
        result switch
        {
            Success<T> { Value: var t } => f(t),
            Error error => Error<T2>(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Applies <paramref="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>await f(value)</c> if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> BindTask<T, T2>(this Result<T> result, Func<T, ValueTask<Result<T2>>> f) =>
        result switch
        {
            Success<T> { Value: var t } => await f(t),
            Error error => Error<T2>(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// LINQ projection support. Enables syntax <c>from value in result select value</c>
    /// </summary>
    public static Result<T2> Select<T, T2>(this Result<T> result, Func<T, T2> f) =>
        result.Map(f);

    /// <summary>
    /// LINQ flattening support. Enables syntax <c>from x in result1 from y in result2 select x + y</c>
    /// </summary>
    public static Result<TResult> SelectMany<T, T2, TResult>(this Result<T> result, Func<T, Result<T2>> f,
                                                             Func<T, T2, TResult> selector) =>
        result.Bind(value => f(value)
              .Map(value2 => selector(value, value2)));

    /// <summary>
    /// Matches the result and returns the result of the corresponding function.
    /// </summary>
    public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> onSuccess, Func<Error, TResult> onError) =>
        result switch
        {
            Success<T> { Value: var t } => onSuccess(t),
            Error error => onError(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Matches the result and executes the corresponding action.
    /// </summary>
    public static void Match<T>(this Result<T> result, Action<T> onSuccess, Action<Error> onError)
    {
        switch (result)
        {
            case Success<T> { Value: var t }:
                onSuccess(t);
                break;
            case Error error:
                onError(error);
                break;
            case null:
                throw new InvalidOperationException("Result cannot be null.");
        }
    }

    /// <summary>
    /// Returns the success value if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static T IfError<T>(this Result<T> result, Func<Error, T> f) =>
        result switch
        {
            Success<T> { Value: var t } => t,
            Error error => f(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Returns this result if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static Result<T> IfError<T>(this Result<T> result, Func<Error, Result<T>> f) =>
        result switch
        {
            Success<T> => result,
            Error error => f(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the result is an error.
    /// </summary>
    public static void IfError<T>(this Result<T> result, Action<Error> f)
    {
        switch (result)
        {
            case Error error:
                f(error);
                break;
        }
    }

    /// <summary>
    /// Returns the success value if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<T> IfErrorTask<T>(this Result<T> result, Func<Error, ValueTask<T>> f) =>
        result switch
        {
            Success<T> { Value: var t } => t,
            Error error => await f(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Returns this result if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<Result<T>> IfErrorTask<T>(this Result<T> result, Func<Error, ValueTask<Result<T>>> f) =>
        result switch
        {
            Success<T> => result,
            Error error => await f(error),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the result is an error.
    /// </summary>
    public static async ValueTask IfErrorTask<T>(this Result<T> result, Func<Error, ValueTask> f)
    {
        switch (result)
        {
            case Error error:
                await f(error);
                break;
        }
    }

    /// <summary>
    /// Returns the success value or throws the error as an exception.
    /// </summary>
    /// <exception cref="Exception">Thrown when the result is an error.</exception>
    public static T IfErrorThrow<T>(this Result<T> result) =>
        result switch
        {
            Success<T> { Value: var t } => t,
            Error error => throw error.ToException(),
            null => throw new InvalidOperationException("Result cannot be null.")
        };

    /// <summary>
    /// Converts the result to a nullable reference type.
    /// </summary>
    /// <returns>The success value if successful, otherwise <see langword="null"/>.</returns>
    public static T? IfErrorNull<T>(this Result<T> result) where T : class =>
        result switch
        {
            Success<T> { Value: var t } => t,
            _ => null
        };

    /// <summary>
    /// Converts the result to a nullable value type.
    /// </summary>
    /// <returns>The success value if successful, otherwise <see langword="null"/>.</returns>
    public static T? IfErrorNullable<T>(this Result<T> result) where T : struct =>
        result switch
        {
            Success<T> { Value: var t } => t,
            _ => null
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the result is successful.
    /// </summary>
    public static void Iter<T>(this Result<T> result, Action<T> f)
    {
        if (result is Success<T> { Value: var t })
        {
            f(t);
        }
    }

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the result is successful.
    /// </summary>
    public static async ValueTask IterTask<T>(this Result<T> result, Func<T, ValueTask> f)
    {
        if (result is Success<T> { Value: var t })
        {
            await f(t);
        }
    }

    /// <summary>
    /// Converts the result to an <see cref="Option{T}"/>, discarding error information.
    /// </summary>
    /// <returns><c>Some(value)</c> if successful, otherwise <see cref="Option.None"/>.</returns>
    public static Option<T> ToOption<T>(this Result<T> result) =>
        result switch
        {
            Success<T> { Value: var t } => Option.Some(t),
            _ => Option.None
        };
}
