using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace common.tests;

public class EnumerableExtensionsTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void Head_with_empty_enumerable_returns_none()
    {
        var emptyEnumerable = Enumerable.Empty<int>();

        var result = emptyEnumerable.Head();

        result.Should().BeNone();
    }

    [Fact]
    public void Head_with_an_element_returns_some_with_first_element()
    {
        var gen = from first in Gen.Int
                  from tail in Gen.Int.Array
                  let array = tail.Prepend(first)
                  select (first, array);

        gen.Sample(x =>
        {
            var (first, array) = x;

            var result = array.Head();

            result.Should().BeSome().Which.Should().Be(first);
        });
    }

    [Fact]
    public void Head_works_with_infinite_sequences()
    {
        var enumerable = Enumerable.Range(1, int.MaxValue);

        var result = enumerable.Head();

        result.Should().BeSome();
    }

    [Fact]
    public void SingleOrNone_with_empty_enumerable_returns_none()
    {
        var emptyEnumerable = Enumerable.Empty<int>();

        var result = emptyEnumerable.SingleOrNone();

        result.Should().BeNone();
    }

    [Fact]
    public void SingleOrNone_with_one_element_returns_some_with_that_element()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = new[] { value }.SingleOrNone();

            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void SingleOrNone_with_multiple_elements_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 1
                  select array;

        gen.Sample(array =>
        {
            var result = array.SingleOrNone();

            result.Should().BeNone();
        });
    }

    [Fact]
    public void Choose_result_length_never_exceeds_source()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.OptionSelector
                  select (array, selector);

        gen.Sample(x =>
        {
            var (array, selector) = x;

            var result = array.Choose(selector);

            result.Should().HaveCountLessThanOrEqualTo(array.Length);
        });
    }

    [Fact]
    public void Choose_maintains_order_of_elements()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            Option<(string, int Index)> selector((int item, int index) pair)
            {
                var gen = from option in Generator.GenerateOption(Gen.String)
                          select from x in option
                                 select (x, pair.index);

                return gen.Single();
            }

            var result = array.Select((item, index) => (item, index))
                              .Choose(selector);

            result.Select(x => x.Index).Should().BeInAscendingOrder();
        });
    }

    [Fact]
    public void Choose_with_all_somes_has_same_count_as_original()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.AllSomesSelector
                  select (array, selector);

        gen.Sample(x =>
        {
            var (array, selector) = x;

            var result = array.Choose(selector);

            result.Should().HaveSameCount(array);
        });
    }

    [Fact]
    public void Choose_with_all_nones_returns_an_empty_sequence()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var selector = Generator.AllNonesSelector;

            var result = array.Choose(selector);

            result.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_result_length_never_exceeds_source()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.OptionAsyncSelector
                  select (array, selector);

        await gen.SampleAsync(async x =>
        {
            var (array, selector) = x;

            var result = array.Choose(selector);

            // Assert
            var resultArray = await result.ToArrayAsync(CancellationToken);
            resultArray.Should().HaveCountLessThanOrEqualTo(array.Length);
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_maintains_order_of_elements()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            ValueTask<Option<(string, int Index)>> selector((int item, int index) pair)
            {
                var gen = from option in Generator.GenerateOption(Gen.String)
                          select from x in option
                                 select (x, pair.index);

                return ValueTask.FromResult(gen.Single());
            }

            var result = array.Select((item, index) => (item, index))
                              .Choose(selector);

            // Assert
            var resultArray = await result.ToArrayAsync(CancellationToken);
            resultArray.Select(x => x.Index).Should().BeInAscendingOrder();
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_all_somes_has_same_count_as_original()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.AllSomesAsyncSelector
                  select (array, selector);

        await gen.SampleAsync(async x =>
        {
            var (array, selector) = x;

            var result = array.Choose(selector);

            // Assert
            var resultArray = await result.ToArrayAsync(CancellationToken);
            resultArray.Should().HaveSameCount(array);
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_all_nones_returns_an_empty_sequence()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var selector = Generator.AllNonesAsyncSelector;

            var result = array.Choose(selector);

            // Assert
            var resultArray = await result.ToArrayAsync(CancellationToken);
            resultArray.Should().BeEmpty();
        });
    }

    [Fact]
    public void Pick_with_empty_enumerable_returns_none()
    {
        var gen = Generator.OptionSelector;

        gen.Sample(selector =>
        {
            var source = Enumerable.Empty<int>();

            var result = source.Pick(selector);

            result.Should().BeNone();
        });
    }

    [Fact]
    public void Pick_with_all_nones_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  select array;

        gen.Sample(array =>
        {
            var selector = Generator.AllNonesSelector;

            var result = array.Pick(selector);

            result.Should().BeNone();
        });
    }

    [Fact]
    public void Pick_with_all_somes_returns_some()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 0
                  from selector in Generator.AllSomesSelector
                  select (array, selector);

        gen.Sample(x =>
        {
            var (array, selector) = x;

            var result = array.Pick(selector);

            result.Should().BeSome();
        });
    }

    // Traverse has three laws:
    // - Identity law: array.Traverse(x => Some(x)) == Some(array)
    // - Naturality law: array.Traverse(x => f(x).ToOption()) == array.Traverse(f).ToOption()
    // - Composition law: array.Traverse(x => f(x).Map(g)) == array.Traverse(f).Map(array => array.Traverse(g))

    [Fact]
    public void Traverse_with_result_passes_identity_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var result = array.Traverse(Result.Success, CancellationToken);

            result.Should().BeSuccess().Which.Should().Equal(array);
        });
    }

    [Fact]
    public void Traverse_with_result_passes_naturality_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var selector = Selectors.MixedResult;

            // Act
            var path1 = array.Traverse(selector, CancellationToken)
                             .ToOption();

            var path2 = array.Traverse(x => selector(x).ToOption(), CancellationToken);

            // Assert
            path1.Match(path1Array => path2.Should().BeSome().Which.Should().Equal(path1Array),
                        () => path2.Should().BeNone());
        });
    }

    [Fact]
    public void Traverse_with_result_passes_composition_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            // Arrange
            var resultSelector = Selectors.MixedResult;
            var optionSelector = Selectors.MixedOption;

            // Act
            var path1 = array.Traverse(x => resultSelector(x).Map(optionSelector), CancellationToken)
                             .Map(optionArray => optionArray.Traverse(x => x, CancellationToken));

            var path2 = array.Traverse(resultSelector, CancellationToken)
                             .Map(array => array.Traverse(optionSelector, CancellationToken));

            // Assert
            path1.Match(path1Option => path1Option.Match(path1Array => path2.Should().BeSuccess().Which.Should().BeSome().Which.Should().Equal(path1Array),
                                                         () => path2.Should().BeSuccess().Which.Should().BeNone()),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public void Traverse_with_option_passes_identity_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var result = array.Traverse(Option.Some, CancellationToken);

            result.Should().BeSome().Which.Should().Equal(array);
        });
    }

    [Fact]
    public void Traverse_with_option_passes_naturality_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            // Arrange
            var selector = Selectors.MixedOption;

            // Act
            var path1 = array.Traverse(selector, CancellationToken)
                             .ToResult(() => Error.From("test error"));

            var path2 = array.Traverse(x => selector(x).ToResult(() => Error.From("test error")), CancellationToken);

            // Assert
            path1.Match(path1Array => path2.Should().BeSuccess().Which.Should().Equal(path1Array),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public void Traverse_with_option_passes_composition_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            // Arrange
            var optionSelector = Selectors.MixedOption;
            var resultSelector = Selectors.MixedResult;

            // Act
            var path1 = array.Traverse(x => optionSelector(x).Map(resultSelector), CancellationToken)
                             .Map(resultArray => resultArray.Traverse(x => x, CancellationToken));

            var path2 = array.Traverse(optionSelector, CancellationToken)
                             .Map(array => array.Traverse(resultSelector, CancellationToken));

            // Assert
            path1.Match(path1Result => path1Result.Match(path1Array => path2.Should().BeSome().Which.Should().BeSuccess().Which.Should().Equal(path1Array),
                                                         error => path2.Should().BeSome().Which.Should().Be(error)),
                        () => path2.Should().BeNone());
        });
    }

    [Fact]
    public void Iter_calls_action_for_each_element()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var addedItems = ImmutableArray.Create<int>();

            array.Iter(x => ImmutableInterlocked.Update(ref addedItems, items => items.Add(x)),
                       CancellationToken);

            addedItems.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public void Iter_with_cancellation_token_respects_cancellation()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 2
                  from cancelAfter in Gen.Int[1, array.Length - 1]
                  select (array, cancelAfter);

        gen.Sample(x =>
        {
            var (array, cancelAfter) = x;
            using var CancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Action action = () => array.Iter(x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    CancellationTokenSource.Cancel();
                }
            }, CancellationTokenSource.Token);

            action.Should().Throw<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public void IterParallel_calls_action_for_each_element()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Generator.GenerateOption(Gen.Int[1, array.Length + 1])
                  select (array, maxDegreesOfParallelism);

        gen.Sample(x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            array.IterParallel(x => ImmutableInterlocked.Update(ref addedItems, items => items.Add(x)),
                               maxDegreesOfParallelism,
                               CancellationToken);

            addedItems.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public void IterParallel_with_max_degree_of_parallelism_limits_parallelism()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1]
                  select (array, maxDegreesOfParallelism);

        gen.Sample(x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var iterations = 0;

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                array.IterParallel(_ =>
                {
                    iterations++;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken);
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031 // Do not catch general exception types

            // Ensure that the number of iterations did not exceed the max degree of parallelism
            iterations.Should().BeLessThanOrEqualTo(maxDegreesOfParallelism);
        });
    }

    [Fact]
    public async Task IterTask_calls_action_for_each_element()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var addedItems = ImmutableArray.Create<int>();

            await array.IterTask(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, CancellationToken);

            addedItems.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task IterTask_with_cancellation_token_respects_cancellation()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 2
                  from cancelAfter in Gen.Int[1, array.Length - 1]
                  select (array, cancelAfter);

        await gen.SampleAsync(async x =>
        {
            var (array, cancelAfter) = x;
            using var CancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Func<Task> f = async () => await array.IterTask(async x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    await CancellationTokenSource.CancelAsync();
                }
            }, CancellationTokenSource.Token);

            await f.Should().ThrowAsync<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public async Task IterTaskParallel_calls_action_for_each_element()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Generator.GenerateOption(Gen.Int[1, array.Length + 1])
                  select (array, maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            await array.IterTaskParallel(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, maxDegreesOfParallelism, CancellationToken);

            addedItems.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task IterTaskParallel_with_max_degree_of_parallelism_limits_parallelism()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1]
                  select (array, maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var iterations = 0;

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                await array.IterTaskParallel(async _ =>
                {
                    iterations++;
                    await ValueTask.CompletedTask;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken);
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031 // Do not catch general exception types

            iterations.Should().BeLessThanOrEqualTo(maxDegreesOfParallelism);
        });
    }

    [Fact]
    public void Tap_executes_side_effect_and_preserves_original_enumerable()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var tapCount = 0;

            var result = array.Tap(_ => tapCount++)
                              .ToArray();

            result.Should().BeEquivalentTo(array);
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public void Tap_is_lazily_evaluated()
    {
        var gen = Gen.Int.Array.Where(arr => arr.Length > 0);

        gen.Sample(array =>
        {
            var tapCount = 0;

            // Create the tapped enumerable but don't enumerate it yet
            var tappedEnumerable = array.Tap(_ => tapCount++);

            // Side effect should not have executed yet
            tapCount.Should().Be(0);

            // Now enumerate it
            var _ = tappedEnumerable.ToArray();

            // Side effect should have executed for each element
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public void Tap_can_be_chained_multiple_times()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var firstTapCount = 0;
            var secondTapCount = 0;

            var result = array.Tap(_ => firstTapCount++)
                              .Tap(_ => secondTapCount++)
                              .ToArray();

            result.Should().BeEquivalentTo(array);
            firstTapCount.Should().Be(array.Length);
            secondTapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public void Unzip_unzips_items()
    {
        var gen = from firstArray in Gen.Int.Array
                  from secondArray in Gen.String.Array[firstArray.Length]
                  select (firstArray, secondArray);

        gen.Sample(x =>
        {
            var (firstArray, secondArray) = x;
            var zippedArray = firstArray.Zip(secondArray);

            var (firstResult, secondResult) = zippedArray.Unzip();

            firstResult.Should().BeEquivalentTo(firstArray);
            secondResult.Should().BeEquivalentTo(secondArray);
        });
    }
}

public class AsyncEnumerableExtensionsTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Head_with_empty_enumerable_returns_none()
    {
        var emptyEnumerable = AsyncEnumerable.Empty<int>();

        var result = await emptyEnumerable.Head(CancellationToken);

        result.Should().BeNone();
    }

    [Fact]
    public async Task Head_with_an_element_returns_some_with_first_element()
    {
        var gen = from first in Gen.Int
                  from tail in Gen.Int.Array
                  let array = tail.Prepend(first)
                  select (first, array.ToAsyncEnumerable());

        await gen.SampleAsync(async x =>
        {
            var (first, array) = x;

            var result = await array.Head(CancellationToken);

            result.Should().BeSome().Which.Should().Be(first);
        });
    }

    [Fact]
    public async Task Choose_result_length_never_exceeds_source()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.OptionSelector
                  select (array.ToAsyncEnumerable(), selector);

        await gen.SampleAsync(async x =>
        {
            var (source, selector) = x;

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(CancellationToken);
            var sourceLength = await source.CountAsync(CancellationToken);
            resultArray.Should().HaveCountLessThanOrEqualTo(sourceLength);
        });
    }

    [Fact]
    public async Task Choose_maintains_order_of_elements()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            Option<(string, int Index)> selector((int item, int index) pair)
            {
                var gen = from option in Generator.GenerateOption(Gen.String)
                          select from x in option
                                 select (x, pair.index);

                return gen.Single();
            }

            var result = source.Select((item, index) => (item, index))
                                .Choose(selector);

            var resultIndexes = await result.Select(x => x.Index)
                                            .ToArrayAsync(CancellationToken);

            resultIndexes.Should().BeInAscendingOrder();
        });
    }

    [Fact]
    public async Task Choose_with_all_somes_has_same_count_as_original()
    {
        var gen = from array in Gen.Int.Array
                  from selector in Generator.AllSomesSelector
                  select (array.ToAsyncEnumerable(), selector);

        await gen.SampleAsync(async x =>
        {
            var (source, selector) = x;

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(CancellationToken);
            var sourceArray = await source.ToArrayAsync(CancellationToken);
            resultArray.Should().HaveSameCount(sourceArray);
        });
    }

    [Fact]
    public async Task Choose_with_all_nones_returns_an_empty_sequence()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            var selector = Generator.AllNonesSelector;

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(CancellationToken);
            resultArray.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Pick_with_empty_enumerable_returns_none()
    {
        var gen = Generator.OptionSelector;

        await gen.SampleAsync(async selector =>
        {
            var source = AsyncEnumerable.Empty<int>();

            var result = await source.Pick(selector, CancellationToken);

            result.Should().BeNone();
        });
    }

    [Fact]
    public async Task Pick_with_all_nones_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            var selector = Generator.AllNonesSelector;

            var result = await source.Pick(selector, CancellationToken);

            result.Should().BeNone();
        });
    }

    [Fact]
    public async Task Pick_with_all_somes_returns_some()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 0
                  from selector in Generator.AllSomesSelector
                  select (array.ToAsyncEnumerable(), selector);

        await gen.SampleAsync(async x =>
        {
            var (source, selector) = x;

            var result = await source.Pick(selector, CancellationToken);

            result.Should().BeSome();
        });
    }

    [Fact]
    public async Task Traverse_with_all_success_returns_success_with_expected_array()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();
            var f = (int x) => ValueTask.FromResult(Result.Success(x));

            var result = await asyncEnumerable.Traverse(f, CancellationToken);

            result.Should().BeSuccess().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_identity_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var source = array.ToAsyncEnumerable();

            var result = await source.Traverse(Selectors.ResultIdentityAsync, CancellationToken);

            result.Should().BeSuccess().Which.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_naturality_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var source = array.ToAsyncEnumerable();
            var selector = Selectors.MixedResultAsync;

            var path1 = (await source.Traverse(selector, CancellationToken)).ToOption();

            var path2 = await source.Traverse(async x => (await selector(x)).ToOption(), CancellationToken);

            path1.Match(path1Array => path2.Should().BeSome().Which.Should().Equal(path1Array),
                        () => path2.Should().BeNone());
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_composition_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var source = array.ToAsyncEnumerable();
            var resultSelector = Selectors.MixedResultAsync;
            var optionSelector = Selectors.MixedOptionAsync;

            var path1FirstPass = await source.Traverse(async x =>
                                    {
                                        var result = await resultSelector(x);
                                        return await result.MapTask(optionSelector);
                                    }, CancellationToken);

            var path1 = await path1FirstPass.MapTask(optionArray => ValueTask.FromResult(optionArray.Traverse(x => x, CancellationToken)));

            var path2FirstPass = await source.Traverse(resultSelector, CancellationToken);
            var path2 = await path2FirstPass.MapTask(values => values.ToAsyncEnumerable()
                                                                     .Traverse(optionSelector, CancellationToken));

            path1.Match(path1Option => path1Option.Match(path1Array => path2.Should().BeSuccess().Which.Should().BeSome().Which.Should().Equal(path1Array),
                                                         () => path2.Should().BeSuccess().Which.Should().BeNone()),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public async Task Traverse_with_option_passes_identity_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();

            var result = await asyncEnumerable.Traverse(Selectors.OptionIdentityAsync, CancellationToken);

            result.Should().BeSome().Which.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Traverse_with_option_passes_naturality_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();
            var selector = Selectors.MixedOptionAsync;

            var path1 = (await asyncEnumerable.Traverse(selector, CancellationToken)).ToResult(() => Error.From("test error"));

            var path2 = await asyncEnumerable.Traverse(async x => (await selector(x)).ToResult(() => Error.From("test error")), CancellationToken);

            path1.Match(path1Array => path2.Should().BeSuccess().Which.Should().Equal(path1Array),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public async Task Traverse_with_option_passes_composition_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();
            var optionSelector = Selectors.MixedOptionAsync;
            var resultSelector = Selectors.MixedResultAsync;

            var path1FirstPass = await asyncEnumerable.Traverse(async x =>
            {
                var option = await optionSelector(x);
                return await option.MapTask(resultSelector);
            }, CancellationToken);

            var path1 = await path1FirstPass.MapTask(resultArray => ValueTask.FromResult(resultArray.Traverse(x => x, CancellationToken)));

            var path2FirstPass = await asyncEnumerable.Traverse(optionSelector, CancellationToken);

            var path2 = await path2FirstPass.MapTask(values => values.ToAsyncEnumerable().Traverse(resultSelector, CancellationToken));

            path1.Match(path1Result => path1Result.Match(path1Array => path2.Should().BeSome().Which.Should().BeSuccess().Which.Should().Equal(path1Array),
                                                         error => path2.Should().BeSome().Which.Should().Be(error)),
                        () => path2.Should().BeNone());
        });
    }
    [Fact]
    public async Task IterTask_calls_action_for_each_element()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();
            var addedItems = ImmutableArray.Create<int>();

            await asyncEnumerable.IterTask(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, CancellationToken);

            var expected = await asyncEnumerable.ToArrayAsync(CancellationToken);
            addedItems.Should().BeEquivalentTo(expected);
        });
    }

    [Fact]
    public async Task IterTask_with_cancellation_token_respects_cancellation()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 2
                  from cancelAfter in Gen.Int[1, array.Length - 1]
                  select (array.ToAsyncEnumerable(), cancelAfter);

        await gen.SampleAsync(async x =>
        {
            var (array, cancelAfter) = x;
            using var CancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Func<Task> f = async () => await array.IterTask(async x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    await CancellationTokenSource.CancelAsync();
                }
            }, CancellationTokenSource.Token);

            await f.Should().ThrowAsync<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public async Task IterTaskParallel_calls_action_for_each_element()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Generator.GenerateOption(Gen.Int[1, array.Length + 1])
                  select (array.ToAsyncEnumerable(), maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            await array.IterTaskParallel(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, maxDegreesOfParallelism, CancellationToken);

            var expected = await array.ToArrayAsync(CancellationToken);
            addedItems.Should().BeEquivalentTo(expected);
        });
    }

    [Fact]
    public async Task IterTaskParallel_with_max_degree_of_parallelism_limits_parallelism()
    {
        var gen = from array in Gen.Int.Array
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1]
                  select (array.ToAsyncEnumerable(), maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var iterations = 0;

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                await array.IterTaskParallel(async _ =>
                {
                    iterations++;
                    await ValueTask.CompletedTask;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken);
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031 // Do not catch general exception types

            iterations.Should().BeLessThanOrEqualTo(maxDegreesOfParallelism);
        });
    }

    [Fact]
    public async Task Tap_executes_side_effect_and_preserves_original_async_enumerable()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var tapCount = 0;

            var result = await array.ToAsyncEnumerable()
                                    .Tap(_ => tapCount++)
                                    .ToArrayAsync(CancellationToken);

            result.Should().BeEquivalentTo(array);
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public async Task Tap_is_lazily_evaluated_for_async_enumerable()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var tapCount = 0;

            // Create the tapped async enumerable but don't enumerate it yet
            var tappedAsyncEnumerable = array.ToAsyncEnumerable()
                                             .Tap(_ => tapCount++);

            // Side effect should not have executed yet
            tapCount.Should().Be(0);

            // Now enumerate it
            await tappedAsyncEnumerable.ToArrayAsync(CancellationToken);

            // Side effect should have executed for each element
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public async Task TapTask_executes_side_effect_and_preserves_original_async_enumerable()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var tapCount = 0;

            var result = await array.ToAsyncEnumerable()
                                    .TapTask(async _ =>
                                    {
                                        tapCount++;
                                        await ValueTask.CompletedTask;
                                    })
                                    .ToArrayAsync(CancellationToken);

            result.Should().BeEquivalentTo(array);
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public async Task TapTask_is_lazily_evaluated_for_async_enumerable()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var tapCount = 0;

            // Create the tapped async enumerable but don't enumerate it yet
            var tappedAsyncEnumerable = array.ToAsyncEnumerable()
                                             .TapTask(async _ =>
                                             {
                                                 tapCount++;
                                                 await ValueTask.CompletedTask;
                                             });

            // Side effect should not have executed yet
            tapCount.Should().Be(0);

            // Now enumerate it
            await tappedAsyncEnumerable.ToArrayAsync(CancellationToken);

            // Side effect should have executed for each element
            tapCount.Should().Be(array.Length);
        });
    }

    [Fact]
    public async Task Unzip_unzips_items()
    {
        var gen = from firstArray in Gen.Int.Array
                  from secondArray in Gen.String.Array[firstArray.Length]
                  select (firstArray, secondArray);

        await gen.SampleAsync(async x =>
        {
            var (firstArray, secondArray) = x;
            var zippedArray = firstArray.Zip(secondArray)
                                        .ToAsyncEnumerable();

            var (firstResult, secondResult) = await zippedArray.Unzip(CancellationToken);

            firstResult.Should().BeEquivalentTo(firstArray);
            secondResult.Should().BeEquivalentTo(secondArray);
        });
    }
}

