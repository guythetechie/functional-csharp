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
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            Option<string> selector(int x) => Generator.GenerateOption(Gen.String).Single();

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
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            Option<string> selector(int x) => Option.Some(Gen.String.Single());

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
            Option<string> selector(int x) => Option.None;

            var result = array.Choose(selector);

            result.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_result_length_never_exceeds_source()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            ValueTask<Option<string>> selector(int x)
            {
                var optionGenerator = Generator.GenerateOption(Gen.String);
                var option = optionGenerator.Single();
                return ValueTask.FromResult(option);
            }

            var result = array.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
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

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            resultArray.Select(x => x.Index).Should().BeInAscendingOrder();
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_all_somes_has_same_count_as_original()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            ValueTask<Option<string>> selector(int x)
            {
                var stringValue = Gen.String.Single();
                var option = Option.Some(stringValue);
                return ValueTask.FromResult(option);
            }

            var result = array.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            resultArray.Should().HaveSameCount(array);
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_all_nones_returns_an_empty_sequence()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            ValueTask<Option<string>> selector(int x) => ValueTask.FromResult(Option<string>.None());

            var result = array.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            resultArray.Should().BeEmpty();
        });
    }

    [Fact]
    public void Pick_with_no_somes_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  select array;

        gen.Sample(array =>
        {
            var result = array.Pick(x => Option<int>.None());

            result.Should().BeNone();
        });
    }

    [Fact]
    public void Pick_with_some_returns_first_some_value()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 0
                  from value in Gen.OneOfConst(array)
                  select (value, array);

        gen.Sample(x =>
        {
            (var value, var array) = x;
            Option<int> chooser(int x) => x == value ? Option.Some(x) : Option.None;

            var result = array.Pick(chooser);

            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Traverse_with_all_success_returns_success_with_expected_array()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var f = (int x) => Result.Success(x);
            var result = array.Traverse(f, TestContext.Current.CancellationToken);
            result.Should().BeSuccess().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public void Traverse_with_errors_returns_error_with_combined_messages()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 2
                  from errorCount in Gen.Int[1, array.Length]
                  from errorIndices in Gen.Shuffle(Enumerable.Range(0, array.Length - 1).ToList(), errorCount)
                  select (array, errorIndices.ToImmutableHashSet());

        gen.Sample(x =>
        {
            var (array, errorIndices) = x;
            var indexedArray = array.Select((value, index) => (value, index));

            var f = ((int value, int index) x) =>
                errorIndices.Contains(x.index)
                    ? Result.Error<int>(Error.From($"error:{x.index}"))
                    : Result.Success(x.value);

            var result = indexedArray.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeError().Which.Messages.Should().HaveSameCount(errorIndices);
        });
    }

    [Fact]
    public void Traverse_with_all_some_returns_some_with_expected_array()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var f = (int x) => Option.Some(x);
            var result = array.Traverse(f, TestContext.Current.CancellationToken);
            result.Should().BeSome().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public void Traverse_with_none_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  from oddNumber in Gen.Int.Where(x => x % 2 != 0)
                  let arrayWithOdd = array.Append(oddNumber).ToArray()
                  from shuffledArray in Gen.Shuffle(arrayWithOdd)
                  select shuffledArray;

        gen.Sample(array =>
        {
            var f = (int x) => x % 2 == 0 ? Option.Some(x) : Option.None;

            var result = array.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeNone();
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
                       TestContext.Current.CancellationToken);

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
            using var cancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Action action = () => array.Iter(x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    cancellationTokenSource.Cancel();
                }
            }, cancellationTokenSource.Token);

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
                               TestContext.Current.CancellationToken);

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
                }, maxDegreesOfParallelism, TestContext.Current.CancellationToken);
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
            }, TestContext.Current.CancellationToken);

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
            using var cancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Func<Task> f = async () => await array.IterTask(async x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    await cancellationTokenSource.CancelAsync();
                }
            }, cancellationTokenSource.Token);

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
            }, maxDegreesOfParallelism, TestContext.Current.CancellationToken);

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
                }, maxDegreesOfParallelism, TestContext.Current.CancellationToken);
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
    [Fact]
    public async Task Head_with_empty_enumerable_returns_none()
    {
        var emptyEnumerable = AsyncEnumerable.Empty<int>();

        var result = await emptyEnumerable.Head(TestContext.Current.CancellationToken);

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

            var result = await array.Head(TestContext.Current.CancellationToken);

            result.Should().BeSome().Which.Should().Be(first);
        });
    }

    [Fact]
    public async Task Choose_result_length_never_exceeds_source()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            Option<string> selector(int x) => Generator.GenerateOption(Gen.String).Single();

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            var sourceLength = await source.CountAsync(TestContext.Current.CancellationToken);
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
                                            .ToArrayAsync(TestContext.Current.CancellationToken);

            resultIndexes.Should().BeInAscendingOrder();
        });
    }

    [Fact]
    public async Task Choose_with_all_somes_has_same_count_as_original()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            Option<string> selector(int x) => Option.Some(Gen.String.Single());

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            var sourceArray = await source.ToArrayAsync(TestContext.Current.CancellationToken);
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
            Option<string> selector(int x) => Option.None;

            var result = source.Choose(selector);

            var resultArray = await result.ToArrayAsync(TestContext.Current.CancellationToken);
            resultArray.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Pick_with_no_somes_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async array =>
        {
            var result = await array.Pick(x => Option<int>.None(), TestContext.Current.CancellationToken);

            result.Should().BeNone();
        });
    }

    [Fact]
    public async Task Pick_with_some_returns_first_some_value()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 0
                  from value in Gen.OneOfConst(array)
                  select (value, array.ToAsyncEnumerable());

        await gen.SampleAsync(async x =>
        {
            (var value, var array) = x;
            Option<int> chooser(int x) => x == value ? Option.Some(x) : Option.None;

            var result = await array.Pick(chooser, TestContext.Current.CancellationToken);

            result.Should().BeSome().Which.Should().Be(value);
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

            var result = await asyncEnumerable.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeSuccess().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task Traverse_with_errors_returns_error_with_combined_messages()
    {
        var gen = from array in Gen.Int.Array
                  where array.Length > 2
                  from errorCount in Gen.Int[1, array.Length]
                  from errorIndices in Gen.Shuffle(Enumerable.Range(0, array.Length - 1).ToList(), errorCount)
                  select (array, errorIndices.ToImmutableHashSet());

        await gen.SampleAsync(async x =>
        {
            var (array, errorIndices) = x;
            var indexedArray = array.Select((value, index) => (value, index));
            var asyncEnumerable = indexedArray.ToAsyncEnumerable();

            var f = ((int value, int index) x) =>
                ValueTask.FromResult(
                    errorIndices.Contains(x.index)
                        ? Result.Error<int>(Error.From($"error:{x.index}"))
                        : Result.Success(x.value));

            var result = await asyncEnumerable.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeError().Which.Messages.Should().HaveSameCount(errorIndices);
        });
    }

    [Fact]
    public async Task Traverse_with_all_some_returns_some_with_expected_array()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var asyncEnumerable = array.ToAsyncEnumerable();
            var f = (int x) => ValueTask.FromResult(Option.Some(x));

            var result = await asyncEnumerable.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeSome().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task Traverse_with_none_returns_none()
    {
        var gen = from array in Gen.Int.Array
                  from oddNumber in Gen.Int.Where(x => x % 2 != 0)
                  let arrayWithOdd = array.Append(oddNumber).ToArray()
                  from shuffledArray in Gen.Shuffle(arrayWithOdd)
                  select shuffledArray.ToAsyncEnumerable();

        await gen.SampleAsync(async asyncEnumerable =>
        {
            var f = (int x) => ValueTask.FromResult(x % 2 == 0 ? Option.Some(x) : Option.None);

            var result = await asyncEnumerable.Traverse(f, TestContext.Current.CancellationToken);

            result.Should().BeNone();
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
            }, TestContext.Current.CancellationToken);

            var expected = await asyncEnumerable.ToArrayAsync(TestContext.Current.CancellationToken);
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
            using var cancellationTokenSource = new CancellationTokenSource();
            var callCount = 0;

            Func<Task> f = async () => await array.IterTask(async x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    await cancellationTokenSource.CancelAsync();
                }
            }, cancellationTokenSource.Token);

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
            }, maxDegreesOfParallelism, TestContext.Current.CancellationToken);

            var expected = await array.ToArrayAsync(TestContext.Current.CancellationToken);
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
                }, maxDegreesOfParallelism, TestContext.Current.CancellationToken);
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
                                    .ToArrayAsync(TestContext.Current.CancellationToken);

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
            await tappedAsyncEnumerable.ToArrayAsync(TestContext.Current.CancellationToken);

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
                                    .ToArrayAsync(TestContext.Current.CancellationToken);

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
            await tappedAsyncEnumerable.ToArrayAsync(TestContext.Current.CancellationToken);

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

            var (firstResult, secondResult) = await zippedArray.Unzip(TestContext.Current.CancellationToken);

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