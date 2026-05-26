using CsCheck;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class SuccessT_ToString_Tests()
{
    [Test]
    public async Task Returns_Success_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var success = new Result<object>.Success(value);

            // Act
            var result = success.ToString();

            // Assert
            await Assert.That(result)
                        .Contains("Success")
                        .And
                        .Contains(value.ToString()!);
        });
    }
}

public class ResultT_Error_ImplicitOperator_Tests()
{
    [Test]
    public async Task Implicitly_converts_Error_to_a_result_in_the_error_state()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            Result<object> result = error;

            // Assert
            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}

public class ResultT_ToString_Tests
{
    [Test]
    public async Task Success_returns_Success_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var success = new Result<object>.Success(value);
            var result = new Result<object>(success);

            // Act
            var resultString = result.ToString();

            // Assert
            await Assert.That(resultString)
                        .Contains("Success")
                        .And
                        .Contains(value.ToString()!);
        });
    }

    [Test]
    public async Task Error_returns_Error_with_value()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = new Result<object>(error);

            // Act
            var resultString = result.ToString();

            // Assert
            await Assert.That(resultString)
                        .Contains("Error")
                        .And
                        .Contains(error.ToString());
        });
    }
}

public class ResultT_Equality_Tests
{
    [Test]
    public async Task Equality_is_reflexive()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Assert
            await Assert.That(result.Equals(result))
                        .IsTrue();

#pragma warning disable CS1718 // Comparison made to same variable
            await Assert.That(result == result)
#pragma warning restore CS1718 // Comparison made to same variable
                        .IsTrue();
        });
    }

    [Test]
    public async Task Equality_is_symmetric()
    {
        var gen = from x1 in Generator.Object
                  from error1 in ErrorGenerator.Any
                  from x2 in Generator.Object
                  from error2 in ErrorGenerator.Any
                  let resultGenerator =
                    Gen.OneOfConst(new Result<object>(new Result<object>.Success(x1)),
                                   new Result<object>(error1),
                                   new Result<object>(new Result<object>.Success(x2)),
                                   new Result<object>(error2))
                  from result1 in resultGenerator
                  from result2 in resultGenerator
                  select (result1, result2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Assert
            await Assert.That(result1.Equals(result2))
                        .IsEqualTo(result2.Equals(result1));

            await Assert.That(result1 == result2)
                        .IsEqualTo(result2 == result1);
        });
    }

    [Test]
    public async Task Equality_is_transitive()
    {
        var gen = from x1 in Generator.Object
                  from error1 in ErrorGenerator.Any
                  from x2 in Generator.Object
                  from error2 in ErrorGenerator.Any
                  let resultGenerator =
                    Gen.OneOfConst(new Result<object>(new Result<object>.Success(x1)),
                                   new Result<object>(error1),
                                   new Result<object>(new Result<object>.Success(x2)),
                                   new Result<object>(error2))
                  from result1 in resultGenerator
                  from result2 in resultGenerator
                  from result3 in resultGenerator
                  select (result1, result2, result3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2, result3) = tuple;

            // Assert
            if (result1.Equals(result2) && result2.Equals(result3))
            {
                await Assert.That(result1.Equals(result3))
                            .IsTrue();

                await Assert.That(result1 == result3)
                            .IsTrue();
            }
        });
    }
}

public class Result_Success_Tests()
{
    [Test]
    public async Task Returns_a_result_in_the_success_state()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Act
            var result = Result.Success(x);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }
}

public class Result_Error_Tests()
{
    [Test]
    public async Task Returns_a_result_in_the_error_state()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            var result = Result.Error<object>(error);

            // Assert
            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}

public class Result_Map_Tests()
{
    [Test]
    public async Task Satisfies_functor_identity()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var mappedResult = result.Map(x => x);

            // Assert
            await Assert.That(mappedResult)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = result.Map(x => g(f(x)));

