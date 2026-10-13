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
                  from predicate in MapperGenerator.ObjectPredicate
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
                  from f in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToOption
                  from g in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in
                      from g in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToResult
                  from g in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToResult
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
                  from f in MapperGenerator.ObjectToOption
                  from g in MapperGenerator.ObjectToResult
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
                  from f in MapperGenerator.ObjectToOption
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
            void f(object obj) => processedItems.Enqueue(obj);

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
        var gen = Generator.Object.Array.Nonempty;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancellationToken = cts.Token;

            void f() => source.Iter(obj => { }, cancellationToken);

            // Assert
            await Assert.That(f)
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
        var gen = Generator.Object.Array.Nonempty;

        await gen.SampleAsync(async source =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancellationToken = cts.Token;

            static async ValueTask f1(object obj) => await Task.Yield();

            async Task f() => await source.IterTask(f1, cancellationToken);

            // Assert
            await Assert.That(f)
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
            void f(object obj) => processedItems.Enqueue(obj);

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

            void f(object obj)
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
            }

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
            static void f(object obj) { }

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
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
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
                  from f in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToOption
                  from g in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in
                      from g in MapperGenerator.ObjectToOption
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
                  from f in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToResult
                      select new Func<object, ValueTask<Result<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in MapperGenerator.ObjectToOption
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
                      from f in MapperGenerator.ObjectToResult
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
                      from f in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in MapperGenerator.ObjectToResult
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
                      from f in MapperGenerator.ObjectToOption
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
            static void f(object obj) { }

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
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
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
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
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
public class Enumerable_IterTaskParallel_Tests()
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
                    await Task.Yield();
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

public class AsyncEnumerable_Head_WithPredicate_Tests
{
    [Test]
    public async Task Is_equivalent_to_Where_then_Head()
    {
        var gen = from source in Generator.Object.Array
                  from predicate in MapperGenerator.ObjectPredicate
                  select (source.ToAsyncEnumerable(), predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var result1 = await source.Head(predicate, Common.CancellationToken);

            var result2 = await source.Where(predicate)
                                      .Head(Common.CancellationToken);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class AsyncEnumerable_SingleOrNone_Tests
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = AsyncEnumerable.Empty<object>();

        // Act
        var result = await source.SingleOrNone(Common.CancellationToken);

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
            var source = Enumerable.Repeat(value, 1).ToAsyncEnumerable();

            // Act
            var result = await source.SingleOrNone(Common.CancellationToken);

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
                  select source.ToAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = await source.SingleOrNone(Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }
}

public class AsyncEnumerable_Iter_Tests()
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
            void f(object obj) => processedItems.Enqueue(obj);

            // Act
            await source.Iter(f, Common.CancellationToken);

            // Assert
            await Assert.That(processedItems)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }
}
