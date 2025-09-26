using System;
using System.Threading.Tasks;

namespace common.tests;

internal static class Selectors
{
    public static Func<int, int> Identity { get; } =
        i => i;
        
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

    public static Func<int, Option<int>> MixedOption2 { get; } =
        i => i % 2 == 0
            ? i + 2
            : Option.None;

    public static Func<int, Option<int>> MixedOption3 { get; } =
        i => i % 4 == 0
            ? i * 3
            : Option.None;

    public static Func<int, Option<int>> MixedOption4 { get; } =
        i => i % 5 == 0
            ? i - 2
            : Option.None;

    public static Func<int, Option<int>> AllNones { get; } =
        i => Option.None;

    public static Func<int, Option<int>> OptionIdentity { get; } =
        Option.Some;

    public static Func<int, ValueTask<Option<int>>> MixedOptionAsync { get; } =
        i => ValueTask.FromResult(MixedOption(i));

    public static Func<int, ValueTask<Option<int>>> OptionIdentityAsync { get; } =
        i => ValueTask.FromResult(Option.Some(i));
}