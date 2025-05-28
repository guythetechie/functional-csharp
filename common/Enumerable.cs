using System;
using System.Collections.Generic;
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

    public static async ValueTask Iter<T>(this IAsyncEnumerable<T> source, Action<T> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken) =>
        await source.IterTask(async item =>
                              {
                                  action(item);
                                  await ValueTask.CompletedTask;
                              },
                              maxDegreeOfParallelism,
                              cancellationToken);

    public static async ValueTask IterTask<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action, Option<int> maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };
        maxDegreeOfParallelism.Iter(max => options.MaxDegreeOfParallelism = max);

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }
}

public static class DictionaryExtensions
{
    public static Option<TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var value)
            ? Option.Some(value)
            : Option.None;
}