            var result2 = result.Map(f)
                                .Map(g);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_bind_then_return()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var result1 = result.Map(f);
            var result2 = result.Bind(x => Result.Success(f(x)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_MapTask_Tests()
{
    [Test]
    public async Task Satisfies_functor_identity()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            static async ValueTask<object> f(object x)
            {
                await Task.Yield();
                return x;
            }

            // Act
            var mappedResult = await result.MapTask(f);

            // Assert
            await Assert.That(mappedResult)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition()
    {
        var gen = from result in ResultGenerator.Any
                  from f1 in MapperGenerator.ObjectToObject
                  let f = new Func<object, ValueTask<object>>(async x =>
                  {
                      await Task.Yield();
                      return f1(x);
                  })
                  from g1 in MapperGenerator.ObjectToObject
                  let g = new Func<object, ValueTask<object>>(async x =>
                  {
                      await Task.Yield();
                      return g1(x);
                  })
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = await result.MapTask(async x => await g(await f(x)));

            var result2 = await (await result.MapTask(f)).MapTask(g);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_BindTask_then_Return()
    {
        var gen = from result in ResultGenerator.Any
                  from f1 in MapperGenerator.ObjectToObject
                  let f = new Func<object, ValueTask<object>>(async x =>
                  {
                      await Task.Yield();
                      return f1(x);
                  })
                  select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var result1 = await result.MapTask(f);

            var result2 = await result.BindTask(async x =>
            {
                await Task.Yield();
                return Result.Success(await f(x));
            });

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_MapError_Tests()
{
    [Test]
    public async Task Satisfies_functor_identity()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var mappedResult = result.MapError(x => x);

            // Assert
            await Assert.That(mappedResult)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ErrorToError
                  from g in MapperGenerator.ErrorToError
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = result.MapError(x => g(f(x)));

            var result2 = result.MapError(f)
                                .MapError(g);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Map_and_MapError_are_independent()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ErrorToError
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = result.Map(f)
                                .MapError(g);

            var result2 = result.MapError(g)
                                .Map(f);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_Bind_Tests()
{
    [Test]
    public async Task Satisfies_monad_left_identity()
    {
        var gen = from x in Generator.Object
                  from f in MapperGenerator.ObjectToResult
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var result = Result.Success(x);

            // Act
            var result1 = result.Bind(f);
            var result2 = f(x);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var boundResult = result.Bind(Result.Success);

            // Assert
            await Assert.That(boundResult)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToResult
                  from g in MapperGenerator.ObjectToResult
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = result.Bind(f)
                                .Bind(g);

            var result2 = result.Bind(x => f(x).Bind(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Error_result_returns_original_error()
    {
        var gen = from error in ErrorGenerator.Any
                  from f in MapperGenerator.ObjectToResult
                  select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var boundResult = result.Bind(f);

            // Assert
            await Assert.That(boundResult)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}

public class Result_BindTask_Tests()
{
    [Test]
    public async Task Satisfies_monad_left_identity()
    {
        var gen = from x in Generator.Object
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<object, ValueTask<Result<object>>>(async x =>
                  {
                      await Task.Yield();
                      return f1(x);
                  })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var result = Result.Success(x);

            // Act
            var result1 = await result.BindTask(f);
            var result2 = await f(x);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            static async ValueTask<Result<object>> f(object x)
            {
                await Task.Yield();
                return Result.Success(x);
            }

            // Act
            var boundResult = await result.BindTask(f);

            // Assert
            await Assert.That(boundResult)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity()
    {
        var gen = from result in ResultGenerator.Any
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<object, ValueTask<Result<object>>>(async x =>
                  {
                      await Task.Yield();
                      return f1(x);
                  })
                  from g1 in MapperGenerator.ObjectToResult
                  let g = new Func<object, ValueTask<Result<object>>>(async x =>
                  {
                      await Task.Yield();
                      return g1(x);
                  })
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = await (await result.BindTask(f)).BindTask(g);

            var result2 = await result.BindTask(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_Select_Tests()
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_map()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var result1 = from x in result
                          select f(x);

            var result2 = result.Select(f);

            var result3 = result.Map(f);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2)
                        .And
                        .IsEqualTo(result3);
        });
    }
}

public class Result_SelectMany_Tests()
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_bind()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToResult
                  from g1 in MapperGenerator.ObjectToObject
                  from g2 in MapperGenerator.ObjectToObject
                      // Nothing special, just a repeatable function with two parameters
                  let g = new Func<object, object, object>((x, y) => g2(g1(x).ToString() + y.ToString()))
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = from x in result
                          from y in f(x)
                          select g(x, y);

            var result2 = result.SelectMany(f, g);

            var result3 = result.Bind(x => f(x).Map(y => g(x, y)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2)
                        .And
                        .IsEqualTo(result3);
        });
    }
}

public class Result_Match_Tests()
{
    [Test]
    public async Task Success_returns_success_function_result()
    {
        var gen = from x in Generator.Object
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ErrorToObject
                  select (x, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f, g) = tuple;
            var result = Result.Success(x);

            // Act
            var matchResult = result.Match(f, g);

            // Assert
            await Assert.That(matchResult)
                        .IsEqualTo(f(x));
        });
    }

    [Test]
    public async Task Error_returns_error_function_result()
    {
        var gen = from error in ErrorGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ErrorToObject
                  select (error, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f, g) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var matchResult = result.Match(f, g);

            // Assert
            await Assert.That(matchResult)
                        .IsEqualTo(g(error));
        });
    }

    [Test]
    public async Task Reconstructs_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var reconstructed = result.Match(Result.Success,
                                             error => Result.Error<object>(error));

            // Assert
            await Assert.That(reconstructed)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Fuses_with_Map()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
                  from h in MapperGenerator.ErrorToObject
                  select (result, f, g, h);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g, h) = tuple;

            // Act
            var result1 = result.Map(f)
                                .Match(g, h);

            var result2 = result.Match(x => g(f(x)), h);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Fuses_with_MapError()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ErrorToError
                  from h in MapperGenerator.ErrorToObject
                  select (result, f, g, h);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g, h) = tuple;

            // Act
            var result1 = result.MapError(g)
                                .Match(f, h);

            var result2 = result.Match(f, error => h(g(error)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Fuses_with_Bind()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToResult
                  from g in MapperGenerator.ObjectToObject
                  from h in MapperGenerator.ErrorToObject
                  select (result, f, g, h);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g, h) = tuple;

            // Act
            var result1 = result.Bind(f)
                                .Match(g, h);

            var result2 = result.Match(x => f(x).Match(g, h), h);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_Match_WithAction_Tests()
{
    [Test]
    public async Task Success_executes_success_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            object? observed = null;
            var f = (object value) => observed = value;

            var observedError = default(Error);
            var g = (Error error) => observedError = error;

            // Act
            result.Match(f, g);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);

            await Assert.That(observedError)
                        .IsNull();
        });
    }

    [Test]
    public async Task Error_executes_error_action()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            object? observed = null;
            var f = (object value) => observed = value;

            var observedError = default(Error);
            var g = (Error error) => observedError = error;

            // Act
            result.Match(f, g);

            // Assert
            await Assert.That(observed)
                        .IsNull();

            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }
}

