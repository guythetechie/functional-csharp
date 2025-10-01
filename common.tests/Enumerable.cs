using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Collections.Generic;
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
    public void Choose_satisfies_monad_left_identity()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            var result = array.Choose(Option.Some);

            result.Should().Equal(array);
        });
    }

    [Fact]
    public void Choose_satisfies_monad_right_identity()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f) = tuple;

            // Act
            var path1 = array.Choose(f);

            var path2 = array.Choose(f)
                             .Choose(Option.Some);

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public void Choose_satisfies_monad_associativity()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  from g in Generator.StringToIntOption
                  select (array, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;

            // Act
            var path1 = array.Choose(f)
                             .Choose(g);

            var path2 = array.Choose(x => f(x).Bind(g));

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_handles_asynchronous_choose()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;

            // Act
            var path1 = array.Choose(f);

            var path2 = await array.Choose(x => ValueTask.FromResult(f(x)))
                                   .ToArrayAsync(CancellationToken);

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public void Pick_returns_first_some_or_none()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f) = tuple;

            // Act
            var result = array.Pick(f);

            // Assert
            var filteredArray = array.Select(f).Where(x => x.IsSome);
            result.Match(some => filteredArray.First().Should().BeSome().Which.Should().Be(some),
                         () => filteredArray.Should().BeEmpty());
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
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringResult
                  select (array, f);

        gen.Sample(tuple =>
        {
            var (array, f) = tuple;

            // Act
            var path1 = array.Traverse(f, CancellationToken)
                             .ToOption();

            var path2 = array.Traverse(x => f(x).ToOption(), CancellationToken);

            // Assert
            path1.Match(path1Array => path2.Should().BeSome().Which.Should().Equal(path1Array),
                        () => path2.Should().BeNone());
        });
    }

    [Fact]
    public void Traverse_with_result_passes_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringResult
                  from g in Generator.StringToIntOption
                  select (array, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;

            // Act
            var path1 = array.Traverse(x => f(x).Map(g), CancellationToken)
                             .Map(optionArray => optionArray.Traverse(x => x, CancellationToken));

            var path2 = array.Traverse(f, CancellationToken)
                             .Map(array => array.Traverse(g, CancellationToken));

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
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f) = tuple;

            // Act
            var path1 = array.Traverse(f, CancellationToken)
                             .ToResult(() => Error.From("test error"));

            var path2 = array.Traverse(x => f(x).ToResult(() => Error.From("test error")), CancellationToken);

            // Assert
            path1.Match(path1Array => path2.Should().BeSuccess().Which.Should().Equal(path1Array),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public void Traverse_with_option_passes_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  from g in Generator.StringToIntResult
                  select (array, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;

            // Act
            var path1 = array.Traverse(x => f(x).Map(g), CancellationToken)
                             .Map(resultArray => resultArray.Traverse(x => x, CancellationToken));

            var path2 = array.Traverse(f, CancellationToken)
                             .Map(array => array.Traverse(g, CancellationToken));

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
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1].ToOption()
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
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1].ToOption()
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
    public void Tap_satisfies_identity_law()
    {
        var gen = Gen.Int.Array;

        gen.Sample(array =>
        {
            // Act
            var result = array.Tap(_ => { });

            // Assert
            result.Should().Equal(array);
        });
    }

    [Fact]
    public void Tap_satisfies_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToString
                  from g in Generator.IntToString
                  select (array, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;
            var path1Sum = string.Empty;
            var path2Sum = string.Empty;

            // Act
            var path1Array = array.Tap(x => path1Sum += f(x))
                                  .Tap(x => path1Sum += g(x));

            var path2Array = array.Tap(x =>
                                    {
                                        path2Sum += f(x);
                                        path2Sum += g(x);
                                    })
                                  .ToArray();

            // Assert
            path1Array.Should().Equal(path2Array);
            path1Sum.Should().Be(path2Sum);
        });
    }

    [Fact]
    public void Tap_is_lazy()
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
    public void Unzip_reverses_zip()
    {
        var gen = from first in Gen.Int.Array
                  from second in Gen.String.Array[first.Length]
                  select (first, second);

        gen.Sample(tuple =>
        {
            // Arrange
            var (first, second) = tuple;
            var zipped = first.Zip(second);

            // Act
            var (unzipped1, unzipped2) = zipped.Unzip();

            // Assert
            unzipped1.Should().Equal(first);
            unzipped2.Should().Equal(second);
        });
    }

    [Fact]
    public void Zip_reverses_unzip()
    {
        var gen = from first in Gen.Int.Array
                  from second in Gen.String.Array[first.Length]
                  select first.Zip(second);

        gen.Sample(pairs =>
        {
            // Act
            var (first, second) = pairs.Unzip();
            var rezipped = first.Zip(second);

            // Assert
            rezipped.Should().Equal(pairs);
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
    public async Task Head_works_with_infinite_sequences()
    {
        var enumerable = Enumerable.Range(1, int.MaxValue)
                                   .ToAsyncEnumerable();

        var result = await enumerable.Head(CancellationToken);

        result.Should().BeSome();
    }

    [Fact]
    public async Task Choose_satisfies_monad_left_identity()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            var source = array.ToAsyncEnumerable();

            var result = await source.Choose(Option.Some)
                                     .ToArrayAsync(CancellationToken);

            result.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Choose_satisfies_monad_right_identity()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1 = await source.Choose(f)
                                    .ToArrayAsync(CancellationToken);

            var path2 = await source.Choose(f)
                                    .Choose(Option.Some)
                                    .ToArrayAsync(CancellationToken);

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public async Task Choose_satisfies_monad_associativity()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  from g in Generator.StringToIntOption
                  select (array, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1 = await source.Choose(f)
                                    .Choose(g)
                                    .ToArrayAsync(CancellationToken);

            var path2 = await source.Choose(x => f(x).Bind(g))
                                    .ToArrayAsync(CancellationToken);

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public async Task Choose_with_async_selector_handles_asynchronous_choose()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1 = await source.Choose(f)
                                    .ToArrayAsync(CancellationToken);

            var path2 = await array.Choose(x => ValueTask.FromResult(f(x)))
                                   .ToArrayAsync(CancellationToken);

            // Assert
            path1.Should().Equal(path2);
        });
    }

    [Fact]
    public async Task Pick_returns_first_some_or_none()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOption
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var result = await source.Pick(f, CancellationToken);

            // Assert
            var filteredArray = array.Select(f).Where(x => x.IsSome);
            result.Match(some => filteredArray.First().Should().BeSome().Which.Should().Be(some),
                         () => filteredArray.Should().BeEmpty());
        });
    }

    [Fact]
    public async Task Traverse_with_all_success_returns_success_with_expected_array()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            // Arrange
            var asyncEnumerable = array.ToAsyncEnumerable();
            async ValueTask<Result<int>> f(int x)
            {
                await ValueTask.CompletedTask;
                return Result.Success(x);
            }

            // Act
            var result = await asyncEnumerable.Traverse(f, CancellationToken);

            // Assert
            result.Should().BeSuccess().Which.Should().BeEquivalentTo(array);
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_identity_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            // Arrange
            var source = array.ToAsyncEnumerable();
            async ValueTask<Result<int>> f(int x)
            {
                await ValueTask.CompletedTask;
                return Result.Success(x);
            }

            // Act
            var result = await source.Traverse(f, CancellationToken);

            // Assert
            result.Should().BeSuccess().Which.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_naturality_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringResultTask
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1 = (await source.Traverse(f, CancellationToken)).ToOption();

            var path2 = await source.Traverse(async x => (await f(x)).ToOption(), CancellationToken);

            // Assert
            path1.Match(path1Array => path2.Should().BeSome().Which.Should().Equal(path1Array),
                        () => path2.Should().BeNone());
        });
    }

    [Fact]
    public async Task Traverse_with_result_passes_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringResultTask
                  from g in Generator.StringToIntOptionTask
                  select (array, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1FirstPass = await source.Traverse(async x =>
                                    {
                                        var result = await f(x);
                                        return await result.MapTask(g);
                                    }, CancellationToken);

            var path1 = await path1FirstPass.MapTask(optionArray => ValueTask.FromResult(optionArray.Traverse(x => x, CancellationToken)));

            var path2FirstPass = await source.Traverse(f, CancellationToken);
            var path2 = await path2FirstPass.MapTask(values => values.ToAsyncEnumerable()
                                                                     .Traverse(g, CancellationToken));

            // Assert
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
            // Arrange
            var source = array.ToAsyncEnumerable();
            var f = (int x) => ValueTask.FromResult(Option.Some(x));

            // Act
            var result = await source.Traverse(f, CancellationToken);

            // Assert
            result.Should().BeSome().Which.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Traverse_with_option_passes_naturality_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOptionTask
                  select (array, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1 = (await source.Traverse(f, CancellationToken)).ToResult(() => Error.From("test error"));

            var path2 = await source.Traverse(async x => (await f(x)).ToResult(() => Error.From("test error")), CancellationToken);

            path1.Match(path1Array => path2.Should().BeSuccess().Which.Should().Equal(path1Array),
                        error => path2.Should().BeError().Which.Should().Be(error));
        });
    }

    [Fact]
    public async Task Traverse_with_option_passes_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToStringOptionTask
                  from g in Generator.StringToIntResultTask
                  select (array, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;
            var source = array.ToAsyncEnumerable();

            // Act
            var path1FirstPass = await source.Traverse(async x =>
                                    {
                                        var result = await f(x);
                                        return await result.MapTask(g);
                                    }, CancellationToken);

            var path1 = await path1FirstPass.MapTask(optionArray => ValueTask.FromResult(optionArray.Traverse(x => x, CancellationToken)));

            var path2FirstPass = await source.Traverse(f, CancellationToken);
            var path2 = await path2FirstPass.MapTask(values => values.ToAsyncEnumerable()
                                                                     .Traverse(g, CancellationToken));

            // Assert
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
            // Arrange
            var source = array.ToAsyncEnumerable();

            var addedItems = ImmutableArray.Create<int>();
            async ValueTask f(int x)
            {
                ImmutableInterlocked.Update(ref addedItems, items => items.Add(x));
                await ValueTask.CompletedTask;
            }
            ;

            // Act
            await source.IterTask(f, CancellationToken);

            // Assert
            addedItems.Should().BeEquivalentTo(array);
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
                  from maxDegreesOfParallelism in Gen.Int[1, array.Length + 1].ToOption()
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
    public async Task Tap_satisfies_identity_law()
    {
        var gen = Gen.Int.Array;

        await gen.SampleAsync(async array =>
        {
            // Arrange
            var source = array.ToAsyncEnumerable();

            // Act
            var result = await source.Tap(_ => { })
                                     .ToArrayAsync(CancellationToken);

            // Assert
            result.Should().Equal(array);
        });
    }

    [Fact]
    public async Task Tap_satisfies_composition_law()
    {
        var gen = from array in Gen.Int.Array
                  from f in Generator.IntToString
                  from g in Generator.IntToString
                  select (array, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f, g) = tuple;

            var source = array.ToAsyncEnumerable();
            var path1Sum = string.Empty;
            var path2Sum = string.Empty;

            // Act
            var path1Array = await source.Tap(x => path1Sum += f(x))
                                         .Tap(x => path1Sum += g(x))
                                         .ToArrayAsync(CancellationToken);

            var path2Array = await source.Tap(x =>
                                              {
                                                  path2Sum += f(x);
                                                  path2Sum += g(x);
                                              })
                                         .ToArrayAsync(CancellationToken);

            // Assert
            path1Array.Should().Equal(path2Array);
            path1Sum.Should().Be(path2Sum);
        });
    }

    [Fact]
    public async Task Tap_is_lazy()
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
    public async Task TapTask_with_sync_action_is_equivalent_to_Tap()
    {
        var gen = from array in Gen.Int.Array
                  from f1 in Generator.IntToString
                  select (array, f1);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (array, f1) = tuple;

            var source = array.ToAsyncEnumerable();
            var f = (int x, List<string> accumulator) => accumulator.Add(f1(x));

            var tapAccumulator = new List<string>();
            var tapTaskAccumulator = new List<string>();

            // Act
            var tapResult = await source.Tap(x => f(x, tapAccumulator))
                                        .ToArrayAsync(CancellationToken);

            var tapTaskResult = await source.TapTask(async x =>
                                                    {
                                                        f(x, tapTaskAccumulator);
                                                        await ValueTask.CompletedTask;
                                                    })
                                           .ToArrayAsync(CancellationToken);

            // Assert
            tapResult.Should().Equal(tapTaskResult);
            tapAccumulator.Should().Equal(tapTaskAccumulator);
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

file static class Extensions
{
    public static Result<T> ToResult<T>(this Option<T> option, Func<Error> errorIfNone) =>
        option.Match(Result.Success, () => errorIfNone());
}