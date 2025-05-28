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
    public void Choose_with_all_somes_has_same_count_as_original()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var result = array.Choose(x => Option.Some(x * 2));

            result.Should().HaveSameCount(array);
        });
    }

    [Fact]
    public void Choose_with_all_nones_returns_an_empty_sequence()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var result = array.Choose(_ => Option<int>.None());

            result.Should().BeEmpty();
        });
    }

    [Fact]
    public void Choose_with_mixed_some_and_none_returns_only_some_values()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            Option<int> chooser(int x) => x % 2 == 0 ? Option.Some(x) : Option.None;

            var result = array.Choose(chooser);

            var expected = array.Where(x => x % 2 == 0);
            result.Should().BeEquivalentTo(expected);
        });
    }

    [Fact]
    public void Iter_calls_action_for_each_element()
    {
        var gen = from array in Gen.Int.Array
                  let maxDegreesOfParallelismGen = Gen.OneOf(Gen.Const(-1), Gen.Int[1, array.Length + 1])
                  from maxDegreesOfParallelism in Generator.GenerateOption(maxDegreesOfParallelismGen)
                  select (array, maxDegreesOfParallelism);

        gen.Sample(x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            array.Iter(x => ImmutableInterlocked.Update(ref addedItems, items => items.Add(x)),
                       maxDegreesOfParallelism,
                       CancellationToken.None);

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
            }, maxDegreeOfParallelism: 1, cancellationTokenSource.Token);

            action.Should().Throw<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public void Iter_with_max_degree_of_parallelism_limits_parallelism()
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
                array.Iter(_ =>
                {
                    iterations++;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken.None);
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
        var gen = from array in Gen.Int.Array
                  let maxDegreesOfParallelismGen = Gen.OneOf(Gen.Const(-1), Gen.Int[1, array.Length + 1])
                  from maxDegreesOfParallelism in Generator.GenerateOption(maxDegreesOfParallelismGen)
                  select (array, maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            await array.IterTask(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, maxDegreeOfParallelism: Option.None, CancellationToken.None);

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
            }, maxDegreeOfParallelism: 1, cancellationTokenSource.Token);

            await f.Should().ThrowAsync<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public async Task IterTask_with_max_degree_of_parallelism_limits_parallelism()
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
                await array.IterTask(async _ =>
                {
                    iterations++;
                    await ValueTask.CompletedTask;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken.None);
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031 // Do not catch general exception types

            iterations.Should().BeLessThanOrEqualTo(maxDegreesOfParallelism);
        });
    }
}

public class AsyncEnumerableExtensionsTests
{
    [Fact]
    public async Task Head_with_empty_enumerable_returns_none()
    {
        var emptyEnumerable = AsyncEnumerable.Empty<int>();

        var result = await emptyEnumerable.Head(CancellationToken.None);

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

            var result = await array.Head(CancellationToken.None);

            result.Should().BeSome().Which.Should().Be(first);
        });
    }

    [Fact]
    public async Task Choose_with_all_somes_has_same_count_as_original()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async array =>
        {
            var result = array.Choose(x => Option.Some(x * 2));

            var actual = await result.ToArrayAsync();
            var expected = await result.ToArrayAsync();
            actual.Should().HaveSameCount(expected);
        });
    }

    [Fact]
    public async Task Choose_with_all_nones_returns_an_empty_sequence()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async array =>
        {
            var result = array.Choose(_ => Option<int>.None());

            var resultArray = await result.ToArrayAsync();
            resultArray.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Choose_with_mixed_some_and_none_returns_only_some_values()
    {
        var gen = from array in Gen.Int.Array
                  select array.ToAsyncEnumerable();

        await gen.SampleAsync(async array =>
        {
            Option<int> chooser(int x) => x % 2 == 0 ? Option.Some(x) : Option.None;

            var result = array.Choose(chooser);

            var expected = await array.Where(x => x % 2 == 0).ToArrayAsync();
            var actual = await result.ToArrayAsync();
            actual.Should().BeEquivalentTo(expected);
        });
    }

    [Fact]
    public async Task Iter_calls_action_for_each_element()
    {
        var gen = from array in Gen.Int.Array
                  let maxDegreesOfParallelismGen = Gen.OneOf(Gen.Const(-1), Gen.Int[1, array.Length + 1])
                  from maxDegreesOfParallelism in Generator.GenerateOption(maxDegreesOfParallelismGen)
                  select (array.ToAsyncEnumerable(), maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            await array.Iter(x => ImmutableInterlocked.Update(ref addedItems, items => items.Add(x)),
                             maxDegreesOfParallelism,
                             CancellationToken.None);

            var expected = await array.ToArrayAsync();
            addedItems.Should().BeEquivalentTo(expected);
        });
    }

    [Fact]
    public async Task Iter_with_cancellation_token_respects_cancellation()
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

            Func<Task> f = async () => await array.Iter(x =>
            {
                callCount++;
                if (callCount > cancelAfter)
                {
                    cancellationTokenSource.Cancel();
                }
            }, maxDegreeOfParallelism: 1, cancellationTokenSource.Token);

            await f.Should().ThrowAsync<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public async Task Iter_with_max_degree_of_parallelism_limits_parallelism()
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
                await array.Iter(_ =>
                {
                    iterations++;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken.None);
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
        var gen = from array in Gen.Int.Array
                  let maxDegreesOfParallelismGen = Gen.OneOf(Gen.Const(-1), Gen.Int[1, array.Length + 1])
                  from maxDegreesOfParallelism in Generator.GenerateOption(maxDegreesOfParallelismGen)
                  select (array.ToAsyncEnumerable(), maxDegreesOfParallelism);

        await gen.SampleAsync(async x =>
        {
            var (array, maxDegreesOfParallelism) = x;
            var addedItems = ImmutableArray.Create<int>();

            await array.IterTask(async x =>
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }, maxDegreeOfParallelism: Option.None, CancellationToken.None);

            var expected = await array.ToArrayAsync();
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
            }, maxDegreeOfParallelism: 1, cancellationTokenSource.Token);

            await f.Should().ThrowAsync<OperationCanceledException>();
            callCount.Should().BeGreaterThanOrEqualTo(cancelAfter);
        });
    }

    [Fact]
    public async Task IterTask_with_max_degree_of_parallelism_limits_parallelism()
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
                await array.IterTask(async _ =>
                {
                    iterations++;
                    await ValueTask.CompletedTask;
                    throw new InvalidOperationException();
                }, maxDegreesOfParallelism, CancellationToken.None);
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031 // Do not catch general exception types

            iterations.Should().BeLessThanOrEqualTo(maxDegreesOfParallelism);
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