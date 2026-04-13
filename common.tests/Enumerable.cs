using CsCheck;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Enums;

namespace common.tests;

file static class Common
{
    public static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    public static Result<T> ToResult<T>(this Option<T> option) =>
        option.Match(Result.Success,
                     () => Result.Error<T>(Error.From("Option is None.")));
}

public class Enumerable_Head_Tests
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = Enumerable.Empty<object>();

        // Act
        var result = source.Head();

        // Assert
        await Assert.That(result)
                    .IsNone();
    }

    [Test]
    public async Task Non_empty_sequence_returns_first_item()
    {
        var gen = from first in Generator.Object
                  from tail in Generator.Object.Array
                  let source = tail.Prepend(first)
                  select (first, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, source) = tuple;

            // Act
            var result = source.Head();

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(first);
        });
    }
}

public class Enumerable_Head_WithPredicate_Tests
{
    [Test]
    public async Task Is_equivalent_to_Where_then_Head()
    {
        var gen = from source in Generator.Object.Array
                  from predicate in Generator.ObjectPredicate
                  select (source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var result1 = source.Head(predicate);

            var result2 = source.Where(predicate)
                                .Head();

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Enumerable_SingleOrNone_Tests
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = Enumerable.Empty<object>();

        // Act
        var result = source.SingleOrNone();

        // Assert
        await Assert.That(result)
                    .IsNone();
    }

    [Test]
    public async Task Single_item_returns_some()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var source = Enumerable.Repeat(value, 1);

            // Act
            var result = source.SingleOrNone();

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Multiple_items_returns_none()
    {
        var gen = from source in Generator.Object.Array
                  where source.Length > 1
                  select source;

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.SingleOrNone();

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }
}

public class Enumerable_Choose_Tests()
{
    [Test]
    public async Task Satisfies_left_identity()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.Choose(Option.Some);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_right_identity()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToOption
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = source.Choose(f)
                                .Choose(Option.Some);

            var result2 = source.Choose(f);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_associativity()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToOption
                  from g in Generator.ObjectToOption
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Choose(f)
                                .Choose(g);

            var result2 = source.Choose(x => f(x).Bind(g));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Choose_WithAsyncSelector_Tests()
{
    [Test]
    public async Task Satisfies_left_identity()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            static async ValueTask<Option<object>> f(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result = source.Choose(f);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_right_identity()
    {
        var gen = from source in Generator.Object.Array
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            static async ValueTask<Option<object>> some(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result1 = source.Choose(f).Choose(some);

            var result2 = source.Choose(f);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_associativity()
    {
        var gen = from source in Generator.Object.Array
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in
                      from g in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return g(x);
                      })
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Choose(f).Choose(g);

            var result2 = source.Choose(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Pick_Tests
{
    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToOption
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = source.Pick(f);

            var result2 = source.Choose(f)
                                .Head();

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Enumerable_Traverse_WithResult_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.Traverse(Result.Success, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToResult
                  from g in Generator.ObjectToOption
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
                                .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var result2 = source.Traverse(f, Common.CancellationToken)
                                .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_naturality()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToResult
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = source.Traverse(f, Common.CancellationToken)
                                .ToOption();

            var result2 = source.Traverse(x => f(x).ToOption(), Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }
}

public class Enumerable_Traverse_WithOption_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.Traverse(Option.Some, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToOption
                  from g in Generator.ObjectToResult
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
                                .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var result2 = source.Traverse(f, Common.CancellationToken)
                                .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_naturality()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToOption
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = source.Traverse(f, Common.CancellationToken)
                                .ToResult();

            var result2 = source.Traverse(x => f(x).ToResult(), Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }
}

public class Enumerable_Iter_Tests()
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var processedItems = new ConcurrentQueue<object>();
            var f = (object obj) => processedItems.Enqueue(obj);

            // Act
            source.Iter(f, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Throws_on_cancellation()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancellationToken = cts.Token;

            var f = (object obj) => { };

            // Act
            var action = () => source.Iter(f, cancellationToken);

            // Assert
            await Assert.That(action)
                        .Throws<OperationCanceledException>();
        });
    }
}

public class Enumerable_IterTask_Tests()
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var processedItems = new ConcurrentQueue<object>();
            async ValueTask f(object obj)
            {
                await Task.Yield();
                processedItems.Enqueue(obj);
            }

            // Act
            await source.IterTask(f, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Throws_on_cancellation()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancellationToken = cts.Token;

            static async ValueTask f(object obj) => await Task.Yield();

            // Act
            var action = async () => await source.IterTask(f, cancellationToken);

            // Assert
            await Assert.That(action)
                        .Throws<OperationCanceledException>();
        });
    }
}

public class Enumerable_IterParallel_Tests()
{
    [Test]
    public async Task Acts_on_each_item()
    {
        var gen = from source in Generator.Object.Array
                  from maxDegreeOfParallelism in
                    Gen.Int[1, source.Length > 0 ? source.Length : 1]
                       .OptionOf()
                  select (source, maxDegreeOfParallelism);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, maxDegreeOfParallelism) = tuple;

            var processedItems = new ConcurrentQueue<object>();
            var f = (object obj) => processedItems.Enqueue(obj);

            // Act
            source.IterParallel(f, maxDegreeOfParallelism, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Any);
        });
    }

    [Test]
    public async Task Respects_maximum_degree_of_parallelism()
    {
        var gen = from source in Generator.Object.Array
                  from maxDegreeOfParallelism in
                    Gen.Int[1, source.Length > 0 ? source.Length : 1]
                       .OptionOf()
                  select (source, maxDegreeOfParallelism);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, maxDegreeOfParallelism) = tuple;

            var locked = new object();
            var running = 0;
            var maxObservedDegreeOfParallelism = 0;

            var f = (object obj) =>
            {
                lock (locked)
                {
                    running++;
                    maxObservedDegreeOfParallelism = Math.Max(maxObservedDegreeOfParallelism, running);
                }

                try
                {
                    Thread.Sleep(10);
                }
                finally
                {
                    lock (locked)
                    {
                        running--;
                    }
                }
            };

            // Act
            source.IterParallel(f, maxDegreeOfParallelism, Common.CancellationToken);

            // Assert
            await Assert.That(maxObservedDegreeOfParallelism)
                        .IsLessThanOrEqualTo(maxDegreeOfParallelism.IfNone(() => int.MaxValue));
        });
    }
}

