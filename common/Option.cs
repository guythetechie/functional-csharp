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

    public bool IsNone => !isSome;

    public bool IsSome => isSome;

#pragma warning disable CA1000 // Do not declare static members on generic types
    internal static Option<T> Some(T value) =>
        new(value);

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

    public T2 Match<T2>(Func<T, T2> some, Func<T2> none) =>
        IsSome ? some(value!) : none();

    public void Match(Action<T> some, Action none)
    {
        if (IsSome)
            some(value!);
        else
            none();
    }

    public static implicit operator Option<T>(T value) =>
        Some(value);

    public static implicit operator Option<T>(None _) =>
        None();
}

public sealed record None
{
    public static None Instance { get; } = new();

    private None() { }

    public override string ToString() =>
        "None";

    public bool Equals(None? other) =>
        other is not null;

    public override int GetHashCode() =>
        0;
}

#pragma warning disable CA1716 // Identifiers should not match keywords
public static class Option
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    public static Option<T> Some<T>(T value) =>
        Option<T>.Some(value);

    public static None None =>
        None.Instance;

    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option.Match(t => predicate(t) ? option : None,
                     () => None);

    public static Option<T2> Map<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Match(t => Some(f(t)),
                     () => None);

    public static Option<T2> Bind<T, T2>(this Option<T> option, Func<T, Option<T2>> f) =>
        option.Match(t => f(t),
                     () => None);

    public static Option<T2> Select<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option.Map(f);

    public static Option<TResult> SelectMany<T, T2, TResult>(this Option<T> option, Func<T, Option<T2>> f,
                                                         Func<T, T2, TResult> selector) =>
        option.Bind(t => f(t).Map(t2 => selector(t, t2)));

    public static T IfNone<T>(this Option<T> option, Func<T> f) =>
        option.Match(t => t,
                     f);

    public static void Iter<T>(this Option<T> option, Action<T> f) =>
        option.Match(f,
                     () => { });

    public static T IfNoneThrow<T>(this Option<T> option, Exception exception) =>
        option.Match(t => t,
                     () => throw exception);

    public static async ValueTask IterTask<T>(this Option<T> option, Func<T, ValueTask> f) =>
        await option.Match<ValueTask>(async t => await f(t),
                                      () => ValueTask.CompletedTask);
}