public class Result_IfError_WithValueFallback_Tests()
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = from x in Generator.Object
                  from y in Generator.Object
                  select (x, y);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, y) = tuple;
            var result = Result.Success(x);

            // Act
            var ifErrorResult = result.IfError(error => y);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_the_fallback()
    {
        var gen = from error in ErrorGenerator.Any
                  from f1 in MapperGenerator.ObjectToObject
                  let f = new Func<Error, object>(f1)
                  select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var ifErrorResult = result.IfError(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(f(error));
        });
    }
}

public class IfError_WithResultFallback_Tests()
{
    [Test]
    public async Task Success_returns_the_original_result()
    {
        var gen = from x in Generator.Object
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, Result<object>>(f1)
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var result = Result.Success(x);

            // Act
            var ifErrorResult = result.IfError(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_the_fallback_result()
    {
        var gen = from error in ErrorGenerator.Any
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, Result<object>>(f1)
                  select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var ifErrorResult = result.IfError(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(f(error));
        });
    }

    [Test]
    public async Task Satisfies_recovery_associativity()
    {
        var gen = from result in ResultGenerator.Any
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, Result<object>>(f1)
                  from g1 in MapperGenerator.ObjectToResult
                  let g = new Func<Error, Result<object>>(g1)
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = result.IfError(f)
                                .IfError(g);

            var result2 = result.IfError(error => f(error).IfError(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_IterError_Tests()
{
    [Test]
    public async Task Success_does_not_execute_action()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            void f(Error error) => actionExecuted = true;

            // Act
            result.IterError(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }

    [Test]
    public async Task Error_executes_action()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            var observedError = default(Error);
            Action<Error> f = e => observedError = e;

            // Act
            result.IterError(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }
}

public class IfErrorTask_WithValueFallback_Tests()
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = from x in Generator.Object
                  from y in Generator.Object
                  let f = new Func<Error, ValueTask<object>>(async error =>
                  {
                      await Task.Yield();
                      return y;
                  })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var result = Result.Success(x);

            // Act
            var ifErrorResult = await result.IfErrorTask(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_the_fallback()
    {
        var gen = from error in ErrorGenerator.Any
                  from f1 in MapperGenerator.ObjectToObject
                  let f = new Func<Error, ValueTask<object>>(async error =>
                  {
                      await Task.Yield();
                      return f1(error);
                  })
                  select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var ifErrorResult = await result.IfErrorTask(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(await f(error));
        });
    }
}

public class IfErrorTask_WithResultFallback_Tests()
{
    [Test]
    public async Task Success_returns_the_original_result()
    {
        var gen = from x in Generator.Object
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, ValueTask<Result<object>>>(async error =>
                  {
                      await Task.Yield();
                      return f1(error);
                  })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var result = Result.Success(x);

            // Act
            var ifErrorResult = await result.IfErrorTask(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_the_fallback_result()
    {
        var gen = from error in ErrorGenerator.Any
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, ValueTask<Result<object>>>(async error =>
                  {
                      await Task.Yield();
                      return f1(error);
                  })
                  select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var ifErrorResult = await result.IfErrorTask(f);

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(await f(error));
        });
    }

    [Test]
    public async Task Satisfies_recovery_associativity()
    {
        var gen = from result in ResultGenerator.Any
                  from f1 in MapperGenerator.ObjectToResult
                  let f = new Func<Error, ValueTask<Result<object>>>(async error =>
                  {
                      await Task.Yield();
                      return f1(error);
                  })
                  from g1 in MapperGenerator.ObjectToResult
                  let g = new Func<Error, ValueTask<Result<object>>>(async error =>
                  {
                      await Task.Yield();
                      return g1(error);
                  })
                  select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var result1 = await (await result.IfErrorTask(f)).IfErrorTask(g);

            var result2 = await result.IfErrorTask(async error => await (await f(error)).IfErrorTask(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Result_IterErrorTask_Tests()
{
    [Test]
    public async Task Success_does_not_execute_action()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            async ValueTask f(Error error)
            {
                await Task.Yield();
                actionExecuted = true;
            }

            // Act
            await result.IterErrorTask(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }

    [Test]
    public async Task Error_executes_action()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            var observedError = default(Error);
            async ValueTask f(Error error)
            {
                await Task.Yield();
                observedError = error;
            }

            // Act
            await result.IterErrorTask(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }
}

public class Result_IfErrorThrow_Tests()
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            // Act
            var ifErrorResult = result.IfErrorThrow();

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_throws_an_exception()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);
            var f = () => result.IfErrorThrow();

            // Assert
            var errorException = error.ToException();

            await Assert.That(f)
                        .Throws<Exception>()
                        .WithMessage(errorException.Message, StringComparison.Ordinal)
                        .And
                        .IsOfType(errorException.GetType());
        });
    }
}

public class Result_IfErrorNull_Tests()
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            // Act
            var ifErrorResult = result.IfErrorNull();

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_null()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            // Act
            var ifErrorResult = result.IfErrorNull();

            // Assert
            await Assert.That(ifErrorResult)
                        .IsNull();
        });
    }
}

