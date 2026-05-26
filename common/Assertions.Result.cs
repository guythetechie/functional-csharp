using System.Threading.Tasks;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace common;

public sealed class ResultIsSuccessAssertion<T>(AssertionContext<Result<T>> context)
    : MappedValueAssertion<Result<T>, T>(context,
                                         async result =>
                                         {
                                             await ValueTask.CompletedTask;

                                             return result.MapError(error => Error.From($"it is error: '{error}'"));
                                         })
{
    public ValueAssertion<T> WhoseValue =>
        GetMappedAssertion();

    protected override string GetExpectation() => "result to be Success.";
}

public sealed class ResultIsErrorAssertion<T>(AssertionContext<Result<T>> context)
     : MappedValueAssertion<Result<T>, Error>(context, async result =>
       {
           await ValueTask.CompletedTask;

           return result.Match(success => Error.From("it is success"),
                               error => Result.Success(error));
       })
{
    public ValueAssertion<Error> Which =>
        GetMappedAssertion();

    protected override string GetExpectation() => "result to be Error";
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