using System;
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
/// Wraps a value of <typeparamref name="T"/>.
/// </summary>
public sealed record Some<T>(T Value)
{
    public override string ToString() =>
        $"Some({Value})";
}

/// <summary>
/// Represents a type that may or may not contain a value of type <typeparamref name="T"/>.
/// </summary>
public readonly union Option<T>(Some<T>, None) : IEquatable<Option<T>>
{
    public bool IsSome => this is Some<T>;

    public bool IsNone => this is None;

    public static implicit operator Option<T>(None none) =>
        new(none);

    public override string ToString() =>
        this switch
        {
            null => "<null>",
            Some<T> { Value: var value } => $"Some: {value}",
            None => "None",
        };

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Option<T> other && Equals(other);

    public bool Equals(Option<T> other) =>
        (this, other) switch
        {
            (None, None) => true,
            (Some<T> some1, Some<T> some2) => some1.Equals(some2),
            _ => false
        };

    public override int GetHashCode() =>
        this switch
        {
            None => 0,
            Some<T> some => some.GetHashCode(),
            _ => 0
        };

    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);
}

public static class Option
{
    /// <summary>
    /// Wraps <paramref name="value"/> in an <see cref="Option{T}"/>.
    /// </summary>
    public static Option<T> Some<T>(T value) =>
       new(new Some<T>(value));

    /// <summary>
    /// Option without a value.
    /// </summary>
    public static None None { get; }

    /// <summary>
    /// Returns the option if <paramref name="predicate"/> succeeds, otherwise <see cref="common.None"/>.
    /// </summary>
    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option switch
        {
            Some<T> { Value: var t } when predicate(t) => option,
            _ => None
        };

    /// <summary>
    /// Applies <paramref name="f"/> to the wrapped value.
    /// </summary>
    /// <returns><c>Some(f(value))</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static Option<T2> Map<T, T2>(this Option<T> option, Func<T, T2> f) =>
        option switch
        {
            Some<T> { Value: var t } => Some(f(t)),
            _ => None
        };

    /// <summary>
    /// Asynchronously applies <paramref name="f"/> to the wrapped value.
    /// </summary>
    /// <returns><c>Some(await f(value))</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static async ValueTask<Option<T2>> MapTask<T, T2>(this Option<T> option, Func<T, ValueTask<T2>> f) =>
        option switch
        {
            Some<T> { Value: var t } => Some(await f(t)),
            _ => None,
        };

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>f(value)</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static Option<T2> Bind<T, T2>(this Option<T> option, Func<T, Option<T2>> f) =>
        option switch
        {
            Some<T> { Value: var t } => f(t),
            _ => None
        };

    /// <summary>
    /// Applies <paramref name="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>await f(value)</c> if Some, otherwise <see cref="common.None"/>.</returns>
    public static async ValueTask<Option<T2>> BindTask<T, T2>(this Option<T> option, Func<T, ValueTask<Option<T2>>> f) =>
        option switch
        {
            Some<T> { Value: var t } => await f(t),
            _ => None,
        };

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
    /// Matches the option and returns the result of the corresponding function.
    /// </summary>
    public static TResult Match<T, TResult>(this Option<T> option, Func<T, TResult> onSome, Func<TResult> onNone) =>
        option switch
        {
            Some<T> { Value: var t } => onSome(t),
            _ => onNone()
        };

    /// <summary>
    /// Matches the option and executes the corresponding action.
    /// </summary>
    public static void Match<T>(this Option<T> option, Action<T> onSome, Action onNone)
    {
        if (option is Some<T> { Value: var t })
        {
            onSome(t);
        }
        else
        {
            onNone();
        }
    }

    /// <summary>
    /// Returns the wrapped value if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static T IfNone<T>(this Option<T> option, Func<T> f) =>
        option switch
        {
            Some<T> { Value: var t } => t,
            _ => f()
        };

    /// <summary>
    /// Returns this option if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static Option<T> IfNone<T>(this Option<T> option, Func<Option<T>> f) =>
        option switch
        {
            Some<T> => option,
            _ => f()
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the option is None.
    /// </summary>
    public static void IfNone<T>(this Option<T> option, Action f)
    {
        if (option is None)
        {
            f();
        }
    }

    /// <summary>
    /// Returns the wrapped value if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<T> IfNoneTask<T>(this Option<T> option, Func<ValueTask<T>> f) =>
        option switch
        {
            Some<T> { Value: var t } => t,
            _ => await f()
        };

    /// <summary>
    /// Returns this option if Some, otherwise the result of <paramref name="f"/>.
    /// </summary>
    public static async ValueTask<Option<T>> IfNoneTask<T>(this Option<T> option, Func<ValueTask<Option<T>>> f) =>
        option switch
        {
            Some<T> => option,
            _ => await f()
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the option is None.
    /// </summary>
    public static async ValueTask IfNoneTask<T>(this Option<T> option, Func<ValueTask> f)
    {
        if (option is None)
        {
            await f();
        }
    }

    /// <summary>
    /// Converts the option to a nullable reference type.
    /// </summary>
    /// <returns>The wrapped value if Some, otherwise <see langword="null"/>.</returns>
    public static T? IfNoneNull<T>(this Option<T> option) where T : class =>
        option switch
        {
            Some<T> { Value: var t } => t,
            _ => null
        };

    /// <summary>
    /// Converts the option to a nullable value type.
    /// </summary>
    /// <returns>The wrapped value if Some, otherwise <see langword="null"/>.</returns>
    public static T? IfNoneNullable<T>(this Option<T> option) where T : struct =>
        option switch
        {
            Some<T> { Value: var t } => t,
            _ => null
        };

    /// <summary>
    /// Executes <paramref name="f"/> when the option is Some.
    /// </summary>
    public static void Iter<T>(this Option<T> option, Action<T> f)
    {
        if (option is Some<T> { Value: var t })
        {
            f(t);
        }
    }

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the option is Some.
    /// </summary>
    public static async ValueTask IterTask<T>(this Option<T> option, Func<T, ValueTask> f)
    {
        if (option is Some<T> { Value: var t })
        {
            await f(t);
        }
    }
}
