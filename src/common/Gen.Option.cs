using CsCheck;
using System.Linq;

namespace common;

public static class OptionGenerator
{
    public static Gen<Option<object>> Some { get; } =
        from x in Generator.Object
        select Option.Some(x);

    public static Gen<Option<object>> None { get; } =
        Gen.Const(Option<object>.None);

    public static Gen<Option<object>> Any { get; } =
        Gen.Frequency((9, Some),
                      (1, None));

    public static Gen<Option<T>> OptionOf<T>(this Gen<T> gen) =>
        Gen.Frequency((9, from x in gen
                          select Option.Some(x)),
                      (1, Gen.Const(Option<T>.None)));

    public static Gen<(Option<object>, Option<object>)> EqualPair { get; } =
        from option1 in Any
        from option2 in GenerateEqual(option1)
        select (option1, option2);

    public static Gen<(Option<object>, Option<object>)> UnequalPair { get; } =
        from option1 in Any
        from option2 in GenerateUnequal(option1)
        select (option1, option2);

    public static Gen<(Option<object>, Option<object>)> Pair { get; } =
        Gen.OneOf(EqualPair,
                  UnequalPair,
                  Gen.Select(Any, Any));

    public static Gen<Option<object>> GenerateEqual(Option<object> option) =>
        Gen.Const(option.Match(value => Option.Some(value),
                               () => Option.None));

    public static Gen<Option<object>> GenerateUnequal(Option<object> option) =>
        option.Match(value1 => from option2 in Any
                               where option2.Match(value2 => value1.Equals(value2) is false,
                                                   () => true)
                               select option2,
                     () => Some);
}