using CsCheck;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class ExceptionGenerator_Any_Tests
{
    [Test]
    public async Task Can_generate_leaf_exception()
    {
        // Arrange
        var gen = ExceptionGenerator.Any;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(exception => exception is not AggregateException);
    }

    [Test]
    public async Task Can_generate_nested_aggregate_exception()
    {
        // Arrange
        var gen = ExceptionGenerator.Any;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(exception => exception is AggregateException aggregate
                                                          && aggregate.InnerExceptions.Any(inner => inner is AggregateException));
    }

    [Test]
    public async Task Aggregate_nesting_does_not_exceed_three_levels()
    {
        var gen = ExceptionGenerator.Any;

        await gen.SampleAsync(async exception =>
        {
            // Arrange
            static int aggregateDepth(Exception value) =>
                value is AggregateException aggregate
                    ? 1 + aggregate.InnerExceptions.Max(aggregateDepth)
                    : 0;

            // Act
            var depth = aggregateDepth(exception);

            // Assert
            await Assert.That(depth <= 3)
                        .IsTrue();
        });
    }
}

public class ErrorGenerator_WithMessage_Tests
{
    [Test]
    public async ValueTask Generates_error_with_message()
    {
        var gen = ErrorGenerator.WithMessage;

        await gen.SampleAsync(async error =>
        {
            await Assert.That(error.Messages).IsNotEmpty();
        });
    }
}

public class ErrorGenerator_WithException_Tests
{
    [Test]
    public async ValueTask Generates_error_with_exception()
    {
        var gen = ErrorGenerator.WithException;

        await gen.SampleAsync(async error =>
        {
            await Assert.That(error.Exceptions).IsNotEmpty();
        });
    }
}

public class ErrorGenerator_Any_Tests
{
    [Test]
    public async ValueTask Can_generate_error_with_message()
    {
        var gen = ErrorGenerator.WithMessage;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(error => error.Messages.Count > 0);
    }

    [Test]
    public async ValueTask Can_generate_error_with_exception()
    {
        var gen = ErrorGenerator.WithException;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(error => error.Exceptions.Count > 0);
    }
}

public class ErrorGenerator_EqualPair_Tests
{
    [Test]
    public async Task Generates_equal_errors()
    {
        var gen = ErrorGenerator.EqualPair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Assert
            await Assert.That(error1)
                        .IsEqualTo(error2);
        });
    }
}

public class ErrorGenerator_UnequalPair_Tests
{
    [Test]
    public async Task Generates_unequal_errors()
    {
        var gen = ErrorGenerator.UnequalPair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Assert
            await Assert.That(error1.Equals(error2))
                        .IsFalse();
        });
    }
}

public class ErrorGenerator_Pair_Tests
{
    [Test]
    public async Task Can_generate_equal_errors()
    {
        // Arrange
        var gen = ErrorGenerator.Pair;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(pair => pair.Item1.Equals(pair.Item2));
    }

    [Test]
    public async Task Can_generate_unequal_errors()
    {
        // Arrange
        var gen = ErrorGenerator.Pair;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(pair => pair.Item1.Equals(pair.Item2) is false);
    }
}