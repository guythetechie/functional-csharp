using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace common.tests;

internal sealed class EitherAssertions<TLeft, TRight>(Either<TLeft, TRight> subject, AssertionChain assertionChain) : ReferenceTypeAssertions<Either<TLeft, TRight>, EitherAssertions<TLeft, TRight>>(subject, assertionChain)
{
    protected override string Identifier { get; } = "either";

    public AndWhichConstraint<EitherAssertions<TLeft, TRight>, TLeft> BeLeft([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsLeft)
            .FailWith("Expected {context:either} to be left{reason}, but it was right with value {0}.",
                       () => Subject.Match(_ => throw new UnreachableException(), right => right));

        return new AndWhichConstraint<EitherAssertions<TLeft, TRight>, TLeft>(
            this,
            Subject.IfRightThrow(new UnreachableException()));
    }

    public AndWhichConstraint<EitherAssertions<TLeft, TRight>, TRight> BeRight([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsRight)
            .FailWith("Expected {context:either} to be right{reason}, but it was left with value {0}.",
                       () => Subject.Match(left => left, _ => throw new UnreachableException()));

        return new AndWhichConstraint<EitherAssertions<TLeft, TRight>, TRight>(
            this,
            Subject.IfLeftThrow(new UnreachableException()));
    }

    public AndConstraint<EitherAssertions<TLeft, TRight>> Be(Either<TLeft, TRight> expected, [StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Equals(expected))
            .FailWith("Expected {context:either} to be {0}{reason}, but it was {1}.", expected, Subject);

        return new AndConstraint<EitherAssertions<TLeft, TRight>>(this);
    }
}


internal sealed class OptionAssertions<T>(Option<T> subject, AssertionChain assertionChain) : ReferenceTypeAssertions<Option<T>, OptionAssertions<T>>(subject, assertionChain)
{
    protected override string Identifier { get; } = "option";

    public AndWhichConstraint<OptionAssertions<T>, T> BeSome([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsSome)
            .FailWith("Expected {context:option} to be some{reason}, but it was none.");

        return new AndWhichConstraint<OptionAssertions<T>, T>(
            this,
            Subject.IfNoneThrow(() => new UnreachableException()));
    }

    public AndConstraint<OptionAssertions<T>> BeNone([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsNone)
            .FailWith("Expected {context:option} to be none{reason}, but it was some with value {0}.",
                       () => Subject.Match(value => value, () => throw new UnreachableException()));

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    public AndConstraint<OptionAssertions<T>> Be(Option<T> expected, [StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Equals(expected))
            .FailWith("Expected {context:option} to be {0}{reason}, but it was {1}.", expected, Subject);

        return new AndConstraint<OptionAssertions<T>>(this);
    }
}

internal sealed class ResultAssertions<T>(Result<T> subject, AssertionChain assertionChain) : ReferenceTypeAssertions<Result<T>, ResultAssertions<T>>(subject, assertionChain)
{
    protected override string Identifier { get; } = "result";

    public AndWhichConstraint<ResultAssertions<T>, T> BeSuccess([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsSuccess)
            .FailWith("Expected {context:result} to be success{reason}, but it was error with error {0}.",
                       () => Subject.Match(_ => throw new UnreachableException(), error => error));

        return new AndWhichConstraint<ResultAssertions<T>, T>(
            this,
            Subject.IfErrorThrow());
    }

    public AndWhichConstraint<ResultAssertions<T>, Error> BeError([StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsError)
            .FailWith("Expected {context:result} to be error{reason}, but it was success with value {0}.",
                       () => Subject.IfErrorThrow());

        return new AndWhichConstraint<ResultAssertions<T>, Error>(
            this,
            Subject.Match(success => throw (new UnreachableException()), error => error));
    }

    public AndConstraint<ResultAssertions<T>> Be(Result<T> expected, [StringSyntax("CompositeFormat")] string because = "", params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Equals(expected))
            .FailWith("Expected {context:result} to be {0}{reason}, but it was {1}.", expected, Subject);

        return new AndConstraint<ResultAssertions<T>>(this);
    }
}

internal static class AssertionExtensions
{
    public static EitherAssertions<TLeft, TRight> Should<TLeft, TRight>(this Either<TLeft, TRight> subject) =>
        new(subject, AssertionChain.GetOrCreate());

    public static OptionAssertions<T> Should<T>(this Option<T> subject) =>
        new(subject, AssertionChain.GetOrCreate());

    public static ResultAssertions<T> Should<T>(this Result<T> subject) =>
        new(subject, AssertionChain.GetOrCreate());
}