public class Result_IfErrorNullable_Tests()
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            // Act
            var ifErrorResult = result.IfErrorNullable();

            // Assert
            await Assert.That(ifErrorResult)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_returns_null()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<int>(error);

            // Act
            var ifErrorResult = result.IfErrorNullable();

            // Assert
            await Assert.That(ifErrorResult)
                        .IsNull();
        });
    }
}

public class Result_Iter_Tests()
{
    [Test]
    public async Task Successful_result_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            object? passedValue = null;
            void f(object value) => passedValue = value;

            // Act
            result.Iter(f);

            // Assert
            await Assert.That(passedValue)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_result_does_not_execute_action()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            void f(object value) => actionExecuted = true;

            // Act
            result.Iter(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Result_IterTask_Tests()
{
    [Test]
    public async Task Successful_result_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            object? passedValue = null;
            async ValueTask f(object value)
            {
                await Task.Yield();
                passedValue = value;
            }

            // Act
            await result.IterTask(f);

            // Assert
            await Assert.That(passedValue)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_result_does_not_execute_action()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            async ValueTask f(object value)
            {
                await Task.Yield();
                actionExecuted = true;
            }

            // Act
            await result.IterTask(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Result_ToOption_Tests()
{
    [Test]
    public async Task Successful_result_returns_Some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);

            // Act
            var option = result.ToOption();

            // Assert
            await Assert.That(option)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_result_returns_None()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            // Act
            var option = result.ToOption();

            // Assert
            await Assert.That(option)
                        .IsNone();
        });
    }
}

