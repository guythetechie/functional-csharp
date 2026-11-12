using CsCheck;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class ResultT_Match_Tests
{
    [Test]
    public async Task Success_returns_success_function_result()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToObject
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            object onError(Error _) => throw new UnreachableException();

            // Act
            var matched = result.Match(f, onError);

            // Assert
            await Assert.That(matched)
                        .IsEqualTo(f(value));
        });
    }

    [Test]
    public async Task Error_returns_error_function_result()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ErrorToObject
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            object onSuccess(object _) => throw new UnreachableException();

            // Act
            var matched = result.Match(onSuccess, f);

            // Assert
            await Assert.That(matched)
                        .IsEqualTo(f(error));
        });
    }

    [Test]
    public async Task Reconstructs_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var reconstructed = result.Match(Result.Success, Result.Error<object>);

            // Assert
            await Assert.That(reconstructed)
                        .IsEqualTo(result);
        });
    }
}

public class ResultT_Match_WithAction_Tests
{
    [Test]
    public async Task Success_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            object? observedPayload = null;
            void onSuccess(object payload) => observedPayload = payload;

            void onError(Error _) => throw new UnreachableException();

            // Act
            result.Match(onSuccess, onError);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_observes_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            void onSuccess(object _) => throw new UnreachableException();

            Error? observedError = null;
            void onError(Error payload) => observedError = payload;

            // Act
            result.Match(onSuccess, onError);

            // Assert
            await Assert.That(observedError)
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
            var result = Result.Success(value);

            // Act
            var text = result.ToString();

            // Assert
            await Assert.That(text)
                        .Contains("Success")
                        .And
                        .Contains(value.ToString() ?? string.Empty);
        });
    }

    [Test]
    public async Task Error_returns_Error_with_payload()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            // Act
            var text = result.ToString();

            // Assert
            await Assert.That(text)
                        .Contains("Error")
                        .And
                        .Contains(error.ToString() ?? string.Empty);
        });
    }
}

public class ResultT_Equality_Tests
{
    [Test]
    public async Task Satisfies_equality_reflexivity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var equal = result.Equals(result);

            // Assert
            await Assert.That(equal)
                        .IsTrue();
        });
    }

    [Test]
    public async Task Satisfies_equality_symmetry_law()
    {
        var gen = ResultGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Act
            var path1 = result1.Equals(result2);
            var path2 = result2.Equals(result1);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_equality_transitivity_law()
    {
        var gen =
            from result1 in ResultGenerator.Any
            from result2 in Gen.OneOf(ResultGenerator.Any, ResultGenerator.GenerateEqual(result1))
            from result3 in Gen.OneOf(ResultGenerator.Any, ResultGenerator.GenerateEqual(result2))
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
            }
        });
    }

    [Test]
    public async Task Success_equality_matches_payload_equality()
    {
        var gen =
            from payload1 in Generator.Object
            from payload2 in Gen.OneOf(Generator.Object, Gen.Const(payload1))
            select (payload1, payload2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (payload1, payload2) = tuple;
            var result1 = Result.Success(payload1);
            var result2 = Result.Success(payload2);

            // Act
            var payloadEquality = payload1.Equals(payload2);
            var resultEquality = result1.Equals(result2);

            // Assert
            await Assert.That(resultEquality)
                        .IsEqualTo(payloadEquality);
        });
    }

    [Test]
    public async Task Error_equality_matches_payload_equality()
    {
        var gen =
            from error1 in ErrorGenerator.Any
            from error2 in Gen.OneOf(ErrorGenerator.Any, Gen.Const(error1))
            select (error1, error2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error1, error2) = tuple;
            var result1 = Result.Error<object>(error1);
            var result2 = Result.Error<object>(error2);

            // Act
            var payloadEquality = error1.Equals(error2);
            var resultEquality = result1.Equals(result2);

            // Assert
            await Assert.That(resultEquality)
                        .IsEqualTo(payloadEquality);
        });
    }

    [Test]
    public async Task Success_and_error_are_not_equal()
    {
        var gen = Gen.Select(ResultGenerator.Success, ResultGenerator.Error);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Assert
            await Assert.That(result1.Equals(result2))
                        .IsFalse();

            await Assert.That(result2.Equals(result1))
                        .IsFalse();
        });
    }

    [Test]
    public async Task Equality_operator_matches_Equals()
    {
        var gen = ResultGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Act
            var equal = result1 == result2;

            // Assert
            await Assert.That(equal)
                        .IsEqualTo(result1.Equals(result2));
        });
    }

    [Test]
    public async Task Inequality_operator_negates_equality()
    {
        var gen = ResultGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Act
            var equality = result1 == result2;
            var inequality = result1 != result2;

            // Assert
            await Assert.That(inequality)
                        .IsEqualTo(!equality);
        });
    }

    [Test]
    public async Task Object_Equals_matches_typed_Equals()
    {
        var gen = ResultGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Act
            var objectResult = Equals(result1, result2);
            var typedResult = result1.Equals(result2);

            // Assert
            await Assert.That(objectResult)
                        .IsEqualTo(typedResult);
        });
    }

    [Test]
    public async Task Object_Equals_returns_false_for_null_or_other_types()
    {
        var gen =
            from result in ResultGenerator.Any
            from other in
                Gen.OneOf(Generator.NullObject,
                          from value in Generator.Object
                          where value is not Result<object>
                          select value)
            select (result, other);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, other) = tuple;

            // Act
            var equals = result.Equals(other);

            // Assert
            await Assert.That(equals)
                        .IsFalse();
        });
    }
}

