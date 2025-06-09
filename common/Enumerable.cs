using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Provides extension methods for working with IEnumerable&lt;T&gt; in a functional style.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Gets the first element of the sequence as an option.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>Some(first element) if the sequence has elements, otherwise None.</returns>
    public static Option<T> Head<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();

        return enumerator.MoveNext()
                ? Option.Some(enumerator.Current)
                : Option.None;
    }

    /// <summary>
    /// Filters and transforms elements using an option-returning selector.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">Function that returns an option for each element.</param>
    /// <returns>A sequence containing only the values where the selector returned Some.</returns>
    public static IEnumerable<T2> Choose<T, T2>(this IEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .Select(option => option.IfNone(() => throw new UnreachableException("All options should be in the 'Some' state.")));

    /// <summary>
    /// Finds the first element that produces a Some value when transformed.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">Function that returns an option for each element.</param>
    /// <returns>The first Some value produced by the selector, or None if no element produces Some.</returns>
    public static Option<T2> Pick<T, T2>(this IEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .DefaultIfEmpty(Option.None)
              .First();

   /// <summary>
   /// Applies a result-returning function to each element, collecting successes or aggregating errors.
   /// </summary>
   /// <typeparam name="T">The source element type.</typeparam>
   /// <typeparam name="T2">The result element type.</typeparam>
   /// <param name="source">The source enumerable.</param>
   /// <param name="selector">Function that returns a result for each element.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Success with all results if all succeed, otherwise an error with all failures combined.</returns>
   public static Result<ImmutableArray<T2>> Traverse<T, T2>(this IEnumerable<T> source, Func<T, Result<T2>> selector, CancellationToken cancellationToken)
    {
        var results = new List<T2>();
        var errors = new List<Error>();

        source.Iter(item => selector(item).Match(results.Add, errors.Add),
                    maxDegreeOfParallelism: 1,
                    cancellationToken);

        return errors.Count > 0
                ? errors.Aggregate((first, second) => first + second)
                : Result.Success(results.ToImmutableArray());
    }

    /// <summary>
    /// Applies an option-returning function to each element, succeeding only if all elements succeed.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="selector">Function that returns an option for each element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Some with all results if all succeed, otherwise None.</returns>
    public static Option<ImmutableArray<T2>> Traverse<T, T2>(this IEnumerable<T> source, Func<T, Option<T2>> selector, CancellationToken cancellationToken)
    {
        var results = new List<T2>();
        var hasNone = false;

        source.Iter(item => selector(item).Match(results.Add, () => hasNone = true),
                    maxDegreeOfParallelism: 1,
                    cancellationToken);

        return hasNone
                ? Option.None
                : Option.Some(results.ToImmutableArray());
    }

    /// <summary>
    /// Executes an action on each element in parallel.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The action to execute for each element.</param>
    /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static void Iter<T>(this IEnumerable<T> source, Action<T> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        Parallel.ForEach(source, options, item => action(item));
    }

    /// <summary>
    /// Executes an async action on each element in parallel.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The async action to execute for each element.</param>
    /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async ValueTask IterTask<T>(this IEnumerable<T> source, Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }

    /// <summary>
    /// Executes a side effect action on each element as it's enumerated.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The side effect action to execute.</param>
    /// <returns>The original enumerable unchanged, useful for debugging or logging without affecting data flow.</returns>
    public static IEnumerable<T> Tap<T>(this IEnumerable<T> source, Action<T> action) =>
        source.Select(item =>
        {
            action(item);
            return item;
        });

    /// <summary>
    /// Separates an enumerable of tuples into a tuple of immutable arrays.
    /// </summary>
    /// <typeparam name="T1">The first tuple element type.</typeparam>
    /// <typeparam name="T2">The second tuple element type.</typeparam>
    /// <param name="source">The source enumerable of tuples.</param>
    /// <returns>A tuple containing two immutable arrays with the separated elements.</returns>
    public static (ImmutableArray<T1>, ImmutableArray<T2>) Unzip<T1, T2>(this IEnumerable<(T1, T2)> source)
    {
        var list1 = new List<T1>();
        var list2 = new List<T2>();

        foreach (var (item1, item2) in source)
        {
            list1.Add(item1);
            list2.Add(item2);
        }

        return ([.. list1], [.. list2]);
    }
}

/// <summary>
/// Provides extension methods for working with IAsyncEnumerable&lt;T&gt; in a functional style.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Gets the first element of the async sequence as an option.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source async sequence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Some(first element) if the sequence has elements, otherwise None.</returns>
    public static async ValueTask<Option<T>> Head<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);

        return await enumerator.MoveNextAsync()
                ? Option.Some(enumerator.Current)
                : Option.None;
    }

    /// <summary>
    /// Filters and transforms async elements using an option-returning selector.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source async sequence.</param>
    /// <param name="selector">Function that returns an option for each element.</param>
    /// <returns>An async sequence containing only the values where the selector returned Some.</returns>
    public static IAsyncEnumerable<T2> Choose<T, T2>(this IAsyncEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .Select(option => option.IfNone(() => throw new UnreachableException("All options should be in the 'Some' state.")));

    /// <summary>
    /// Finds the first async element that produces a Some value when transformed.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source async sequence.</param>
    /// <param name="selector">Function that returns an option for each element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first Some value produced by the selector, or None if no element produces Some.</returns>
    public static async ValueTask<Option<T2>> Pick<T, T2>(this IAsyncEnumerable<T> source, Func<T, Option<T2>> selector, CancellationToken cancellationToken) =>
        await source.Select(selector)
                    .Where(option => option.IsSome)
                    .DefaultIfEmpty(Option.None)
                    .FirstAsync(cancellationToken);

   /// <summary>
   /// Applies an async result-returning function to each element, collecting successes or aggregating errors.
   /// </summary>
   /// <typeparam name="T">The source element type.</typeparam>
   /// <typeparam name="T2">The result element type.</typeparam>
   /// <param name="source">The source async enumerable.</param>
   /// <param name="selector">Async function that returns a result for each element.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Success with all results if all succeed, otherwise an error with all failures combined.</returns>
   public static async ValueTask<Result<ImmutableArray<T2>>> Traverse<T, T2>(this IAsyncEnumerable<T> source, Func<T, ValueTask<Result<T2>>> selector, CancellationToken cancellationToken)
    {
        var results = new List<T2>();
        var errors = new List<Error>();

        await source.IterTask(async item =>
                              {
                                  var result = await selector(item);
                                  result.Match(results.Add, errors.Add);
                              },
                              maxDegreeOfParallelism: 1,
                              cancellationToken);

        return errors.Count > 0
                ? errors.Aggregate((first, second) => first + second)
                : Result.Success(results.ToImmutableArray());
    }

    /// <summary>
    /// Applies an async option-returning function to each element, succeeding only if all elements succeed.
    /// </summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="T2">The result element type.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="selector">Async function that returns an option for each element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Some with all results if all succeed, otherwise None.</returns>
    public static async ValueTask<Option<ImmutableArray<T2>>> Traverse<T, T2>(this IAsyncEnumerable<T> source, Func<T, ValueTask<Option<T2>>> selector, CancellationToken cancellationToken)
    {
        var results = new List<T2>();
        var hasNone = false;

        await source.IterTask(async item =>
                              {
                                  var option = await selector(item);
                                  option.Match(results.Add, () => hasNone = true);
                              },
                              maxDegreeOfParallelism: 1,
                              cancellationToken);

        return hasNone
                ? Option.None
                : Option.Some(results.ToImmutableArray());
    }

    /// <summary>
    /// Executes an async action on each element in parallel.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="action">The async action to execute for each element.</param>
    /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async ValueTask IterTask<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }

    /// <summary>
    /// Executes a side effect action on each async element as it's enumerated.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="action">The side effect action to execute.</param>
    /// <returns>The original async enumerable unchanged, useful for debugging or logging without affecting data flow.</returns>
    public static IAsyncEnumerable<T> Tap<T>(this IAsyncEnumerable<T> source, Action<T> action) =>
        source.Select(item =>
        {
            action(item);
            return item;
        });

    /// <summary>
    /// Executes an async side effect action on each async element as it's enumerated.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="action">The async side effect action to execute.</param>
    /// <returns>The original async enumerable unchanged, useful for debugging or logging without affecting data flow.</returns>
    public static IAsyncEnumerable<T> TapTask<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action) =>
        source.Select(async (T item, CancellationToken _) =>
        {
            await action(item);
            return item;
        });

    /// <summary>
    /// Separates an async enumerable of tuples into a tuple of immutable arrays.
    /// </summary>
    /// <typeparam name="T1">The first tuple element type.</typeparam>
    /// <typeparam name="T2">The second tuple element type.</typeparam>
    /// <param name="source">The source async enumerable of tuples.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing two immutable arrays with the separated elements.</returns>
    public static async ValueTask<(ImmutableArray<T1>, ImmutableArray<T2>)> Unzip<T1, T2>(this IAsyncEnumerable<(T1, T2)> source, CancellationToken cancellationToken)
    {
        var list1 = new List<T1>();
        var list2 = new List<T2>();

        await foreach (var (item1, item2) in source.WithCancellation(cancellationToken))
        {
            list1.Add(item1);
            list2.Add(item2);
        }

        return ([.. list1], [.. list2]);
    }
}

/// <summary>
/// Provides extension methods for safe dictionary operations.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Safely retrieves a value from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to find.</param>
    /// <returns>Some(value) if the key exists, otherwise None.</returns>
    public static Option<TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var value)
            ? Option.Some(value)
            : Option.None;
}