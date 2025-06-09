using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common;

#pragma warning disable CA1716 // Identifiers should not match keywords
/// <summary>
/// Represents an optional value.
/// With nullable reference types enabled:
/// - For non-nullable reference types (e.g., string), <c>Option.Some(null)</c> is a compile-time error.
/// - For nullable types (e.g., string?), <c>Option.Some(null)</c> is treated as <c>None</c>.
/// </summary>
public sealed record Option<T>
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    private readonly T? value;
    private readonly bool isSome;

    private Option()
    {
        isSome = false;
    }

    private Option(T value)
    {
        this.value = value;
        isSome = true;
    }

    /// <summary>
    /// Returns whether this option contains no value.
    /// </summary>
    public bool IsNone => !isSome;

    /// <summary>
    /// Returns whether this option contains a value.
    /// </summary>
    public bool IsSome => isSome;

#pragma warning disable CA1000 // Do not declare static members on generic types
    internal static Option<T> Some(T value) =>
        new(value);

    /// <summary>
    /// Creates an option with no value.
    /// </summary>
    /// <returns>An option representing no value.</returns>
    public static Option<T> None() =>
        new();
#pragma warning restore CA1000 // Do not declare static members on generic types

    public override string ToString() =>
        IsSome ? $"Some({value})" : "None";

    public bool Equals(Option<T>? other) =>
        (this, other) switch
        {
            (_, null) => false,
            ({ IsSome: true }, { IsSome: true }) => EqualityComparer<T?>.Default.Equals(value, other.value),
            ({ IsNone: true }, { IsNone: true }) => true,
            _ => false
        };

    public override int GetHashCode() =>
        value is null
        ? 0
        : EqualityComparer<T?>.Default.GetHashCode(value);

    /// <summary>
    /// Pattern matches on the option, executing the appropriate function.
    /// </summary>
    /// <typeparam name="T2">The return type.</typeparam>
    /// <param name="some">Function executed if the option contains a value.</param>
    /// <param name="none">Function executed if the option is empty.</param>
    /// <returns>The result of the executed function.</returns>
    public T2 Match<T2>(Func<T, T2> some, Func<T2> none) =>
        IsSome ? some(value!) : none();

    /// <summary>
    /// Pattern matches on the option for side effects.
    /// </summary>
    /// <param name="some">Action executed if the option contains a value.</param>
    /// <param name="none">Action executed if the option is empty.</param>
    public void Match(Action<T> some, Action none)
    {
        if (IsSome)
            some(value!);
        else
            none();
    }

    /// <summary>
    /// Implicitly converts a value to Some(value).
    /// </summary>
    public static implicit operator Option<T>(T value) =>
        Some(value);

    /// <summary>
    /// Implicitly converts None to an empty option.
    /// </summary>
    public static implicit operator Option<T>(None _) =>
        None();
}

/// <summary>
/// Represents the absence of a value in an option.
/// </summary>
public readonly record struct None
{
    public override string ToString() =>
        "None";

    public override int GetHashCode() => 0;
}

#pragma warning disable CA1716 // Identifiers should not match keywords
public static class Option
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    /// <summary>
    /// Creates an option containing the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>Some(value).</returns>
    public static Option<T> Some<T>(T value) =>
        Option<T>.Some(value);

    /// <summary>
    /// A None value for creating empty options.
    /// </summary>
    public static None None { get; }

    /// <summary>
    /// Filters an option based on a predicate.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="option">The option to filter.</param>
    /// <param name="predicate">The predicate function.</param>
    /// <returns>The option if it contains a value that satisfies the predicate, otherwise None.</returns>
    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option.Match(t => predicate(t) ? option : None,
                     () => None);

    /// <summary>
    /// Transforms the option value using the specified function.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="option">The option to transform.</param>
    /// <param name="f">The transformation function.</param>
    /// <returns>Some(f(value)) if the option contains a value, otherwise None.</returns>
    public static Option<T2> Map<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Match(t => Some(f(t)),
                     () => None);

    /// <summary>
    /// Applies an option-returning function to the option value.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="option">The option to bind.</param>
    /// <param name="f">The function that returns an option.</param>
    /// <returns>f(value) if the option contains a value, otherwise None.</returns>
    public static Option<T2> Bind<T, T2>(this Option<T> option, Func<T, Option<T2>> f) =>
        option.Match(t => f(t),
                     () => None);

    /// <summary>
    /// Projects the option value (LINQ support).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The result value type.</typeparam>
    /// <param name="option">The option to project.</param>
    /// <param name="f">The projection function.</param>
    /// <returns>The projected option.</returns>
    public static Option<T2> Select<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Map(f);

    /// <summary>
    /// Projects and flattens nested options (LINQ support).
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="T2">The intermediate value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="option">The source option.</param>
    /// <param name="f">The function that returns an intermediate option.</param>
    /// <param name="selector">The result selector function.</param>
    /// <returns>The flattened result option.</returns>
    public static Option<TResult> SelectMany<T, T2, TResult>(this Option<T> option, Func<T, Option<T2>> f,
                                                         Func<T, T2, TResult> selector) =>
        option.Bind(t => f(t).Map(t2 => selector(t, t2)));

    /// <summary>
    /// Returns the option value or a default value if None.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to check.</param>
    /// <param name="f">Function that provides the default value.</param>
    /// <returns>The option value if present, otherwise the default value.</returns>
    public static T IfNone<T>(this Option<T> option, Func<T> f) =>
        option.Match(t => t,
                     f);

    /// <summary>
    /// Returns the option or a fallback option if None.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to check.</param>
    /// <param name="f">Function that provides the fallback option.</param>
    /// <returns>The original option if Some, otherwise the fallback option.</returns>
    public static Option<T> IfNone<T>(this Option<T> option, Func<Option<T>> f) =>
        option.Match(t => option,
                     f);

    /// <summary>
    /// Returns the option value or throws an exception if None.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to check.</param>
    /// <param name="getException">Function that creates the exception to throw.</param>
    /// <returns>The option value.</returns>
    /// <exception cref="Exception">Thrown when the option is None.</exception>
    public static T IfNoneThrow<T>(this Option<T> option, Func<Exception> getException) =>
        option.Match(t => t,
                     () => throw getException());

    /// <summary>
    /// Converts the option to a nullable reference type.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <returns>The option value if present, otherwise null.</returns>
    public static T? IfNoneNull<T>(this Option<T> option) where T : class =>
    option.Match(t => (T?)t,
                 () => null);

    /// <summary>
    /// Converts the option to a nullable value type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <returns>The option value if present, otherwise null.</returns>
    public static T? IfNoneNullable<T>(this Option<T> option) where T : struct =>
        option.Match(t => (T?)t,
                     () => null);

    /// <summary>
    /// Executes an action if the option contains a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to check.</param>
    /// <param name="f">The action to execute.</param>
    public static void Iter<T>(this Option<T> option, Action<T> f) =>
        option.Match(f,
                     () => { });

    /// <summary>
    /// Executes an async action if the option contains a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option to check.</param>
    /// <param name="f">The async action to execute.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async ValueTask IterTask<T>(this Option<T> option, Func<T, ValueTask> f) =>
        await option.Match<ValueTask>(async t => await f(t),
                                      () => ValueTask.CompletedTask);
}