public class ResultT_GetHashCode_Tests
{
    [Test]
    public async Task Equal_values_have_equal_hash_codes()
    {
        var gen = ResultGenerator.EqualPair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result1, result2) = tuple;

            // Act
            var hash1 = result1.GetHashCode();
            var hash2 = result2.GetHashCode();

            // Assert
            await Assert.That(hash1)
                        .IsEqualTo(hash2);
        });
    }
}

public class ResultT_Error_ImplicitOperator_Tests
{
    [Test]
    public async Task Converts_Error_to_error_result()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Act
            Result<object> result = error;

            // Assert
            await Assert.That(result)
                        .IsEqualTo(Result.Error<object>(error));
        });
    }
}

public class SuccessT2_ToString_Tests
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
            var text = success.ToString();

            // Assert
            await Assert.That(text)
                        .Contains("Success")
                        .And
                        .Contains(value.ToString() ?? string.Empty);
        });
    }
}

public class Result_Success_Tests
{
    [Test]
    public async Task Returns_success_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Act
            var result = Result.Success(value);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}

public class Result_Error_Tests
{
    [Test]
    public async Task Returns_error_with_payload()
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

public class Result_Map_Tests
{
    [Test]
    public async Task Satisfies_functor_identity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var mapped = result.Map(x => x);

