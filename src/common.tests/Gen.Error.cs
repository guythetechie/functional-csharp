using CsCheck;
using System.Threading.Tasks;

namespace common.tests;

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