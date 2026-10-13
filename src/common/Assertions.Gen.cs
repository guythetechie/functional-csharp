using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CsCheck;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace common;

public sealed class GenGeneratesValueSatisfyingAssertion<T>(AssertionContext<Gen<T>> context, Func<T, bool> predicate, int attempts)
    : MappedValueAssertion<Gen<T>, T>(context,
                                      async gen =>
                                      {
                                          await ValueTask.CompletedTask;

                                          var result = Result.Error<T>(Error.From($"found none within {attempts} attempts"));

                                          gen.Sample(t =>
                                          {
                                              if (result.IsError && predicate(t))
                                              {
                                                  result = Result.Success(t);
                                              }
                                          }, iter: attempts, threads: 1);

                                          return result;
                                      })
{
    public ValueAssertion<T> WhoseValue =>
        GetMappedAssertion();

    protected override string GetExpectation() => "to generate a value that satisfies the predicate";
}

public static class GenAssertionExtensions
{
    extension<T>(IAssertionSource<Gen<T>> source)
    {
        public GenGeneratesValueSatisfyingAssertion<T> GeneratesValueSatisfying(Func<T, bool> predicate, int attempts = 1000, [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
        {
            source.Context.ExpressionBuilder.Append($".GeneratesValueSatisfying({predicateExpression})");
            return new(source.Context, predicate, attempts);
        }
    }
}