            // Assert
            await Assert.That(mapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition_law()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            from g in MapperGenerator.ObjectToObject
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = result.Map(x => g(f(x)));
            var path2 = result.Map(f).Map(g);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_MapTask_Tests
{
    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var traversed = await result.MapTask(ValueTask.FromResult);

            // Assert
            await Assert.That(traversed)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Traversing_with_pure_mapper_is_equivalent_to_map()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;
            ValueTask<object> lift(object x) => ValueTask.FromResult(f(x));

            // Act
            var path1 = await result.MapTask(lift);
            var path2 = result.Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_after_traversal_is_equivalent_to_mapping_inside()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            from g in MapperGenerator.ObjectToObject
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var traversed = await result.MapTask(f);
            var path1 = traversed.Map(g);

            var path2 = await result.MapTask(async x => g(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Composing_async_mappers_is_equivalent_to_successive_mapping()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            from g in MapperGenerator.ObjectToAsyncObject
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var intermediate = await result.MapTask(f);
            var path1 = await intermediate.MapTask(g);

            var path2 = await result.MapTask(async x => await g(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_is_equivalent_to_async_binding_then_wrapping()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var path1 = await result.MapTask(f);
            var path2 = await result.BindTask(async x => Result.Success(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_MapError_Tests
{
    [Test]
    public async Task Satisfies_functor_identity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var mapped = result.MapError(x => x);

            // Assert
            await Assert.That(mapped)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition_law()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ErrorToError
            from g in MapperGenerator.ErrorToError
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = result.MapError(x => g(f(x)));
            var path2 = result.MapError(f).MapError(g);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_success_then_error_is_equivalent_to_mapping_error_then_success()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            from g in MapperGenerator.ErrorToError
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = result.Map(f).MapError(g);
            var path2 = result.MapError(g).Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_Mapping_Semantics_Tests
{
    [Test]
    public async Task Map_transforms_success_payload()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToObject
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var mapped = result.Map(f);

            // Assert
            await Assert.That(mapped)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(f(value));
        });
    }

    [Test]
    public async Task MapError_transforms_error_payload()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ErrorToError
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var mapped = result.MapError(f);

            // Assert
            await Assert.That(mapped)
                        .IsError()
                        .Which
                        .IsEqualTo(f(error));
        });
    }
}

public class Result_Bind_Tests
{
    [Test]
    public async Task Satisfies_monad_left_identity_law()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToResult
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var path1 = result.Bind(f);
            var path2 = f(value);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var bound = result.Bind(Result.Success);

            // Assert
            await Assert.That(bound)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity_law()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToResult
            from g in MapperGenerator.ObjectToResult
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = result.Bind(f).Bind(g);
            var path2 = result.Bind(x => f(x).Bind(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_is_equivalent_to_binding_then_wrapping()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var path1 = result.Map(f);
            var path2 = result.Bind(x => Result.Success(f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Error_preserves_original_error()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ObjectToResult
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var bound = result.Bind(f);

            // Assert
            await Assert.That(bound)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}

public class Result_BindTask_Tests
{
    [Test]
    public async Task Satisfies_monad_left_identity_law()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToAsyncResult
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var path1 = await result.BindTask(f);
            var path2 = await f(value);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity_law()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            async ValueTask<Result<object>> @return(object value)
            {
                await Task.Yield();
                return Result.Success(value);
            }

            // Act
            var bound = await result.BindTask(@return);

            // Assert
            await Assert.That(bound)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity_law()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncResult
            from g in MapperGenerator.ObjectToAsyncResult
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var intermediate = await result.BindTask(f);
            var path1 = await intermediate.BindTask(g);

            var path2 = await result.BindTask(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Binding_with_pure_binder_is_equivalent_to_bind()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToResult
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;
            ValueTask<Result<object>> lift(object x) => ValueTask.FromResult(f(x));

            // Act
            var path1 = await result.BindTask(lift);
            var path2 = result.Bind(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Binding_is_equivalent_to_traversal_then_flattening()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncResult
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var path1 = await result.BindTask(f);

            var traversed = await result.MapTask(f);
            var path2 = traversed.Bind(x => x);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_Select_Tests
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_map()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var path1 = from x in result
                        select f(x);
            var path2 = result.Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_SelectMany_Tests
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_bind()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToResult
            from g in MapperGenerator.ObjectToObject
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = from x in result
                        from y in f(x)
                        select (x, g(y));
            var path2 = result.Bind(x => f(x).Map(y => (x, g(y))));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_IfError_WithValueFallback_Tests
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ErrorToObject
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var unwrapped = result.IfError(f);

            // Assert
            await Assert.That(unwrapped)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_returns_fallback_for_its_error()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ErrorToObject
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var unwrapped = result.IfError(f);

            // Assert
            await Assert.That(unwrapped)
                        .IsEqualTo(f(error));
        });
    }
}

public class Result_IfError_WithResultFallback_Tests
{
    [Test]
    public async Task Recovering_an_error_is_equivalent_to_applying_fallback()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ErrorToResult
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var path1 = result.IfError(f);
            var path2 = f(error);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Recovering_with_error_constructor_returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Act
            var recovered = result.IfError(Result.Error<object>);

            // Assert
            await Assert.That(recovered)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Composing_recoveries_is_equivalent_to_successive_recovery()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ErrorToResult
            from g in MapperGenerator.ErrorToResult
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var path1 = result.IfError(f).IfError(g);
            var path2 = result.IfError(x => f(x).IfError(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Success_returns_original_result()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ErrorToResult
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var recovered = result.IfError(f);

            // Assert
            await Assert.That(recovered)
                        .IsEqualTo(result);
        });
    }
}

public class Result_IfErrorTask_WithValueFallback_Tests
{
    [Test]
    public async Task Matches_effectful_elimination()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ErrorToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            async ValueTask<object> fallback(Error error)
            {
                await Task.Yield();
                return f(error);
            }

            // Act
            var path1 = await result.IfErrorTask(fallback);
            var path2 = await result.Match(ValueTask.FromResult, fallback);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_IfErrorTask_WithResultFallback_Tests
{
    [Test]
    public async Task Success_returns_original_result()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ErrorToAsyncResult
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var result = Result.Success(value);

            // Act
            var recovered = await result.IfErrorTask(f);

            // Assert
            await Assert.That(recovered)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Recovering_an_error_is_equivalent_to_applying_fallback()
    {
        var gen =
            from error in ErrorGenerator.Any
            from f in MapperGenerator.ErrorToAsyncResult
            select (error, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (error, f) = tuple;
            var result = Result.Error<object>(error);

            // Act
            var path1 = await result.IfErrorTask(f);
            var path2 = await f(error);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Recovering_with_error_constructor_returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            async ValueTask<Result<object>> f(Error error)
            {
                await Task.Yield();
                return Result.Error<object>(error);
            }

            // Act
            var recovered = await result.IfErrorTask(f);

            // Assert
            await Assert.That(recovered)
                        .IsEqualTo(result);
        });
    }

    [Test]
    public async Task Composing_recoveries_is_equivalent_to_successive_recovery()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ErrorToAsyncResult
            from g in MapperGenerator.ErrorToAsyncResult
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            // Act
            var intermediate = await result.IfErrorTask(f);
            var path1 = await intermediate.IfErrorTask(g);

            var path2 = await result.IfErrorTask(async x => await (await f(x)).IfErrorTask(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Recovering_with_pure_fallback_is_equivalent_to_if_error()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ErrorToResult
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;
            ValueTask<Result<object>> lift(Error error) => ValueTask.FromResult(f(error));

            // Act
            var path1 = await result.IfErrorTask(lift);
            var path2 = result.IfError(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_IfErrorThrow_Tests
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            // Act
            var unwrapped = result.IfErrorThrow();

            // Assert
            await Assert.That(unwrapped)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_throws_its_exception()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);
            var expected = error.ToException();
            var action = () => result.IfErrorThrow();

            // Assert
            await Assert.That(action)
                        .Throws<Exception>()
                        .WithMessage(expected.Message, StringComparison.Ordinal);
        });
    }
}

public class Result_IfErrorNull_Tests
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            // Act
            var unwrapped = result.IfErrorNull();

            // Assert
            await Assert.That(unwrapped)
                        .IsEqualTo(value);
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
            var unwrapped = result.IfErrorNull();

            // Assert
            await Assert.That(unwrapped)
                        .IsNull();
        });
    }
}

public class Result_IfErrorNullable_Tests
{
    [Test]
    public async Task Success_returns_its_value()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            // Act
            var unwrapped = result.IfErrorNullable();

