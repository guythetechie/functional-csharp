using CsCheck;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace common.tests;

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
        var gen =
            from first in Generator.Object
            from source in
                from tail in Generator.Object.ArrayOf()
                select tail.Prepend(first)
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
    public async Task No_matching_item_returns_none()
    {
        var gen =
            from predicate in MapperGenerator.ObjectPredicate
            from source in
                from values in Generator.Object.ArrayOf()
                select values.Where(value => predicate(value) is false)
            select (source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var result = source.Head(predicate);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Returns_first_matching_item()
    {
        var gen =
            from matching in
                from predicate in MapperGenerator.ObjectPredicate
                from first in Generator.Object
                where predicate(first)
                select (first, predicate)
            let first = matching.first
            let predicate = matching.predicate
            from source in
                from prefix in
                    from candidates in Generator.Object.ArrayOf()
                    select candidates.Where(candidate => predicate(candidate) is false)
                from suffix in Generator.Object.ArrayOf()
                select ImmutableArray.CreateRange([.. prefix,
                                                   first,
                                                   .. suffix])
            select (first, source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, source, predicate) = tuple;

            // Act
            var result = source.Head(predicate);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(first);
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
        var gen =
            from source in Generator.Object.ArrayOf()
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

public class Enumerable_LastOrNone_Tests
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = Enumerable.Empty<int>();

        // Act
        var result = source.LastOrNone();

        // Assert
        await Assert.That(result)
                    .IsNone();
    }

    [Test]
    public async Task Non_empty_sequence_returns_last_item()
    {
        var gen =
            from last in Generator.Object
            from source in
                from prefix in Generator.Object.ArrayOf()
                select prefix.Append(last)
            select (last, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (last, source) = tuple;

            // Act
            var result = source.LastOrNone();

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(last);
        });
    }

    [Test]
    public async Task Present_default_value_returns_some()
    {
        await test<int>();
        await test<string>();
        await test<bool>();
        await test<object>();

        static async ValueTask test<T>()
        {
            // Arrange
            var @default = default(T);
            var source = Enumerable.Repeat(@default, 1);

            // Act
            var result = source.LastOrNone();

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(@default);
        }
    }
}

public class Enumerable_LastOrNone_WithPredicate_Tests
{
    [Test]
    public async Task Empty_sequence_returns_none()
    {
        // Arrange
        var source = Enumerable.Empty<int>();
        bool predicate(int value) => value > 0;

        // Act
        var result = source.LastOrNone(predicate);

        // Assert
        await Assert.That(result)
                    .IsNone();
    }

