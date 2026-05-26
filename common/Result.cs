using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value of type <typeparamref name="T"/> or fail with an <see cref="common.Error"/>.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly union Result<T>(Result<T>.Success, Error) : IEquatable<Result<T>>
{
    /// <summary>
    /// Gets whether the result is successful.
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Gets whether the result is an error.
    /// </summary>
    public bool IsError => this is Error;

    /// <summary>
    /// Matches the result and returns the result of the corresponding function.
    /// </summary>
    public T2 Match<T2>(Func<T, T2> onSuccess, Func<Error, T2> onError) =>
        this switch
        {
            Success { Value: var t } => onSuccess(t),
            Error error => onError(error),
            null => throw new UnreachableException("Result cannot be null.")
        };

    /// <summary>
    /// Matches the result and executes the corresponding action.
    /// </summary>
    public void Match(Action<T> onSuccess, Action<Error> onError)
    {
        switch (this)
        {
            case Success { Value: var t }:
                onSuccess(t);
                break;
            case Error error:
                onError(error);
                break;
            case null:
                throw new UnreachableException("Result cannot be null.");
        }
    }

    public override string ToString() =>
        this switch
        {
            null => "<null>",
            Success { Value: var value } => $"Success: {value}",
            Error error => $"Error: {error}",
        };

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Result<T> other && Equals(other);

    public bool Equals(Result<T> other) =>
        (this, other) switch
        {
            (Error error1, Error error2) => error1.Equals(error2),
            (Success success1, Success success2) => success1.Equals(success2),
            _ => false
        };

    public override int GetHashCode() =>
        this switch
        {
            Error error => error.GetHashCode(),
            Success success => success.GetHashCode(),
            _ => 0
        };

    public static implicit operator Result<T>(Error error) =>
        new(error);

    public static bool operator ==(Result<T> left, Result<T> right) =>
        left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) =>
        !left.Equals(right);

    /// <summary>
    /// Represents a successful result containing a value of type <typeparamref name="T"/>.
    /// </summary>
    public sealed record Success(T Value)
    {
        public override string ToString() =>
            $"Success({Value})";
    }
}

public static class Result
{
    /// <summary>
    /// Wraps <paramref name="value"/> in a successful <see cref="Result{T}"/>.
    /// </summary>
    public static Result<T> Success<T>(T value) =>
        new Result<T>.Success(value);

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
        result.Match(t => Success(f(t)),
                     Error<T2>);

    /// <summary>
    /// Asynchronously applies <paramref name="f"/> to the success value.
    /// </summary>
    /// <returns><c>Success(await f(value))</c> if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> MapTask<T, T2>(this Result<T> result, Func<T, ValueTask<T2>> f) =>
        await result.Match(async t => Success(await f(t)),
                           async error =>
                           {
                               await ValueTask.CompletedTask;
                               return Error<T2>(error);
                           });

