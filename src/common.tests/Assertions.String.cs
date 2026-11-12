using CsCheck;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.Core;

namespace common.tests;

public static class StringAssertionExtensions
{
    extension(IAssertionSource<string> source)
    {
        public Assertion<string> IsSubstringOf(string? text, StringComparison comparison = StringComparison.Ordinal) =>
#pragma warning disable TUnitAssertions0003
            source.Satisfies(value => value is not null && text is not null && text.Contains(value, comparison),
                             $"to be a substring of \"{text}\" using {comparison}");
#pragma warning restore TUnitAssertions0003
    }
}

public class String_IsSubstringOf_Tests
{
    [Test]
    public async Task Passes_when_substring_is_present()
    {
        var gen =
            from substring in StringGenerator.NonNullOrWhitespace
            from text in
                from prefix in StringGenerator.Any
                from suffix in StringGenerator.Any
                select prefix + substring + suffix
            select (substring, text);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (substring, text) = tuple;

            // Assert
            await Assert.That(substring)
                        .IsSubstringOf(text);
        });
    }

    [Test]
    public async Task Fails_when_substring_is_absent()
    {
        var gen =
            from substring in StringGenerator.Any
            from text in StringGenerator.Any
            where text.Contains(substring, StringComparison.Ordinal) is false
            select (substring, text);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (substring, text) = tuple;

            async Task action() => await Assert.That(substring).IsSubstringOf(text);

            // Assert
            await Assert.That(action)
                        .Throws<Exception>()
                        .WithMessageContaining("to be a substring of", StringComparison.Ordinal);
        });
    }

    [Test]
    public async Task Defaults_to_ordinal_comparison()
    {
        var gen =
            from text in
                from characters in Gen.Char['a', 'z'].HashSetOf(1, 20)
                select new string([.. characters])
            let substring = text.ToUpperInvariant()
            select (substring, text);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (substring, text) = tuple;

            async Task action() => await Assert.That(substring).IsSubstringOf(text);

            // Assert
            await Assert.That(action)
                        .Throws<Exception>();
        });
    }

    [Test]
    public async Task Respects_requested_comparison()
    {
        var gen =
            from comparison in Gen.OneOf(StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)
            from substring in
                from characters in Gen.Char['a', 'z'].HashSetOf(1, 20)
                select new string([.. characters])
            let embedded = comparison switch
            {
                StringComparison.Ordinal => substring,
                StringComparison.OrdinalIgnoreCase => substring.ToUpperInvariant(),
                var other => throw new ArgumentOutOfRangeException(nameof(comparison))
            }
            from text in
                from prefix in StringGenerator.Any
                from suffix in StringGenerator.Any
                select prefix + embedded + suffix
            select (substring, text, comparison);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (substring, text, comparison) = tuple;

            // Assert
            await Assert.That(substring)
                        .IsSubstringOf(text, comparison);
        });
    }
}