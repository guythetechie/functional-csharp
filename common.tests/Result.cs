using common;
using CsCheck;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace common.tests;

public class ResultTests
{
    [Fact]
    public void Success_returns_a_result_in_success_state()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = Result.Success(value);

            result.Should().BeSuccess().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Error_returns_a_result_in_error_state()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            var result = Result.Error<int>(error);

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void IsSuccess_returns_true_for_success_result()
    {
        var gen = Generator.GenerateSuccessResult(Gen.Int);

        gen.Sample(result =>
        {
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public void IsSuccess_returns_false_for_error_result()
    {
        var gen = Generator.GenerateErrorResult<int>();

        gen.Sample(result =>
        {
            result.IsSuccess.Should().BeFalse();
        });
    }

    [Fact]
    public void IsError_returns_false_for_success_result()
    {
        var gen = Generator.GenerateSuccessResult(Gen.Int);

        gen.Sample(result =>
        {
            result.IsError.Should().BeFalse();
        });
    }

    [Fact]
    public void IsError_returns_true_for_error_result()
    {
        var gen = Generator.GenerateErrorResult<int>();

        gen.Sample(result =>
        {
            result.IsError.Should().BeTrue();
        });
    }

    [Fact]
    public void Map_with_error_returns_original_error()
    {
        var gen = from error in Generator.Error
                  from mapResult in Gen.String
                  select (error, mapResult);

        gen.Sample(x =>
        {
            var (error, mapResult) = x;
            var errorResult = Result.Error<int>(error);
            var result = errorResult.Map(_ => mapResult);

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void Map_with_success_applies_function()
    {
        var gen = from successResult in Generator.GenerateSuccessResult(Gen.Int)
                  from mapResult in Gen.String
                  select (successResult, mapResult);

        gen.Sample(x =>
        {
            var (successResult, mapResult) = x;
            var result = successResult.Map(_ => mapResult);

            result.Should().BeSuccess().Which.Should().Be(mapResult);
        });
    }

    [Fact]
    public void MapError_with_success_returns_success_value()
    {
        var gen = Gen.Int;

        gen.Sample(successValue =>
        {
            var result = Result.Success(successValue);
            var mapped = result.MapError(_ => Error.From("Some error"));

            mapped.Should().BeSuccess().Which.Should().Be(successValue);
        });
    }

    [Fact]
    public void MapError_with_error_applies_function()
    {
        var gen = from errorResult in Generator.GenerateErrorResult<int>()
                  from newError in Generator.Error
                    select (errorResult, newError);

        gen.Sample(x =>
        {
            var (errorResult, newError) = x;
            var result = errorResult.MapError(_ => newError);

            result.Should().BeError().Which.Should().Be(newError);
        });
    }

    [Fact]
    public void Bind_with_error_returns_original_error()
    {
        var gen = from error in Generator.Error
                  from bindResult in Generator.GenerateResult(Gen.String)
                  select (error, bindResult);

        gen.Sample(x =>
        {
            var (error, bindResult) = x;
            var errorResult = Result.Error<int>(error);

            var result = errorResult.Bind(_ => bindResult);

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void Bind_with_success_applies_function()
    {
        var gen = from successResult in Generator.GenerateSuccessResult(Gen.Int)
                  from bindResult in Generator.GenerateResult(Gen.String)
                  select (successResult, bindResult);

        gen.Sample(x =>
        {
            var (successResult, bindResult) = x;

            var result = successResult.Bind(_ => bindResult);

            result.Should().Be(bindResult);
        });
    }

    [Fact]
    public void Match_with_error_returns_error_function_result()
    {
        var gen = from errorResult in Generator.GenerateErrorResult<int>()
                  from errorFunctionResult in Gen.String
                  select (errorResult, errorFunctionResult);

        gen.Sample(x =>
        {
            var (errorResult, errorFunctionResult) = x;
            var result = errorResult.Match(_ => string.Empty, _ => errorFunctionResult);

            result.Should().Be(errorFunctionResult);
        });
    }

    [Fact]
    public void Match_with_success_returns_success_function_result()
    {
        var gen = from successValue in Gen.Int
                  from successFunctionResult in Gen.String
                  select (successValue, successFunctionResult);

        gen.Sample(x =>
        {
            var (successValue, successFunctionResult) = x;
            var result = Result.Success(successValue);
            var matched = result.Match(_ => successFunctionResult, _ => string.Empty);

            matched.Should().Be(successFunctionResult);
        });
    }

    [Fact]
    public void Match_with_error_calls_error_action()
    {
        var gen = Generator.GenerateErrorResult<int>();

        gen.Sample(errorResult =>
        {
            var called = false;

            errorResult.Match(_ => { }, _ => called = true);

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void Match_with_success_calls_success_action()
    {
        var gen = Generator.GenerateSuccessResult(Gen.Int);

        gen.Sample(result =>
        {
            var called = false;

            result.Match(_ => called = true, _ => { });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void IfError_with_error_returns_fallback()
    {
        var gen = from errorResult in Generator.GenerateErrorResult<int>()
                  from fallback in Gen.Int
                  select (errorResult, fallback);

        gen.Sample(x =>
        {
            var (errorResult, fallback) = x;
            var result = errorResult.IfError(_ => fallback);

            result.Should().Be(fallback);
        });
    }

    [Fact]
    public void IfError_with_success_returns_success_value()
    {
        var gen = Gen.Int;

        gen.Sample(successValue =>
        {
            var result = Result.Success(successValue);
            var fallback = result.IfError(_ => 0);

            fallback.Should().Be(successValue);
        });
    }

    [Fact]
    public void IfError_returning_result_with_error_returns_fallback_result()
    {
        var gen = from errorResult in Generator.GenerateErrorResult<int>()
                  from fallbackResult in Generator.GenerateResult(Gen.Int)
                  select (errorResult, fallbackResult);

        gen.Sample(x =>
        {
            var (errorResult, fallbackResult) = x;

            var result = errorResult.IfError(_ => fallbackResult);

            result.Should().Be(fallbackResult);
        });
    }

    [Fact]
    public void IfError_returning_result_with_success_returns_original_result()
    {
        var gen = from successResult in Generator.GenerateSuccessResult(Gen.Int)
                  from fallbackResult in Generator.GenerateResult(Gen.Int)
                  select (successResult, fallbackResult);

        gen.Sample(x =>
        {
            var (successResult, fallbackResult) = x;

            var result = successResult.IfError(_ => fallbackResult);

            result.Should().Be(successResult);
        });
    }

    [Fact]
    public void Iter_with_error_does_not_call_action()
    {
        var gen = Generator.GenerateErrorResult<int>();

        gen.Sample(errorResult =>
        {
            var called = false;

            errorResult.Iter(_ => called = true);

            called.Should().BeFalse();
        });
    }

    [Fact]
    public void Iter_with_success_calls_action()
    {
        var gen = Generator.GenerateSuccessResult(Gen.Int);

        gen.Sample(result =>
        {
            var called = false;

            result.Iter(_ => called = true);

            called.Should().BeTrue();
        });
    }

    [Fact]
    public async Task IterTask_with_error_does_not_call_action()
    {
        var gen = Generator.GenerateErrorResult<int>();

        await gen.SampleAsync(async errorResult =>
        {
            var called = false;

            await errorResult.IterTask(async _ =>
            {
                await ValueTask.CompletedTask;
                called = true;
            });

            called.Should().BeFalse();
        });
    }

    [Fact]
    public async Task IterTask_with_success_calls_action()
    {
        var gen = Generator.GenerateSuccessResult(Gen.Int);

        await gen.SampleAsync(async result =>
        {
            var called = false;

            await result.IterTask(async _ =>
            {
                await ValueTask.CompletedTask;
                called = true;
            });

            called.Should().BeTrue();
        });
    }

    [Fact]
    public void IfErrorThrow_with_error_throws_exception()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            var errorResult = Result.Error<int>(error);
            var errorException = error.ToException();

            var action = () => errorResult.IfErrorThrow();

            switch (errorException)
            {
                case AggregateException aggregateException:
                    action.Should().Throw<AggregateException>().And.InnerExceptions.Should().HaveSameCount(aggregateException.InnerExceptions);
                    break;
                default:
                    action.Should().Throw<Exception>().Which.Message.Should().Be(errorException.Message);
                    break;
            }
        });
    }

    [Fact]
    public void IfErrorThrow_with_success_returns_success_value()
    {
        var gen = Gen.Int;

        gen.Sample(successValue =>
        {
            var result = Result.Success(successValue);
            var value = result.IfErrorThrow();

            value.Should().Be(successValue);
        });
    }

    [Fact]
    public void IfErrorNull_with_success_returns_value()
    {
        var gen = Gen.String.Where(x => x is not null);

        gen.Sample(value =>
        {
            var result = Result.Success(value);

            var nullableResult = result.IfErrorNull();

            nullableResult.Should().Be(value);
        });
    }

    [Fact]
    public void IfErrorNull_with_error_returns_null()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            var result = Result.Error<string>(error);

            var nullableResult = result.IfErrorNull();

            nullableResult.Should().BeNull();
        });
    }

    [Fact]
    public void IfErrorNullable_with_success_returns_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = Result.Success(value);

            var nullableResult = result.IfErrorNullable();

            nullableResult.Should().Be(value);
        });
    }

    [Fact]
    public void IfErrorNullable_with_error_returns_null()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            var result = Result.Error<int>(error);

            var nullableResult = result.IfErrorNullable();

            nullableResult.Should().BeNull();
        });
    }

    [Fact]
    public void Equals_with_success_and_same_value_returns_true()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result1 = Result.Success(value);
            var result2 = Result.Success(value);

            var equals = result1.Equals(result2);

            equals.Should().BeTrue();
        });
    }