            // Assert
            await Assert.That(unwrapped)
                        .IsEqualTo(value);
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
            var unwrapped = result.IfErrorNullable();

            // Assert
            await Assert.That(unwrapped)
                        .IsNull();
        });
    }
}

public class Result_Tap_Tests
{
    [Test]
    public async Task Success_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            object? observedPayload = null;
            void f(object payload) => observedPayload = payload;

            // Act
            result.Tap(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public void Error_does_not_execute_callback()
    {
        var gen = ErrorGenerator.Any;

        gen.Sample(error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            void f(object _) => throw new UnreachableException();

            // Act
            result.Tap(f);
        });
    }

    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            void f(object _) { }

            // Act
            var tapped = result.Tap(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }
}

public class Result_TapTask_Tests
{
    [Test]
    public async Task Success_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            object? observedPayload = null;

            async ValueTask f(object payload)
            {
                await Task.Yield();
                observedPayload = payload;
            }

            // Act
            await result.TapTask(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_does_not_execute_callback()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            ValueTask f(object _) => throw new UnreachableException();

            // Act
            await result.TapTask(f);
        });
    }

    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            ValueTask f(object _) => ValueTask.CompletedTask;

            // Act
            var tapped = await result.TapTask(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }
}

public class Result_TapError_Tests
{
    [Test]
    public async Task Error_observes_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            Error? observedError = null;
            void f(Error payload) => observedError = payload;

