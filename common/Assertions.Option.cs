using System.Threading.Tasks;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace common;

public sealed class OptionIsSomeAssertion<T>(AssertionContext<Option<T>> context)
    : MappedValueAssertion<Option<T>, T>(context,
                                         async option =>
                                         {
                                             await ValueTask.CompletedTask;

                                             return option.ToResult(() => Error.From("it is None"));
                                         })
{
    public ValueAssertion<T> WhoseValue =>
        GetMappedAssertion();

    protected override string GetExpectation() => "option to be Some";
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
            (null, _) => AssertionResult.Failed("option is null"),
            (var option, _) =>
                option.Match(t => AssertionResult.Failed($"it is Some with value {t}"),
                             () => AssertionResult.Passed)
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