public class DictionaryExtensionsTests
{
    [Fact]
    public void Find_with_missing_key_returns_none()
    {
        var gen = from kvp in Gen.Select(Gen.Int, Gen.String).Array
                  let dictionary = kvp.DistinctBy(x => x.Item1).ToImmutableDictionary(x => x.Item1, x => x.Item2)
                  from key in Gen.Int
                  where dictionary.ContainsKey(key) is false
                  select (dictionary, key);

        gen.Sample(x =>
        {
            var (dictionary, key) = x;

            var result = dictionary.Find(key);

            result.Should().BeNone();
        });
    }

    [Fact]
    public void Find_with_existing_key_returns_some_with_value()
    {
        var gen = from kvp in Gen.Select(Gen.Int, Gen.String).Array
                  let dictionary = kvp.DistinctBy(x => x.Item1).ToImmutableDictionary(x => x.Item1, x => x.Item2)
                  from key in Gen.Int
                  from value in Gen.String
                  select (dictionary.SetItem(key, value), key, value);

        gen.Sample(x =>
        {
            var (dictionary, key, value) = x;

            var result = dictionary.Find(key);

            result.Should().BeSome().Which.Should().Be(value);
        });
    }
}

file static class Selectors
{
    public static Func<int, Result<int>> MixedResult { get; } =
        i => i % 2 == 0
            ? i + 2
            : Error.From($"Odd number: {i}");

    public static Func<int, ValueTask<Result<int>>> MixedResultAsync { get; } =
        i => ValueTask.FromResult(MixedResult(i));

    public static Func<int, ValueTask<Result<int>>> ResultIdentityAsync { get; } =
        i => ValueTask.FromResult(Result.Success(i));

    public static Func<int, Option<int>> MixedOption { get; } =
        i => i % 3 == 0
            ? i - 1
            : Option.None;

    public static Func<int, ValueTask<Option<int>>> MixedOptionAsync { get; } =
        i => ValueTask.FromResult(MixedOption(i));

    public static Func<int, ValueTask<Option<int>>> OptionIdentityAsync { get; } =
        i => ValueTask.FromResult(Option.Some(i));
}

file static class Extensions
{
    public static Result<T> ToResult<T>(this Option<T> option, Func<Error> errorIfNone) =>
        option.Match(Result.Success, () => errorIfNone());
}