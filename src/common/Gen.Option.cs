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
}