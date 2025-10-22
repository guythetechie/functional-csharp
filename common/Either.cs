using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents a value that can be one of two types, either <typeparamref name="TLeft"/> or <typeparamref name="TRight"/>.
/// </summary>
public sealed record Either<TLeft, TRight>
{
    private readonly TLeft? left;
    private readonly TRight? right;
    private readonly bool isLeft;

    private Either(TLeft left)
    {
        this.left = left;
        isLeft = true;
    }

    private Either(TRight right)
    {
        this.right = right;
        isLeft = false;
    }

    /// <summary>
    /// True when the either contains a left value.
    /// </summary>
    public bool IsLeft => isLeft;

    /// <summary>
    /// True when the either contains a right value.
    /// </summary>
    public bool IsRight => isLeft is false;

#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Wraps <paramref name="left"/> in a left either.
    /// </summary>
    public static Either<TLeft, TRight> Left(TLeft left) =>
        new(left);

    /// <summary>
    /// Wraps <paramref name="right"/> in a right either.
    /// </summary>
    public static Either<TLeft, TRight> Right(TRight right) =>
        new(right);
#pragma warning restore CA1000 // Do not declare static members on generic types

    /// <summary>
    /// Returns the result of <paramref name="onLeft"/> or <paramref name="onRight"/> based on the either's state.
    /// </summary>
    /// <param name="onLeft">Executes when the either is Left.</param>
    /// <param name="onRight">Executes when the either is Right.</param>
    public T Match<T>(Func<TLeft, T> onLeft, Func<TRight, T> onRight) =>
        IsLeft ? onLeft(left!) : onRight(right!);

    /// <summary>
    /// Executes <paramref name="onLeft"/> or <paramref name="onRight"/> based on the either's state.
    /// </summary>
    /// <param name="onLeft">Executes when the either is Left.</param>
    /// <param name="onRight">Executes when the either is Right.</param>
    public void Match(Action<TLeft> onLeft, Action<TRight> onRight)
    {
        if (IsLeft)
        {
            onLeft(left!);
        }
        else
        {
            onRight(right!);
        }
    }

    public override string ToString() =>
        IsLeft ? $"Left: {left}" : $"Right: {right}";

    public bool Equals(Either<TLeft, TRight>? other) =>
        (this, other) switch
        {
            (_, null) => false,
            ({ IsLeft: true }, { IsLeft: true }) =>
                EqualityComparer<TLeft?>.Default.Equals(left, other.left),
            ({ IsRight: true }, { IsRight: true }) =>
                EqualityComparer<TRight?>.Default.Equals(right, other.right),
            _ => false
        };

    public override int GetHashCode() =>
        HashCode.Combine(left, right);

    /// <summary>
    /// Converts a left value to <c>Left(left)</c>.
    /// </summary>
    public static implicit operator Either<TLeft, TRight>(TLeft left) =>
        Left(left);

    /// <summary>
    /// Converts a right value to <c>Right(right)</c>.
    /// </summary>
    public static implicit operator Either<TLeft, TRight>(TRight right) =>
        Right(right);
}

/// <summary>
/// Provides static methods for creating and working with Either instances.
/// </summary>
public static class Either
{
    /// <summary>
    /// Wraps <paramref name="left"/> in a left either.
    /// </summary>
    public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) =>
        Either<TLeft, TRight>.Left(left);

    /// <summary>
    /// Wraps <paramref name="right"/> in a right either.
    /// </summary>
    public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) =>
        Either<TLeft, TRight>.Right(right);

    /// <summary>
    /// Applies <paramref name="f"/> to the right value.
    /// </summary>
    /// <returns><c>Right(f(right))</c> if Right, otherwise the original Left.</returns>
    public static Either<TLeft, TRight2> Map<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => Right<TLeft, TRight2>(f(right)));

    /// <summary>
    /// Applies <paramref="f"/> and flattens the result.
    /// </summary>
    /// <returns><c>f(right)</c> if Right, otherwise the original Left.</returns>
    public static Either<TLeft, TRight2> Bind<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => f(right));

    /// <summary>
    /// LINQ projection support. Enables syntax <c>from value in either select value</c>
    /// </summary>
    public static Either<TLeft, TRight2> Select<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Map(f);

    /// <summary>
    /// LINQ flattening support. Enables syntax <c>from x in either1 from y in either2 select x + y</c>
    /// </summary>
    public static Either<TLeft, TResult> SelectMany<TLeft, TRight, TRight2, TResult>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f,
                                                         Func<TRight, TRight2, TResult> selector) =>
        either.Bind(right => f(right)
              .Map(right2 => selector(right, right2)));

    /// <summary>
    /// Returns the right value if Right, otherwise converts the left value.
    /// </summary>
    public static TRight IfLeft<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TLeft, TRight> f) =>
        either.Match(f,
                     right => right);

    /// <summary>
    /// Returns the left value if Left, otherwise converts the right value.
    /// </summary>
    public static TLeft IfRight<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, TLeft> f) =>
        either.Match(left => left,
                     f);

    /// <summary>
    /// Executes <paramref name="f"/> when the either is Right.
    /// </summary>
    public static void Iter<TLeft, TRight>(this Either<TLeft, TRight> either, Action<TRight> f) =>
        either.Match(_ => { },
                     f);

    /// <summary>
    /// Asynchronously executes <paramref name="f"/> when the either is Right.
    /// </summary>
    public static async ValueTask IterTask<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, ValueTask> f) =>
        await either.Match<ValueTask>(_ => ValueTask.CompletedTask,
                                      async right => await f(right));

    /// <summary>
    /// Returns the right value if Right, otherwise throws the exception.
    /// </summary>
    /// <exception cref="Exception">Thrown when the either is Left.</exception>
    public static TRight IfLeftThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(_ => throw exception,
                     right => right);

    /// <summary>
    /// Returns the left value if Left, otherwise throws the exception.
    /// </summary>
    /// <exception cref="Exception">Thrown when the either is Right.</exception>
    public static TLeft IfRightThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(left => left,
                     _ => throw exception);
}