    /// <summary>
    /// Applies <paramref name="f"/> to the error.
    /// </summary>
    /// <returns>The original success, or <c>Error(f(error))</c> if error.</returns>
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> f) =>
        result.Match(t => result,
                     error => Error<T>(f(error)));

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>f(value)</c> if successful, otherwise the original error.</returns>
    public static Result<T2> Bind<T, T2>(this Result<T> result, Func<T, Result<T2>> f) =>
        result.Match(f, Error<T2>);

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>await f(value)</c> if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> BindTask<T, T2>(this Result<T> result, Func<T, ValueTask<Result<T2>>> f) =>
        await result.Match(async t => await f(t),
                           async error =>
                           {
                               await ValueTask.CompletedTask;
                               return Error<T2>(error);
                           });

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
        result.Bind(t => f(t).Map(t2 => selector(t, t2)));

    /// <summary>
    /// Returns the success value if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static T IfError<T>(this Result<T> result, Func<Error, T> f) =>
        result.Match(t => t, f);

    /// <summary>
    /// Returns this result if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static Result<T> IfError<T>(this Result<T> result, Func<Error, Result<T>> f) =>
        result.Match(_ => result, f);

    /// <summary>
    /// Returns the success value if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<T> IfErrorTask<T>(this Result<T> result, Func<Error, ValueTask<T>> f) =>
        await result.Match(async t =>
                          {
                              await ValueTask.CompletedTask;
                              return t;
                          },
                          async error => await f(error));

    /// <summary>
    /// Returns this result if successful, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<Result<T>> IfErrorTask<T>(this Result<T> result, Func<Error, ValueTask<Result<T>>> f) =>
        await result.Match(async _ =>
                          {
                              await ValueTask.CompletedTask;
                              return result;
                          },
                          async error => await f(error));

    /// <summary>
    /// Returns the success value or throws the error as an exception.
    /// </summary>
    /// <exception cref="Exception">Thrown when the result is an error.</exception>
    public static T IfErrorThrow<T>(this Result<T> result) =>
        result.Match(t => t,
                     error => throw error.ToException());

    /// <summary>
    /// Converts the result to a nullable reference type.
    /// </summary>
    /// <returns>The success value if successful, otherwise <see langword="null"/>.</returns>
    public static T? IfErrorNull<T>(this Result<T> result) where T : class =>
        result.Match<T?>(t => t,
                         _ => null);

    /// <summary>
    /// Converts the result to a nullable value type.
    /// </summary>
    /// <returns>The success value if successful, otherwise <see langword="null"/>.</returns>
    public static T? IfErrorNullable<T>(this Result<T> result) where T : struct =>
        result.Match<T?>(t => t,
                         _ => null);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> when the result is successful.
    /// </summary>
    /// <returns>The original result.</returns>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> onSuccess) =>
        result.Match(t =>
                     {
                         onSuccess(t);
                         return result;
                     },
                     _ => result);

    /// <summary>
    /// Asynchronously executes <paramref name="onSuccess"/> when the result is successful.
    /// </summary>
    /// <returns>The original result.</returns>
    public static async ValueTask<Result<T>> TapTask<T>(this Result<T> result, Func<T, ValueTask> onSuccess) =>
        await result.Match(async t =>
                          {
                              await onSuccess(t);
                              return result;
                          },
                          async _ =>
                          {
                              await ValueTask.CompletedTask;
                              return result;
                          });

    /// <summary>
    /// Executes <paramref name="onError"/> when the result is an error.
    /// </summary>
    /// <returns>The original result.</returns>
    public static Result<T> TapError<T>(this Result<T> result, Action<Error> onError) =>
        result.Match(_ => result,
                     error =>
                     {
                         onError(error);
                         return result;
                     });

    /// <summary>
    /// Asynchronously executes <paramref name="onError"/> when the result is an error.
    /// </summary>
    /// <returns>The original result.</returns>
    public static async ValueTask<Result<T>> TapErrorTask<T>(this Result<T> result, Func<Error, ValueTask> onError) =>
        await result.Match(async _ =>
                           {
                               await ValueTask.CompletedTask;
                               return result;
                           },
                           async error =>
                           {
                               await onError(error);
                               return result;
                           });

    /// <summary>
    /// Executes <paramref name="f"/> when the result is successful.
    /// </summary>
    public static void Iter<T>(this Result<T> result, Action<T> f) =>
        result.Match(f, _ => { });

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the result is successful.
    /// </summary>
    public static ValueTask IterTask<T>(this Result<T> result, Func<T, ValueTask> f) =>
        result.Match(f, _ => ValueTask.CompletedTask);

    /// <summary>
    /// Executes <paramref name="f"/> when the result is an error.
    /// </summary>
    public static void IterError<T>(this Result<T> result, Action<Error> f) =>
        result.Match(_ => { }, f);

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the result is an error.
    /// </summary>
    public static ValueTask IterErrorTask<T>(this Result<T> result, Func<Error, ValueTask> f) =>
        result.Match(_ => ValueTask.CompletedTask, f);

    /// <summary>
    /// Converts the result to an <see cref="Option{T}"/>, discarding error information.
    /// </summary>
    /// <returns><see cref="Option.Some{T}(T)"/> with the success value if successful, otherwise <see cref="Option.None"/>.</returns>
    /// <seealso cref="Option.ToResult{T}(Option{T}, Func{Error})"/>
    public static Option<T> ToOption<T>(this Result<T> result) =>
        result.Match(Option.Some, _ => Option.None);
}
