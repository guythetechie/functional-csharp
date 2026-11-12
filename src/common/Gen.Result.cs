using CsCheck;
using System.Linq;

namespace common;

public static class ResultGenerator
{

    public static Gen<Result<object>> Success { get; } =
        from x in Generator.Object
        select Result.Success(x);

    public static Gen<Result<object>> Error { get; } =
        from error in ErrorGenerator.Any
        select Result.Error<object>(error);

    public static Gen<Result<object>> Any { get; } =
        Gen.Frequency((9, Success),
                      (1, Error));

    public static Gen<(Result<object>, Result<object>)> EqualPair { get; } =
        from result1 in Any
        from result2 in GenerateEqual(result1)
        select (result1, result2);

    public static Gen<(Result<object>, Result<object>)> UnequalPair { get; } =
        from result1 in Any
        from result2 in GenerateUnequal(result1)
        select (result1, result2);

    public static Gen<(Result<object>, Result<object>)> Pair { get; } =
        Gen.OneOf(EqualPair,
                  UnequalPair,
                  Gen.Select(Any, Any));

    public static Gen<Result<object>> GenerateEqual(Result<object> result) =>
        Gen.Const(result.Match(Result.Success, error => Result.Error<object>(error with { })));

    public static Gen<Result<object>> GenerateUnequal(Result<object> result) =>
        result.Match(value1 => from result2 in Any
                               where result2.Match(value2 => value1.Equals(value2) is false,
                                                   _ => true)
                               select result2,
                     error1 => from result2 in Any
                               where result2.Match(_ => true,
                                                   error2 => error1.Equals(error2) is false)
                               select result2);
}