    [Test]
    public async Task No_matching_item_returns_none()
    {
        var gen = Generator.Object.ArrayOf();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            bool predicate(object value) => false;

            // Act
            var result = source.LastOrNone(predicate);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Is_equivalent_to_Where_then_Reverse_then_Head()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from predicate in MapperGenerator.ObjectPredicate
            select (source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var path1 = source.LastOrNone(predicate);

            var path2 = source.Where(predicate)
                              .Reverse()
                              .Head();

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Matching_default_value_returns_some()
    {
        var gen =
            from source in
                from array in Gen.Int.ArrayOf()
                select array.Append(default)
            from predicate in
                from predicate in MapperGenerator.IntPredicate
                where predicate(default)
                select predicate
            select (source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var result = source.LastOrNone(predicate);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(default);
        });
    }
}

public class Enumerable_Choose_Tests
{
    [Test]
    public async Task Choosing_some_returns_original_sequence()
    {
        var gen = Generator.Object.ArrayOf();

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
    public async Task Inserting_none_preserves_chosen_values()
    {
        var gen =
            from values in Generator.Object.ArrayOf()
            from source in
                from blocks in
                    Generator.Traverse(values,
                                       value => from prefix in OptionGenerator.None.ArrayOf()
                                                from suffix in OptionGenerator.None.ArrayOf()
                                                select ImmutableArray.CreateRange([.. prefix,
                                                                                   Option.Some(value),
                                                                                   .. suffix]))
                select blocks.SelectMany(block => block)
            select (values, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (values, source) = tuple;

            // Act
            var result = source.Choose(x => x);

            // Assert
            await Assert.That(result)
                        .IsEquivalentTo(values, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Successive_choices_are_equivalent_to_binding_selectors()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            from g in MapperGenerator.ObjectToOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Choose(f)
                              .Choose(g);

            var path2 = source.Choose(x => f(x).Bind(g));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Choose_WithAsyncSelector_Tests
{
    [Test]
    public async Task Choosing_some_returns_original_sequence()
    {
        var gen = Generator.Object.ArrayOf();

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
    public async Task Choosing_with_pure_selector_is_equivalent_to_synchronous_choose()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            async ValueTask<Option<object>> lift(object x)
            {
                await Task.Yield();
                return f(x);
            }

            // Act
            var path1 = source.Choose(lift);
            var path2 = source.Choose(f);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Successive_choices_are_equivalent_to_binding_selectors()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToAsyncOption
            from g in MapperGenerator.ObjectToAsyncOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Choose(f).Choose(g);

            var path2 = source.Choose(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Pick_Tests
{
    [Test]
    public async Task Returns_first_some_value()
    {
        var gen =
            from value in Generator.Object
            from source in
                from prefix in OptionGenerator.None.ArrayOf()
                from suffix in OptionGenerator.Any.ArrayOf()
                select ImmutableArray.CreateRange([.. prefix,
                                                   Option.Some(value),
                                                   .. suffix])
            select (value, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, source) = tuple;

            // Act
            var result = source.Pick(x => x);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var path1 = source.Pick(f);

            var path2 = source.Choose(f)
                              .Head();

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Enumerable_Traverse_WithResult_Tests
{
    [Test]
    public async Task Empty_sequence_returns_success_with_empty_values()
    {
        // Arrange
        var source = Enumerable.Empty<object>();
        Result<object> f(object _) => throw new UnreachableException();

        // Act
        var result = source.Traverse(f, Common.CancellationToken);

        // Assert
        await Assert.That(result)
                    .IsSuccess()
                    .WhoseValue
                    .Member(values => values.Length, length => length.IsEqualTo(0));
    }

    [Test]
    public async Task Retains_errors_from_all_failing_elements()
    {
        var gen =
            from errors in ErrorGenerator.Any.ArrayOf(2, 8)
            from source in
                from values in Generator.Object.ArrayOf()
                select ImmutableArray.CreateRange([.. errors.Select(Result.Error<object>),
                                                   .. values.Select(Result.Success)])
            select (errors, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (errors, source) = tuple;

            // Act
            var result = source.Traverse(x => x, Common.CancellationToken);

            // Assert
            var expectedError = errors.Aggregate((error1, error2) => error1 + error2);

            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEquivalentTo(expectedError);
        });
    }

    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen = Generator.Object.ArrayOf();

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
    public async Task Satisfies_traversal_composition_law_for_Result_then_Option()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToResult
            from g in MapperGenerator.ObjectToOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
                              .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var path2 = source.Traverse(f, Common.CancellationToken)
                              .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_traversal_naturality_law_for_ToOption()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToResult
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var path1 = source.Traverse(f, Common.CancellationToken)
                              .ToOption();

            var path2 = source.Traverse(x => f(x).ToOption(), Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }
}

public class Enumerable_Traverse_WithOption_Tests
{
    [Test]
    public async Task Empty_sequence_returns_some_with_empty_values()
    {
        // Arrange
        var source = Enumerable.Empty<object>();
        Option<object> f(object _) => throw new UnreachableException();

        // Act
        var result = source.Traverse(f, Common.CancellationToken);

        // Assert
        await Assert.That(result)
                    .IsSome()
                    .WhoseValue
                    .Member(values => values.Length, length => length.IsEqualTo(0));
    }

    [Test]
    public async Task Any_none_returns_none()
    {
        var gen =
            from prefix in OptionGenerator.Some.ArrayOf()
            from suffix in OptionGenerator.Any.ArrayOf()
            select ImmutableArray.CreateRange([.. prefix,
                                               Option<object>.None,
                                               .. suffix]);

        await gen.SampleAsync(async source =>
        {
            // Act
            var result = source.Traverse(x => x, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen = Generator.Object.ArrayOf();

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
    public async Task Satisfies_traversal_composition_law_for_Option_then_Result()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            from g in MapperGenerator.ObjectToResult
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Traverse(x => f(x).Map(g), Common.CancellationToken)
                              .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var path2 = source.Traverse(f, Common.CancellationToken)
                              .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Converting_to_result_commutes_with_traversal()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;
            Error errorFactory() => Error.From("Option is None.");

            // Act
            var path1 = source.Traverse(f, Common.CancellationToken)
                              .ToResult(errorFactory);

            var path2 = source.Traverse(x => f(x).ToResult(errorFactory),
                                        Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }
}

public class Enumerable_Iter_Tests
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen = Generator.Object.ArrayOf();

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

}

public class Enumerable_IterTask_Tests
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen = Generator.Object.ArrayOf();

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

public class Enumerable_IterParallel_Tests
{
    [Test]
    public async Task Acts_on_each_item()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
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
        var gen =
            from source in Generator.Object.ArrayOf(1, 16)
            from maxDegreeOfParallelism in
              Gen.Int[1, 4]
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
                    _ = Thread.Yield();
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
            source.IterParallel(f, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

            // Assert
            await Assert.That(maxObservedDegreeOfParallelism)
                        .IsLessThanOrEqualTo(maxDegreeOfParallelism);
        });
    }
}

public class Enumerable_IterTaskParallel_Tests
{
    [Test]
    public async Task Acts_on_each_item()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
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
        var gen =
            from source in Generator.Object.ArrayOf(1, 16)
            from maxDegreeOfParallelism in
              Gen.Int[1, 4]
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
            await source.IterTaskParallel(f, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

            // Assert
            await Assert.That(maxObservedDegreeOfParallelism)
                        .IsLessThanOrEqualTo(maxDegreeOfParallelism);
        });
    }
}

public class Enumerable_Tap_Tests
{
    [Test]
    public async Task Observes_source_elements_in_order()
    {
        var gen = Generator.Object.ArrayOf();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            var observed = new List<object>();
            void f(object value) => observed.Add(value);

            // Act
            _ = source.Tap(f).ToArray();

            // Assert
            await Assert.That(observed)
                        .IsEquivalentTo(source, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Preserves_source_elements_in_order()
    {
        var gen = Generator.Object.ArrayOf();

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
    public async Task Successive_taps_are_equivalent_to_combining_callbacks()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
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
            var path1 = source.Tap(x => path1Effects.Add(f(x)))
                              .Tap(x => path1Effects.Add(g(x)));

            var path2 = source.Tap(x =>
            {
                path2Effects.Add(f(x));
                path2Effects.Add(g(x));
            });

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class Enumerable_Unzip_Tests
{
    [Test]
    public async Task Unzip_reverses_Zip()
    {
        var gen =
            from length in Gen.Int[0, 100]
            from source1 in Generator.Object.ArrayOf(length)
            from source2 in Generator.Object.ArrayOf(length)
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
        var gen = Gen.Select(Generator.Object, Generator.Object).ArrayOf();

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

public class AsyncEnumerable_Head_Tests
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
        var gen =
            from first in Generator.Object
            from source in
                from tail in Generator.Object.ArrayOf()
                select tail.Prepend(first)
                           .ToYieldingAsyncEnumerable()
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

public class AsyncEnumerable_Head_WithPredicate_Tests
{
    [Test]
    public async Task No_matching_item_returns_none()
    {
        var gen =
            from predicate in MapperGenerator.ObjectPredicate
            from source in
                from values in Generator.Object.ArrayOf()
                select values.Where(value => predicate(value) is false)
                             .ToYieldingAsyncEnumerable()
            select (source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, predicate) = tuple;

            // Act
            var result = await source.Head(predicate, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Returns_first_matching_item()
    {
        var gen =
            from matching in
                from predicate in MapperGenerator.ObjectPredicate
                from first in Generator.Object
                where predicate(first)
                select (first, predicate)
            let first = matching.first
            let predicate = matching.predicate
            from source in
                from prefix in
                    from candidates in Generator.Object.ArrayOf()
                    select
                        from candidate in candidates
                        where predicate(candidate) is false
                        select candidate
                from suffix in Generator.Object.ArrayOf()
                select ImmutableArray.CreateRange([.. prefix,
                                                   first,
                                                   .. suffix])
                                     .ToYieldingAsyncEnumerable()
            select (first, source, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, source, predicate) = tuple;

            // Act
            var result = await source.Head(predicate, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(first);
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
            var source = Enumerable.Repeat(value, 1)
                                   .ToYieldingAsyncEnumerable();

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
        var gen =
            from source in Generator.Object.ArrayOf()
            where source.Length > 1
            select source.ToYieldingAsyncEnumerable();

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

public class AsyncEnumerable_Choose_Tests
{
    [Test]
    public async Task Choosing_some_returns_original_sequence()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Choosing_is_equivalent_to_synchronous_choose()
    {
        var gen =
            from source in
                Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var path1 = source.ToYieldingAsyncEnumerable()
                              .Choose(f);

            var path2 = source.Choose(f);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Successive_choices_are_equivalent_to_binding_selectors()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToOption
            from g in MapperGenerator.ObjectToOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Choose(f)
                              .Choose(g);

            var path2 = source.Choose(x => f(x).Bind(g));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Choose_WithAsyncSelector_Tests
{
    [Test]
    public async Task Choosing_some_returns_original_sequence()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Choosing_with_pure_selector_is_equivalent_to_synchronous_choose()
    {
        var gen =
            from source in
                Generator.Object.ArrayOf()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            async ValueTask<Option<object>> lift(object x)
            {
                await Task.Yield();
                return f(x);
            }

            // Act
            var path1 = source.ToYieldingAsyncEnumerable()
                              .Choose(lift);

            var path2 = source.Choose(f);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Successive_choices_are_equivalent_to_binding_selectors()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncOption
            from g in MapperGenerator.ObjectToAsyncOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            // Act
            var path1 = source.Choose(f)
                              .Choose(g);

            var path2 = source.Choose(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Pick_Tests
{
    [Test]
    public async Task Returns_first_some_value()
    {
        var gen =
            from value in Generator.Object
            from source in
                from prefix in OptionGenerator.None.ArrayOf()
                from suffix in OptionGenerator.Any.ArrayOf()
                select ImmutableArray.CreateRange([.. prefix,
                                                   Option.Some(value),
                                                   .. suffix])
                                     .ToYieldingAsyncEnumerable()
            select (value, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, source) = tuple;

            // Act
            var result = await source.Pick(x => x, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var path1 = await source.Pick(f, Common.CancellationToken);
            var path2 = await source.Choose(f)
                                    .Head(Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class AsyncEnumerable_Pick_WithAsyncSelector_Tests
{
    [Test]
    public async Task Returns_first_some_value()
    {
        var gen =
            from value in Generator.Object
            from source in
                from prefix in OptionGenerator.None.ArrayOf()
                from suffix in OptionGenerator.Any.ArrayOf()
                select ImmutableArray.CreateRange([.. prefix,
                                                   Option.Some(value),
                                                   .. suffix])
                                     .ToYieldingAsyncEnumerable()
            select (value, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, source) = tuple;

            async ValueTask<Option<object>> f(Option<object> option)
            {
                await Task.Yield();
                return option;
            }

            // Act
            var result = await source.Pick(f, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Is_equivalent_to_Choose_then_Head()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            // Act
            var path1 = await source.Pick(f, Common.CancellationToken);

            var path2 = await source.Choose(f)
                                    .Head(Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class AsyncEnumerable_Traverse_WithResult_Tests
{
    [Test]
    public async Task Synchronous_selector_is_equivalent_to_async_lifting()
    {
        var gen =
            from source in
                from values in Generator.Object.ArrayOf()
                select values.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToResult
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            async ValueTask<Result<object>> lift(object value)
            {
                await Task.Yield();
                return f(value);
            }

            // Act
            var path1 = await source.Traverse(f, Common.CancellationToken);
            var path2 = await source.Traverse(lift, Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Retains_errors_from_all_failing_elements()
    {
        var gen =
            from errors in ErrorGenerator.Any.ArrayOf(2, 8)
            from source in
                from values in Generator.Object.ArrayOf(0, 8)
                select ImmutableArray.CreateRange([.. errors.Select(Result.Error<object>),
                                                   .. values.Select(Result.Success)])
                                     .ToYieldingAsyncEnumerable()
            select (errors, source);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (errors, source) = tuple;

            async ValueTask<Result<object>> f(Result<object> value)
            {
                await Task.Yield();
                return value;
            }

            // Act
            var result = await source.Traverse(f, Common.CancellationToken);

            // Assert
            var expectedError = errors.Aggregate((error1, error2) => error1 + error2);

            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEquivalentTo(expectedError);
        });
    }

    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Satisfies_traversal_composition_law_for_Result_then_Option()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncResult
            from g in MapperGenerator.ObjectToOption
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            async ValueTask<Result<Option<object>>> composed(object value)
            {
                var result = await f(value);
                return result.Map(g);
            }

            // Act
            var path1 = (await source.Traverse(composed, Common.CancellationToken))
                                     .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var path2 = (await source.Traverse(f, Common.CancellationToken))
                                     .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_traversal_naturality_law_for_ToOption()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncResult
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            async ValueTask<Option<object>> converted(object value)
            {
                var result = await f(value);
                return result.ToOption();
            }

            // Act
            var path1 = (await source.Traverse(f, Common.CancellationToken))
                                     .ToOption();

            var path2 = await source.Traverse(converted, Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }
}

public class AsyncEnumerable_Traverse_WithOption_Tests
{
    [Test]
    public async Task Synchronous_selector_is_equivalent_to_async_lifting()
    {
        var gen =
            from source in
                from values in Generator.Object.ArrayOf()
                select values.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;

            async ValueTask<Option<object>> lift(object value)
            {
                await Task.Yield();
                return f(value);
            }

            // Act
            var path1 = await source.Traverse(f, Common.CancellationToken);
            var path2 = await source.Traverse(lift, Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Any_none_returns_none()
    {
        var gen =
            from prefix in OptionGenerator.Some.ArrayOf()
            from suffix in OptionGenerator.Any.ArrayOf()
            select ImmutableArray.CreateRange([.. prefix,
                                               Option<object>.None,
                                               .. suffix])
                                 .ToYieldingAsyncEnumerable();

        await gen.SampleAsync(async source =>
        {
            // Arrange
            async ValueTask<Option<object>> f(Option<object> value)
            {
                await Task.Yield();
                return value;
            }

            // Act
            var result = await source.Traverse(f, Common.CancellationToken);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Satisfies_traversal_composition_law_for_Option_then_Result()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncOption
            from g in MapperGenerator.ObjectToResult
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;

            async ValueTask<Option<Result<object>>> composed(object value)
            {
                var option = await f(value);
                return option.Map(g);
            }

            // Act
            var path1 = (await source.Traverse(composed, Common.CancellationToken))
                                     .Map(values => values.Traverse(x => x, Common.CancellationToken));

            var path2 = (await source.Traverse(f, Common.CancellationToken))
                                     .Map(values => values.Traverse(g, Common.CancellationToken));

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }

    [Test]
    public async Task Converting_to_result_commutes_with_traversal()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToAsyncOption
            select (source, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f) = tuple;
            Error errorFactory() => Error.From("Option is None.");

            async ValueTask<Result<object>> converted(object value)
            {
                var option = await f(value);
                return option.ToResult(errorFactory);
            }

            // Act
            var path1 = (await source.Traverse(f, Common.CancellationToken))
                                     .ToResult(errorFactory);

            var path2 = await source.Traverse(converted, Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2);
        });
    }
}

public class AsyncEnumerable_Iter_Tests
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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

public class AsyncEnumerable_IterTask_Tests
{
    [Test]
    public async Task Acts_on_each_item_in_order()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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

public class AsyncEnumerable_IterTaskParallel_Tests
{
    [Test]
    public async Task Acts_on_each_item()
    {
        var gen =
            from source in
                from values in Generator.Object.ArrayOf()
                select values.ToYieldingAsyncEnumerable()
            from maxDegreeOfParallelism in
                Gen.Int[1, 4].OptionOf()
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
        var gen =
            from source in
                from values in Generator.Object.ArrayOf(1, 16)
                select values.ToYieldingAsyncEnumerable()
            from maxDegreeOfParallelism in Gen.Int[1, 4]
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
            await source.IterTaskParallel(f, Option.Some(maxDegreeOfParallelism), Common.CancellationToken);

            // Assert
            await Assert.That(maxObservedDegreeOfParallelism)
                        .IsLessThanOrEqualTo(maxDegreeOfParallelism);
        });
    }
}

public class AsyncEnumerable_Tap_Tests
{
    [Test]
    public async Task Observes_source_elements_in_order()
    {
        var gen = Generator.Object.ArrayOf();

        await gen.SampleAsync(async values =>
        {
            // Arrange
            var source = values.ToYieldingAsyncEnumerable();

            var observed = new List<object>();
            void f(object value) => observed.Add(value);

            // Act
            _ = await source.Tap(f).ToArrayAsync(Common.CancellationToken);

            // Assert
            await Assert.That(observed)
                        .IsEquivalentTo(values, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Preserves_source_elements_in_order()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Successive_taps_are_equivalent_to_combining_callbacks()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
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
            var path1 = await source.Tap(x => path1Effects.Add(f(x)))
                                    .Tap(x => path1Effects.Add(g(x)))
                                    .ToArrayAsync(Common.CancellationToken);

            var path2 = await source.Tap(x =>
                                        {
                                            path2Effects.Add(f(x));
                                            path2Effects.Add(g(x));
                                        })
                                    .ToArrayAsync(Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_TapTask_Tests
{
    [Test]
    public async Task Observes_source_elements_in_order()
    {
        var gen = Generator.Object.ArrayOf();

        await gen.SampleAsync(async values =>
        {
            // Arrange
            var source = values.ToYieldingAsyncEnumerable();

            var observed = new List<object>();

            async ValueTask f(object value)
            {
                await Task.Yield();
                observed.Add(value);
            }

            // Act
            await source.TapTask(f)
                        .ToArrayAsync(Common.CancellationToken);

            // Assert
            await Assert.That(observed)
                        .IsEquivalentTo(values, CollectionOrdering.Matching);
        });
    }

    [Test]
    public async Task Preserves_source_elements_in_order()
    {
        var gen =
            from source in Generator.Object.ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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
    public async Task Successive_taps_are_equivalent_to_combining_callbacks()
    {
        var gen =
            from source in
                from source in Generator.Object.ArrayOf()
                select source.ToYieldingAsyncEnumerable()
            from f in MapperGenerator.ObjectToObject
            from g in MapperGenerator.ObjectToObject
            select (source, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (source, f, g) = tuple;
            var path1Effects = new List<object>();
            var path2Effects = new List<object>();

            async ValueTask observeFirst(object value)
            {
                await Task.Yield();
                path1Effects.Add(f(value));
            }

            async ValueTask observeSecond(object value)
            {
                await Task.Yield();
                path1Effects.Add(g(value));
            }

            async ValueTask observeBoth(object value)
            {
                await Task.Yield();
                path2Effects.Add(f(value));
                path2Effects.Add(g(value));
            }

            // Act
            var path1 = await source.TapTask(observeFirst)
                                    .TapTask(observeSecond)
                                    .ToArrayAsync(Common.CancellationToken);

            var path2 = await source.TapTask(observeBoth)
                                    .ToArrayAsync(Common.CancellationToken);

            // Assert
            await Assert.That(path1)
                        .IsEquivalentTo(path2, CollectionOrdering.Matching);

            await Assert.That(path1Effects)
                        .IsEquivalentTo(path2Effects, CollectionOrdering.Matching);
        });
    }
}

public class AsyncEnumerable_Unzip_Tests
{
    [Test]
    public async Task Unzip_reverses_Zip()
    {
        var gen =
            from length in Gen.Int[0, 100]
            from source1 in
                from source in Generator.Object.ArrayOf(length)
                select source.ToYieldingAsyncEnumerable()
            from source2 in
                from source in Generator.Object.ArrayOf(length)
                select source.ToYieldingAsyncEnumerable()
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
        var gen =
            from source in Gen.Select(Generator.Object, Generator.Object).ArrayOf()
            select source.ToYieldingAsyncEnumerable();

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

public class Dictionary_Find_Tests
{
    [Test]
    public async Task Missing_key_returns_none()
    {
        var gen =
            from dictionary in
                from entries in Gen.Dictionary(Generator.Object, Generator.Object)
                select entries.ToImmutableDictionary()
            from key in Generator.Object
            where dictionary.ContainsKey(key) is false
            select (dictionary, key);

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
        var gen =
            from key in Generator.Object
            from value in Generator.Object
            from dictionary in
                from dictionary in Gen.Dictionary(Generator.Object, Generator.Object)
                where dictionary.ContainsKey(key) is false
                select dictionary.ToImmutableDictionary()
                                 .Add(key, value)
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

file static class Common
{
    public static async IAsyncEnumerable<T> ToYieldingAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }

    public static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    extension<T>(IAssertionSource<Option<ImmutableArray<T>>> source)
    {
        public Assertion<Option<ImmutableArray<T>>> IsEquivalentTo(Option<ImmutableArray<T>> expected) =>
            source.Satisfies(x => x,
                             actual => expected.Match<IAssertion>(array => actual.IsSome()
                                                                                 .WhoseValue
                                                                                 .IsEquivalentTo(array, CollectionOrdering.Matching),
                                                                  () => actual.IsNone()));
    }

    extension<T>(IAssertionSource<Result<ImmutableArray<T>>> source)
    {
        public Assertion<Result<ImmutableArray<T>>> IsEquivalentTo(Result<ImmutableArray<T>> expected) =>
            source.Satisfies(x => x,
                             actual => expected.Match<IAssertion>(array => actual.IsSuccess()
                                                                                 .WhoseValue
                                                                                 .IsEquivalentTo(array, CollectionOrdering.Matching),
                                                                  error => actual.IsError()
                                                                                 .Which
                                                                                 .IsEqualTo(error)));
    }

    extension<T>(IAssertionSource<Result<Option<ImmutableArray<T>>>> source)
    {
        public Assertion<Result<Option<ImmutableArray<T>>>> IsEquivalentTo(Result<Option<ImmutableArray<T>>> expected) =>
            source.Satisfies(x => x,
                             actual => expected.Match<IAssertion>(option => actual.IsSuccess()
                                                                                  .WhoseValue
                                                                                  .IsEquivalentTo(option),
                                                                  error => actual.IsError()
                                                                                 .Which
                                                                                 .IsEqualTo(error)));
    }

    extension<T>(IAssertionSource<Option<Result<ImmutableArray<T>>>> source)
    {
        public Assertion<Option<Result<ImmutableArray<T>>>> IsEquivalentTo(Option<Result<ImmutableArray<T>>> expected) =>
            source.Satisfies(x => x,
                             actual => expected.Match<IAssertion>(result => actual.IsSome()
                                                                                  .WhoseValue
                                                                                  .IsEquivalentTo(result),
                                                                  () => actual.IsNone()));
    }
}