    [Fact]
    public void Equals_with_error_and_same_error_returns_true()
    {
        var gen = Gen.String;

        gen.Sample(message =>
        {
            var error = Error.From(message);
            var result1 = Result.Error<int>(error);
            var result2 = Result.Error<int>(error);

            var equals = result1.Equals(result2);

            equals.Should().BeTrue();
        });
    }

    [Fact]
    public void Equals_with_success_and_different_values_returns_false()
    {
        var gen = from value1 in Gen.Int
                  from value2 in Gen.Int
                  where value1 != value2
                  select (value1, value2);

        gen.Sample(x =>
        {
            var (value1, value2) = x;
            var result1 = Result.Success(value1);
            var result2 = Result.Success(value2);

            var equals = result1.Equals(result2);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_error_and_different_errors_returns_false()
    {
        var gen = from error1 in Generator.Error
                  from error2 in Generator.Error
                  where !error1.Equals(error2)
                  select (error1, error2);

        gen.Sample(x =>
        {
            var (error1, error2) = x;
            var result1 = Result.Error<int>(error1);
            var result2 = Result.Error<int>(error2);

            var equals = result1.Equals(result2);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void Equals_with_success_and_error_returns_false()
    {
        var gen = from successResult in Generator.GenerateSuccessResult(Gen.Int)
                  from errorResult in Generator.GenerateErrorResult<int>()
                  select (successResult, errorResult);

        gen.Sample(x =>
        {
            var (successResult, errorResult) = x;

            var equals = successResult.Equals(errorResult);

            equals.Should().BeFalse();
        });
    }

    [Fact]
    public void LINQ_query_syntax_successes_returns_success()
    {
        var gen = from result1 in Generator.GenerateSuccessResult(Gen.Int)
                  from result2 in Generator.GenerateSuccessResult(Gen.Int)
                  from innerResult in Gen.String
                  select (result1, result2, innerResult);

        gen.Sample(x =>
        {
            var (result1, result2, innerResult) = x;

            var result = from _ in result1
                         from __ in result2
                         select innerResult;

            result.Should().BeSuccess().Which.Should().Be(innerResult);
        });
    }

    [Fact]
    public void LINQ_query_syntax_with_error_returns_error()
    {
        var gen = from successResult in Generator.GenerateSuccessResult(Gen.Int)
                  from error in Generator.Error
                  select (successResult, error);

        gen.Sample(x =>
        {
            var (successResult, error) = x;
            var errorResult = Result.Error<int>(error);

            var result = from _ in successResult
                         from __ in errorResult
                         select __;

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void ToString_success_returns_formatted_string()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = Result.Success(value);
            var toString = result.ToString();

            toString.Should().Be($"Success: {value}");
        });
    }

    [Fact]
    public void ToString_error_returns_formatted_string()
    {
        var gen = Gen.String;

        gen.Sample(message =>
        {
            var error = Error.From(message);
            var result = Result.Error<int>(error);
            var toString = result.ToString();

            toString.Should().Be($"Error: {error}");
        });
    }

    [Fact]
    public void GetHashCode_same_success_values_return_same_hash()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result1 = Result.Success(value);
            var result2 = Result.Success(value);

            var hash1 = result1.GetHashCode();
            var hash2 = result2.GetHashCode();

            hash1.Should().Be(hash2);
        });
    }

    [Fact]
    public void GetHashCode_same_error_values_return_same_hash()
    {
        var gen = Gen.String;

        gen.Sample(message =>
        {
            var error = Error.From(message);
            var result1 = Result.Error<int>(error);
            var result2 = Result.Error<int>(error);

            var hash1 = result1.GetHashCode();
            var hash2 = result2.GetHashCode();

            hash1.Should().Be(hash2);
        });
    }

    [Fact]
    public void Implicit_conversion_from_value_creates_success()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            Result<int> result = value;

            result.Should().BeSuccess().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Implicit_conversion_from_error_creates_error()
    {
        var gen = Gen.String;

        gen.Sample(message =>
        {
            var error = Error.From(message);
            Result<int> result = error;

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void Static_Success_factory_method_creates_success()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = Result.Success(value);

            result.Should().BeSuccess().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Static_Error_factory_method_creates_error()
    {
        var gen = Gen.String;

        gen.Sample(message =>
        {
            var error = Error.From(message);
            var result = Result.Error<int>(error);

            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void ToOption_with_success_returns_some()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            var result = Result.Success(value);
            var option = result.ToOption();

            option.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void ToOption_with_error_returns_none()
    {
        var gen = Generator.GenerateErrorResult<int>();

        gen.Sample(result =>
        {
            var option = result.ToOption();

            option.Should().BeNone();
        });
    }
}