public class Enumerable_Tap_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = Generator.Object.Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var f = (object obj) => { };

            // Act
            var result = source.Tap(f);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in Generator.Object.Array
                  from f in Generator.ObjectToObject
                  from g in Generator.ObjectToObject
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;
            var path1Effects = new List<object>();
            var path2Effects = new List<object>();

            // Act
            var result1 = source.Tap(x => path1Effects.Add(f(x)))
                                .Tap(x => path1Effects.Add(g(x)));

            var result2 = source.Tap(x =>
                                {
                                    path2Effects.Add(f(x));
                                    path2Effects.Add(g(x));
                                });

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Unzip_Tests()
{
    [Test]
    public async Task Unzip_reverses_Zip()
    {
        var gen = from length in Gen.Int[0, 100]
                  from source1 in Generator.Object.Array[length]
                  from source2 in Generator.Object.Array[length]
                  select (source1, source2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source1, source2) = tuple;
            var zipped = source1.Zip(source2);

            // Act
            var (unzippedSource1, unzippedSource2) = zipped.Unzip();

            // Assert
            await Assert.That(unzippedSource1)
                        .IsEquivalentTo(source1, CollectionOrdering.Matching);

            await Assert.That(unzippedSource2)
                        .IsEquivalentTo(source2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Zip_reverses_Unzip()
    {
        var gen = Gen.Select(Generator.Object, Generator.Object).Array;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var unzipped = source.Unzip();
            var (source1, source2) = unzipped;

            // Act
            var zipped = source1.Zip(source2);

            // Assert
            await Assert.That(zipped)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Head_Tests()
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = AsyncEnumerable.Empty<object>();

        // Act
        var result = await source.Head(Common.CancellationToken);

        // Assert
        await Assert.That(result)
                    .IsNone();
    }

    [Test]
    public async Task Non_empty_sequence_returns_first_item()
    {
        var gen = from first in Generator.Object
                  from tail in Generator.Object.Array
                  let source = tail.Prepend(first).ToAsyncEnumerable()
                  select (first, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, source) = tuple;

            // Act
            var result = await source.Head(Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(first);
        });
    }
}

public class AsyncEnumerable_Choose_Tests
{
    [Test]
    public async Task Satisfies_left_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.Choose(Option.Some);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_right_identity()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in Generator.ObjectToOption
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = source.Choose(f)
                                .Choose(Option.Some);

            var result2 = source.Choose(f);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_associativity()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in Generator.ObjectToOption
                  from g in Generator.ObjectToOption
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Choose(f).Choose(g);

            var result2 = source.Choose(x => f(x).Bind(g));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Choose_WithAsyncSelector_Tests()
{
    [Test]
    public async Task Satisfies_left_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            static async ValueTask<Option<object>> f(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result = source.Choose(f);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_right_identity()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            static async ValueTask<Option<object>> some(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result1 = source.Choose(f).Choose(some);

            var result2 = source.Choose(f);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_associativity()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in
                      from g in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return g(x);
                      })
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = source.Choose(f).Choose(g);

            var result2 = source.Choose(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Pick_Tests
{
    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in Generator.ObjectToOption
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = await source.Pick(f, Common.CancellationToken);
            var result2 = await source.Choose(f)
                                      .Head(Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class AsyncEnumerable_Pick_WithAsyncSelector_Tests
{
    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = await source.Pick(f, Common.CancellationToken);
            var result2 = await source.Choose(f)
                                      .Head(Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class AsyncEnumerable_Traverse_WithResult_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            static async ValueTask<Result<object>> f(object x)
            {
                await Task.Yield();
                return Result.Success(x);
            }

            // Act
            var result = await source.Traverse(f, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToResult
                      select new Func<object, ValueTask<Result<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in Generator.ObjectToOption
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = (await source.Traverse(async x =>
                                        {
                                            await Task.Yield();
                                            var result = await f(x);
                                            return result.Map(g);
                                        }, Common.CancellationToken))
                                       .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var result2 = (await source.Traverse(f, Common.CancellationToken))
                                       .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_naturality()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToResult
                      select new Func<object, ValueTask<Result<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = (await source.Traverse(f, Common.CancellationToken))
                                       .ToOption();

            var result2 = await source.Traverse(async x =>
                                        {
                                            var result = await f(x);
                                            return result.ToOption();
                                        }, Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }
}

public class AsyncEnumerable_Traverse_WithOption_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            static async ValueTask<Option<object>> f(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result = await source.Traverse(f, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in Generator.ObjectToResult
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var result1 = (await source.Traverse(async x =>
                                        {
                                            await Task.Yield();
                                            var option = await f(x);
                                            return option.Map(g);
                                        }, Common.CancellationToken))
                                       .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var result2 = (await source.Traverse(f, Common.CancellationToken))
                                       .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_naturality()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in
                      from f in Generator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var result1 = (await source.Traverse(f, Common.CancellationToken))
                                       .ToResult();

            var result2 = await source.Traverse(async x =>
                                        {
                                            var option = await f(x);
                                            return option.ToResult();
                                        }, Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2);
        });
    }
}

public class AsyncEnumerable_IterTask_Tests()
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var processedItems = new ConcurrentQueue<object>();
            async ValueTask f(object obj)
            {
                await Task.Yield();
                processedItems.Enqueue(obj);
            }

            // Act
            await source.IterTask(f, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_IterTaskParallel_Tests()
{
    [Test]
    public async Task Acts_on_each_item()
    {
        var gen = from source in Generator.Object.Array
                  from maxDegreeOfParallelism in
                    Gen.Int[1, source.Length > 0 ? source.Length : 1]
                       .OptionOf()
                  select (source, maxDegreeOfParallelism);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, maxDegreeOfParallelism) = tuple;

            var processedItems = new ConcurrentQueue<object>();
            async ValueTask f(object obj)
            {
                await Task.Yield();
                processedItems.Enqueue(obj);
            }

            // Act
            await source.IterTaskParallel(f, maxDegreeOfParallelism, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Any);
        });
    }

    [Test]
    public async Task Respects_maximum_degree_of_parallelism()
    {
        var gen = from source in Generator.Object.Array
                  from maxDegreeOfParallelism in
                    Gen.Int[1, source.Length > 0 ? source.Length : 1]
                       .OptionOf()
                  select (source, maxDegreeOfParallelism);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, maxDegreeOfParallelism) = tuple;

            var locked = new object();
            var running = 0;
            var maxObservedDegreeOfParallelism = 0;

            async ValueTask f(object obj)
            {
                lock (locked)
                {
                    running++;
                    maxObservedDegreeOfParallelism = Math.Max(maxObservedDegreeOfParallelism, running);
                }

                try
                {
                    await Task.Delay(10, Common.CancellationToken);
                }
                finally
                {
                    lock (locked)
                    {
                        running--;
                    }
                }
            }

            // Act
            await source.IterTaskParallel(f, maxDegreeOfParallelism, Common.CancellationToken);

            // Assert
            await Assert.That(maxObservedDegreeOfParallelism)
                        .IsLessThanOrEqualTo(maxDegreeOfParallelism.IfNone(() => int.MaxValue));
        });
    }
}

public class AsyncEnumerable_Tap_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var f = (object obj) => { };

            // Act
            var result = source.Tap(f);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in Generator.ObjectToObject
                  from g in Generator.ObjectToObject
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;
            var path1Effects = new List<object>();
            var path2Effects = new List<object>();

            // Act
            var result1 = source.Tap(x => path1Effects.Add(f(x)))
                                .Tap(x => path1Effects.Add(g(x)));

            var result2 = source.Tap(x =>
                                {
                                    path2Effects.Add(f(x));
                                    path2Effects.Add(g(x));
                                });

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_TapTask_Tests
{
    [Test]
    public async Task Satisfies_identity()
    {
        var gen = from source in Generator.Object.Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            static async ValueTask f(object obj) => await Task.Yield();

            // Act
            var result = source.TapTask(f);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Satisfies_composition()
    {
        var gen = from source in
                      from source in Generator.Object.Array
                      select source.ToAsyncEnumerable()
                  from f in Generator.ObjectToObject
                  from g in Generator.ObjectToObject
                  select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;
            var path1Effects = new List<object>();
            var path2Effects = new List<object>();

            // Act
            var result1 = source.TapTask(async x =>
                                {
                                    await Task.Yield();
                                    path1Effects.Add(f(x));
                                })
                                .TapTask(async x =>
                                {
                                    await Task.Yield();
                                    path1Effects.Add(g(x));
                                });

            var result2 = source.TapTask(async x =>
                                {
                                    await Task.Yield();

                                    path2Effects.Add(f(x));
                                    path2Effects.Add(g(x));
                                });

            // Assert
            await Assert.That(result1)
                        .IsEquivalentTo(result2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Unzip_Tests()
{
    [Test]
    public async Task Unzip_reverses_Zip()
    {
        var gen = from length in Gen.Int[0, 100]
                  from source1 in
                      from source in Generator.Object.Array[length]
                      select source.ToAsyncEnumerable()
                  from source2 in
                      from source in Generator.Object.Array[length]
                      select source.ToAsyncEnumerable()
                  select (source1, source2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source1, source2) = tuple;
            var zipped = source1.Zip(source2);

            // Act
            var (unzippedSource1, unzippedSource2) = await zipped.Unzip(Common.CancellationToken);

            // Assert
            await Assert.That(unzippedSource1)
                        .IsEquivalentTo(source1, CollectionOrdering.Matching);

            await Assert.That(unzippedSource2)
                        .IsEquivalentTo(source2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Zip_reverses_Unzip()
    {
        var gen = from source in Gen.Select(Generator.Object, Generator.Object).Array
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var unzipped = await source.Unzip(Common.CancellationToken);
            var (source1, source2) = unzipped;

            // Act
            var zipped = source1.Zip(source2);

            // Assert
            await Assert.That(zipped)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }
}

public class Dictionary_Find_Tests()
{
    [Test]
    public async Task Missing_key_returns_none()
    {
        var gen = from dictionary in Gen.Dictionary(Generator.Object, Generator.Object)
                  from key in Generator.Object
                  where dictionary.ContainsKey(key) is false
                  select (dictionary.ToImmutableDictionary(), key);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (dictionary, key) = tuple;

            // Act
            var result = dictionary.Find(key);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Existing_key_returns_value()
    {
        var gen = from key in Generator.Object
                  from value in Generator.Object
                  from dictionary in
                      from dictionary in Gen.Dictionary(Generator.Object, Generator.Object)
                      where dictionary.ContainsKey(key) is false
                      select dictionary.ToImmutableDictionary().Add(key, value)
                  select (dictionary, key, value);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (dictionary, key, value) = tuple;

            // Act
            var result = dictionary.Find(key);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}


// file static class Common
// {
//     public static CancellationToken CancellationToken =>
//         TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

//     public static Error TestError { get; } = Error.From("test error");

//     public static Option<int> NoneIntOption { get; } = Option.None;

//     public static Gen<Func<int, Option<string>>> IntToStringOptionGenerator { get; } =
//         from f in Generator.IntToString
//         select new Func<int, Option<string>>(x => Math.Abs(x % 10) < 9
//                                                         ? Option.Some(f(x))
//                                                         : Option.None);

//     public static Gen<Func<string, Option<int>>> StringToIntOptionGenerator { get; } =
//         from f in Generator.StringToInt
//         select new Func<string, Option<int>>(s =>
//         {
//             var intValue = f(s);

//             return Math.Abs(intValue % 10) < 9
//                     ? Option.Some(intValue)
//                     : Option.None;
//         });

//     public static Gen<Func<int, Result<string>>> IntToStringResultGenerator { get; } =
//         from f in Generator.IntToString
//         from error in Generator.Error
//         select new Func<int, Result<string>>(x => Math.Abs(x % 10) < 9
//                                                         ? Result.Success(f(x))
//                                                         : Result.Error<string>(error));

//     public static Gen<Func<string, Result<int>>> StringToIntResultGenerator { get; } =
//         from f in Generator.StringToInt
//         from error in Generator.Error
//         select new Func<string, Result<int>>(s =>
//         {
//             var intValue = f(s);

//             return Math.Abs(intValue % 10) < 9
//                     ? Result.Success(intValue)
//                     : Result.Error<int>(error);
//         });

//     public static Gen<Option<int>> MaxDegreeOfParallelismGenerator(int max) =>
//         Gen.Frequency((1, Gen.Const(NoneIntOption)),
//                       (9, Gen.Int[1, Math.Max(max, 1)]
//                                .Select(Option.Some)));

//     public static void UpdateMax(ref int maxObserved, int current)
//     {
//         while (true)
//         {
//             var snapshot = maxObserved;

//             if (current <= snapshot)
//             {
//                 return;
//             }

//             if (Interlocked.CompareExchange(ref maxObserved, current, snapshot) == snapshot)
//             {
//                 return;
//             }
//         }
//     }
// }

// file static class AssertEx
// {
//     public static async Task IsSomeSequenceEqual<T>(Option<ImmutableArray<T>> option, IEnumerable<T> expected)
//     {
//         await Assert.That(option)
//                     .IsSome();

//         await Assert.That(option.Match(values => values.SequenceEqual(expected),
//                                        () => false))
//                     .IsTrue();
//     }

//     public static async Task IsSuccessSequenceEqual<T>(Result<ImmutableArray<T>> result, IEnumerable<T> expected)
//     {
//         await Assert.That(result)
//                     .IsSuccess();

//         await Assert.That(result.Match(values => values.SequenceEqual(expected),
//                                        _ => false))
//                     .IsTrue();
//     }

//     public static async Task AreEqual<T>(Option<ImmutableArray<T>> actual, Option<ImmutableArray<T>> expected)
//     {
//         switch (expected)
//         {
//             case Some<ImmutableArray<T>> { Value: var expectedValues }:
//                 await IsSomeSequenceEqual(actual, expectedValues);
//                 break;

//             default:
//                 await Assert.That(actual)
//                             .IsNone();
//                 break;
//         }
//     }

//     public static async Task AreEqual<T>(Result<ImmutableArray<T>> actual, Result<ImmutableArray<T>> expected)
//     {
//         switch (expected)
//         {
//             case Success<ImmutableArray<T>> { Value: var expectedValues }:
//                 await IsSuccessSequenceEqual(actual, expectedValues);
//                 break;

//             case Error expectedError:
//                 await Assert.That(actual)
//                             .IsError()
//                             .Which
//                             .IsEqualTo(expectedError);
//                 break;
//         }
//     }

//     public static async Task AreEqual<T>(Result<Option<ImmutableArray<T>>> actual, Result<Option<ImmutableArray<T>>> expected)
//     {
//         switch (expected)
//         {
//             case Success<Option<ImmutableArray<T>>> { Value: var expectedOption }:
//                 await Assert.That(actual)
//                             .IsSuccess();

//                 var actualOption = actual.Match(value => value,
//                                                 _ => Option.None);

//                 await AreEqual(actualOption, expectedOption);
//                 break;

//             case Error expectedError:
//                 await Assert.That(actual)
//                             .IsError()
//                             .Which
//                             .IsEqualTo(expectedError);
//                 break;
//         }
//     }

//     public static async Task AreEqual<T>(Option<Result<ImmutableArray<T>>> actual, Option<Result<ImmutableArray<T>>> expected)
//     {
//         switch (expected)
//         {
//             case Some<Result<ImmutableArray<T>>> { Value: var expectedResult }:
//                 await Assert.That(actual)
//                             .IsSome();

//                 var actualResult = actual.Match(value => value,
//                                                 () => Result.Error<ImmutableArray<T>>(Common.TestError));

//                 await AreEqual(actualResult, expectedResult);
//                 break;

//             default:
//                 await Assert.That(actual)
//                             .IsNone();
//                 break;
//         }
//     }
// }

// file static class Extensions
// {
//     public static Result<T> ToResult<T>(this Option<T> option, Func<Error> errorIfNone) =>
//         option.Match(Result.Success,
//                      () => Result.Error<T>(errorIfNone()));
// }

// public class Enumerable_Head_Tests
// {
//     [Test]
//     public async Task Empty_sequence_returns_none()
//     {
//         // Arrange
//         var source = Enumerable.Empty<object>();

//         // Act
//         var result = source.Head();

//         // Assert
//         await Assert.That(result)
//                     .IsNone();
//     }

//     [Test]
//     public async Task Non_empty_sequence_returns_first_item()
//     {
//         var gen = from first in Generator.Object
//                   from tail in Generator.Object.Array
//                   let source = tail.Prepend(first)
//                   select (first, source);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (first, source) = tuple;

//             // Act
//             var result = source.Head();

//             // Assert
//             await Assert.That(result)
//                         .IsSome()
//                         .WhoseValue
//                         .IsEqualTo(first);
//         });
//     }

//     [Test]
//     public async Task Short_circuits_after_the_first_item()
//     {
//         var gen = from source in Generator.Object.Array
//                   select source.Select((x, index) =>
//                   {
//                       return index > 1
//                                 ? throw new InvalidOperationException("Head should not enumerate past the first item.")
//                                 : x;
//                   });

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var f = () => source.Head();

//             // Assert
//             await Assert.That(f)
//                         .ThrowsNothing();
//         });
//     }
// }

// public class Enumerable_Head_WithPredicate_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_Where_then_Head()
//     {
//         var gen = from source in Generator.Object.Array
//                   from predicate in Generator.ObjectPredicate
//                   select (source, predicate);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, predicate) = tuple;

//             // Act
//             var result1 = source.Head(predicate);

//             var result2 = source.Where(predicate)
//                                 .Head();

//             // Assert
//             await Assert.That(result1)
//                         .IsEqualTo(result2);
//         });
//     }
// }

// public class Enumerable_SingleOrNone_Tests
// {
//     [Test]
//     public async Task Empty_sequence_returns_none()
//     {
//         // Arrange
//         var source = Enumerable.Empty<object>();

//         // Act
//         var result = source.SingleOrNone();

//         // Assert
//         await Assert.That(result)
//                     .IsNone();
//     }

//     [Test]
//     public async Task Single_item_returns_some()
//     {
//         var gen = Generator.Object;

//         await gen.SampleAsync(async value =>
//         {
//             // Arrange
//             var source = Enumerable.Repeat(value, 1);

//             // Act
//             var result = source.SingleOrNone();

//             // Assert
//             await Assert.That(result)
//                         .IsSome()
//                         .WhoseValue
//                         .IsEqualTo(value);
//         });
//     }

//     [Test]
//     public async Task Multiple_items_returns_none()
//     {
//         var gen = from source in Generator.Object.Array
//                   where source.Length > 1
//                   select source;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.SingleOrNone();

//             // Assert
//             await Assert.That(result)
//                         .IsNone();
//         });
//     }

//     [Test]
//     public async Task Stops_enumerating_after_the_second_item()
//     {
//         var gen = from source in Generator.Object.Array
//                   where source.Length > 2
//                   select source.Select((x, index) =>
//                   {
//                       return index > 1
//                                 ? throw new InvalidOperationException("SingleOrNone should not enumerate past the second item.")
//                                 : x;
//                   });

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var f = () => source.SingleOrNone();

//             // Assert
//             await Assert.That(f)
//                         .ThrowsNothing();
//         });
//     }
// }

// public class Enumerable_Choose_Tests
// {
//     [Test]
//     public async Task Satisfies_left_identity()
//     {
//         var gen = Generator.Object.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.Choose(Option.Some);

//             // Assert
//             await Assert.That(result)
//                         .IsEquivalentTo(source, CollectionOrdering.Matching);
//         });
//     }

//     [Test]
//     public async Task Satisfies_right_identity()
//     {
//         var gen = from source in Generator.Object.Array
//                   from f in Generator.ObjectToOption
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             // Act
//             var result1 = source.Choose(f);

//             var result2 = source.Choose(f)
//                                 .Choose(Option.Some);

//             // Assert
//             await Assert.That(result1)
//                         .IsEquivalentTo(result2, CollectionOrdering.Matching);
//         });
//     }

//     [Test]
//     public async Task Satisfies_associativity()
//     {
//         var gen = from source in Generator.Object.Array
//                   from f in Generator.ObjectToOption
//                   from g in Generator.ObjectToOption
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;

//             // Act
//             var result1 = source.Choose(f)
//                                 .Choose(g);

//             var result2 = source.Choose(x => f(x).Bind(g));

//             // Assert
//             await Assert.That(result1)
//                         .IsEquivalentTo(result2, CollectionOrdering.Matching);
//         });
//     }

//     [Test]
//     public async Task Filters_out_none_values()
//     {
//         var gen = from source in Generator.Object.Array
//                   from f in Generator.ObjectToOption
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             // Act
//             var result = source.Choose(f)
//                                .ToImmutableHashSet();

//             // Assert
//             await Assert.That(source)
//                         .All(x => f(x).IsSome);
//         });
//     }
// }

// public class Enumerable_Choose_WithAsyncSelector_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_the_synchronous_selector_version()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             static async ValueTask<Option<string>> wrap(int x, Func<int, Option<string>> inner)
//             {
//                 await Task.Yield();
//                 return inner(x);
//             }

//             // Act
//             var result = await source.Choose(x => wrap(x, f))
//                                      .ToArrayAsync(Common.CancellationToken);

//             var expected = source.Choose(f)
//                                  .ToArray();

//             // Assert
//             await Assert.That(result.SequenceEqual(expected))
//                         .IsTrue();
//         });
//     }
// }

// public class Enumerable_Pick_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_Choose_then_Head()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             // Act
//             var result1 = source.Pick(f);
//             var result2 = source.Choose(f)
//                                 .Head();

//             // Assert
//             await Assert.That(result1)
//                         .IsEqualTo(result2);
//         });
//     }
// }

// public class Enumerable_Traverse_WithResult_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.Traverse(Result.Success, Common.CancellationToken);

//             // Assert
//             await AssertEx.IsSuccessSequenceEqual(result, source);
//         });
//     }

//     [Test]
//     public async Task Satisfies_naturality()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringResultGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             // Act
//             var result1 = source.Traverse(f, Common.CancellationToken)
//                                 .ToOption();

//             var result2 = source.Traverse(x => f(x).ToOption(), Common.CancellationToken);

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringResultGenerator
//                   from g in Common.StringToIntOptionGenerator
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;

//             // Act
//             var result1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
//                                 .Map(values => values.Traverse(x => x, Common.CancellationToken));

//             var result2 = source.Traverse(f, Common.CancellationToken)
//                                 .Map(values => values.Traverse(g, Common.CancellationToken));

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Accumulates_all_errors()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 0
//                   select source;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.Traverse(x => Result.Error<string>(Error.From($"error-{x}")),
//                                          Common.CancellationToken);

//             var expectedError = source.Select(x => Error.From($"error-{x}"))
//                                       .Aggregate((first, second) => first + second);

//             // Assert
//             await Assert.That(result)
//                         .IsError()
//                         .Which
//                         .IsEqualTo(expectedError);
//         });
//     }
// }

// public class Enumerable_Traverse_WithOption_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.Traverse(Option.Some, Common.CancellationToken);

//             // Assert
//             await AssertEx.IsSomeSequenceEqual(result, source);
//         });
//     }

//     [Test]
//     public async Task Satisfies_naturality()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;

//             // Act
//             var result1 = source.Traverse(f, Common.CancellationToken)
//                                 .ToResult(() => Common.TestError);

//             var result2 = source.Traverse(x => f(x).ToResult(() => Common.TestError),
//                                           Common.CancellationToken);

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   from g in Common.StringToIntResultGenerator
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;

//             // Act
//             var result1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
//                                 .Map(values => values.Traverse(x => x, Common.CancellationToken));

//             var result2 = source.Traverse(f, Common.CancellationToken)
//                                 .Map(values => values.Traverse(g, Common.CancellationToken));

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }
// }

// public class Enumerable_Iter_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item_in_source_order()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var visited = new List<int>();

//             // Act
//             source.Iter(visited.Add, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_cancellation_throws_operation_canceled_exception()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 2
//                   from cancelAfter in Gen.Int[1, source.Length - 1]
//                   select (source, cancelAfter);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, cancelAfter) = tuple;
//             using var cancellationTokenSource = new CancellationTokenSource();
//             var callCount = 0;
//             var threw = false;

//             // Act
//             try
//             {
//                 source.Iter(_ =>
//                 {
//                     callCount++;

//                     if (callCount > cancelAfter)
//                     {
//                         cancellationTokenSource.Cancel();
//                     }
//                 }, cancellationTokenSource.Token);
//             }
//             catch (OperationCanceledException)
//             {
//                 threw = true;
//             }

//             // Assert
//             await Assert.That(threw)
//                         .IsTrue();
//             await Assert.That(callCount >= cancelAfter + 1)
//                         .IsTrue();
//         });
//     }
// }

// public class Enumerable_IterTask_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item_in_source_order()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var visited = new List<int>();

//             // Act
//             await source.IterTask(async x =>
//             {
//                 visited.Add(x);
//                 await Task.Yield();
//             }, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_cancellation_throws_operation_canceled_exception()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 2
//                   from cancelAfter in Gen.Int[1, source.Length - 1]
//                   select (source, cancelAfter);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, cancelAfter) = tuple;
//             using var cancellationTokenSource = new CancellationTokenSource();
//             var callCount = 0;
//             var threw = false;

//             // Act
//             try
//             {
//                 await source.IterTask(async _ =>
//                 {
//                     callCount++;

//                     if (callCount > cancelAfter)
//                     {
//                         await cancellationTokenSource.CancelAsync();
//                     }
//                 }, cancellationTokenSource.Token);
//             }
//             catch (OperationCanceledException)
//             {
//                 threw = true;
//             }

//             // Assert
//             await Assert.That(threw)
//                         .IsTrue();
//             await Assert.That(callCount >= cancelAfter + 1)
//                         .IsTrue();
//         });
//     }
// }

// public class Enumerable_IterParallel_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item()
//     {
//         var gen = from source in Gen.Int.Array
//                   from maxDegreeOfParallelism in Common.MaxDegreeOfParallelismGenerator(source.Length + 1)
//                   select (source, maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, maxDegreeOfParallelism) = tuple;
//             var visited = new ConcurrentBag<int>();

//             // Act
//             source.IterParallel(visited.Add, maxDegreeOfParallelism, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.OrderBy(x => x)
//                                      .SequenceEqual(source.OrderBy(x => x)))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_max_degree_of_parallelism_respects_the_bound()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 0 && source.Length <= 20
//                   from maxDegreeOfParallelism in Gen.Int[1, source.Length]
//                   select (source, maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, maxDegreeOfParallelism) = tuple;
//             var active = 0;
//             var maxObserved = 0;

//             // Act
//             source.IterParallel(_ =>
//             {
//                 var current = Interlocked.Increment(ref active);
//                 Common.UpdateMax(ref maxObserved, current);
//                 Thread.Sleep(10);
//                 Interlocked.Decrement(ref active);
//             }, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

//             // Assert
//             await Assert.That(maxObserved <= maxDegreeOfParallelism)
//                         .IsTrue();
//         });
//     }
// }

// public class Enumerable_IterTaskParallel_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item()
//     {
//         var gen = from source in Gen.Int.Array
//                   from maxDegreeOfParallelism in Common.MaxDegreeOfParallelismGenerator(source.Length + 1)
//                   select (source, maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, maxDegreeOfParallelism) = tuple;
//             var visited = new ConcurrentBag<int>();

//             // Act
//             await source.IterTaskParallel(async x =>
//             {
//                 visited.Add(x);
//                 await Task.Yield();
//             }, maxDegreeOfParallelism, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.OrderBy(x => x)
//                                      .SequenceEqual(source.OrderBy(x => x)))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_max_degree_of_parallelism_respects_the_bound()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 0 && source.Length <= 20
//                   from maxDegreeOfParallelism in Gen.Int[1, source.Length]
//                   select (source, maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, maxDegreeOfParallelism) = tuple;
//             var active = 0;
//             var maxObserved = 0;

//             // Act
//             await source.IterTaskParallel(async _ =>
//             {
//                 var current = Interlocked.Increment(ref active);
//                 Common.UpdateMax(ref maxObserved, current);
//                 await Task.Delay(10);
//                 Interlocked.Decrement(ref active);
//             }, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

//             // Assert
//             await Assert.That(maxObserved <= maxDegreeOfParallelism)
//                         .IsTrue();
//         });
//     }
// }

// public class Enumerable_Tap_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Act
//             var result = source.Tap(_ => { })
//                                .ToArray();

//             // Assert
//             await Assert.That(result.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Generator.IntToString
//                   from g in Generator.IntToString
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;
//             var path1Effects = new List<string>();
//             var path2Effects = new List<string>();

//             // Act
//             var result1 = source.Tap(x => path1Effects.Add(f(x)))
//                                 .Tap(x => path1Effects.Add(g(x)))
//                                 .ToArray();

//             var result2 = source.Tap(x =>
//                                 {
//                                     path2Effects.Add(f(x));
//                                     path2Effects.Add(g(x));
//                                 })
//                                 .ToArray();

//             // Assert
//             await Assert.That(result1.SequenceEqual(result2))
//                         .IsTrue();
//             await Assert.That(path1Effects.SequenceEqual(path2Effects))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Is_lazy()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var tapCount = 0;
//             var tapped = source.Tap(_ => tapCount++);

//             // Assert
//             await Assert.That(tapCount)
//                         .IsEqualTo(0);

//             // Act
//             _ = tapped.ToArray();

//             // Assert
//             await Assert.That(tapCount)
//                         .IsEqualTo(source.Length);
//         });
//     }
// }

// public class Enumerable_Unzip_Tests
// {
//     [Test]
//     public async Task Unzip_reverses_Zip()
//     {
//         var gen = from first in Gen.Int.Array
//                   from second in Gen.String.Array[first.Length]
//                   select (first, second);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (first, second) = tuple;
//             var zipped = first.Zip(second);

//             // Act
//             var (unzippedFirst, unzippedSecond) = zipped.Unzip();

//             // Assert
//             await Assert.That(unzippedFirst.SequenceEqual(first))
//                         .IsTrue();
//             await Assert.That(unzippedSecond.SequenceEqual(second))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Zip_reverses_Unzip()
//     {
//         var gen = from first in Gen.Int.Array
//                   from second in Gen.String.Array[first.Length]
//                   select first.Zip(second)
//                               .ToArray();

//         await gen.SampleAsync(async pairs =>
//         {
//             // Arrange
//             var (first, second) = pairs.Unzip();

//             // Act
//             var rezipped = first.Zip(second)
//                                 .ToArray();

//             // Assert
//             await Assert.That(rezipped.SequenceEqual(pairs))
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_Head_Tests
// {
//     [Test]
//     public async Task With_empty_sequence_returns_none()
//     {
//         // Arrange
//         var source = AsyncEnumerable.Empty<int>();

//         // Act
//         var result = await source.Head(Common.CancellationToken);

//         // Assert
//         await Assert.That(result)
//                     .IsNone();
//     }

//     [Test]
//     public async Task With_non_empty_sequence_returns_first_item()
//     {
//         var gen = from first in Gen.Int
//                   from tail in Gen.Int.Array
//                   let source = tail.Prepend(first)
//                                    .ToAsyncEnumerable()
//                   select (first, source);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (first, source) = tuple;

//             // Act
//             var result = await source.Head(Common.CancellationToken);

//             // Assert
//             await Assert.That(result)
//                         .IsSome()
//                         .WhoseValue
//                         .IsEqualTo(first);
//         });
//     }
// }

// public class AsyncEnumerable_Choose_Tests
// {
//     [Test]
//     public async Task Satisfies_left_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();

//             // Act
//             var result = await asyncSource.Choose(Option.Some)
//                                           .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Satisfies_right_identity()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             // Act
//             var result1 = await asyncSource.Choose(f)
//                                            .ToArrayAsync(Common.CancellationToken);

//             var result2 = await asyncSource.Choose(f)
//                                            .Choose(Option.Some)
//                                            .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result1.SequenceEqual(result2))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Satisfies_associativity()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   from g in Common.StringToIntOptionGenerator
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             // Act
//             var result1 = await asyncSource.Choose(f)
//                                            .Choose(g)
//                                            .ToArrayAsync(Common.CancellationToken);

//             var result2 = await asyncSource.Choose(x => f(x).Bind(g))
//                                            .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result1.SequenceEqual(result2))
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_Choose_WithAsyncSelector_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_the_synchronous_selector_version()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Option<string>> asyncSelector(int x)
//             {
//                 await Task.Yield();
//                 return f(x);
//             }

//             // Act
//             var result = await asyncSource.Choose(asyncSelector)
//                                           .ToArrayAsync(Common.CancellationToken);

//             var expected = await asyncSource.Choose(f)
//                                             .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result.SequenceEqual(expected))
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_Pick_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_Choose_then_Head()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             // Act
//             var result1 = await asyncSource.Pick(f, Common.CancellationToken);
//             var result2 = await asyncSource.Choose(f)
//                                            .Head(Common.CancellationToken);

//             // Assert
//             await Assert.That(result1)
//                         .IsEqualTo(result2);
//         });
//     }
// }

// public class AsyncEnumerable_Traverse_WithResult_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Result<int>> f(int x)
//             {
//                 await Task.Yield();
//                 return Result.Success(x);
//             }

//             // Act
//             var result = await asyncSource.Traverse(f, Common.CancellationToken);

//             // Assert
//             await AssertEx.IsSuccessSequenceEqual(result, source);
//         });
//     }

//     [Test]
//     public async Task Satisfies_naturality()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringResultGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Result<string>> asyncF(int x)
//             {
//                 await Task.Yield();
//                 return f(x);
//             }

//             // Act
//             var result1 = (await asyncSource.Traverse(asyncF, Common.CancellationToken)).ToOption();
//             var result2 = await asyncSource.Traverse(async x => (await asyncF(x)).ToOption(),
//                                                      Common.CancellationToken);

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringResultGenerator
//                   from g in Common.StringToIntOptionGenerator
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Result<string>> asyncF(int x)
//             {
//                 await Task.Yield();
//                 return f(x);
//             }

//             async ValueTask<Option<int>> asyncG(string x)
//             {
//                 await Task.Yield();
//                 return g(x);
//             }

//             // Act
//             var result1FirstPass = await asyncSource.Traverse(async x =>
//             {
//                 var result = await asyncF(x);
//                 return await result.MapTask(asyncG);
//             }, Common.CancellationToken);

//             var result1 = await result1FirstPass.MapTask(values => ValueTask.FromResult(values.Traverse(x => x,
//                                                                                                           Common.CancellationToken)));

//             var result2FirstPass = await asyncSource.Traverse(asyncF, Common.CancellationToken);
//             var result2 = await result2FirstPass.MapTask(values => values.ToAsyncEnumerable()
//                                                                    .Traverse(asyncG, Common.CancellationToken));

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Accumulates_all_errors()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 0
//                   select source;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Result<string>> f(int x)
//             {
//                 await Task.Yield();
//                 return Result.Error<string>(Error.From($"error-{x}"));
//             }

//             // Act
//             var result = await asyncSource.Traverse(f, Common.CancellationToken);

//             var expectedError = source.Select(x => Error.From($"error-{x}"))
//                                       .Aggregate((first, second) => first + second);

//             // Assert
//             await Assert.That(result)
//                         .IsError()
//                         .Which
//                         .IsEqualTo(expectedError);
//         });
//     }
// }

// public class AsyncEnumerable_Traverse_WithOption_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Option<int>> f(int x)
//             {
//                 await Task.Yield();
//                 return Option.Some(x);
//             }

//             // Act
//             var result = await asyncSource.Traverse(f, Common.CancellationToken);

//             // Assert
//             await AssertEx.IsSomeSequenceEqual(result, source);
//         });
//     }

//     [Test]
//     public async Task Satisfies_naturality()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Option<string>> asyncF(int x)
//             {
//                 await Task.Yield();
//                 return f(x);
//             }

//             // Act
//             var result1 = (await asyncSource.Traverse(asyncF, Common.CancellationToken)).ToResult(() => Common.TestError);
//             var result2 = await asyncSource.Traverse(async x => (await asyncF(x)).ToResult(() => Common.TestError),
//                                                      Common.CancellationToken);

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Common.IntToStringOptionGenerator
//                   from g in Common.StringToIntResultGenerator
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();

//             async ValueTask<Option<string>> asyncF(int x)
//             {
//                 await Task.Yield();
//                 return f(x);
//             }

//             async ValueTask<Result<int>> asyncG(string x)
//             {
//                 await Task.Yield();
//                 return g(x);
//             }

//             // Act
//             var result1FirstPass = await asyncSource.Traverse(async x =>
//             {
//                 var option = await asyncF(x);
//                 return await option.MapTask(asyncG);
//             }, Common.CancellationToken);

//             var result1 = await result1FirstPass.MapTask(values => ValueTask.FromResult(values.Traverse(x => x,
//                                                                                                           Common.CancellationToken)));

//             var result2FirstPass = await asyncSource.Traverse(asyncF, Common.CancellationToken);
//             var result2 = await result2FirstPass.MapTask(values => values.ToAsyncEnumerable()
//                                                                    .Traverse(asyncG, Common.CancellationToken));

//             // Assert
//             await AssertEx.AreEqual(result1, result2);
//         });
//     }
// }

// public class AsyncEnumerable_IterTask_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item_in_source_order()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();
//             var visited = new List<int>();

//             // Act
//             await asyncSource.IterTask(async x =>
//             {
//                 visited.Add(x);
//                 await Task.Yield();
//             }, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_cancellation_throws_operation_canceled_exception()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 2
//                   from cancelAfter in Gen.Int[1, source.Length - 1]
//                   select (source.ToAsyncEnumerable(), cancelAfter);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, cancelAfter) = tuple;
//             using var cancellationTokenSource = new CancellationTokenSource();
//             var callCount = 0;
//             var threw = false;

//             // Act
//             try
//             {
//                 await source.IterTask(async _ =>
//                 {
//                     callCount++;

//                     if (callCount > cancelAfter)
//                     {
//                         await cancellationTokenSource.CancelAsync();
//                     }
//                 }, cancellationTokenSource.Token);
//             }
//             catch (OperationCanceledException)
//             {
//                 threw = true;
//             }

//             // Assert
//             await Assert.That(threw)
//                         .IsTrue();
//             await Assert.That(callCount >= cancelAfter + 1)
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_IterTaskParallel_Tests
// {
//     [Test]
//     public async Task Calls_action_for_each_item()
//     {
//         var gen = from source in Gen.Int.Array
//                   from maxDegreeOfParallelism in Common.MaxDegreeOfParallelismGenerator(source.Length + 1)
//                   select (source.ToAsyncEnumerable(), source, maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (asyncSource, source, maxDegreeOfParallelism) = tuple;
//             var visited = new ConcurrentBag<int>();

//             // Act
//             await asyncSource.IterTaskParallel(async x =>
//             {
//                 visited.Add(x);
//                 await Task.Yield();
//             }, maxDegreeOfParallelism, Common.CancellationToken);

//             // Assert
//             await Assert.That(visited.OrderBy(x => x)
//                                      .SequenceEqual(source.OrderBy(x => x)))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task With_max_degree_of_parallelism_respects_the_bound()
//     {
//         var gen = from source in Gen.Int.Array
//                   where source.Length > 0 && source.Length <= 20
//                   from maxDegreeOfParallelism in Gen.Int[1, source.Length]
//                   select (source.ToAsyncEnumerable(), maxDegreeOfParallelism);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, maxDegreeOfParallelism) = tuple;
//             var active = 0;
//             var maxObserved = 0;

//             // Act
//             await source.IterTaskParallel(async _ =>
//             {
//                 var current = Interlocked.Increment(ref active);
//                 Common.UpdateMax(ref maxObserved, current);
//                 await Task.Delay(10);
//                 Interlocked.Decrement(ref active);
//             }, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

//             // Assert
//             await Assert.That(maxObserved <= maxDegreeOfParallelism)
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_Tap_Tests
// {
//     [Test]
//     public async Task Satisfies_identity()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var asyncSource = source.ToAsyncEnumerable();

//             // Act
//             var result = await asyncSource.Tap(_ => { })
//                                           .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result.SequenceEqual(source))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Satisfies_composition()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Generator.IntToString
//                   from g in Generator.IntToString
//                   select (source, f, g);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f, g) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();
//             var path1Effects = new List<string>();
//             var path2Effects = new List<string>();

//             // Act
//             var result1 = await asyncSource.Tap(x => path1Effects.Add(f(x)))
//                                            .Tap(x => path1Effects.Add(g(x)))
//                                            .ToArrayAsync(Common.CancellationToken);

//             var result2 = await asyncSource.Tap(x =>
//                                            {
//                                                path2Effects.Add(f(x));
//                                                path2Effects.Add(g(x));
//                                            })
//                                            .ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(result1.SequenceEqual(result2))
//                         .IsTrue();
//             await Assert.That(path1Effects.SequenceEqual(path2Effects))
//                         .IsTrue();
//         });
//     }

//     [Test]
//     public async Task Is_lazy()
//     {
//         var gen = Gen.Int.Array;

//         await gen.SampleAsync(async source =>
//         {
//             // Arrange
//             var tapCount = 0;
//             var tapped = source.ToAsyncEnumerable()
//                                .Tap(_ => tapCount++);

//             // Assert
//             await Assert.That(tapCount)
//                         .IsEqualTo(0);

//             // Act
//             _ = await tapped.ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(tapCount)
//                         .IsEqualTo(source.Length);
//         });
//     }
// }

// public class AsyncEnumerable_TapTask_Tests
// {
//     [Test]
//     public async Task Is_equivalent_to_Tap_for_synchronous_effects()
//     {
//         var gen = from source in Gen.Int.Array
//                   from f in Generator.IntToString
//                   select (source, f);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (source, f) = tuple;
//             var asyncSource = source.ToAsyncEnumerable();
//             var tapEffects = new List<string>();
//             var tapTaskEffects = new List<string>();

//             // Act
//             var tapResult = await asyncSource.Tap(x => tapEffects.Add(f(x)))
//                                              .ToArrayAsync(Common.CancellationToken);

//             var tapTaskResult = await asyncSource.TapTask(async x =>
//             {
//                 tapTaskEffects.Add(f(x));
//                 await Task.Yield();
//             }).ToArrayAsync(Common.CancellationToken);

//             // Assert
//             await Assert.That(tapResult.SequenceEqual(tapTaskResult))
//                         .IsTrue();
//             await Assert.That(tapEffects.SequenceEqual(tapTaskEffects))
//                         .IsTrue();
//         });
//     }
// }

// public class AsyncEnumerable_Unzip_Tests
// {
//     [Test]
//     public async Task Unzip_reverses_Zip()
//     {
//         var gen = from first in Gen.Int.Array
//                   from second in Gen.String.Array[first.Length]
//                   select (first, second);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (first, second) = tuple;
//             var zipped = first.Zip(second)
//                               .ToAsyncEnumerable();

//             // Act
//             var (unzippedFirst, unzippedSecond) = await zipped.Unzip(Common.CancellationToken);

//             // Assert
//             await Assert.That(unzippedFirst.SequenceEqual(first))
//                         .IsTrue();
//             await Assert.That(unzippedSecond.SequenceEqual(second))
//                         .IsTrue();
//         });
//     }
// }

// public class Dictionary_Find_Tests
// {
//     [Test]
//     public async Task With_missing_key_returns_none()
//     {
//         var gen = from pairs in Gen.Select(Gen.Int, Gen.String).Array
//                   let dictionary = pairs.DistinctBy(x => x.Item1)
//                                         .ToImmutableDictionary(x => x.Item1, x => x.Item2)
//                   from key in Gen.Int
//                   where dictionary.ContainsKey(key) is false
//                   select (dictionary, key);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (dictionary, key) = tuple;

//             // Act
//             var result = dictionary.Find(key);

//             // Assert
//             await Assert.That(result)
//                         .IsNone();
//         });
//     }

//     [Test]
//     public async Task With_existing_key_returns_some_with_the_value()
//     {
//         var gen = from pairs in Gen.Select(Gen.Int, Gen.String).Array
//                   let dictionary = pairs.DistinctBy(x => x.Item1)
//                                         .ToImmutableDictionary(x => x.Item1, x => x.Item2)
//                   from key in Gen.Int
//                   from value in Gen.String
//                   select (dictionary.SetItem(key, value), key, value);

//         await gen.SampleAsync(async tuple =>
//         {
//             // Arrange
//             var (dictionary, key, value) = tuple;

//             // Act
//             var result = dictionary.Find(key);

//             // Assert
//             await Assert.That(result)
//                         .IsSome()
//                         .WhoseValue
//                         .IsEqualTo(value);
//         });
//     }
// }
