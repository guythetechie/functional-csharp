using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace common;

public abstract class MappedValueAssertion<TSource, TMapped>(AssertionContext<TSource> context, Func<TSource, ValueTask<Result<TMapped>>> mapper) : Assertion<TSource>(context)
{
    private Option<TMapped> value = Option.None;

    protected abstract override string GetExpectation();

    protected ValueAssertion<TMapped> GetMappedAssertion([CallerMemberName] string expressionSegment = "")
    {
        Context.ExpressionBuilder.Append($".{expressionSegment}");

        return new TAssertion<TMapped>(Context.Map(async _ =>
        {
            await AssertAsync();

            return value.IfNoneThrow(() => new InvalidOperationException("Assertion has not yet been evaluated or did not pass."));
        }));
    }

    protected sealed override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TSource> metadata)
    {
        value = Option.None;

        if (metadata.Exception is not null)
        {
            return AssertionResult.Failed($"threw {metadata.Exception.GetType().Name} with message {metadata.Exception.Message}", metadata.Exception);
        }

        if (metadata.Value is not { } source)
        {
            return AssertionResult.Failed("was null");
        }

        var result = await mapper(source);

        return await result.Match(async mapped =>
                                  {
                                      var mappedMetadata = new EvaluationMetadata<TMapped>(mapped,
                                                                                           null,
                                                                                           metadata.StartTime,
                                                                                           metadata.EndTime);

                                      var assertionResult = await CheckMappedAsync(mappedMetadata);

                                      if (assertionResult.IsPassed)
                                      {
                                          value = Option.Some(mapped);
                                      }

                                      return assertionResult;
                                  },
                                  error => Task.FromResult(MapErrorToAssertionResult(error)));
    }

    protected virtual Task<AssertionResult> CheckMappedAsync(EvaluationMetadata<TMapped> metadata) =>
        Task.FromResult(AssertionResult.Passed);

    protected virtual AssertionResult MapErrorToAssertionResult(Error error) =>
        AssertionResult.Failed(error.ToString());

    private sealed class TAssertion<T>(AssertionContext<T> context) : ValueAssertion<T>(context);
}