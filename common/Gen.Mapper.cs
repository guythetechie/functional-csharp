using CsCheck;
using System;
using System.Linq;

namespace common;

public static class MapperGenerator
{
    public static Gen<Func<int, int>> IntToInt { get; } =
        Gen.OneOf(// Add x to the integer, ensuring that the output doesn't exceed the bounds of an int
                  from x in Gen.Int
                  select (Func<int, int>)(i => (int)Math.Clamp((long)i + x, int.MinValue, int.MaxValue)),
                  // Multiply the integer by x, ensuring that the output doesn't exceed the bounds of an int
                  from x in Gen.Int[-10, 10]
                  select (Func<int, int>)(i => (int)Math.Clamp((long)i * x, int.MinValue, int.MaxValue)));

    public static Gen<Func<string, int>> StringToInt { get; } =
        from f in IntToInt
        select (Func<string, int>)(s =>
        {
            var sum = s.Sum(c => (long)c);
            var intSum = (int)Math.Clamp(sum, int.MinValue, int.MaxValue);
            return f(intSum);
        });

    public static Gen<Func<int, bool>> IntPredicate { get; } =
        from x in Gen.Int[3, 10]
        select (Func<int, bool>)(i => i % x == 0);

    public static Gen<Func<string, bool>> StringPredicate { get; } =
        from f in IntPredicate
        from g in StringToInt
        select (Func<string, bool>)(s => f(g(s)));

    public static Gen<Func<object, bool>> ObjectPredicate { get; } =
        Gen.OneOf(from predicate in StringPredicate
                  select new Func<object, bool>(x => predicate(x.ToString())),
                  from f in StringToInt
                  from predicate in IntPredicate
                  select new Func<object, bool>(x => predicate(f(x.ToString()))));

    public static Gen<Func<int, string>> IntToString { get; } =
        Gen.OneOf(// Convert the integer to a string in a given base
                  from @base in Gen.OneOfConst(2, 8, 10, 16)
                  select (Func<int, string>)(i => Convert.ToString(i, @base)),
                  // Add a prefix and suffix to the integer when converting to a string
                  from prefix in Gen.Char.AlphaNumeric.Array[1, 10]
                  from suffix in Gen.Char.AlphaNumeric.Array[1, 10]
                  select (Func<int, string>)(i => $"{new string(prefix)}{i}{new string(suffix)}"));

    public static Gen<Func<object, object>> ObjectToObject { get; } =
        Gen.OneOf(Gen.Const(new Func<object, object>(x => x)),
                  Gen.Const(new Func<object, object>(x => x?.ToString() ?? string.Empty)),
                  from x2 in Generator.Object
                  select new Func<object, object>(x => x2),
                  from f in ObjectPredicate
                  select new Func<object, object>(x => f(x)),
                  from f in StringToInt
                  select new Func<object, object>(x => f(x.ToString())),
                  from f in IntToString
                  from g in StringToInt
                  select new Func<object, object>(x => f(g(x.ToString()))));
                  
    public static Gen<Func<object, Option<object>>> ObjectToOption { get; } =
        from predicate in ObjectPredicate
        from f in ObjectToObject
        select new Func<object, Option<object>>(x => predicate(x)
                                                        ? Option.Some(f(x))
                                                        : Option.None);

    public static Gen<Func<Error, Error>> ErrorToError { get; } =
        from updatedError in ErrorGenerator.Any
        select new Func<Error, Error>(error => error + updatedError);

    public static Gen<Func<Error, object>> ErrorToObject { get; } =
        from f in ObjectToObject
        select new Func<Error, object>(f);

    public static Gen<Func<object, Result<object>>> ObjectToResult { get; } =
        from predicate in ObjectPredicate
        from f in ObjectToObject
        from error in ErrorGenerator.Any
        select new Func<object, Result<object>>(x => predicate(x)
                                                        ? Result.Success(f(x))
                                                        : Result.Error<object>(error + Error.From($"Predicate failed for {x}")));
}