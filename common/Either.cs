using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents a value that can be one of two types, either left or right.
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
    /// Gets whether this either contains a left value.
    /// </summary>
    public bool IsLeft => isLeft;

    /// <summary>
    /// Gets whether this either contains a right value.
    /// </summary>
    public bool IsRight => isLeft is false;

#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Creates an either containing a left value.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <returns>Left(left).</returns>
    public static Either<TLeft, TRight> Left(TLeft left) =>
        new(left);

    /// <summary>
    /// Creates an either containing a right value.
    /// </summary>
    /// <param name="right">The right value.</param>
    /// <returns>Right(right).</returns>
    public static Either<TLeft, TRight> Right(TRight right) =>
        new(right);
#pragma warning restore CA1000 // Do not declare static members on generic types

    /// <summary>
    /// Pattern matches on the either state.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="onLeft">Function executed if the either contains a left value.</param>
    /// <param name="onRight">Function executed if the either contains a right value.</param>
    /// <returns>The result of the executed function.</returns>
    public T Match<T>(Func<TLeft, T> onLeft, Func<TRight, T> onRight) =>
        IsLeft ? onLeft(left!) : onRight(right!);

    /// <summary>
    /// Pattern matches on the either state for side effects.
    /// </summary>
    /// <param name="onLeft">Action executed if the either contains a left value.</param>
    /// <param name="onRight">Action executed if the either contains a right value.</param>
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
    /// Implicitly converts a left value to Left(left).
    /// </summary>
    public static implicit operator Either<TLeft, TRight>(TLeft left) =>
        Left(left);

    /// <summary>
    /// Implicitly converts a right value to Right(right).
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
    /// Creates an either containing a left value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="left">The left value.</param>
    /// <returns>Left(left).</returns>
    public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) =>
        Either<TLeft, TRight>.Left(left);

    /// <summary>
    /// Creates an either containing a right value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="right">The right value.</param>
    /// <returns>Right(right).</returns>
    public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) =>
        Either<TLeft, TRight>.Right(right);

    /// <summary>
    /// Transforms the right value using a function.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The source right type.</typeparam>
    /// <typeparam name="TRight2">The result right type.</typeparam>
    /// <param name="either">The either to transform.</param>
    /// <param name="f">The transformation function.</param>
    /// <returns>Right(f(right)) if Right, otherwise the original Left.</returns>
    public static Either<TLeft, TRight2> Map<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => Right<TLeft, TRight2>(f(right)));

    /// <summary>
    /// Chains either operations together (monadic bind).
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The source right type.</typeparam>
    /// <typeparam name="TRight2">The result right type.</typeparam>
    /// <param name="either">The either to bind.</param>
    /// <param name="f">The function that returns an either.</param>
    /// <returns>f(right) if Right, otherwise the original Left.</returns>
    public static Either<TLeft, TRight2> Bind<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => f(right));

    /// <summary>
    /// Projects the either right value (LINQ support).
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The source right type.</typeparam>
    /// <typeparam name="TRight2">The result right type.</typeparam>
    /// <param name="either">The either to project.</param>
    /// <param name="f">The projection function.</param>
    /// <returns>The projected either.</returns>
    public static Either<TLeft, TRight2> Select<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Map(f);

    /// <summary>
    /// Projects and flattens nested eithers (LINQ support).
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The source right type.</typeparam>
    /// <typeparam name="TRight2">The intermediate right type.</typeparam>
    /// <typeparam name="TResult">The result right type.</typeparam>
    /// <param name="either">The source either.</param>
    /// <param name="f">The function that returns an intermediate either.</param>
    /// <param name="selector">The result selector function.</param>
    /// <returns>The flattened result either.</returns>
    public static Either<TLeft, TResult> SelectMany<TLeft, TRight, TRight2, TResult>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f,
                                                         Func<TRight, TRight2, TResult> selector) =>
        either.Bind(right => f(right)
              .Map(right2 => selector(right, right2)));

    /// <summary>
    /// Extracts the right value or converts the left value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="f">Function that converts left to right.</param>
    /// <returns>The right value if Right, otherwise f(left).</returns>
    public static TRight IfLeft<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TLeft, TRight> f) =>
        either.Match(f,
                     right => right);

    /// <summary>
    /// Extracts the left value or converts the right value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="f">Function that converts right to left.</param>
    /// <returns>The left value if Left, otherwise f(right).</returns>
    public static TLeft IfRight<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, TLeft> f) =>
        either.Match(left => left,
                     f);

    /// <summary>
    /// Executes an action if the either contains a right value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="f">The action to execute.</param>
    public static void Iter<TLeft, TRight>(this Either<TLeft, TRight> either, Action<TRight> f) =>
        either.Match(_ => { },
                     f);

    /// <summary>
    /// Executes an async action if the either contains a right value.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="f">The async action to execute.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async ValueTask IterTask<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, ValueTask> f) =>
        await either.Match<ValueTask>(_ => ValueTask.CompletedTask,
                                      async right => await f(right));

    /// <summary>
    /// Extracts the right value or throws an exception.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The right value.</returns>
    /// <exception cref="Exception">Thrown when the either contains a left value.</exception>
    public static TRight IfLeftThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(_ => throw exception,
                     right => right);

    /// <summary>
    /// Extracts the left value or throws an exception.
    /// </summary>
    /// <typeparam name="TLeft">The left type.</typeparam>
    /// <typeparam name="TRight">The right type.</typeparam>
    /// <param name="either">The either to check.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The left value.</returns>
    /// <exception cref="Exception">Thrown when the either contains a right value.</exception>
    public static TLeft IfRightThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(left => left,
                     _ => throw exception);
}