using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace common;

public static class EnumerableExtensions
{
    /// <summary>
    /// Returns an <see cref="Option{T}"/> containing the first element of the sequence, or <see cref="Option{T}.None"/> if the sequence is empty.
    /// </summary>
    public static Option<T> Head<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();

        return enumerator.MoveNext()
                ? Option.Some(enumerator.Current)
                : Option.None;
    }

    /// <summary>
    /// Applies the function <paramref name="selector"/> to each element, then return values for which the function returns <see cref="Option{T2}.Some"/>.
    /// </summary>
    public static IEnumerable<T2> Choose<T, T2>(this IEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .Select(option => option.IfNone(() => throw new UnreachableException("All options should be in the 'Some' state.")));

    /// <summary>
    /// Returns the first <see cref="Option{T2}"/> that is in the <see cref="Option{T2}.Some"/> state after applying <paramref name="selector"/> to each element.
    /// If no element produces a <see cref="Option{T2}.Some"/> value, returns <see cref="Option{T2}.None"/>.
    /// </summary>
    public static Option<T2> Pick<T, T2>(this IEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .DefaultIfEmpty(Option.None)
              .First();

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

    public static void Iter<T>(this IEnumerable<T> source, Action<T> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        Parallel.ForEach(source, options, item => action(item));
    }

    public static async ValueTask IterTask<T>(this IEnumerable<T> source, Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }

    /// <summary>
    /// Executes a side effect action on each element as it's enumerated, returning the original enumerable unchanged.
    /// Useful for debugging, logging, or other side effects in LINQ chains without affecting the data flow.
    /// </summary>
    public static IEnumerable<T> Tap<T>(this IEnumerable<T> source, Action<T> action) =>
        source.Select(item =>
        {
            action(item);
            return item;
        });
}

public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Returns an <see cref="Option{T}"/> containing the first element of the sequence, or <see cref="Option{T}.None"/> if the sequence is empty.
    /// </summary>
    public static async ValueTask<Option<T>> Head<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);

        return await enumerator.MoveNextAsync()
                ? Option.Some(enumerator.Current)
                : Option.None;
    }

    /// <summary>
    /// Applies the function <paramref name="selector"/> to each element, then return values for which the function returns <see cref="Option{T2}.Some"/>.
    /// </summary>
    public static IAsyncEnumerable<T2> Choose<T, T2>(this IAsyncEnumerable<T> source, Func<T, Option<T2>> selector) =>
        source.Select(selector)
              .Where(option => option.IsSome)
              .Select(option => option.IfNone(() => throw new UnreachableException("All options should be in the 'Some' state.")));

    /// <summary>
    /// Returns the first <see cref="Option{T2}"/> that is in the <see cref="Option{T2}.Some"/> state after applying <paramref name="selector"/> to each element.
    /// If no element produces a <see cref="Option{T2}.Some"/> value, returns <see cref="Option{T2}.None"/>.
    /// </summary>
    public static async ValueTask<Option<T2>> Pick<T, T2>(this IAsyncEnumerable<T> source, Func<T, Option<T2>> selector, CancellationToken cancellationToken) =>
        await source.Select(selector)
                    .Where(option => option.IsSome)
                    .DefaultIfEmpty(Option.None)
                    .FirstAsync(cancellationToken);

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

    public static async ValueTask IterTask<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }

    /// <summary>
    /// Executes a side effect action on each element as it's enumerated, returning the original async enumerable unchanged.
    /// Useful for debugging, logging, or other side effects in async LINQ chains without affecting the data flow.
    /// </summary>
    public static IAsyncEnumerable<T> Tap<T>(this IAsyncEnumerable<T> source, Action<T> action) =>
        source.Select(item =>
        {
            action(item);
            return item;
        });

    /// <summary>
    /// Executes an async side effect action on each element as it's enumerated, returning the original async enumerable unchanged.
    /// Useful for debugging, logging, or other async side effects in async LINQ chains without affecting the data flow.
    /// </summary>
    public static IAsyncEnumerable<T> TapTask<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action) =>
        source.Select(async (T item, CancellationToken _) =>
        {
            await action(item);
            return item;
        });
}

public static class DictionaryExtensions
{
    public static Option<TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var value)
            ? Option.Some(value)
            : Option.None;
}