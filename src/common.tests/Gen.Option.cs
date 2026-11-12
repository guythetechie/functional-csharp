using CsCheck;
using System.Threading.Tasks;

namespace common.tests;

public class OptionGenerator_Some_Tests
{
    [Test]
    public async ValueTask Generates_some_option()
    {
        var gen = OptionGenerator.Some;

        await gen.SampleAsync(async option =>
        {
            await Assert.That(option).IsSome();
        });
    }
}

public class OptionGenerator_None_Tests
{
    [Test]
    public async ValueTask Generates_none_option()
    {
        var gen = OptionGenerator.None;

        await gen.SampleAsync(async option =>
        {
            await Assert.That(option).IsNone();
        });
    }
}

public class OptionGenerator_Any_Tests
{
    [Test]
    public async ValueTask Can_generate_some_option()
    {
        var gen = OptionGenerator.Some;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(option => option.IsSome);
    }

    [Test]
    public async ValueTask Can_generate_none_option()
    {
        var gen = OptionGenerator.None;

        await Assert.That(gen)
                    .GeneratesValueSatisfying(option => option.IsNone);
    }
}