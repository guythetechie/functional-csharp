using CsCheck;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class Error_From_WithMessages_Tests
{
    [Test]
    public async Task Preserves_supplied_messages()
    {
        var gen =
            StringGenerator.NonNullOrWhitespace
                           .HashSetOf()
                           .Where(set => set.Count > 0);

        await gen.SampleAsync(async messages =>
        {
            // Act
            var error = Error.From([.. messages]);

            // Assert
            await Assert.That(error.Messages)
                        .IsSupersetOf(messages);
        });
    }

    [Test]
    public async Task Empty_input_throws_argument_exception()
    {
        // Arrange
        var action = () => Error.From(messages: []);

        // Assert
        await Assert.That(action)
                    .Throws<ArgumentException>();
    }
}

public class Error_From_WithExceptions_Tests
{
    [Test]
    public async Task Preserves_supplied_exceptions()
    {
        var gen =
            ExceptionGenerator.Any
                              .HashSetOf()
                              .Where(set => set.Count > 0);

        await gen.SampleAsync(async exceptions =>
        {
            // Act
            var error = Error.From([.. exceptions]);

            // Assert
            await Assert.That(error.Exceptions)
                        .IsSupersetOf(exceptions);
        });
    }

    [Test]
    public async Task Empty_input_throws_argument_exception()
    {
        // Arrange
        var action = () => Error.From(exceptions: []);

        // Assert
        await Assert.That(action)
                    .Throws<ArgumentException>();
    }
}

public class Error_ToException_Tests
{
    [Test]
    public async Task Contains_original_messages()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var exception = error.ToException();

            // Assert
            ImmutableHashSet<string> exceptionMessages =
                [.. from normalizedException in ExtractExceptions(exception)
                    select normalizedException.Message];

            await Assert.That(exceptionMessages)
                        .IsSupersetOf(error.Messages);
        });
    }

    private static IEnumerable<Exception> ExtractExceptions(Exception exception) =>
        exception switch
        {
            AggregateException aggregate =>
                [aggregate,
                 .. aggregate.InnerExceptions.SelectMany(ExtractExceptions)],
            var other => [other]
        };

    [Test]
    public async Task Contains_original_exceptions()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var exception = error.ToException();

            // Assert
            var normalizedExceptions = ExtractExceptions(exception).ToImmutableHashSet();

            await Assert.That(normalizedExceptions)
                        .IsSupersetOf(error.Exceptions);
        });
    }
}

public class Error_ToString_Tests
{
    [Test]
    public async Task Includes_messages_and_exception_messages()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var text = error.ToString();

            // Assert
            IEnumerable<string> expectedSubstrings =
                [.. from message in error.Messages
                    select message.Trim(),
                 .. from exception in error.Exceptions
                    select exception.Message.Trim()];

            await Assert.That(expectedSubstrings)
                        .All()
                        .Satisfy(substring => substring.IsSubstringOf(text));
        });
    }
}

public class Error_String_ImplicitOperator_Tests
{
    [Test]
    public async Task Converts_message_to_error()
    {
        var gen = StringGenerator.NonNullOrWhitespace;

        await gen.SampleAsync(async message =>
        {
            // Act
            Error error = message;

            // Assert
            await Assert.That(error)
                        .IsEqualTo(Error.From(message));
        });
    }
}

public class Error_Exception_ImplicitOperator_Tests
{
    [Test]
    public async Task Converts_exception_to_error()
    {
        var gen = ExceptionGenerator.Any;

        await gen.SampleAsync(async exception =>
        {
            // Act
            Error error = exception;

            // Assert
            await Assert.That(error)
                        .IsEqualTo(Error.From(exception));
        });
    }
}

public class Error_AdditionOperator_Tests
{
    [Test]
    public async Task Satisfies_semigroup_associativity_law()
    {
        var gen =
            from error1 in ErrorGenerator.Any
            from error2 in ErrorGenerator.Any
            from error3 in ErrorGenerator.Any
            select (error1, error2, error3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2, error3) = tuple;

            // Act
            var path1 = (error1 + error2) + error3;
            var path2 = error1 + (error2 + error3);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_commutativity_law()
    {
        var gen = Gen.Select(ErrorGenerator.Any, ErrorGenerator.Any);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var path1 = error1 + error2;
            var path2 = error2 + error1;

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_idempotence_law()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var combined = error + error;

            // Assert
            await Assert.That(combined)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public async Task Combines_both_payload_sets()
    {
        var gen = Gen.Select(ErrorGenerator.Any, ErrorGenerator.Any);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var combined = error1 + error2;

            // Assert
            await Assert.That(combined.Messages)
                        .IsSupersetOf(error1.Messages.Concat(error2.Messages));

            await Assert.That(combined.Exceptions)
                        .IsSupersetOf(error1.Exceptions.Concat(error2.Exceptions));
        });
    }
}

public class Error_Equality_Tests
{
    [Test]
    public async Task Satisfies_equality_reflexivity_law()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var equal = error.Equals(error);

            // Assert
            await Assert.That(equal)
                        .IsTrue();
        });
    }

    [Test]
    public async Task Satisfies_equality_symmetry_law()
    {
        var gen = ErrorGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var path1 = error1.Equals(error2);
            var path2 = error2.Equals(error1);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_equality_transitivity_law()
    {
        var gen =
            from error1 in ErrorGenerator.Any
            from error2 in Gen.OneOf(ErrorGenerator.Any, ErrorGenerator.GenerateEqual(error1))
            from error3 in Gen.OneOf(ErrorGenerator.Any, ErrorGenerator.GenerateEqual(error2))
            select (error1, error2, error3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2, error3) = tuple;

            // Assert
            if (error1.Equals(error2) && error2.Equals(error3))
            {
                await Assert.That(error1.Equals(error3))
                            .IsTrue();
            }
        });
    }

    [Test]
    public async Task Equality_operator_matches_Equals()
    {
        var gen = ErrorGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var equal = error1 == error2;

            // Assert
            await Assert.That(equal)
                        .IsEqualTo(error1.Equals(error2));
        });
    }

    [Test]
    public async Task Inequality_operator_negates_equality()
    {
        var gen = ErrorGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var equality = error1 == error2;
            var inequality = error1 != error2;

            // Assert
            await Assert.That(inequality)
                        .IsEqualTo(!equality);
        });
    }

    [Test]
    public async Task Object_Equals_matches_typed_Equals()
    {
        var gen = ErrorGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var objectResult = Equals(error1, error2);
            var typedResult = error1.Equals(error2);

            // Assert
            await Assert.That(objectResult)
                        .IsEqualTo(typedResult);
        });
    }

    [Test]
    public async Task Equals_returns_false_for_null_or_other_types()
    {
        var gen =
            from error in ErrorGenerator.Any
            from other in
                Gen.OneOf(Generator.NullObject,
                          Gen.Const((Error?)null),
                          from value in Generator.Object
                          where value is not Error
                          select value)
            select (error, other);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, other) = tuple;

            // Act
            var result = error.Equals(other);

            // Assert
            await Assert.That(result)
                        .IsFalse();
        });
    }
}

public class Error_GetHashCode_Tests
{
    [Test]
    public async Task Equal_values_have_equal_hash_codes()
    {
        var gen = ErrorGenerator.EqualPair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;

            // Act
            var hash1 = error1.GetHashCode();
            var hash2 = error2.GetHashCode();

            // Assert
            await Assert.That(hash1)
                        .IsEqualTo(hash2);
        });
    }
}