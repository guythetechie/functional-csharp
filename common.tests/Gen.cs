using CsCheck;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

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

    public static Gen<Func<int, Option<int>>> AllSomesSelector { get; } =
        from option in GenerateSomeOption(Gen.Int)
        select new Func<int, Option<int>>(_ => option);

    public static Gen<Func<int, ValueTask<Option<int>>>> AllSomesAsyncSelector { get; } =
        from option in GenerateSomeOption(Gen.Int)
        select new Func<int, ValueTask<Option<int>>>(_ => ValueTask.FromResult(option));

    public static Func<int, Option<int>> AllNonesSelector { get; } =
        _ => Option.None;

    public static Func<int, ValueTask<Option<int>>> AllNonesAsyncSelector { get; } =
        _ => ValueTask.FromResult(Option<int>.None());

    public static Gen<Func<int, Option<int>>> OptionSelector { get; } =
        from option in GenerateOption(Gen.Int)
        select new Func<int, Option<int>>(_ => option);

    public static Gen<Func<int, ValueTask<Option<int>>>> OptionAsyncSelector { get; } =
        from option in GenerateOption(Gen.Int)
        select new Func<int, ValueTask<Option<int>>>(_ => ValueTask.FromResult(option));

    public static Gen<Func<int, Result<int>>> AllSuccessesSelector { get; } =
        from result in GenerateSuccessResult(Gen.Int)
        select new Func<int, Result<int>>(_ => result);

    public static Gen<Func<int, ValueTask<Result<int>>>> AllSuccessesAsyncSelector { get; } =
        from result in GenerateSuccessResult(Gen.Int)
        select new Func<int, ValueTask<Result<int>>>(_ => ValueTask.FromResult(result));

    public static Func<int, Result<int>> AllErrorsSelector { get; } =
        _ => Result.Error<int>(common.Error.From("error"));

    public static Func<int, ValueTask<Result<int>>> AllErrorsAsyncSelector { get; } =
        _ => ValueTask.FromResult(Result.Error<int>(common.Error.From("error")));

    public static Gen<Func<int, Result<int>>> ResultSelector { get; } =
        from result in GenerateResult(Gen.Int)
        select new Func<int, Result<int>>(_ => result);

    public static Gen<Func<int, ValueTask<Result<int>>>> ResultAsyncSelector { get; } =
        from result in GenerateResult(Gen.Int)
        select new Func<int, ValueTask<Result<int>>>(_ => ValueTask.FromResult(result));

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

    public static Gen<Func<int, T>> GenerateIntFunc<T>(Gen<T> gen) =>
        from t in gen
        select new Func<int, T>(_ => t);
}