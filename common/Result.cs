using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
public sealed record Result<T>
{
    private readonly T? value;
    private readonly Error? error;
    private readonly bool isSuccess;

    private Result(T value)
    {
        this.value = value;
        isSuccess = true;
    }

    private Result(Error error)
    {
        this.error = error;
        isSuccess = false;
    }

    /// <summary>
    /// Gets whether this result represents a success.
    /// </summary>
    public bool IsSuccess => isSuccess;

    /// <summary>
    /// Gets whether this result represents an error.
    /// </summary>
    public bool IsError => isSuccess is false;

#pragma warning disable CA1000 // Do not declare static members on generic types
    internal static Result<T> Success(T value) =>
        new(value);

    internal static Result<T> Error(Error error) =>
        new(error);
#pragma warning restore CA1000 // Do not declare static members on generic types

    /// <summary>
    /// Pattern matches on the result state.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSuccess">Function executed if the result is successful.</param>
    /// <param name="onError">Function executed if the result is an error.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError) =>
        IsSuccess ? onSuccess(value!) : onError(error!);

    /// <summary>
    /// Pattern matches on the result state for side effects.
    /// </summary>
    /// <param name="onSuccess">Action executed if the result is successful.</param>
    /// <param name="onError">Action executed if the result is an error.</param>
    public void Match(Action<T> onSuccess, Action<Error> onError)
    {
        if (IsSuccess)
        {
            onSuccess(value!);
        }
        else
        {
            onError(error!);
        }
    }

    public override string ToString() =>
        IsSuccess ? $"Success: {value}" : $"Error: {error}";

    public bool Equals(Result<T>? other) =>
        (this, other) switch
        {
            (_, null) => false,
            ({ IsSuccess: true }, { IsSuccess: true }) =>
                EqualityComparer<T?>.Default.Equals(value, other.value),
            ({ IsError: true }, { IsError: true }) =>
                EqualityComparer<Error?>.Default.Equals(error, other.error),
            _ => false
        };

    public override int GetHashCode() =>
        HashCode.Combine(value, error);

    /// <summary>
    /// Implicitly converts a value to Success(value).
    /// </summary>
    public static implicit operator Result<T>(T value) =>
        Success(value);

    /// <summary>
    /// Implicitly converts an error to Error(error).
    /// </summary>
    public static implicit operator Result<T>(Error error) =>
        Error(error);
}

/// <summary>
/// Provides static methods for creating and working with Result instances.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>Success(value).</returns>
    public static Result<T> Success<T>(T value) =>
        Result<T>.Success(value);

    /// <summary>
    /// Creates an error result containing an error.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error to wrap.</param>
    /// <returns>Error(error).</returns>
    public static Result<T> Error<T>(Error error) =>
        Result<T>.Error(error);

    /// <summary>
    /// Transforms the success value using a function.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="f">The transformation function.</param>
    /// <returns>Success(f(value)) if successful, otherwise the original error.</returns>
    public static Result<T2> Map<T, T2>(this Result<T> result, Func<T, T2> f) =>
        result.Match(value => Success(f(value)),
                     error => Error<T2>(error));

    /// <summary>
    /// Asynchronously transforms the success value using a function that returns a ValueTask.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="f">The async transformation function.</param>
    /// <returns>Success(await f(value)) if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> MapTask<T, T2>(this Result<T> result, Func<T, ValueTask<T2>> f) =>
        await result.Match(async value => Success(await f(value)),
                           async error => await ValueTask.FromResult(Error<T2>(error)));

    /// <summary>
    /// Transforms the error, preserving any success value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="f">The error transformation function.</param>
    /// <returns>The original success, or Error(f(error)) if error.</returns>
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> f) =>
        result.Match(value => Success(value),
                     error => Error<T>(f(error)));

    /// <summary>
    /// Chains result operations together (monadic bind).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="f">The function that returns a result.</param>
    /// <returns>f(value) if successful, otherwise the original error.</returns>
    public static Result<T2> Bind<T, T2>(this Result<T> result, Func<T, Result<T2>> f) =>
        result.Match(value => f(value),
                     error => Error<T2>(error));

    /// <summary>
    /// Asynchronously chains result operations together (monadic bind with async function).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="f">The async function that returns a result.</param>
    /// <returns>await f(value) if successful, otherwise the original error.</returns>
    public static async ValueTask<Result<T2>> BindTask<T, T2>(this Result<T> result, Func<T, ValueTask<Result<T2>>> f) =>
        await result.Match(async value => await f(value),
                           async error => await ValueTask.FromResult(Error<T2>(error)));

    /// <summary>
    /// Projects the result value (LINQ support).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="f">The projection function.</param>
    /// <returns>The projected result.</returns>
    public static Result<T2> Select<T, T2>(this Result<T> result, Func<T, T2> f) =>
        result.Map(f);

    /// <summary>
    /// Projects and flattens nested results (LINQ support).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The intermediate value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="f">The function that returns an intermediate result.</param>
    /// <param name="selector">The result selector function.</param>
    /// <returns>The flattened result.</returns>
    public static Result<TResult> SelectMany<T, T2, TResult>(this Result<T> result, Func<T, Result<T2>> f,
                                                             Func<T, T2, TResult> selector) =>
        result.Bind(value => f(value)
              .Map(value2 => selector(value, value2)));

    /// <summary>
    /// Provides a fallback value for error results.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="f">Function that provides the fallback value.</param>
    /// <returns>The success value if successful, otherwise the fallback value.</returns>
    public static T IfError<T>(this Result<T> result, Func<Error, T> f) =>
        result.Match(value => value,
                     f);

    /// <summary>
    /// Provides a fallback result for error results.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="f">Function that provides the fallback result.</param>
    /// <returns>The original result if successful, otherwise the fallback.</returns>
    public static Result<T> IfError<T>(this Result<T> result, Func<Error, Result<T>> f) =>
        result.Match(_ => result,
                     f);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="f">The action to execute.</param>
    public static void Iter<T>(this Result<T> result, Action<T> f) =>
        result.Match(f,
                     _ => { });

    /// <summary>
    /// Executes an async action if the result is successful.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="f">The async action to execute.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async ValueTask IterTask<T>(this Result<T> result, Func<T, ValueTask> f) =>
        await result.Match<ValueTask>(async value => await f(value),
                                      _ => ValueTask.CompletedTask);

    /// <summary>
    /// Extracts the success value or throws the error as an exception.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <returns>The success value.</returns>
    /// <exception cref="Exception">Thrown when the result is an error.</exception>
    public static T IfErrorThrow<T>(this Result<T> result) =>
        result.Match(value => value,
                     error => throw error.ToException());

    /// <summary>
    /// Converts the result to a nullable reference type.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The success value if successful, otherwise null.</returns>
    public static T? IfErrorNull<T>(this Result<T> result) where T : class =>
        result.Match(value => (T?)value,
                     _ => null);

    /// <summary>
    /// Converts the result to a nullable value type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The success value if successful, otherwise null.</returns>
    public static T? IfErrorNullable<T>(this Result<T> result) where T : struct =>
        result.Match(value => (T?)value,
                     _ => null);

    /// <summary>
    /// Converts a result to an option, discarding error information.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>Some(value) if the result is successful, otherwise None.</returns>
    public static Option<T> ToOption<T>(this Result<T> result) =>
        result.Match(Option.Some, _ => Option.None);
}
