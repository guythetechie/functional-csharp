using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Singleton that represents an empty option.
/// </summary>
public readonly record struct None
{
    public override string ToString() =>
        "None";

    public override int GetHashCode() => 0;
}

/// <summary>
/// Represents a value that may or may not exist.
/// </summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
public readonly union Option<T>(Option<T>.Some, None) : IEquatable<Option<T>>
{
    /// <summary>
    /// Gets whether the option contains a value.
    /// </summary>
    public bool IsSome => this is Some;

    /// <summary>
    /// Gets whether the option contains no value.
    /// </summary>
    public bool IsNone => this is None;

    /// <summary>
    /// Matches the option and returns the result of the corresponding function.
    /// </summary>
    public T2 Match<T2>(Func<T, T2> onSome, Func<T2> onNone) =>
        this switch
        {
            Some { Value: var t } => onSome(t),
            common.None => onNone(),
            null => throw new UnreachableException($"Option cannot be null")
        };

    /// <summary>
    /// Matches the option and executes the corresponding action.
    /// </summary>
    public void Match(Action<T> onSome, Action onNone)
    {
        switch (this)
        {
            case Some { Value: var t }:
                onSome(t);
                break;
            case common.None:
                onNone();
                break;
            case null:
                throw new UnreachableException($"Option cannot be null");
        }
    }

    public override string ToString() =>
        this switch
        {
            null => "<null>",
            Some { Value: var value } => $"Some: {value}",
            common.None => "None",
        };

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Option<T> other && Equals(other);

    public bool Equals(Option<T> other) =>
        (this, other) switch
        {
            (common.None, common.None) => true,
            (Some some1, Some some2) => some1.Equals(some2),
            _ => false
        };

    public override int GetHashCode() =>
        this switch
        {
            Some some => some.GetHashCode(),
            _ => 0
        };

    public static implicit operator Option<T>(None none) =>
        new(none);

    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static Option<T> None { get; } = new None();
#pragma warning restore CA1000 // Do not declare static members on generic types

    /// <summary>
    /// Wraps a value of <typeparamref name="T"/>.
    /// </summary>
    public sealed record Some(T Value)
    {
        public override string ToString() =>
            $"Some({Value})";
    }
}

public static class Option
{
    /// <summary>
    /// Wraps <paramref name="value"/> in an <see cref="Option{T}"/>.
    /// </summary>
    public static Option<T> Some<T>(T value) =>
        new Option<T>.Some(value);

    /// <summary>
    /// Option without a value.
    /// </summary>
    public static None None { get; }

    /// <summary>
    /// Returns the original option when it is Some and <paramref name="predicate"/> returns true; otherwise returns <see cref="common.None"/>.
    /// </summary>
    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option.Match(t => predicate(t) ? option : None,
                     () => None);

