using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace common.tests;

public class OptionTests
{
    [Fact]
    public void Some_returns_an_option_in_the_some_state()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var option = Option.Some(value);

            // Assert
            option.Should().BeSome().Which.Should().Be(value);
            option.IsNone.Should().BeFalse();
        });
    }

    [Fact]
    public void Somes_with_equal_values_are_equal()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var option1 = Option.Some(value);
            var option2 = Option.Some(value);

            // Act
            var result = option1 == option2;

            // Assert
            result.Should().BeTrue();
        });
    }

    [Fact]
    public void Somes_with_different_values_are_not_equal()
    {
        var gen = from x in Gen.Int
                  from y in Gen.Int
                  where x != y
                  select (x, y);

        gen.Sample(pair =>
        {
            // Arrange
            var (x, y) = pair;
            var option1 = Option.Some(x);
            var option2 = Option.Some(y);

            // Act
            var result = option1 == option2;

            // Assert
            result.Should().BeFalse();
        });
    }

    [Fact]
    public void Somes_never_equal_nones()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var some = Option.Some(value);
            var none = Option<int>.None();

            // Act
            var result = some == none;

            // Assert
            result.Should().BeFalse();
        });
    }

    [Fact]
    public void Nones_are_equal()
    {
        // Arrange
        var none1 = Option<int>.None();
        var none2 = Option<int>.None();

        // Act
        var result = none1 == none2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_matches_for_matching_somes()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var option1 = Option.Some(value);
            var option2 = Option.Some(value);

            // Act
            var hash1 = option1.GetHashCode();
            var hash2 = option2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        });
    }

    [Fact]
    public void GetHashCode_for_none_should_be_zero()
    {
        var none = Option<int>.None();

        var hash = none.GetHashCode();

        hash.Should().Be(0);
    }

    [Fact]
    public void None_returns_an_option_in_the_none_state()
    {
        var option = Option<int>.None();

        // Assert
        option.Should().BeNone();
        option.IsSome.Should().BeFalse();
    }

    [Fact]
    public void Match_with_some_returns_selector_result()
    {
        var gen = from value in Gen.Int
                  from selector in Generator.MapSelector
                  select (value, selector);

        gen.Sample(x =>
        {
            // Arrange
            var (value, selector) = x;
            var option = Option.Some(value);

            // Act
            var result = option.Match(selector, () => -1);

            // Assert
            result.Should().Be(selector(value));
        });
    }

    [Fact]
    public void Match_with_none_returns_default()
    {
        var gen = from selector in Generator.MapSelector
                  from defaultValue in Gen.Int
                  select (selector, defaultValue);

        gen.Sample(x =>
        {
            // Arrange
            var (selector, defaultValue) = x;
            var option = Option<int>.None();

            // Act
            var result = option.Match(selector, () => defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        });
    }

    [Fact]
    public void Match_with_some_executes_some_action()
    {
        var gen = from value in Gen.Int
                  from someSelector in Generator.MapSelector
                  from noneSelector in Generator.MapSelector
                  select (value, someSelector, noneSelector);

        gen.Sample(x =>
        {
            // Arrange
            var (value, someSelector, noneSelector) = x;
            var (someExecuted, noneExecuted) = (false, false);
            var (someSelected, noneSelected) = (0, 0);
            var option = Option.Some(value);

            // Act
            option.Match(value =>
            {
                someExecuted = true;
                someSelected = someSelector(value);
            },
            () =>
            {
                noneExecuted = true;
                noneSelected = noneSelector(value);
            });

            // Assert
            someExecuted.Should().BeTrue();
            someSelected.Should().Be(someSelector(value));
            noneExecuted.Should().BeFalse();
            noneSelected.Should().Be(0);
        });
    }

    [Fact]
    public void Match_with_none_executes_none_action()
    {
        var gen = from value in Gen.Int
                  from someSelector in Generator.MapSelector
                  from noneSelector in Generator.MapSelector
                  select (value, someSelector, noneSelector);

        gen.Sample(x =>
        {
            // Arrange
            var (value, someSelector, noneSelector) = x;
            var (someExecuted, noneExecuted) = (false, false);
            var (someSelected, noneSelected) = (0, 0);
            var option = Option<int>.None();

            // Act
            option.Match(i =>
            {
                someExecuted = true;
                someSelected = someSelector(i);
            },
            () =>
            {
                noneExecuted = true;
                noneSelected = noneSelector(value);
            });

            // Assert
            someExecuted.Should().BeFalse();
            someSelected.Should().Be(0);
            noneExecuted.Should().BeTrue();
            noneSelected.Should().Be(noneSelector(value));
        });
    }

    [Fact]
    public void ToString_with_some_contains_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var option = Option.Some(value);

            option.ToString().Should().Contain(value.ToString());
        });
    }

    [Fact]
    public void ToString_with_none_displays_none()
    {
        var option = Option<int>.None();

        option.ToString().Should().Be("None");
    }

    [Fact]
    public void Implicit_operator_works_for_none()
    {
        // Act
        Option<int> option = Option.None;

        // Assert
        option.Should().BeNone();
    }

    [Fact]
    public void Option_satisfies_monad_left_identity()
    {
        var gen = from value in Gen.Int
                  from selector in Generator.OptionSelector
                  select (value, selector);

        gen.Sample(x =>
        {
            // Arrange
            var (value, selector) = x;
            var option = Option.Some(value);

            // Act
            var result = option.Bind(selector);

            // Assert
            result.Should().Be(selector(value));
        });
    }

    [Fact]
    public void Option_satisfies_monad_right_identity()
    {
        var gen = Generator.Option;

        gen.Sample(option =>
        {
            var result = option.Bind(Option.Some);

            result.Should().Be(option);
        });
    }

    [Fact]
    public void Option_satisfies_monad_associativity()
    {
        var gen = from option in Generator.Option
                  from selector1 in Generator.OptionSelector
                  from selector2 in Generator.OptionSelector
                  select (option, selector1, selector2);

        gen.Sample(x =>
        {
            // Arrange
            var (option, selector1, selector2) = x;

            // Act
            var path1 = option.Bind(selector1)
                              .Bind(selector2);

            var path2 = option.Bind(x => selector1(x).Bind(selector2));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void Map_is_equivalent_to_bind_then_return()
    {
        var gen = from option in Generator.Option
                  from selector in Generator.MapSelector
                  select (option, selector);

        gen.Sample(x =>
        {
            // Arrange
            var (option, selector) = x;

            // Act
            var path1 = option.Map(selector);
            var path2 = option.Bind(x => Option.Some(selector(x)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void Where_is_a_monadic_guard()
    {
        var gen = from option in Generator.Option
                  from predicate in Generator.Predicate
                  select (option, predicate);

        gen.Sample(x =>
        {
            // Arrange
            var (option, predicate) = x;

            // Act
            var path1 = option.Where(predicate);
            var path2 = option.Bind(x => predicate(x) ? Option.Some(x) : Option.None);

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public async Task MapTask_lifts_synchronous_Map()
    {
        var gen = from option in Generator.Option
                  from selector in Generator.MapSelector
                  select (option, selector);

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var (option, selector) = x;

            // Act
            var path1 = option.Map(selector);
            var path2 = await option.MapTask(i => ValueTask.FromResult(selector(i)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public async Task BindTask_lifts_synchronous_Bind()
    {
        var gen = from option in Generator.Option
                  from selector in Generator.OptionSelector
                  select (option, selector);

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var (option, selector) = x;

            // Act
            var path1 = option.Bind(selector);
            var path2 = await option.BindTask(i => ValueTask.FromResult(selector(i)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void IfNone_with_some_returns_the_value()
    {
        var gen = from value in Gen.Int
                  from alternative in Gen.Int
                  select (value, alternative);

        gen.Sample(x =>
        {
            // Arrange
            var (value, alternative) = x;
            var option = Option.Some(value);
            var alternativeExecuted = false;
            var alternativeF = () =>
            {
                alternativeExecuted = true;
                return alternative;
            };

            // Act
            var result = option.IfNone(alternativeF);

            // Assert
            result.Should().Be(value);
            alternativeExecuted.Should().BeFalse();
        });
    }

    [Fact]
    public void IfNone_with_none_returns_the_alternative()
    {
        var gen = Gen.Int;

        gen.Sample(alternative =>
        {
            // Arrange
            var option = Option<int>.None();

            // Act
            var result = option.IfNone(() => alternative);

            // Assert
            result.Should().Be(alternative);
        });
    }

    [Fact]
    public void IfNone_with_some_ignores_option_fallback()
    {
        var gen = from value in Gen.Int
                  from alternative in Gen.Int
                  select (value, alternative);

        gen.Sample(x =>
        {
            // Arrange
            var (value, alternative) = x;
            var option = Option.Some(value);
            var alternativeExecuted = false;
            var alternativeF = () =>
            {
                alternativeExecuted = true;
                return Option.Some(alternative);
            };

            // Act
            var result = option.IfNone(alternativeF);

            // Assert
            result.Should().Be(value);
            alternativeExecuted.Should().BeFalse();
        });
    }

    [Fact]
    public void IfNone_with_none_returns_option_fallback()
    {
        var gen = Generator.Option;

        gen.Sample(alternativeOption =>
        {
            // Arrange
            var option = Option<int>.None();

            // Act
            var result = option.IfNone(() => alternativeOption);

            // Assert
            result.Should().Be(alternativeOption);
        });
    }

    [Fact]
    public void IfNoneThrow_with_some_returns_the_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);
            var exceptionFactoryCalled = false;
            var exceptionFactory = () =>
            {
                exceptionFactoryCalled = true;
                return new InvalidOperationException("should not be called");
            };

            // Act
            var result = option.IfNoneThrow(exceptionFactory);

            // Assert
            result.Should().Be(value);
            exceptionFactoryCalled.Should().BeFalse();
        });
    }

    [Fact]
    public void IfNoneThrow_with_none_throws_the_exception()
    {
        var gen = from message in Gen.String
                  where string.IsNullOrWhiteSpace(message) is false
                  select message;

        gen.Sample(message =>
        {
            // Arrange
            var option = Option<int>.None();
            var exceptionFactory = () => new InvalidOperationException(message);

            // Act
            var action = () => option.IfNoneThrow(exceptionFactory);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                  .WithMessage(message);
        });
    }

    [Fact]
    public void IfNoneNull_with_some_returns_the_value()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);

            // Act
            var result = option.IfNoneNull();

            // Assert
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNull_with_none_returns_null()
    {
        var option = Option<string>.None();

        // Act
        var result = option.IfNoneNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IfNoneNullable_with_some_returns_the_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);

            // Act
            var result = option.IfNoneNullable();

            // Assert
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNullable_with_none_returns_null()
    {
        var option = Option<int>.None();

        // Act
        var result = option.IfNoneNullable();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Iter_with_some_executes_action()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);

            var counter = 0;
            var action = (int value) =>
            {
                counter = value;
            };

            // Act
            option.Iter(action);

            // Assert
            counter.Should().Be(value);
        });
    }

    [Fact]
    public void Iter_with_none_does_not_execute_action()
    {
        var option = Option<int>.None();

        var counter = 0;
        var action = (int _) =>
        {
            counter++;
        };

        // Act
        option.Iter(action);

        // Assert
        counter.Should().Be(0);
    }

    [Fact]
    public async Task IterTask_with_some_executes_action()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            var counter = 0;
            async ValueTask action(int value)
            {
                await ValueTask.CompletedTask;
                counter = value;
            }

            // Act
            await option.IterTask(action);

            // Assert
            counter.Should().Be(value);
        });
    }

    [Fact]
    public async Task IterTask_with_none_does_not_execute_action()
    {
        var option = Option<int>.None();

        var counter = 0;
        async ValueTask action(int value)
        {
            await ValueTask.CompletedTask;
            counter = value;
        }

        // Act
        await option.IterTask(action);

        // Assert
        counter.Should().Be(0);
    }

    [Fact]
    public void Implicit_operator_works_for_some()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Act
            Option<int> option = value;

            // Assert
            option.Should().BeSome().Which.Should().Be(value);
        });
    }
}