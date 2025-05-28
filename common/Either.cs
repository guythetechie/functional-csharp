using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common;

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

    public bool IsLeft => isLeft;

    public bool IsRight => isLeft is false;

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static Either<TLeft, TRight> Left(TLeft left) =>
        new(left);

    public static Either<TLeft, TRight> Right(TRight right) =>
        new(right);
#pragma warning restore CA1000 // Do not declare static members on generic types

    public T Match<T>(Func<TLeft, T> onLeft, Func<TRight, T> onRight) =>
        IsLeft ? onLeft(left!) : onRight(right!);

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
}

public static class Either
{
    public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) =>
        Either<TLeft, TRight>.Left(left);

    public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) =>
        Either<TLeft, TRight>.Right(right);

    public static Either<TLeft, TRight2> Map<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => Right<TLeft, TRight2>(f(right)));

    public static Either<TLeft, TRight2> Bind<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f) =>
        either.Match(left => Left<TLeft, TRight2>(left),
                     right => f(right));

    public static Either<TLeft, TRight2> Select<TLeft, TRight, TRight2>(this Either<TLeft, TRight> either, Func<TRight, TRight2> f) =>
        either.Map(f);

    public static Either<TLeft, TResult> SelectMany<TLeft, TRight, TRight2, TResult>(this Either<TLeft, TRight> either, Func<TRight, Either<TLeft, TRight2>> f,
                                                         Func<TRight, TRight2, TResult> selector) =>
        either.Bind(right => f(right)
              .Map(right2 => selector(right, right2)));

    public static TRight IfLeft<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TLeft, TRight> f) =>
        either.Match(f,
                     right => right);

    public static TLeft IfRight<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, TLeft> f) =>
        either.Match(left => left,
                     f);

    public static void Iter<TLeft, TRight>(this Either<TLeft, TRight> either, Action<TRight> f) =>
        either.Match(_ => { },
                     f);

    public static async ValueTask IterTask<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TRight, ValueTask> f) =>
        await either.Match<ValueTask>(_ => ValueTask.CompletedTask,
                                      async right => await f(right));

    public static TRight IfLeftThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(_ => throw exception,
                     right => right);

    public static TLeft IfRightThrow<TLeft, TRight>(this Either<TLeft, TRight> either, Exception exception) =>
        either.Match(left => left,
                     _ => throw exception);
}