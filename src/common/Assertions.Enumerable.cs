using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace common;

public class AsyncEnumerableIsEquivalentToAssertion<T>(AssertionContext<ImmutableArray<T>> context, IAsyncEnumerable<T> expected, CollectionOrdering ordering, CancellationToken cancellationToken = default) : Assertion<ImmutableArray<T>>(context)
{
    protected override string GetExpectation() => $"to be equivalent to the  expected async enumerable.";

    public override async Task<ImmutableArray<T>> AssertAsync()
    {
        var expectedArray = await expected.ToArrayAsync(cancellationToken);
        var assertion = new IsEquivalentToAssertion<ImmutableArray<T>, T>(Context, expectedArray, ordering);

        return await assertion.AssertAsync();
    }
}

public static class AsyncEnumerableAssertionExtensions
{
    extension<T>(IAssertionSource<IAsyncEnumerable<T>> source)
    {
        public IsEquivalentToAssertion<ImmutableArray<T>, T> IsEquivalentTo(IEnumerable<T> expected, CollectionOrdering ordering = CollectionOrdering.Any, CancellationToken cancellationToken = default, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null, [CallerArgumentExpression(nameof(ordering))] string? orderingExpression = null)
        {
            var context = source.Context;

            context.ExpressionBuilder.Append(".IsEquivalentTo(");
            context.ExpressionBuilder.Append(expectedExpression);

            if (orderingExpression is not null)
            {
                context.ExpressionBuilder.Append(", ");
                context.ExpressionBuilder.Append(orderingExpression);
            }

            context.ExpressionBuilder.Append(')');

            var mappedContext = context.Map(s => Map(s, cancellationToken));

            return new(mappedContext, expected, ordering);
        }

        public AsyncEnumerableIsEquivalentToAssertion<T> IsEquivalentTo(IAsyncEnumerable<T> expected, CollectionOrdering ordering = CollectionOrdering.Any, CancellationToken cancellationToken = default, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null, [CallerArgumentExpression(nameof(ordering))] string? orderingExpression = null)
        {
            var context = source.Context;

            context.ExpressionBuilder.Append(".IsEquivalentTo(");
            context.ExpressionBuilder.Append(expectedExpression);

            if (orderingExpression is not null)
            {
                context.ExpressionBuilder.Append(", ");
                context.ExpressionBuilder.Append(orderingExpression);
            }

            context.ExpressionBuilder.Append(')');

            var mappedContext = context.Map(s => Map(s, cancellationToken));
            return new(mappedContext, expected, ordering, cancellationToken);
        }
    }

    extension<TCollection, T>(IAssertionSource<TCollection> source) where TCollection : IEnumerable<T>
    {
        public AsyncEnumerableIsEquivalentToAssertion<T> IsEquivalentTo(IAsyncEnumerable<T> expected, CollectionOrdering ordering = CollectionOrdering.Any, CancellationToken cancellationToken = default, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null, [CallerArgumentExpression(nameof(ordering))] string? orderingExpression = null)
        {
            var context = source.Context;

            context.ExpressionBuilder.Append(".IsEquivalentTo(");
            context.ExpressionBuilder.Append(expectedExpression);

            if (orderingExpression is not null)
            {
                context.ExpressionBuilder.Append(", ");
                context.ExpressionBuilder.Append(orderingExpression);
            }

            context.ExpressionBuilder.Append(')');

            var mappedContext = context.Map(tCollection =>
            {
                ArgumentNullException.ThrowIfNull(tCollection);
                return tCollection.ToImmutableArray();
            });

            return new(mappedContext, expected, ordering, cancellationToken);
        }
    }

    private static async Task<ImmutableArray<T>> Map<T>(IAsyncEnumerable<T>? source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        var array = await source.ToArrayAsync(cancellationToken);

        return [.. array];
    }
}