            // Act
            result.TapError(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public void Success_does_not_execute_callback()
    {
        var gen = ResultGenerator.Success;

        gen.Sample(result =>
        {
            // Arrange
            void f(Error _) => throw new UnreachableException();

            // Act
            result.TapError(f);
        });
    }

    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            void f(Error _) { }

            // Act
            var tapped = result.TapError(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }
}

public class Result_TapErrorTask_Tests
{
    [Test]
    public async Task Error_observes_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            Error? observedError = null;

            async ValueTask f(Error payload)
            {
                await Task.Yield();
                observedError = payload;
            }

            // Act
            await result.TapErrorTask(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public async Task Success_does_not_execute_callback()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            ValueTask f(Error _) => throw new UnreachableException();

            // Act
            await result.TapErrorTask(f);
        });
    }

    [Test]
    public async Task Returns_original_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            ValueTask f(Error _) => ValueTask.CompletedTask;

            // Act
            var tapped = await result.TapErrorTask(f);

            // Assert
            await Assert.That(tapped)
                        .IsEqualTo(result);
        });
    }
}

public class Result_Iter_Tests
{
    [Test]
    public async Task Success_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            object? observedPayload = null;
            void f(object payload) => observedPayload = payload;

            // Act
            result.Iter(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public void Error_does_not_execute_callback()
    {
        var gen = ErrorGenerator.Any;

        gen.Sample(error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            void f(object _) => throw new UnreachableException();

            // Act
            result.Iter(f);
        });
    }
}

public class Result_IterTask_Tests
{
    [Test]
    public async Task Success_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            object? observedPayload = null;

            async ValueTask f(object payload)
            {
                await Task.Yield();
                observedPayload = payload;
            }

            // Act
            await result.IterTask(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_does_not_execute_callback()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            ValueTask f(object _) => throw new UnreachableException();

            // Act
            await result.IterTask(f);
        });
    }
}

public class Result_IterError_Tests
{
    [Test]
    public async Task Error_observes_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            Error? observedError = null;
            void f(Error payload) => observedError = payload;

            // Act
            result.IterError(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public void Success_does_not_execute_callback()
    {
        var gen = ResultGenerator.Success;

        gen.Sample(result =>
        {
            // Arrange
            void f(Error _) => throw new UnreachableException();

            // Act
            result.IterError(f);
        });
    }
}

public class Result_IterErrorTask_Tests
{
    [Test]
    public async Task Error_observes_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            Error? observedError = null;

