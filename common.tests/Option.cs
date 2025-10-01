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
    public void None_returns_an_option_in_the_none_state()
    {
        var option = Option<int>.None();

        // Assert
        option.Should().BeNone();
        option.IsSome.Should().BeFalse();
    }

    [Fact]
    public void Equality_is_reflexive()
    {
        var gen = Generator.Option;

        gen.Sample(monad =>
        {
            // Assert
            monad.Equals(monad).Should().BeTrue();
#pragma warning disable CS1718 // Comparison made to same variable
            (monad == monad).Should().BeTrue();
#pragma warning restore CS1718 // Comparison made to same variable
        });
    }

    [Fact]
    public void Equality_is_symmetric()
    {
        var gen = from value1 in Gen.Int
                  from option1 in Gen.OneOfConst(Option.Some(value1),
                                                 Option.None)
                  from value2 in Gen.OneOf(Gen.Const(value1), Gen.Int)
                  from option2 in Gen.OneOfConst(Option.Some(value2),
                                                 Option.None)
                  select (option1, option2);

        gen.Sample(tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Assert
            (option1.Equals(option2)).Should().Be(option2.Equals(option1));
            (option1 == option2).Should().Be(option2 == option1);
        });
    }

    [Fact]
    public void Equality_is_transitive()
    {
        var gen = from value1 in Gen.Int
                  from option1 in Gen.OneOfConst(Option.Some(value1),
                                                 Option.None)
                  from value2 in Gen.OneOf(Gen.Const(value1), Gen.Int)
                  from option2 in Gen.OneOfConst(Option.Some(value2),
                                                 Option.None)
                  from value3 in Gen.OneOf(Gen.Const(value1), Gen.Const(value2), Gen.Int)
                  from option3 in Gen.OneOfConst(Option.Some(value3),
                                                 Option.None)
                  select (option1, option2, option3);

        gen.Sample(tuple =>
        {
            // Arrange
            var (option1, option2, option3) = tuple;

            // Assert
            if (option1.Equals(option2) && option2.Equals(option3))
            {
                option1.Equals(option3).Should().BeTrue();
                (option1 == option3).Should().BeTrue();
            }
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

            // Assert
            option1.Equals(option2).Should().BeTrue();
            (option1 == option2).Should().BeTrue();
            option1.GetHashCode().Should().Be(option2.GetHashCode());
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

            // Assert
            option1.Equals(option2).Should().BeFalse();
            (option1 != option2).Should().BeTrue();
        });
    }

    [Fact]
    public void Nones_are_equal()
    {
        // Arrange
        var none1 = Option<int>.None();
        var none2 = Option<int>.None();

        // Assert
        none1.Equals(none2).Should().BeTrue();
        (none1 == none2).Should().BeTrue();
        none1.GetHashCode().Should().Be(none2.GetHashCode());
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

            // Assert
            some.Equals(none).Should().BeFalse();
            (some == none).Should().BeFalse();
        });
    }

    [Fact]
    public void Can_implicitly_convert_value_to_option()
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

    [Fact]
    public void Can_implicitly_convert_none_to_option()
    {
        // Act
        Option<int> option = Option.None;

        // Assert
        option.Should().BeNone();
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
    public void Match_with_some_returns_some_function()
    {
        var gen = from x in Gen.Int
                  from f in Generator.IntToString
                  select (x, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var monad = Option.Some(x);

            var noneFunctionRan = false;
            string g()
            {
                noneFunctionRan = true;
                return string.Empty;
            }

            // Act
            var result = monad.Match(f, g);

            // Assert
            result.Should().Be(f(x));
            noneFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_none_returns_default()
    {
        var gen = from x in Gen.String
                  select (Func<string>)(() => x);

        gen.Sample(g =>
        {
            // Arrange
            var monad = Option<int>.None();

            var someFunctionRan = false;
            string f(int _)
            {
                someFunctionRan = true;
                return string.Empty;
            }

            // Act
            var result = monad.Match(f, g);

            // Assert
            result.Should().Be(g());
            someFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_some_executes_some_action()
    {
        var gen = from x in Gen.Int
                  from f1 in Generator.IntToString
                  select (x, f1);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, f1) = tuple;
            var monad = Option.Some(x);

            var actionedValue = string.Empty;
            void f(int value) => actionedValue += f1(value);

            bool noneActionRan = false;
            void g() => noneActionRan = true;

            // Act
            monad.Match(f, g);

            // Assert
            actionedValue.Should().Be(f1(x));
            noneActionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_none_executes_none_action()
    {
        // Arrange
        var monad = Option<int>.None();

        bool someActionRan = false;
        void f(int _) => someActionRan = true;

        int noneCounter = 0;
        void g() => noneCounter++;

        // Act
        monad.Match(f, g);

        // Assert
        someActionRan.Should().BeFalse();
        noneCounter.Should().Be(1);
    }

    [Fact]
    public void Option_satisfies_monad_left_identity()
    {
        var gen = from x in Gen.Int
                  from f in Generator.IntToStringOption
                  select (x, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var monad = Option.Some(x);

            // Act
            var result = monad.Bind(f);

            // Assert
            result.Should().Be(f(x));
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
        var gen = from monad in Generator.Option
                  from f in Generator.IntToStringOption
                  from g in Generator.StringToIntOption
                  select (monad, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (monad, f, g) = tuple;

            // Act
            var path1 = monad.Bind(f)
                             .Bind(g);

            var path2 = monad.Bind(x => f(x).Bind(g));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void Option_satisfies_alternative_left_identity()
    {
        var gen = Generator.Option;

        gen.Sample(option =>
        {
            // Arrange
            var monad = Option<int>.None();
            var f = () => option;

            // Act
            var result = monad.IfNone(f);

            // Assert
            result.Should().Be(option);
        });
    }

    [Fact]
    public void Option_satisfies_alternative_right_identity()
    {
        var gen = Generator.Option;

        gen.Sample(monad =>
        {
            // Arrange
            var f = () => Option<int>.None();

            // Act
            var result = monad.IfNone(f);

            // Assert
            result.Should().Be(monad);
        });
    }

    [Fact]
    public void Option_satisfies_alternative_associativity()
    {
        var gen = from x in Generator.Option
                  from y in Generator.Option
                  from z in Generator.Option
                  select (x, y, z);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, y, z) = tuple;

            // Act
            var path1 = x.IfNone(() => y)
                         .IfNone(() => z);

            var path2 = x.IfNone(() => y.IfNone(() => z));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void Option_satisfies_monad_plus_left_zero()
    {
        var gen = Generator.IntToStringOption;

        gen.Sample(f =>
        {
            // Arrange
            var monad = Option<int>.None();

            // Act
            var result = monad.Bind(f);

            // Assert
            var expected = Option<string>.None();
            result.Should().Be(expected);
        });
    }

    [Fact]
    public void Map_is_equivalent_to_bind_then_return()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToString
                  select (monad, f);

        gen.Sample(x =>
        {
            // Arrange
            var (monad, f) = x;

            // Act
            var path1 = monad.Map(f);
            var path2 = monad.Bind(x => Option.Some(f(x)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void LINQ_Select_is_syntactic_sugar_for_map()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToString
                  select (monad, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (monad, f) = tuple;

            // Act
            var path1 = from x in monad
                        select f(x);

            var path2 = monad.Select(f);

            var path3 = monad.Map(f);

            // Assert
            path1.Should().Be(path2).And.Be(path3);
        });
    }

    [Fact]
    public void LINQ_SelectMany_is_syntactic_sugar_for_bind()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToStringOption
                  from g1 in Generator.IntToString
                  from g2 in Generator.StringToInt
                  let g = (Func<int, string, int>)((int x, string y) => g2(g1(x) + y))
                  select (monad, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (monad, f, g) = tuple;

            // Act
            var path1 = from x in monad
                        from y in f(x)
                        select g(x, y);

            var path2 = monad.SelectMany(f, g);

            var path3 = monad.Bind(x => f(x).Map(y => g(x, y)));

            // Assert
            path1.Should().Be(path2).And.Be(path3);
        });
    }

    [Fact]
    public async Task MapTask_handles_asynchronous_map()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToString
                  select (monad, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (monad, f) = tuple;

            // Act
            var path1 = monad.Map(f);
            var path2 = await monad.MapTask(x => ValueTask.FromResult(f(x)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public async Task BindTask_handles_asynchronous_bind()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToStringOption
                  select (monad, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (monad, f) = tuple;

            // Act
            var path1 = monad.Bind(f);
            var path2 = await monad.BindTask(x => ValueTask.FromResult(f(x)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public async Task BindTask_is_associative()
    {
        var gen = from monad in Generator.Option
                  from f in Generator.IntToStringOptionTask
                  from g in Generator.StringToIntOptionTask
                  select (monad, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (monad, f, g) = tuple;

            // Act
            var path1 = await (await monad.BindTask(f))
                                          .BindTask(g);

            var path2 = await monad.BindTask(async x => await (await f(x)).BindTask(g));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void Where_is_a_monadic_guard()
    {
        var gen = from option in Generator.Option
                  from predicate in Generator.IntPredicate
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
    public void IfNoneThrow_with_some_returns_the_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Option.Some(value);

            var exceptionFactoryCalled = false;
            var f = () =>
            {
                exceptionFactoryCalled = true;
                return new InvalidOperationException("should not be called");
            };

            // Act
            var result = monad.IfNoneThrow(f);

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
            var monad = Option<int>.None();
            var f = () => new InvalidOperationException(message);

            // Act
            var action = () => monad.IfNoneThrow(f);

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
            var monad = Option.Some(value);

            // Act
            var result = monad.IfNoneNull();

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
            var monad = Option.Some(value);

            // Act
            var result = monad.IfNoneNullable();

            // Assert
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNullable_with_none_returns_null()
    {
        var monad = Option<int>.None();

        // Act
        var result = monad.IfNoneNullable();

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
            var monad = Option.Some(value);

            var actionedValue = 0;
            void f(int value) => actionedValue += value;

            // Act
            monad.Iter(f);

            // Assert
            actionedValue.Should().Be(value);
        });
    }

    [Fact]
    public void Iter_with_none_does_not_execute_action()
    {
        var monad = Option<int>.None();

        var actionedValue = 0;
        void f(int value) => actionedValue += value;

        // Act
        monad.Iter(f);

        // Assert
        actionedValue.Should().Be(0);
    }

    [Fact]
    public async Task IterTask_with_some_executes_action()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            var actionedValue = 0;
            async ValueTask f(int x)
            {
                await ValueTask.CompletedTask;
                actionedValue += x;
            }

            // Act
            await option.IterTask(f);

            // Assert
            actionedValue.Should().Be(value);
        });
    }

    [Fact]
    public async Task IterTask_with_none_does_not_execute_action()
    {
        var option = Option<int>.None();

        var actionedValue = 0;
        async ValueTask f(int value)
        {
            await ValueTask.CompletedTask;
            actionedValue += value;
        }

        // Act
        await option.IterTask(f);

        // Assert
        actionedValue.Should().Be(0);
    }
}