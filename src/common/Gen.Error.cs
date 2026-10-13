using CsCheck;
using System;
using System.Linq;

namespace common;

public static class ErrorGenerator
{
    public static Gen<Error> WithMessage { get; } =
        from messages in StringGenerator.NonNullOrWhitespace.HashSet
        where messages.Count > 0
        select Error.From([.. messages]);

    public static Gen<Error> WithException { get; } =
        from messages in StringGenerator.NonNullOrWhitespace.HashSet
        where messages.Count > 0
        from exceptions in
            Generator.Traverse(messages,
                               message => Gen.OneOfConst<Exception>(new InvalidOperationException(message),
                                                         new ArgumentException(message),
                                                         new ArgumentNullException(message)))
        select Error.From([.. exceptions]);

    public static Gen<Error> Any { get; } =
        Gen.OneOf(WithMessage, WithException);
}