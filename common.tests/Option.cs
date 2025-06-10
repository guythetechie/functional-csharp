using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace common.tests;

public class OptionTests
{
    [Fact]
    public void Some_returns_an_option_in_some_state()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            var option = Option.Some(value);

            option.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void None_returns_an_option_in_none_state()
    {
        Option<string> option = Option.None;

        option.Should().BeNone();
    }

    [Fact]
    public void IsSome_with_some_returns_true()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            option.IsSome.Should().BeTrue();
        });
    }

    [Fact]
    public void IsSome_with_none_returns_false()
    {
        Option<int> option = Option.None;

        option.IsSome.Should().BeFalse();
    }

    [Fact]
    public void IsNone_with_some_returns_false()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            option.IsNone.Should().BeFalse();
        });
    }

    [Fact]
    public void IsNone_with_none_returns_true()
    {
        Option<int> option = Option.None;

        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Map_with_some_applies_function()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from mapResult in Gen.Int
                  select (option, mapResult);

        gen.Sample(x =>
        {
            var (option, mapResult) = x;
            var result = option.Map(_ => mapResult);

            result.Should().BeSome().Which.Should().Be(mapResult);
        });
    }

    [Fact]
    public void Map_with_none_returns_none()
    {
        Option<int> option = Option.None;
        var result = option.Map(x => x * 2);

        result.Should().BeNone();
    }

    [Fact]
    public async Task MapTask_with_some_applies_function()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from mapResult in Gen.Int
                  select (option, mapResult);

        await gen.SampleAsync(async x =>
        {
            var (option, mapResult) = x;

            var result = await option.MapTask(_ => ValueTask.FromResult(mapResult));

            result.Should().BeSome().Which.Should().Be(mapResult);
        });
    }

    [Fact]
    public async Task MapTask_with_none_returns_none()
    {
        Option<int> option = Option.None;

        var result = await option.MapTask(x => ValueTask.FromResult(x * 2));

        result.Should().BeNone();
    }

    [Fact]
    public void Bind_with_some_applies_function()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from bindResult in Generator.GenerateOption(Gen.String)
                  select (option, bindResult);

        gen.Sample(x =>
        {
            var (option, bindResult) = x;
            var result = option.Bind(_ => bindResult);

            result.Should().Be(bindResult);
        });
    }

    [Fact]
    public void Bind_with_none_returns_none()
    {
        var gen = Generator.GenerateOption(Gen.Int);

        gen.Sample(bindResult =>
        {
            Option<int> option = Option.None;
            var result = option.Bind(x => bindResult);
            result.Should().BeNone();
        });
    }

    [Fact]
    public async Task BindTask_with_some_applies_function()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from bindResult in Generator.GenerateOption(Gen.String)
                  select (option, bindResult);

        await gen.SampleAsync(async x =>
        {
            var (option, bindResult) = x;
            var result = await option.BindTask(_ => ValueTask.FromResult(bindResult));

            result.Should().Be(bindResult);
        });
    }

    [Fact]
    public async Task BindTask_with_none_returns_none()
    {
        var gen = Generator.GenerateOption(Gen.Int);

        await gen.SampleAsync(async bindResult =>
        {
            Option<int> option = Option.None;
            var result = await option.BindTask(x => ValueTask.FromResult(bindResult));
            result.Should().BeNone();
        });
    }

    [Fact]
    public void Where_with_some_and_true_predicate_returns_some()
    {
        var gen = from value in Gen.Int
                  select value;

        gen.Sample(value =>
        {
            var option = Option.Some(value);
            var result = option.Where(x => true);
            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Where_with_some_and_false_predicate_returns_none()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            var result = option.Where(x => false);
            result.Should().BeNone();
        });
    }

    [Fact]
    public void Where_with_none_and_true_predicate_returns_none()
    {
        Option<int> option = Option.None;

        var result = option.Where(x => true);

        result.Should().BeNone();
    }

    [Fact]
    public void Where_with_none_and_false_predicate_returns_none()
    {
        Option<int> option = Option.None;

        var result = option.Where(x => false);

        result.Should().BeNone();
    }

    [Fact]
    public void Match_with_some_returns_some_function_result()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from someFunctionResult in Gen.Int
                  select (option, someFunctionResult);

        gen.Sample(x =>
        {
            var (option, someFunctionResult) = x;
            var result = option.Match(_ => someFunctionResult, () => 1);

            result.Should().Be(someFunctionResult);
        });
    }

    [Fact]
    public void Match_with_none_returns_none_function_result()
    {
        var gen = Gen.Int;

        gen.Sample(noneFunctionResult =>
        {
            Option<int> option = Option.None;
            var result = option.Match(_ => 1, () => noneFunctionResult);

            result.Should().Be(noneFunctionResult);
        });
    }

    [Fact]
    public void Match_with_some_calls_some_action()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            var called = false;
            option.Match(_ => called = true, () => { });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void Match_with_none_calls_none_action()
    {
        Option<int> option = Option.None;
        var called = false;

        option.Match(_ => { }, () => called = true);

        called.Should().BeTrue();
    }

    [Fact]
    public void IfNone_with_value_func_and_some_returns_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var option = Option.Some(value);
            var result = option.IfNone(() => 10);
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNone_with_value_func_and_none_returns_fallback()
    {
        var gen = Gen.Int;

        gen.Sample(fallback =>
        {
            Option<int> option = Option.None;
            var result = option.IfNone(() => fallback);
            result.Should().Be(fallback);
        });
    }

    [Fact]
    public void IfNone_with_option_func_and_some_returns_value()
    {
        var gen = from value in Gen.Int
                  from fallback in Generator.GenerateOption(Gen.Int)
                  select (value, fallback);

        gen.Sample(x =>
        {
            var (value, fallback) = x;
            var option = Option.Some(value);

            var result = option.IfNone(() => fallback);

            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void IfNone_with_option_func_and_none_returns_fallback()
    {
        var gen = Generator.GenerateOption(Gen.Int);

        gen.Sample(fallback =>
        {
            Option<int> option = Option.None;

            var result = option.IfNone(() => fallback);

            result.Should().Be(fallback);
        });
    }

    [Fact]
    public void IfNoneThrow_with_some_returns_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var option = Option.Some(value);
            var result = option.IfNoneThrow(() => new UnreachableException());
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNull_with_some_returns_value()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            Option<string> option = Option.Some(value);

            var result = option.IfNoneNull();

            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNull_with_none_returns_null()
    {
        Option<string> option = Option.None;

        var result = option.IfNoneNull();

        result.Should().BeNull();
    }

    [Fact]
    public void IfNoneNullable_with_some_returns_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            Option<int> option = Option.Some(value);

            var result = option.IfNoneNullable();

            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfNoneNullable_with_none_returns_null()
    {
        Option<int> option = Option.None;

        var result = option.IfNoneNullable();

        result.Should().BeNull();
    }

    [Fact]
    public void IfNoneThrow_with_none_throws_exception()
    {
        var gen = from exceptionMessage in Gen.String
                      // .WithMessage assertion does not support empty strings
                  where string.IsNullOrWhiteSpace(exceptionMessage) is false
                  select new InvalidOperationException(exceptionMessage);

        gen.Sample(exception =>
        {
            Option<int> option = Option.None;

            var action = () => option.IfNoneThrow(() => exception);

            action.Should().Throw<Exception>().WithMessage(exception.Message).And.Should().BeOfType(exception.GetType());
        });
    }

    [Fact]
    public void Iter_with_some_calls_action()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            var called = false;
            option.Iter(_ => called = true);

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void Iter_with_none_does_not_call_action()
    {
        Option<int> option = Option.None;
        var called = false;

        option.Iter(x => called = true);

        called.Should().BeFalse();
    }

    [Fact]
    public async Task IterTask_with_some_calls_action()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        await gen.SampleAsync(async option =>
        {
            var called = false;

            await option.IterTask(async _ =>
            {
                await ValueTask.CompletedTask;
                called = true;
            });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public async Task IterTask_with_none_does_not_call_action()
    {
        Option<int> option = Option.None;
        var called = false;

        await option.IterTask(async x =>
        {
            await ValueTask.CompletedTask;
            called = true;
        });

        called.Should().BeFalse();
    }

    [Fact]
    public void Implicit_conversion_from_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            Option<int> option = value;
            option.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Implicit_conversion_from_none()
    {
        Option<int> option = Option.None;

        option.Should().BeNone();
    }

    [Fact]
    public void Equals_with_some_and_same_value_returns_true()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var option1 = Option.Some(value);
            var option2 = Option.Some(value);

            var equals = option1.Equals(option2);

            equals.Should().BeTrue();
        });
    }

    [Fact]
    public void Equals_with_some_and_different_values_returns_false()
    {
        var gen = from value1 in Gen.Int
                  from value2 in Gen.Int
                  where value1 != value2
                  select (value1, value2);

        gen.Sample(x =>
        {
            var (value1, value2) = x;
            var option1 = Option.Some(value1);
            var option2 = Option.Some(value2);

            var equals = option1.Equals(option2);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_some_and_none_returns_false()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            Option<int> noneOption = Option.None;

            var equals = option.Equals(noneOption);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_none_and_none_returns_true()
    {
        Option<int> option1 = Option.None;
        Option<int> option2 = Option.None;

        var equals = option1.Equals(option2);

        equals.Should().BeTrue();
    }

    [Fact]
    public void LINQ_query_syntax_with_somes_returns_some()
    {
        var gen = from option1 in Generator.GenerateSomeOption(Gen.Int)
                  from option2 in Generator.GenerateSomeOption(Gen.Int)
                  from innerResult in Gen.Int
                  select (option1, option2, innerResult);

        gen.Sample(x =>
        {
            var (option1, option2, innerResult) = x;

            var result = from _ in option1
                         from __ in option2
                         select innerResult;

            result.Should().BeSome().Which.Should().Be(innerResult);
        });
    }

    [Fact]
    public void LINQ_query_syntax_with_none_returns_none()
    {
        var gen = from option in Generator.GenerateSomeOption(Gen.Int)
                  from innerResult in Gen.Int
                  select (option, innerResult);

        gen.Sample(x =>
        {
            var (option, innerResult) = x;

            var result = from _ in option
                         from __ in Option<int>.None()
                         select innerResult;

            result.Should().BeNone();
        });
    }

    [Fact]
    public void LINQ_query_syntax_where_with_some_and_true_predicate_returns_some()
    {
        var gen = from value in Gen.Int
                  select value;

        gen.Sample(value =>
        {
            var option = Option.Some(value);

            var result = from _ in option
                         where true
                         select value;

            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void LINQ_query_syntax_where_with_some_and_false_predicate_returns_none()
    {
        var gen = Generator.GenerateSomeOption(Gen.Int);

        gen.Sample(option =>
        {
            var result = from value in option
                         where false
                         select value;

            result.Should().BeNone();
        });
    }

    [Fact]
    public void LINQ_query_syntax_where_with_none_and_true_predicate_returns_none()
    {
        Option<int> option = Option.None;

        var result = from value in option
                     where true
                     select value;

        result.Should().BeNone();
    }

    [Fact]
    public void LINQ_query_syntax_where_with_none_and_false_predicate_returns_none()
    {
        Option<int> option = Option.None;

        var result = from value in option
                     where false
                     select value;

        result.Should().BeNone();
    }
}