    /// <summary>
    /// Applies <paramref name="f"/> to the wrapped value.
    /// </summary>
    /// <returns><c>Some(f(value))</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static Option<T2> Map<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Match(t => Some(f(t)),
                     () => None);

    /// <summary>
    /// Asynchronously applies <paramref name="f"/> to the wrapped value.
    /// </summary>
    /// <returns><c>Some(await f(value))</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static async ValueTask<Option<T2>> MapTask<T, T2>(this Option<T> option, Func<T, ValueTask<T2>> f) =>
        await option.Match(async t => Some(await f(t)),
                           async () =>
                           {
                               await ValueTask.CompletedTask;
                               return Option<T2>.None;
                           });

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>f(value)</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static Option<T2> Bind<T, T2>(this Option<T> option, Func<T, Option<T2>> f) =>
        option.Match(f, () => None);

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>await f(value)</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static async ValueTask<Option<T2>> BindTask<T, T2>(this Option<T> option, Func<T, ValueTask<Option<T2>>> f) =>
        await option.Match(async t => await f(t),
                           async () =>
                           {
                               await ValueTask.CompletedTask;
                               return Option<T2>.None;
                           });

    /// <summary>
    /// LINQ projection support. Enables syntax <c>from value in option select value</c>
    /// </summary>
    public static Option<T2> Select<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Map(f);

    /// <summary>
    /// LINQ flattening support. Enables syntax <c>from x in option1 from y in option2 select x + y</c>
    /// </summary>
    public static Option<TResult> SelectMany<T, T2, TResult>(this Option<T> option,
                                                             Func<T, Option<T2>> f,
                                                             Func<T, T2, TResult> selector) =>
        option.Bind(t => f(t).Map(t2 => selector(t, t2)));

    /// <summary>
    /// Returns the wrapped value if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static T IfNone<T>(this Option<T> option, Func<T> f) =>
        option.Match(t => t, f);

    /// <summary>
    /// Returns this option if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static Option<T> IfNone<T>(this Option<T> option, Func<Option<T>> f) =>
        option.Match(_ => option, f);

    /// <summary>
    /// Returns the wrapped value if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<T> IfNoneTask<T>(this Option<T> option, Func<ValueTask<T>> f) =>
        await option.Match(async t =>
                           {
                               await ValueTask.CompletedTask;
                               return t;
                           },
                           async () => await f());

    /// <summary>
    /// Returns this option if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<Option<T>> IfNoneTask<T>(this Option<T> option, Func<ValueTask<Option<T>>> f) =>
        await option.Match(async _ =>
                           {
                               await ValueTask.CompletedTask;
                               return option;
                           },
                           async () => await f());

    /// <summary>
    /// Converts the option to a nullable reference type.
    /// </summary>
    /// <returns>The wrapped value if Some, otherwise <see langword="null"/>.</returns>
    public static T? IfNoneNull<T>(this Option<T> option) where T : class =>
        option.Match<T?>(t => t,
                         () => null);

    /// <summary>
    /// Converts the option to a nullable value type.
    /// </summary>
    /// <returns>The wrapped value if Some, otherwise <see langword="null"/>.</returns>
    public static T? IfNoneNullable<T>(this Option<T> option) where T : struct =>
        option.Match<T?>(t => t,
                         () => null);

    /// <summary>
    /// Unwraps the option.
    /// </summary>
    /// <returns>The wrapped value if Some, otherwise throws the exception returned by <paramref name="f"/>.</returns>
    /// <exception cref="Exception">Thrown when the option is None.</exception>
    public static T IfNoneThrow<T>(this Option<T> option, Func<Exception> f) =>
        option.Match(t => t,
                     () => throw f());

    /// <summary>
    /// Executes <paramref name="onSome"/> when the option is Some.
    /// </summary>
    /// <returns>The original option.</returns>
    public static Option<T> Tap<T>(this Option<T> option, Action<T> onSome) =>
        option.Match(t =>
                     {
                         onSome(t);
                         return option;
                     },
                     () => option);

    /// <summary>
    /// Asynchronously executes <paramref name="onSome"/> when the option is Some.
    /// </summary>
    /// <returns>The original option.</returns>
    public static async ValueTask<Option<T>> TapTask<T>(this Option<T> option, Func<T, ValueTask> onSome) =>
        await option.Match(async t =>
                          {
                              await onSome(t);
                              return option;
                          },
                          async () =>
                          {
                              await ValueTask.CompletedTask;
                              return option;
                          });

    /// <summary>
    /// Executes <paramref name="onNone"/> when the option is None.
    /// </summary>
    /// <returns>The original option.</returns>
    public static Option<T> TapNone<T>(this Option<T> option, Action onNone) =>
        option.Match(_ => option,
                     () =>
                     {
                         onNone();
                         return option;
                     });

    /// <summary>
    /// Asynchronously executes <paramref name="onNone"/> when the option is None.
    /// </summary>
    /// <returns>The original option.</returns>
    public static async ValueTask<Option<T>> TapNoneTask<T>(this Option<T> option, Func<ValueTask> onNone) =>
        await option.Match(async _ =>
                           {
                               await ValueTask.CompletedTask;
                               return option;
                           },
                           async () =>
                           {
                               await onNone();
                               return option;
                           });

    /// <summary>
    /// Executes <paramref name="f"/> when the option is Some.
    /// </summary>
    public static void Iter<T>(this Option<T> option, Action<T> f) =>
        option.Match(t =>
                     {
                         f(t);
                     },
                     () => { });

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the option is Some.
    /// </summary>
    public static ValueTask IterTask<T>(this Option<T> option, Func<T, ValueTask> f) =>
        option.Match(f, () => ValueTask.CompletedTask);

    /// <summary>
    /// Executes <paramref name="f"/> when the option is None.
    /// </summary>
    public static void IterNone<T>(this Option<T> option, Action f) =>
        option.Match(_ => { }, f);

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the option is None.
    /// </summary>
    public static ValueTask IterNoneTask<T>(this Option<T> option, Func<ValueTask> f) =>
        option.Match(_ => ValueTask.CompletedTask, f);

    /// <summary>
    /// Converts the option to a <see cref="Result{T}"/>.
    /// </summary>
    /// <returns>
    /// <see cref="Result.Success{T}(T)"/> with the wrapped value if Some, otherwise
    /// <see cref="Result.Error{T}(Error)"/> with the result of <paramref name="errorFactory"/>.
    /// </returns>
    public static Result<T> ToResult<T>(this Option<T> option, Func<Error> errorFactory) =>
        option.Match(Result.Success, () => Result.Error<T>(errorFactory()));
}
