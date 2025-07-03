using CsCheck;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace common.tests;

internal static class Generator
{
    public static Gen<Error> NonExceptionalError { get; } =
        from messages in Gen.String.Array
        select common.Error.From(messages);

    public static Gen<Error> ExceptionalError { get; } =
        from message in Gen.String
        let exception = new InvalidOperationException(message)
        select common.Error.From(exception);

    public static Gen<Error> Error { get; } =
        Gen.OneOf(NonExceptionalError, ExceptionalError);

    public static Gen<Option<T>> GenerateSomeOption<T>(Gen<T> gen) =>
        from t in gen
        select Option.Some(t);

    public static Gen<Option<T>> GenerateNoneOption<T>() =>
        Gen.Const<Option<T>>(Option.None);

    public static Gen<Option<T>> GenerateOption<T>(Gen<T> gen) =>
        Gen.Frequency((9, GenerateSomeOption(gen)),
                      (1, GenerateNoneOption<T>()));

    public static Gen<Either<T, T>> GenerateEither<T>(Gen<T> gen) =>
        from t in gen
        from either in Gen.OneOfConst(Either.Left<T, T>(t),
                                      Either.Right<T, T>(t))
        select either;

    public static Gen<Either<TLeft, TRight>> GenerateLeftEither<TLeft, TRight>(Gen<TLeft> leftGen) =>
        from left in leftGen
        select Either.Left<TLeft, TRight>(left);

    public static Gen<Either<TLeft, TRight>> GenerateRightEither<TLeft, TRight>(Gen<TRight> rightGen) =>
        from right in rightGen
        select Either.Right<TLeft, TRight>(right);

    public static Gen<Either<TLeft, TRight>> GenerateEither<TLeft, TRight>(Gen<TLeft> leftGen, Gen<TRight> rightGen) =>
        Gen.OneOf(GenerateLeftEither<TLeft, TRight>(leftGen),
                  GenerateRightEither<TLeft, TRight>(rightGen));

    public static Gen<Result<T>> GenerateErrorResult<T>() =>
        from error in Error
        select Result.Error<T>(error);

    public static Gen<Result<T>> GenerateSuccessResult<T>(Gen<T> gen) =>
        from t in gen
        select Result.Success(t);

    public static Gen<Result<T>> GenerateResult<T>(Gen<T> gen) =>
        Gen.Frequency((9, GenerateSuccessResult(gen)),
                      (1, GenerateErrorResult<T>()));

    public static Gen<ImmutableArray<T2>> Traverse<T1, T2>(IEnumerable<T1> source, Func<T1, Gen<T2>> f) =>
        source.Aggregate(Gen.Const(new List<T2>()),
                         (listGen, item) => Gen.Select(listGen, f(item))
                                               .Select(x =>
                                               {
                                                   var (list, item) = x;
                                                   list.Add(item);
                                                   return list;
                                               }))
               .Select(list => list.ToImmutableArray());
}