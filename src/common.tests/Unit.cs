using System.Threading.Tasks;

namespace common.tests;

public class Unit_Equality_Tests
{
    [Test]
    public async Task All_Unit_values_are_equal_with_equal_hashes()
    {
        // Arrange
        var defaultUnit = default(Unit);
        var instance = Unit.Instance;
        var constructed = new Unit();

        // Assert equality
        await Assert.That(defaultUnit)
                    .IsEqualTo(instance)
                    .And
                    .IsEqualTo(constructed);

        await Assert.That(defaultUnit == instance)
                    .IsTrue();

        await Assert.That(instance == constructed)
                    .IsTrue();

        // Assert hash code equality
        await Assert.That(defaultUnit.GetHashCode())
                    .IsEqualTo(instance.GetHashCode())
                    .And
                    .IsEqualTo(constructed.GetHashCode());
    }
}

public class Unit_ToString_Tests
{
    [Test]
    public async Task Returns_expected_value()
    {
        // Act
        var text = Unit.Instance.ToString();

        // Assert
        await Assert.That(text)
                    .IsEqualTo("()");
    }
}