public class Result_Tap_Tests
{
    [Test]
    public async Task Returns_original_result()
    {
        var gen = from result in ResultGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var tapped = result.Tap(x => { _ = f(x); });

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Success_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);
            object? observed = null;

            // Act
            result.Tap(value => observed = value);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_does_not_execute_action()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;

            // Act
            result.Tap(_ => actionExecuted = true);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Result_TapTask_Tests
{
    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            static async ValueTask f(object x) => await Task.Yield();

            // Act
            var tapped = await result.TapTask(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Success_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var result = Result.Success(x);
            object? observed = null;

            async ValueTask f(object value)
            {
                await Task.Yield();
                observed = value;
            }

            // Act
            await result.TapTask(f);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task Error_does_not_execute_action()
    {
        var gen = ResultGenerator.Error;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            async ValueTask f(object _)
            {
                await Task.Yield();
                actionExecuted = true;
            }

            // Act
            await result.TapTask(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Result_TapError_Tests
{
    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var tapped = result.TapError(_ => { });

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Error_executes_action()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);
            var observedError = default(Error);

            // Act
            result.TapError(e => observedError = e);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public async Task Success_does_not_execute_action()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;

            // Act
            result.TapError(_ => actionExecuted = true);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Result_TapErrorTask_Tests
{
    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            static async ValueTask f(Error _) => await Task.Yield();

            // Act
            var tapped = await result.TapErrorTask(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Error_executes_action()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);
            var observedError = default(Error);

            async ValueTask f(Error e)
            {
                await Task.Yield();
                observedError = e;
            }

            // Act
            await result.TapErrorTask(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public async Task Success_does_not_execute_action()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            var actionExecuted = false;
            async ValueTask f(Error _)
            {
                await Task.Yield();
                actionExecuted = true;
            }

            // Act
            await result.TapErrorTask(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}