            async ValueTask f(Error payload)
            {
                await Task.Yield();
                observedError = payload;
            }

            // Act
            await result.IterErrorTask(f);

            // Assert
            await Assert.That(observedError)
                        .IsEqualTo(error);
        });
    }

    [Test]
    public async Task Success_does_not_execute_callback()
    {
        var gen = ResultGenerator.Success;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            ValueTask f(Error _) => throw new UnreachableException();

            // Act
            await result.IterErrorTask(f);
        });
    }
}

public class Result_ToOption_Tests
{
    [Test]
    public async Task Success_returns_some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var result = Result.Success(value);

            // Act
            var option = result.ToOption();

            // Assert
            await Assert.That(option)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Error_returns_none()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var result = Result.Error<object>(error);

            // Act
            var option = result.ToOption();

            // Assert
            await Assert.That(option)
                        .IsNone();
        });
    }

    [Test]
    public async Task Conversion_preserves_mapping()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            // Act
            var path1 = result.Map(f).ToOption();
            var path2 = result.ToOption().Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_Select_WithValueTask_Tests
{
    [Test]
    public async Task LINQ_is_equivalent_to_awaiting_then_mapping()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (result, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f) = tuple;

            async ValueTask<Result<object>> source()
            {
                await Task.Yield();
                return result;
            }

            // Act
#pragma warning disable CA2012
            var path1 = await (from x in source()
                               select f(x));
#pragma warning restore CA2012

            var awaited = await source();
            var path2 = awaited.Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_SelectMany_WithValueTask_Tests
{
    [Test]
    public async Task LINQ_is_equivalent_to_awaiting_then_binding()
    {
        var gen =
            from result in ResultGenerator.Any
            from f in MapperGenerator.ObjectToAsyncResult
            from g in MapperGenerator.ObjectToObject
            select (result, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (result, f, g) = tuple;

            async ValueTask<Result<object>> source()
            {
                await Task.Yield();
                return result;
            }

            // Act
#pragma warning disable CA2012
            var path1 = await (from x in source()
                               from y in f(x)
                               select (x, g(y)));
#pragma warning restore CA2012

            var awaited = await source();
            var path2 = await awaited.BindTask(async x => (await f(x)).Map(y => (x, g(y))));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Result_ToAsync_WithValue_Tests
{
    [Test]
    public async Task Wraps_awaited_value_in_success()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            async ValueTask<object> f()
            {
                await Task.Yield();
                return value;
            }

            // Act
            var result = await Result.ToAsync(f);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}

public class Result_ToAsync_WithAsyncResult_Tests
{
    [Test]
    public async Task Preserves_awaited_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            async ValueTask<Result<object>> f()
            {
                await Task.Yield();
                return result;
            }

            // Act
            var lifted = await Result.ToAsync(f);

            // Assert
            await Assert.That(lifted)
                        .IsEqualTo(result);
        });
    }
}

public class Result_ToAsync_WithResult_Tests
{
    [Test]
    public async Task Preserves_result()
    {
        var gen = ResultGenerator.Any;

        await gen.SampleAsync(async result =>
        {
            // Arrange
            Result<object> f() => result;

            // Act
            var lifted = await Result.ToAsync(f);

            // Assert
            await Assert.That(lifted)
                        .IsEqualTo(result);
        });
    }
}

public class Result_ToAsync_WithAction_Tests
{
    [Test]
    public async Task Observes_action_and_returns_successful_unit()
    {
        // Arrange
        var executed = false;

        async ValueTask f()
        {
            await Task.Yield();
            executed = true;
        }

        // Act
        var result = await Result.ToAsync(f);

        // Assert
        await Assert.That(executed)
                    .IsTrue();

        await Assert.That(result)
                    .IsSuccess()
                    .WhoseValue
                    .IsEqualTo(Unit.Instance);
    }
}

