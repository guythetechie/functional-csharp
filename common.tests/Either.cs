using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace common.tests;

public class EitherTests
{
    [Fact]
    public void Left_returns_an_either_in_left_state()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            var either = Either.Left<string, int>(value);

            either.Should().BeLeft().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Right_returns_an_either_in_right_state()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var either = Either.Right<string, int>(value);

            either.Should().BeRight().Which.Should().Be(value);
        });
    }

    [Fact]
    public void IsLeft_returns_true_for_left_either()
    {
        var gen = Generator.GenerateLeftEither<string, int>(Gen.String);

        gen.Sample(either =>
        {
            either.IsLeft.Should().BeTrue();
        });
    }

    [Fact]
    public void IsLeft_returns_false_for_right_either()
    {
        var gen = Generator.GenerateRightEither<string, int>(Gen.Int);

        gen.Sample(either =>
        {
            either.IsLeft.Should().BeFalse();
        });
    }

    [Fact]
    public void IsRight_returns_false_for_left_either()
    {
        var gen = Generator.GenerateLeftEither<string, int>(Gen.String);

        gen.Sample(either =>
        {
            either.IsRight.Should().BeFalse();
        });
    }

    [Fact]
    public void IsRight_returns_true_for_right_either()
    {
        var gen = Generator.GenerateRightEither<string, int>(Gen.Int);

        gen.Sample(either =>
        {
            either.IsRight.Should().BeTrue();
        });
    }

    [Fact]
    public void Map_with_left_returns_original_left()
    {
        var gen = from leftValue in Gen.String
                  from mappedValue in Gen.String
                  select (leftValue, mappedValue);

        gen.Sample(x =>
        {
            var (leftValue, mappedValue) = x;
            var either = Either.Left<string, int>(leftValue);
            var result = either.Map(_ => mappedValue);

            result.Should().BeLeft().Which.Should().Be(leftValue);
        });
    }

    [Fact]
    public void Map_with_right_applies_function()
    {
        var gen = from either in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from mapResult in Gen.Int
                  select (either, mapResult);

        gen.Sample(x =>
        {
            var (either, mapResult) = x;
            var result = either.Map(_ => mapResult);

            result.Should().BeRight().Which.Should().Be(mapResult);
        });
    }

    [Fact]
    public void Bind_with_left_returns_original_left()
    {
        var gen = from leftValue in Gen.String
                  from bindResult in Generator.GenerateEither(Gen.String, Gen.Int)
                  select (leftValue, bindResult);

        gen.Sample(x =>
        {
            var (leftValue, bindResult) = x;
            var either = Either.Left<string, int>(leftValue);
            var result = either.Bind(_ => bindResult);

            result.Should().BeLeft().Which.Should().Be(leftValue);
        });
    }

    [Fact]
    public void Bind_with_right_applies_function()
    {
        var gen = from either in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from bindResult in Generator.GenerateEither(Gen.String, Gen.Int)
                  select (either, bindResult);

        gen.Sample(x =>
        {
            var (either, bindResult) = x;
            var result = either.Bind(_ => bindResult);

            result.Should().Be(bindResult);
        });
    }

    [Fact]
    public void Match_with_left_returns_left_function_result()
    {
        var gen = from either in Generator.GenerateLeftEither<string, int>(Gen.String)
                  from leftFunctionResult in Gen.Int
                  select (either, leftFunctionResult);

        gen.Sample(x =>
        {
            var (either, leftFunctionResult) = x;
            var result = either.Match(_ => leftFunctionResult, _ => 1);

            result.Should().Be(leftFunctionResult);
        });
    }

    [Fact]
    public void Match_with_right_returns_right_function_result()
    {
        var gen = from either in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from rightFunctionResult in Gen.Int
                  select (either, rightFunctionResult);

        gen.Sample(x =>
        {
            var (either, rightFunctionResult) = x;
            var result = either.Match(_ => 1, _ => rightFunctionResult);

            result.Should().Be(rightFunctionResult);
        });
    }

    [Fact]
    public void Match_with_left_calls_left_action()
    {
        var gen = Generator.GenerateLeftEither<string, int>(Gen.String);

        gen.Sample(either =>
        {
            var called = false;

            either.Match(_ => called = true, _ => { });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void Match_with_right_calls_right_action()
    {
        var gen = Generator.GenerateRightEither<string, int>(Gen.Int);

        gen.Sample(either =>
        {
            var called = false;

            either.Match(_ => { }, _ => called = true);

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void IfLeft_with_right_returns_right_value()
    {
        var gen = Gen.Int;

        gen.Sample(rightValue =>
        {
            var either = Either.Right<string, int>(rightValue);
            var result = either.IfLeft(_ => 1);

            result.Should().Be(rightValue);
        });
    }

    [Fact]
    public void IfLeft_with_left_returns_fallback_value()
    {
        var gen = from either in Generator.GenerateLeftEither<string, int>(Gen.String)
                  from fallbackValue in Gen.Int
                  select (either, fallbackValue);

        gen.Sample(x =>
        {
            var (either, fallbackValue) = x;
            var result = either.IfLeft(_ => fallbackValue);

            result.Should().Be(fallbackValue);
        });
    }

    [Fact]
    public void IfRight_with_left_returns_left_value()
    {
        var gen = Gen.String;

        gen.Sample(leftValue =>
        {
            var either = Either.Left<string, int>(leftValue);
            var result = either.IfRight(_ => string.Empty);

            result.Should().Be(leftValue);
        });
    }

    [Fact]
    public void IfRight_with_right_returns_fallback_value()
    {
        var gen = from either in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from fallbackValue in Gen.String
                  select (either, fallbackValue);

        gen.Sample(x =>
        {
            var (either, fallbackValue) = x;
            var result = either.IfRight(_ => fallbackValue);

            result.Should().Be(fallbackValue);
        });
    }

    [Fact]
    public void Iter_with_left_does_not_call_action()
    {
        var gen = Generator.GenerateLeftEither<string, int>(Gen.String);

        gen.Sample(either =>
        {
            var called = false;

            either.Iter(_ => called = true);

            called.Should().BeFalse();
        });
    }

    [Fact]
    public void Iter_with_right_calls_action()
    {
        var gen = Generator.GenerateRightEither<string, int>(Gen.Int);

        gen.Sample(either =>
        {
            var called = false;

            either.Iter(_ => called = true);

            called.Should().BeTrue();
        });
    }

    [Fact]
    public async Task IterTask_with_left_does_not_call_action()
    {
        var gen = Generator.GenerateLeftEither<string, int>(Gen.String);

        await gen.SampleAsync(async either =>
        {
            var called = false;

            await either.IterTask(async _ =>
            {
                await ValueTask.CompletedTask;
                called = true;
            });

            called.Should().BeFalse();
        });
    }

    [Fact]
    public async Task IterTask_with_right_calls_action()
    {
        var gen = Generator.GenerateRightEither<string, int>(Gen.Int);

        await gen.SampleAsync(async either =>
        {
            var called = false;

            await either.IterTask(async _ =>
            {
                await ValueTask.CompletedTask;
                called = true;
            });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void IfLeftThrow_with_left_throws_exception()
    {
        var gen = from either in Generator.GenerateLeftEither<string, int>(Gen.String)
                  from exceptionMessage in Gen.String
                      // .WithMessage assertion does not support empty strings
                  where string.IsNullOrWhiteSpace(exceptionMessage) is false
                  let exception = new InvalidOperationException(exceptionMessage)
                  select (either, exception);

        gen.Sample(x =>
        {
            var (either, exception) = x;

            var action = () => either.IfLeftThrow(exception);

            action.Should().Throw<Exception>().WithMessage(exception.Message).And.Should().BeOfType(exception.GetType());
        });
    }

    [Fact]
    public void IfLeftThrow_with_right_returns_right_value()
    {
        var gen = Gen.Int;

        gen.Sample(rightValue =>
        {
            var either = Either.Right<string, int>(rightValue);
            var result = either.IfLeftThrow(new UnreachableException());

            result.Should().Be(rightValue);
        });
    }

    [Fact]
    public void IfRightThrow_with_left_returns_left_value()
    {
        var gen = Gen.String;

        gen.Sample(leftValue =>
        {
            var either = Either.Left<string, int>(leftValue);
            var result = either.IfRightThrow(new UnreachableException());

            result.Should().Be(leftValue);
        });
    }

    [Fact]
    public void IfRightThrow_with_right_throws_exception()
    {
        var gen = from either in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from exceptionMessage in Gen.String
                      // .WithMessage assertion does not support empty strings
                  where string.IsNullOrWhiteSpace(exceptionMessage) is false
                  let exception = new InvalidOperationException(exceptionMessage)
                  select (either, exception);

        gen.Sample(x =>
        {
            var (either, exception) = x;

            var action = () => either.IfRightThrow(exception);

            action.Should().Throw<Exception>().WithMessage(exception.Message).And.Should().BeOfType(exception.GetType());
        });
    }

    [Fact]
    public void Equals_with_left_and_same_value_returns_true()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            var either1 = Either.Left<string, int>(value);
            var either2 = Either.Left<string, int>(value);

            var equals = either1.Equals(either2);

            equals.Should().BeTrue();
        });
    }

    [Fact]
    public void Equals_with_right_and_same_value_returns_true()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var either1 = Either.Right<string, int>(value);
            var either2 = Either.Right<string, int>(value);

            var equals = either1.Equals(either2);

            equals.Should().BeTrue();
        });
    }

    [Fact]
    public void Equals_with_left_and_different_values_returns_false()
    {
        var gen = from value1 in Gen.String
                  from value2 in Gen.String
                  where value1 != value2
                  select (value1, value2);

        gen.Sample(x =>
        {
            var (value1, value2) = x;
            var either1 = Either.Left<string, int>(value1);
            var either2 = Either.Left<string, int>(value2);

            var equals = either1.Equals(either2);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_right_and_different_values_returns_false()
    {
        var gen = from value1 in Gen.Int
                  from value2 in Gen.Int
                  where value1 != value2
                  select (value1, value2);

        gen.Sample(x =>
        {
            var (value1, value2) = x;
            var either1 = Either.Right<string, int>(value1);
            var either2 = Either.Right<string, int>(value2);

            var equals = either1.Equals(either2);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_left_and_right_returns_false()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            var leftEither = Either.Left<string, string>(value);
            var rightEither = Either.Right<string, string>(value);

            var equals = leftEither.Equals(rightEither);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void LINQ_query_syntax_with_rights_returns_right()
    {
        var gen = from either1 in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from either2 in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from innerResult in Gen.Int
                  select (either1, either2, innerResult);

        gen.Sample(x =>
        {
            var (either1, either2, innerResult) = x;

            var result = from _ in either1
                         from __ in either2
                         select innerResult;

            result.Should().BeRight().Which.Should().Be(innerResult);
        });
    }

    [Fact]
    public void LINQ_query_syntax_with_left_returns_left()
    {
        var gen = from rightEither in Generator.GenerateRightEither<string, int>(Gen.Int)
                  from leftValue in Gen.String
                  from innerResult in Gen.Int
                  select (rightEither, leftValue, innerResult);

        gen.Sample(x =>
        {
            var (rightEither, leftValue, innerResult) = x;
            var leftEither = Either.Left<string, int>(leftValue);

            var result = from _ in rightEither
                         from __ in leftEither
                         select innerResult;

            result.Should().BeLeft().Which.Should().Be(leftValue);
        });
    }
}
