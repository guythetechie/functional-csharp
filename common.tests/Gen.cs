using CsCheck;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

internal static partial class Generator
{
    public static Gen<Func<int, int>> IntToInt { get; } =
        Gen.OneOf(// Add x to the integer, ensuring that the output doesn't exceed the bounds of an int
                  from x in Gen.Int
                  select (Func<int, int>)(i => (int)Math.Clamp((long)i + x, int.MinValue, int.MaxValue)),
                  // Multiply the integer by x, ensuring that the output doesn't exceed the bounds of an int
                  from x in Gen.Int[-10, 10]
                  select (Func<int, int>)(i => (int)Math.Clamp((long)i * x, int.MinValue, int.MaxValue)));

    public static Gen<Func<int, string>> IntToString { get; } =
        Gen.OneOf(// Convert the integer to a string in a given base
                  from @base in Gen.OneOfConst(2, 8, 10, 16)
                  select (Func<int, string>)(i => Convert.ToString(i, @base)),
                  // Add a prefix and suffix to the integer when converting to a string
                  from prefix in Gen.Char.AlphaNumeric.Array[1, 10]
                  from suffix in Gen.Char.AlphaNumeric.Array[1, 10]
                  select (Func<int, string>)(i => $"{new string(prefix)}{i}{new string(suffix)}"));

    public static Gen<Func<int, ValueTask<string>>> IntToStringTask { get; } =
        from f in IntToString
        select (Func<int, ValueTask<string>>)(i => ValueTask.FromResult(f(i)));

    public static Gen<Func<string, int>> StringToInt { get; } =
        from f in IntToInt
        select (Func<string, int>)(s =>
        {
            var sum = s.Aggregate(0L, (acc, c) => acc + c);
            var intSum = (int)Math.Clamp(sum, int.MinValue, int.MaxValue);
            return f(intSum);
        });

    public static Gen<Func<string, string>> StringToString { get; } =
        from f in StringToInt
        from g in IntToString
        select (Func<string, string>)(s => g(f(s)));

    public static Gen<Func<int, bool>> IntPredicate { get; } =
        from x in Gen.Int[3, 10]
        select (Func<int, bool>)(i => i % x == 0);

    public static Gen<Func<string, bool>> StringPredicate { get; } =
        from f in IntPredicate
        select (Func<string, bool>)(s =>
        {
            var sum = s.Aggregate(0L, (acc, c) => acc + c);
            var intSum = (int)Math.Clamp(sum, int.MinValue, int.MaxValue);
            return f(intSum);
        });

    private static Gen<Error> NonExceptionalError { get; } =
        from messages in Gen.String.Where(value => string.IsNullOrWhiteSpace(value) is false).Array
        where messages.Length > 0
        select common.Error.From(messages);

    private static Gen<Error> ExceptionalError { get; } =
        from message in Gen.String
        let exception = new InvalidOperationException(message)
        select common.Error.From(exception);

    public static Gen<Error> Error { get; } =
        Gen.OneOf(NonExceptionalError, ExceptionalError);

    public static Gen<Func<Error, Error>> ErrorToError { get; } =
        from seed in Error
        select new Func<Error, Error>(error => error + seed);

    public static Gen<Func<Error, int>> ErrorToInt { get; } =
        from f in StringToInt
        select new Func<Error, int>(error => f(error.ToString()));

    public static Gen<Result<int>> Result { get; } =
        Gen.Int.ToResult();

    public static Gen<Func<int, Result<string>>> IntToStringResult { get; } =
        from f in IntToString
        from error in Error
        select new Func<int, Result<string>>(x => Math.Abs(x % 10) < 9
                                                    ? common.Result.Success(f(x))
                                                    : error);

    public static Gen<Func<int, ValueTask<Result<string>>>> IntToStringResultTask { get; } =
        from f in IntToStringResult
        select new Func<int, ValueTask<Result<string>>>(x => ValueTask.FromResult(f(x)));

    public static Gen<Func<string, Result<int>>> StringToIntResult { get; } =
        from f in StringToInt
        from error in Error
        select new Func<string, Result<int>>(s =>
        {
            var intValue = f(s);

            return Math.Abs(intValue % 10) < 9
                    ? common.Result.Success(intValue)
                    : error;
        });

    public static Gen<Func<string, ValueTask<Result<int>>>> StringToIntResultTask { get; } =
        from f in StringToIntResult
        select new Func<string, ValueTask<Result<int>>>(s => ValueTask.FromResult(f(s)));

    public static Gen<Func<Error, Result<string>>> ErrorToStringResult { get; } =
        from f in ErrorToInt
        from g in IntToStringResult
        select new Func<Error, Result<string>>(error => g(f(error)));

    public static Gen<Func<int, int>> MapSelector { get; } =
        from x in Gen.Int
        select (Func<int, int>)(i =>
        {
            var result = (long)i + x;
            return (int)Math.Clamp(result, int.MinValue, int.MaxValue);
        });

    public static Gen<Option<int>> Option { get; } =
        Gen.Int.ToOption();

    public static Gen<Func<int, Option<string>>> IntToStringOption { get; } =
        from f in IntToString
        select new Func<int, Option<string>>(x => Math.Abs(x % 10) < 9
                                                    ? common.Option.Some(f(x))
                                                    : common.Option.None);

    public static Gen<Func<int, ValueTask<Option<string>>>> IntToStringOptionTask { get; } =
        from f in IntToStringOption
        select new Func<int, ValueTask<Option<string>>>(x => ValueTask.FromResult(f(x)));

    public static Gen<Func<string, Option<int>>> StringToIntOption { get; } =
        from f in StringToInt
        select new Func<string, Option<int>>(s =>
        {
            var intValue = f(s);
            return Math.Abs(intValue % 10) < 9
                    ? common.Option.Some(intValue)
                    : common.Option.None;
        });

    public static Gen<Func<string, ValueTask<Option<int>>>> StringToIntOptionTask { get; } =
        from f in StringToIntOption
        select new Func<string, ValueTask<Option<int>>>(s => ValueTask.FromResult(f(s)));

    public static Gen<Func<int, Option<int>>> AllSomesSelector { get; } =
        from f in MapSelector
        select new Func<int, Option<int>>(x => common.Option.Some(f(x)));

    public static Gen<Func<int, Option<int>>> OptionSelector { get; } =
        from option in MapSelector.ToOption()
        select new Func<int, Option<int>>(x => option.Map(f => f(x)));

    public static Gen<Func<int, ValueTask<Option<int>>>> OptionAsyncSelector { get; } =
        from f in OptionSelector
        select new Func<int, ValueTask<Option<int>>>(x => ValueTask.FromResult(f(x)));

    public static Gen<Func<int, ValueTask<Option<int>>>> AllSomesAsyncSelector { get; } =
        from f in AllSomesSelector
        select new Func<int, ValueTask<Option<int>>>(x => ValueTask.FromResult(f(x)));

    public static Func<int, ValueTask<Option<int>>> AllNonesAsyncSelector { get; } =
        _ => ValueTask.FromResult(Option<int>.None());

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

    public static Gen<Option<T>> ToOption<T>(this Gen<T> gen) =>
        Gen.Frequency((9, gen.Select(common.Option.Some)),
                      (1, Gen.Const(Option<T>.None())));

    public static Gen<Result<T>> ToResult<T>(this Gen<T> gen) =>
        Gen.Frequency((9, gen.Select(common.Result.Success)),
                      (1, from error in Error
                          select common.Result.Error<T>(error)));
}