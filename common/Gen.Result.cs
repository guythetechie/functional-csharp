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
}