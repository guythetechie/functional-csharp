using CsCheck;
using System.Threading.Tasks;

namespace common.tests;

public class ResultGenerator_Success_Tests
{
    [Test]
    public async ValueTask Generates_success_result()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            await Assert.That(result).IsSuccess();
        });
    }
}

public class ResultGenerator_Error_Tests
{
    [Test]
    public async ValueTask Generates_error_result()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            await Assert.That(result).IsError();
        });
    }
}

public class ResultGenerator_Any_Tests
{
    [Test]
    public async ValueTask Can_generate_success_result()
    {
        var gen = ResultGenerator.Success;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(result => result.IsSuccess);
    }

    [Test]
    public async ValueTask Can_generate_error_result()
    {
        var gen = ResultGenerator.Error;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(result => result.IsError);
    }
}