using System;
using System.Threading.Tasks;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace common.tests;

public sealed class ResultIsSuccessAssertion<T>(AssertionContext<Result<T>> context) : Assertion<Result<T>>(context)
{
    protected override string GetExpectation() => "Result to be Success.";

    public ValueAssertion<T> WhoseValue =>
        new TAssertion(Context.Map(result => result switch
        {
            Success<T> { Value: var t } => t,
            Error => throw new InvalidOperationException("result is Error"),
            null => throw new InvalidOperationException("result is null"),
        }));

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<Result<T>> metadata)
    {
        await ValueTask.CompletedTask;

        var value = metadata.Value;
        var exception = metadata.Exception;

        return (value, exception) switch
        {
            (_, not null) => AssertionResult.Failed($"threw {exception.GetType().Name} with message {exception.Message}."),
            (Success<T>, _) => AssertionResult.Passed,
            (Error, _) => AssertionResult.Failed("result is Error"),
            (null, _) => AssertionResult.Failed("result is null"),
        };
    }

    private sealed class TAssertion(AssertionContext<T> context) : ValueAssertion<T>(context);
}

public sealed class ResultIsErrorAssertion<T>(AssertionContext<Result<T>> context) : Assertion<Result<T>>(context)
{
    public ValueAssertion<Error> Which =>
        new ErrorAssertion(Context.Map(result => result switch
        {
            Error error => error,
            Success<T> { Value: var t } => throw new InvalidOperationException($"result is Success with value {t}"),
            null => throw new InvalidOperationException("result is null"),
        }));

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<Result<T>> metadata)
    {
        await ValueTask.CompletedTask;

        var value = metadata.Value;
        var exception = metadata.Exception;

        return (value, exception) switch
        {
            (_, not null) => AssertionResult.Failed($"threw {exception.GetType().Name} with message {exception.Message}."),
            (Error, _) => AssertionResult.Passed,
            (Success<T> { Value: var t }, _) => AssertionResult.Failed($"result is Success with value {t}"),
            (null, _) => AssertionResult.Failed("result is null"),
        };
    }

    protected override string GetExpectation() => "to be Error";

    private sealed class ErrorAssertion(AssertionContext<Error> context) : ValueAssertion<Error>(context);
}

public sealed class OptionIsSomeAssertion<T>(AssertionContext<Option<T>> context) : Assertion<Option<T>>(context)
{
    protected override string GetExpectation() => "Option to be Some.";

    public ValueAssertion<T> WhoseValue =>
        new TAssertion(Context.Map(option => option switch
        {
            Some<T> { Value: var t } => t,
            None => throw new InvalidOperationException("option is None"),
            null => throw new InvalidOperationException("option is null"),
        }));

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<Option<T>> metadata)
    {
        await ValueTask.CompletedTask;

        var value = metadata.Value;
        var exception = metadata.Exception;

        return (value, exception) switch
        {
            (_, not null) => AssertionResult.Failed($"threw {exception.GetType().Name} with message {exception.Message}."),
            (Some<T>, _) => AssertionResult.Passed,
            (None, _) => AssertionResult.Failed("option is None"),
            (null, _) => AssertionResult.Failed("option is null"),
        };
    }

    private sealed class TAssertion(AssertionContext<T> context) : ValueAssertion<T>(context);
}

public static class ResultAssertionExtensions
{
    extension<T>(IAssertionSource<Result<T>> source)
    {
        public ResultIsSuccessAssertion<T> IsSuccess()
        {
            source.Context.ExpressionBuilder.Append($".IsSuccess()");
            return new(source.Context);
        }

        public ResultIsErrorAssertion<T> IsError()
        {
            source.Context.ExpressionBuilder.Append($".IsError()");
            return new(source.Context);
        }
    }
}

public sealed class OptionIsNoneAssertion<T>(AssertionContext<Option<T>> context) : Assertion<Option<T>>(context)
{
    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<Option<T>> metadata)
    {
        await ValueTask.CompletedTask;

        var value = metadata.Value;
        var exception = metadata.Exception;

        return (value, exception) switch
        {
            (_, not null) => AssertionResult.Failed($"threw {exception.GetType().Name} with message {exception.Message}."),
            (None, _) => AssertionResult.Passed,
            (Some<T> { Value: var t }, _) => AssertionResult.Failed($"option is Some with value {t}"),
            (null, _) => AssertionResult.Failed("option is null"),
        };
    }

    protected override string GetExpectation() => "to be None";
}

public static class OptionAssertionExtensions
{
    extension<T>(IAssertionSource<Option<T>> source)
    {
        public OptionIsSomeAssertion<T> IsSome()
        {
            source.Context.ExpressionBuilder.Append($".IsSome()");
            return new(source.Context);
        }

        public OptionIsNoneAssertion<T> IsNone()
        {
            source.Context.ExpressionBuilder.Append($".IsNone()");
            return new(source.Context);
        }
    }
}