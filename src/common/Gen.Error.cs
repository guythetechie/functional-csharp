using CsCheck;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace common;

public static class ExceptionGenerator
{
    private static Gen<Exception> NonAggregateException { get; } =
        from message in StringGenerator.NonNullOrWhitespace
        from exception in
            Gen.OneOf<Exception>(new InvalidOperationException(message),
                                 new ArgumentException(message),
                                 new ArgumentNullException(message))
        select exception;

    public static Gen<Exception> Any { get; } =
        Gen.Recursive<Exception>((depth, gen) => depth switch
        {
            < 3 => Gen.OneOf(NonAggregateException,
                             from exceptions in gen.ArrayOf(1, 3)
                             select new AggregateException(exceptions)),
            >= 3 => NonAggregateException
        });
}

public static class ErrorGenerator
{
    private static Gen<ImmutableHashSet<string>> ErrorMessages { get; } =
        from messages in StringGenerator.NonNullOrWhitespace.HashSetOf()
        where messages.Count > 0
        select messages;

    private static Gen<ImmutableHashSet<Exception>> Exceptions { get; } =
        from exceptions in ExceptionGenerator.Any.HashSetOf()
        where exceptions.Count > 0
        select exceptions;
    
    public static Gen<Error> WithMessage { get; } =
        from messages in ErrorMessages
        select Error.From([.. messages]);

    public static Gen<Error> WithException { get; } =
        from exceptions in Exceptions
        select Error.From([.. exceptions]);

    public static Gen<Error> Mixed { get; } =
        from messages in ErrorMessages
        from exceptions in Exceptions
        select Error.From([.. messages]) + Error.From([.. exceptions]);

    public static Gen<Error> Any { get; } =
        Gen.OneOf(WithMessage,
                  WithException,
                  Mixed);

    public static Gen<(Error, Error)> EqualPair { get; } =
        from error1 in Any
        from error2 in GenerateEqual(error1)
        select (error1, error2);

    public static Gen<(Error, Error)> UnequalPair { get; } =
        from error1 in Any
        from error2 in GenerateUnequal(error1)
        select (error1, error2);

    public static Gen<(Error, Error)> Pair { get; } =
        Gen.OneOf(EqualPair,
                  UnequalPair,
                  Gen.Select(Any, Any));

    public static Gen<Error> GenerateEqual(Error error) =>
        Gen.Const(error with { });

    public static Gen<Error> GenerateUnequal(Error error) =>
        from other in Any
        where error.Equals(other) is false
        select other;
}