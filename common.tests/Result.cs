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
    public void Success_returns_a_result_in_the_success_state()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Act
            var result = Result.Success(value);

            // Assert
            result.Should().BeSuccess().Which.Should().Be(value);
            result.IsError.Should().BeFalse();
        });
    }

    [Fact]
    public void Error_returns_a_result_in_the_error_state()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Act
            var result = Result.Error<int>(error);

            // Assert
            result.Should().BeError().Which.Should().Be(error);
            result.IsSuccess.Should().BeFalse();
        });
    }

    [Fact]
    public void Equality_is_reflexive()
    {
        var gen = Generator.Result;

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
                  from error1 in Generator.Error
                  from result1 in Gen.OneOfConst(Result.Success(value1),
                                                 Result.Error<int>(error1))
                  from value2 in Gen.OneOf(Gen.Const(value1), Gen.Int)
                  from error2 in Gen.OneOf(Gen.Const(error1), Generator.Error)
                  from result2 in Gen.OneOfConst(Result.Success(value2),
                                                 Result.Error<int>(error2))
                  select (result1, result2);

        gen.Sample(tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Assert
            (result1.Equals(result2)).Should().Be(result2.Equals(result1));
            (result1 == result2).Should().Be(result2 == result1);
        });
    }

    [Fact]
    public void Equality_is_transitive()
    {
        var gen = from value1 in Gen.Int
                  from error1 in Generator.Error
                  from result1 in Gen.OneOfConst(Result.Success(value1),
                                                 Result.Error<int>(error1))
                  from value2 in Gen.OneOf(Gen.Const(value1), Gen.Int)
                  from error2 in Gen.OneOf(Gen.Const(error1), Generator.Error)
                  from result2 in Gen.OneOfConst(Result.Success(value2),
                                                 Result.Error<int>(error2))
                  from value3 in Gen.OneOf(Gen.Const(value1), Gen.Const(value2), Gen.Int)
                  from error3 in Gen.OneOf(Gen.Const(error1), Gen.Const(error2), Generator.Error)
                  from result3 in Gen.OneOfConst(Result.Success(value3),
                                                  Result.Error<int>(error3))
                  select (result1, result2, result3);

        gen.Sample(tuple =>
        {
            // Arrange
            var (result1, result2, result3) = tuple;

            // Assert
            if (result1.Equals(result2) && result2.Equals(result3))
            {
                result1.Equals(result3).Should().BeTrue();
                (result1 == result3).Should().BeTrue();
            }
        });
    }

    [Fact]
    public void Results_with_the_same_value_are_equal()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var result1 = Result.Success(value);
            var result2 = Result.Success(value);

            // Assert
            result1.Equals(result2).Should().BeTrue();
            (result1 == result2).Should().BeTrue();
            result1.GetHashCode().Should().Be(result2.GetHashCode());
        });
    }

    [Fact]
    public void Results_with_different_values_are_not_equal()
    {
        var gen = from value1 in Gen.Int
                  from value2 in Gen.Int
                  where value1 != value2
                  select (value1, value2);

        gen.Sample(tuple =>
        {
            // Arrange
            var (value1, value2) = tuple;
            var result1 = Result.Success(value1);
            var result2 = Result.Success(value2);

            // Assert
            result1.Equals(result2).Should().BeFalse();
            (result1 != result2).Should().BeTrue();
        });
    }

    [Fact]
    public void Results_with_the_same_error_are_equal()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var result1 = Result.Error<int>(error);
            var result2 = Result.Error<int>(error);

            // Assert
            result1.Equals(result2).Should().BeTrue();
            (result1 == result2).Should().BeTrue();
            result1.GetHashCode().Should().Be(result2.GetHashCode());
        });
    }

    [Fact]
    public void Results_with_different_errors_are_not_equal()
    {
        var gen = from error1 in Generator.Error
                  from error2 in Generator.Error
                  where error1 != error2
                  select (error1, error2);

        gen.Sample(tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;
            var result1 = Result.Error<int>(error1);
            var result2 = Result.Error<int>(error2);

            // Assert
            result1.Equals(result2).Should().BeFalse();
            (result1 != result2).Should().BeTrue();
        });
    }

    [Fact]
    public void Successful_and_error_results_are_not_equal()
    {
        var gen = from value in Gen.Int
                  from error in Generator.Error
                  select (value, error);

        gen.Sample(tuple =>
        {
            // Arrange
            var (value, error) = tuple;
            var successResult = Result.Success(value);
            var errorResult = Result.Error<int>(error);

            // Assert
            successResult.Equals(errorResult).Should().BeFalse();
            (successResult == errorResult).Should().BeFalse();
        });
    }

    [Fact]
    public void Can_implicitly_convert_value_to_result()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Act
            Result<int> result = value;

            // Assert
            result.Should().BeSuccess().Which.Should().Be(value);
        });
    }

    [Fact]
    public void Can_implicitly_convert_error_to_result()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Act
            Result<int> result = error;

            // Assert
            result.Should().BeError().Which.Should().Be(error);
        });
    }

    [Fact]
    public void ToString_contains_value()
    {
        var gen = Generator.Result;

        gen.Sample(monad =>
        {
            var toString = monad.ToString();

            // Assert
            var expectedSubstring = monad.Match(value => value.ToString(),
                                                error => error.ToString());
                                                
            toString.Should().Contain(expectedSubstring);
        });
    }

    [Fact]
    public void Result_satisfies_monad_left_identity()
    {
        var gen = from value in Gen.Int
                  from f in Generator.IntToStringResult
                  select (value, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var monad = Result.Success(value);

            // Act
            var result = monad.Bind(f);

            // Assert
            result.Should().Be(f(value));
        });
    }

    [Fact]
    public void Result_satisfies_monad_right_identity()
    {
        var gen = Generator.Result;

        gen.Sample(monad =>
        {
            var result = monad.Bind(Result.Success);

            result.Should().Be(monad);
        });
    }

    [Fact]
    public void Result_satisfies_monad_associativity()
    {
        var gen = from monad in Generator.Result
                  from f in Generator.IntToStringResult
                  from g in Generator.StringToIntResult
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
    public void MapError_satisfies_functor_identity()
    {
        var gen = Generator.Result;

        gen.Sample(monad =>
        {
            var result = monad.MapError(error => error);

            result.Should().Be(monad);
        });
    }

    [Fact]
    public void MapError_satisfies_functor_composition()
    {
        var gen = from monad in Generator.Result
                  from f in Generator.ErrorToError
                  from g in Generator.ErrorToError
                  select (monad, f, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (monad, f, g) = tuple;

            // Act
            var path1 = monad.MapError(f)
                             .MapError(g);

            var path2 = monad.MapError(error => g(f(error)));

            // Assert
            path1.Should().Be(path2);
        });
    }

    [Fact]
    public void LINQ_Select_is_syntactic_sugar_for_map()
    {
        var gen = from monad in Generator.Result
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
        var gen = from monad in Generator.Result
                  from f in Generator.IntToStringResult
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
        var gen = from monad in Generator.Result
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
        var gen = from monad in Generator.Result
                  from f in Generator.IntToStringResult
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
        var gen = from monad in Generator.Result
                  from f in Generator.IntToStringResultTask
                  from g in Generator.StringToIntResultTask
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
    public void Match_with_success_returns_success_function()
    {
        var gen = from x in Gen.Int
                  from f in Generator.IntToString
                  select (x, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var monad = Result.Success(x);

            var errorFunctionRan = false;
            string g(Error error)
            {
                errorFunctionRan = true;
                return string.Empty;
            }

            // Act
            var result = monad.Match(f, g);

            // Assert
            result.Should().Be(f(x));
            errorFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_error_returns_error_function()
    {
        var gen = from error in Generator.Error
                  from g in Generator.ErrorToInt
                  select (error, g);

        gen.Sample(tuple =>
        {
            // Arrange
            var (error, g) = tuple;
            var monad = Result.Error<int>(error);

            var successFunctionRan = false;
            int f(int x)
            {
                successFunctionRan = true;
                return 0;
            }

            // Act
            var result = monad.Match(f, g);

            // Assert
            result.Should().Be(g(error));
            successFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_success_executes_success_action()
    {
        var gen = from x in Gen.Int
                  from f1 in Generator.IntToString
                  select (x, f1);

        gen.Sample(tuple =>
        {
            // Arrange
            var (x, f1) = tuple;

            var monad = Result.Success(x);

            var actionedValue = string.Empty;
            void f(int value) => actionedValue = f1(value);

            bool errorFunctionRan = false;
            void g(Error error) => errorFunctionRan = true;

            // Act
            monad.Match(f, g);

            // Assert
            actionedValue.Should().Be(f1(x));
            errorFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void Match_with_error_executes_error_action()
    {
        var gen = from error in Generator.Error
                  from g1 in Generator.ErrorToError
                  select (error, g1);

        gen.Sample(tuple =>
        {
            // Arrange
            var (error, g1) = tuple;

            var monad = Result.Error<int>(error);

            bool successFunctionRan = false;
            void f(int value) => successFunctionRan = true;

            Error actionedError = Error.From("Sample error");
            void g(Error error) => actionedError = g1(error);

            // Act
            monad.Match(f, g);

            // Assert
            actionedError.Should().Be(g1(error));
            successFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void IfError_with_success_returns_success_value()
    {
        var gen = from value in Gen.Int
                  from f1 in Generator.ErrorToInt
                  select (value, f1);

        gen.Sample(tuple =>
        {
            // Arrange
            var (value, f1) = tuple;
            var monad = Result.Success(value);

            var errorFunctionRan = false;
            int f(Error error)
            {
                errorFunctionRan = true;
                return f1(error);
            }

            // Act
            var result = monad.IfError(f);

            // Assert
            result.Should().Be(value);
            errorFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void IfError_with_error_returns_error_function_value()
    {
        var gen = from error in Generator.Error
                  from f in Generator.ErrorToInt
                  select (error, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var monad = Result.Error<int>(error);

            // Act
            var result = monad.IfError(f);

            // Assert
            result.Should().Be(f(error));
        });
    }

    [Fact]
    public void IfError_with_success_returns_original_result()
    {
        var gen = from value in Gen.String
                  let monad = Result.Success(value)
                  from f1 in Generator.ErrorToStringResult
                  select (monad, f1);

        gen.Sample(tuple =>
        {
            // Arrange
            var (monad, f1) = tuple;

            var errorFunctionRan = false;
            Result<string> f(Error error)
            {
                errorFunctionRan = true;
                return f1(error);
            }

            // Act
            var result = monad.IfError(f);

            // Assert
            result.Should().Be(monad);
            errorFunctionRan.Should().BeFalse();
        });
    }

    [Fact]
    public void IfError_with_error_returns_error_fallback()
    {
        var gen = from error in Generator.Error
                  from f in Generator.ErrorToStringResult
                  select (error, f);

        gen.Sample(tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var monad = Result.Error<string>(error);

            // Act
            var result = monad.IfError(f);

            // Assert
            result.Should().Be(f(error));
        });
    }

    [Fact]
    public void IfErrorThrow_with_success_returns_success_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Result.Success(value);

            // Act
            var result = monad.IfErrorThrow();

            // Assert
            result.Should().Be(value);
        });
    }

    [Fact]
    public void IfErrorThrow_with_error_throws()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var monad = Result.Error<int>(error);

            // Act

            Action action = () =>
            {
                monad.IfErrorThrow();
            };

            // Assert
            action.Should().Throw();
        });
    }

    [Fact]
    public void IfErrorNull_with_success_returns_success_value()
    {
        var gen = Gen.String;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Result.Success(value);

            // Act
            var result = monad.IfErrorNull();

            // Assert
            result.Should().Be(value).And.NotBeNull();
        });
    }

    [Fact]
    public void IfErrorNull_with_error_returns_null()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var monad = Result.Error<string>(error);

            // Act
            var result = monad.IfErrorNull();

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public void IfErrorNullable_with_success_returns_success_value()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Result.Success(value);

            // Act
            var result = monad.IfErrorNullable();

            // Assert
            result.Should().Be(value).And.NotBeNull();
        });
    }

    [Fact]
    public void IfErrorNullable_with_error_returns_null()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var monad = Result.Error<int>(error);

            // Act
            var result = monad.IfErrorNullable();

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public void Iter_with_success_executes_action()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Result.Success(value);

            var actionedValue = 0;
            void action(int x) => actionedValue += x;

            // Act
            monad.Iter(action);

            // Assert
            actionedValue.Should().Be(value);
        });
    }

    [Fact]
    public void Iter_with_error_does_not_execute_action()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var monad = Result.Error<int>(error);

            var actionExecuted = false;
            void action(int x) => actionExecuted = true;

            // Act
            monad.Iter(action);

            // Assert
            actionExecuted.Should().BeFalse();
        });
    }

    [Fact]
    public async Task IterTask_with_success_executes_action()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var monad = Result.Success(value);

            var actionedValue = 0;
            async ValueTask action(int x)
            {
                await ValueTask.CompletedTask;
                actionedValue += x;
            }

            // Act
            await monad.IterTask(action);

            // Assert
            actionedValue.Should().Be(value);
        });
    }

    [Fact]
    public async Task IterTask_with_error_does_not_execute_action()
    {
        var gen = Generator.Error;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var monad = Result.Error<int>(error);

            var actionExecuted = false;
            async ValueTask action(int x)
            {
                await ValueTask.CompletedTask;
                actionExecuted = true;
            }

            // Act
            await monad.IterTask(action);

            // Assert
            actionExecuted.Should().BeFalse();
        });
    }

    [Fact]
    public void ToOption_with_success_returns_some()
    {
        var gen = Gen.Int;

        gen.Sample(value =>
        {
            // Arrange
            var monad = Result.Success(value);

            // Act
            var result = monad.ToOption();

            // Assert
            result.Should().BeSome().Which.Should().Be(value);
        });
    }

    [Fact]
    public void ToOption_with_error_returns_none()
    {
        var gen = Generator.Error;

        gen.Sample(error =>
        {
            // Arrange
            var monad = Result.Error<int>(error);

            // Act
            var result = monad.ToOption();

            // Assert
            result.Should().BeNone();
        });
    }
}
