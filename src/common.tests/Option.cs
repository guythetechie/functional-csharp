using CsCheck;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class None2_Tests
{
    [Test]
    public async Task ToString_returns_None()
    {
        // Arrange
        var none = new None();

        // Act
        var result = none.ToString();

        // Assert
        await Assert.That(result)
                    .IsEqualTo("None");
    }

    [Test]
    public async Task None_equals_default_none()
    {
        // Arrange
        var none1 = new None();
        var none2 = default(None);

        // Act
        var result = none1.Equals(none2);

        // Assert
        await Assert.That(result)
                    .IsTrue();
    }

    [Test]
    public async Task Equal_values_have_equal_hash_codes()
    {
        // Arrange
        var none1 = new None();
        var none2 = default(None);

        // Act
        var hash1 = none1.GetHashCode();
        var hash2 = none2.GetHashCode();

        // Assert
        await Assert.That(hash1)
                    .IsEqualTo(hash2);
    }
}

public class OptionT_Match_Tests
{
    [Test]
    public async Task Some_returns_some_function_result()
    {
        var gen =
            from value in Generator.Object
            from mapper in MapperGenerator.ObjectToObject
            select (value, mapper);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, mapper) = tuple;
            var option = Option.Some(value);
            object fallback() => throw new UnreachableException();

            // Act
            var result = option.Match(mapper, fallback);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(mapper(value));
        });
    }

    [Test]
    public async Task None_returns_none_function_result()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async fallback =>
        {
            // Arrange
            var option = Option<object>.None;
            object mapper(object _) => throw new UnreachableException();

            // Act
            var result = option.Match(mapper, () => fallback);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(fallback);
        });
    }

    [Test]
    public async Task Reconstructs_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Match(Option.Some, () => Option<object>.None);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }
}

public class OptionT_Match_WithAction_Tests
{
    [Test]
    public async Task Some_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            object? observedPayload = null;
            void onSome(object payload) => observedPayload = payload;

            void onNone() => throw new UnreachableException();

            // Act
            option.Match(onSome, onNone);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);

        });
    }

    [Test]
    public async Task None_executes_callback()
    {
        // Arrange
        var option = Option<object>.None;

        void onSome(object _) => throw new UnreachableException();

        var noneExecuted = false;
        void onNone() => noneExecuted = true;

        // Act
        option.Match(onSome, onNone);

        // Assert
        await Assert.That(noneExecuted)
                    .IsTrue();
    }
}

public class OptionT_ToString_Tests
{
    [Test]
    public async Task Some_returns_Some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            // Act
            var result = option.ToString();

            // Assert
            await Assert.That(result)
                        .Contains("Some")
                        .And
                        .Contains(value.ToString()!);
        });
    }

    [Test]
    public async Task None_returns_None()
    {
        // Arrange
        var option = new Option<object>(new None());

        // Act
        var result = option.ToString();

        // Assert
        await Assert.That(result)
                    .IsEqualTo("None");
    }
}

public class OptionT_Equality_Tests
{
    [Test]
    public async Task Satisfies_equality_reflexivity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Equals(option);

            // Assert
            await Assert.That(result)
                        .IsTrue();
        });
    }

    [Test]
    public async Task Satisfies_equality_symmetry_law()
    {
        var gen = OptionGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Act
            var forward = option1.Equals(option2);
            var reverse = option2.Equals(option1);

            // Assert
            await Assert.That(forward)
                        .IsEqualTo(reverse);
        });
    }

    [Test]
    public async Task Satisfies_equality_transitivity_law()
    {
        var gen =
            from option1 in OptionGenerator.Any
            from option2 in
                Gen.OneOf(OptionGenerator.Any,
                          OptionGenerator.GenerateEqual(option1))
            from option3 in
                Gen.OneOf(OptionGenerator.Any,
                          OptionGenerator.GenerateEqual(option2))
            select (option1, option2, option3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2, option3) = tuple;

            // Assert
            if (option1.Equals(option2) && option2.Equals(option3))
            {
                await Assert.That(option1.Equals(option3))
                            .IsTrue();
            }
        });
    }

    [Test]
    public async Task Some_equality_matches_payload_equality()
    {
        var gen =
            from payload1 in Generator.Object
            from payload2 in Gen.OneOf(Generator.Object, Gen.Const(payload1))
            select (payload1, payload2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (payload1, payload2) = tuple;
            var option1 = Option.Some(payload1);
            var option2 = Option.Some(payload2);

            // Act
            var payloadEquality = payload1.Equals(payload2);
            var optionEquality = option1.Equals(option2);

            // Assert
            await Assert.That(optionEquality)
                        .IsEqualTo(payloadEquality);
        });
    }

    [Test]
    public async Task Some_and_none_are_not_equal()
    {
        var gen = OptionGenerator.Some;

        await gen.SampleAsync(async some =>
        {
            // Arrange
            var none = Option<object>.None;

            // Assert
            await Assert.That(some.Equals(none))
                        .IsFalse();

            await Assert.That(none.Equals(some))
                        .IsFalse();
        });
    }

    [Test]
    public async Task Nones_are_equal()
    {
        // Arrange
        var first = Option<object>.None;
        var second = new Option<object>(new None());

        // Act
        var result = first.Equals(second);

        // Assert
        await Assert.That(result)
                    .IsTrue();
    }

    [Test]
    public async Task Equality_operator_matches_Equals()
    {
        var gen = OptionGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Act
            var operatorResult = option1 == option2;
            var equalsResult = option1.Equals(option2);

            // Assert
            await Assert.That(operatorResult)
                        .IsEqualTo(equalsResult);
        });
    }

    [Test]
    public async Task Inequality_operator_negates_equality()
    {
        var gen = OptionGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Act
            var equality = option1 == option2;
            var inequality = option1 != option2;

            // Assert
            await Assert.That(inequality)
                        .IsEqualTo(!equality);
        });
    }

    [Test]
    public async Task Object_Equals_matches_typed_Equals()
    {
        var gen = OptionGenerator.Pair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Act
            var objectResult = Equals(option1, option2);
            var typedResult = option1.Equals(option2);

            // Assert
            await Assert.That(objectResult)
                        .IsEqualTo(typedResult);
        });
    }

    [Test]
    public async Task Object_Equals_returns_false_for_null_or_other_types()
    {
        var gen =
            from option in OptionGenerator.Any
            from other in
                Gen.OneOf(Generator.NullObject,
                          from value in Generator.Object
                          where value is not Option<object>
                          select value)
            select (option, other);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, other) = tuple;

            // Act
            var result = option.Equals(other);

            // Assert
            await Assert.That(result)
                        .IsFalse();
        });
    }
}

public class OptionT_GetHashCode_Tests
{
    [Test]
    public async Task Equal_values_have_equal_hash_codes()
    {
        var gen = OptionGenerator.EqualPair;

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Act
            var hash1 = option1.GetHashCode();
            var hash2 = option2.GetHashCode();

            // Assert
            await Assert.That(hash1)
                        .IsEqualTo(hash2);
        });
    }
}

public class OptionT_None_ImplicitOperator_Tests
{
    [Test]
    public async Task Converts_None_to_none_option()
    {
        // Arrange
        var none = new None();

        // Act
        Option<object> option = none;

        // Assert
        await Assert.That(option)
                    .IsNone();
    }
}

public class OptionT_Some_ToString_Tests
{
    [Test]
    public async Task Returns_Some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var some = new Option<object>.Some(value);

            // Act
            var result = some.ToString();

            // Assert
            await Assert.That(result)
                        .Contains("Some")
                        .And
                        .Contains(value.ToString()!);
        });
    }
}

public class Option_Some_Tests
{
    [Test]
    public async Task Returns_some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Act
            var result = Option.Some(value);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}

public class Option_From_ReferenceType_Tests
{
    [Test]
    public async Task Non_null_returns_some_with_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            object? nullable = value;

            // Act
            var result = Option.From(nullable);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Null_returns_none()
    {
        // Arrange
        object? nullable = null;

        // Act
        var result = Option.From(nullable);

        // Assert
        await Assert.That(result)
                    .IsNone();
    }
}

public class Option_From_NullableValueType_Tests
{
    [Test]
    public async Task Value_returns_some_with_value()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            int? nullable = value;

            // Act
            var result = Option.From(nullable);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task Null_returns_none()
    {
        // Arrange
        int? nullable = null;

        // Act
        var result = Option.From(nullable);

        // Assert
        await Assert.That(result)
                    .IsNone();
    }
}

public class Option_Where_Tests
{
    [Test]
    public async Task Successive_filters_are_equivalent_to_conjoining_predicates()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectPredicate
            from g in MapperGenerator.ObjectPredicate
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var path1 = option.Where(f).Where(g);
            var path2 = option.Where(x => f(x) && g(x));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Filtering_is_equivalent_to_binding_with_guard()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectPredicate
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = option.Where(f);
            var path2 = option.Bind(x => f(x) ? Option.Some(x) : Option.None);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_Map_Tests
{
    [Test]
    public async Task Satisfies_functor_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Map(x => x);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_functor_composition_law()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            from g in MapperGenerator.ObjectToObject
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var path1 = option.Map(x => g(f(x)));
            var path2 = option.Map(f).Map(g);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_is_equivalent_to_binding_then_wrapping()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = option.Map(f);
            var path2 = option.Bind(x => Option.Some(f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_MapTask_Tests
{
    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = await option.MapTask(ValueTask.FromResult);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Traversing_with_pure_mapper_is_equivalent_to_map()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = await option.MapTask(x => ValueTask.FromResult(f(x)));
            var path2 = option.Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_after_traversal_is_equivalent_to_mapping_inside()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            from g in MapperGenerator.ObjectToObject
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var traversed = await option.MapTask(f);
            var path1 = traversed.Map(g);

            var path2 = await option.MapTask(async x => g(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Composing_async_mappers_is_equivalent_to_successive_mapping()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            from g in MapperGenerator.ObjectToAsyncObject
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var intermediate = await option.MapTask(f);
            var path1 = await intermediate.MapTask(g);

            var path2 = await option.MapTask(async x => await g(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_is_equivalent_to_async_binding_then_wrapping()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToAsyncObject
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = await option.MapTask(f);
            var path2 = await option.BindTask(async x => Option.Some(await f(x)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_Bind_Tests
{
    [Test]
    public async Task Satisfies_monad_left_identity_law()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToOption
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;
            var option = Option.Some(value);

            // Act
            var path1 = option.Bind(f);
            var path2 = f(value);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Bind(Option.Some);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity_law()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToOption
            from g in MapperGenerator.ObjectToOption
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var path1 = option.Bind(f).Bind(g);
            var path2 = option.Bind(x => f(x).Bind(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_left_zero_law()
    {
        var gen = MapperGenerator.ObjectToOption;

        await gen.SampleAsync(async f =>
        {
            // Arrange
            var option = Option<object>.None;

            // Act
            var result = option.Bind(f);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_right_zero_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Bind(_ => Option<object>.None);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }
}

public class Option_Traverse_WithResult_Tests
{
    [Test]
    public async Task Satisfies_traversal_purity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.Traverse(Result.Success);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Traversing_with_pure_mapper_is_equivalent_to_map()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = option.Traverse(x => Result.Success(f(x)));
            var path2 = Result.Success(option.Map(f));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_after_traversal_is_equivalent_to_mapping_inside()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToResult
            from g in MapperGenerator.ObjectToObject
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var path1 = option.Traverse(f).Map(x => x.Map(g));
            var path2 = option.Traverse(x => f(x).Map(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_traversal_naturality_law_for_ToOption()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToResult
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = option.Traverse(f).ToOption();
            var path2 = option.Match(x => f(x).ToOption().Map(Option.Some),
                                     () => Option.Some(Option<object>.None));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_BindTask_Tests
{
    [Test]
    public async Task Satisfies_monad_left_identity_law()
    {
        var gen =
            from value in Generator.Object
            from f in MapperGenerator.ObjectToAsyncOption
            select (value, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, f) = tuple;

            // Act
            var path1 = await Option.Some(value).BindTask(f);
            var path2 = await f(value);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask<Option<object>> @return(object value)
            {
                await Task.Yield();
                return Option.Some(value);
            }

            // Act
            var result = await option.BindTask(@return);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity_law()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToAsyncOption
            from g in MapperGenerator.ObjectToAsyncOption
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var intermediate = await option.BindTask(f);
            var path1 = await intermediate.BindTask(g);

            var path2 = await option.BindTask(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_left_zero_law()
    {
        var gen = MapperGenerator.ObjectToAsyncOption;

        await gen.SampleAsync(async f =>
        {
            // Arrange
            var option = Option<object>.None;

            // Act
            var result = await option.BindTask(f);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_right_zero_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask<Option<object>> returnNone(object value)
            {
                await Task.Yield();
                return Option<object>.None;
            }

            // Act
            var result = await option.BindTask(returnNone);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Binding_is_equivalent_to_traversal_then_flattening()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToAsyncOption
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = await option.BindTask(f);

            var traversed = await option.MapTask(f);
            var path2 = traversed.Bind(x => x);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_Select_Tests
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_map()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = from x in option
                        select f(x);

            var path2 = option.Map(f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_SelectMany_Tests
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_bind()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in MapperGenerator.ObjectToOption
            from g in
                from leftMapper in MapperGenerator.ObjectToObject
                from rightMapper in MapperGenerator.ObjectToObject
                select new Func<object, object, object>((x, y) => (leftMapper(x), rightMapper(y)))
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var path1 = from x in option
                        from y in f(x)
                        select g(x, y);

            var path2 = option.Bind(x => f(x).Map(y => g(x, y)));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_IfNone_WithValueFallback_Tests
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen =
            from value in Generator.Object
            from fallback in
                from obj in Generator.Object
                select new Func<object>(() => obj)
            select (value, fallback);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, fallback) = tuple;
            var option = Option.Some(value);

            // Act
            var result = option.IfNone(fallback);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_returns_fallback()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async fallback =>
        {
            // Arrange
            var option = Option<object>.None;

            // Act
            var result = option.IfNone(() => fallback);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(fallback);
        });
    }
}

public class Option_IfNone_WithOptionFallback_Tests
{
    [Test]
    public async Task Satisfies_monoid_left_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            var none = Option<object>.None;

            // Act
            var result = none.IfNone(() => option);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monoid_right_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = option.IfNone(() => Option<object>.None);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monoid_associativity_law()
    {
        var gen =
            from first in OptionGenerator.Any
            from second in OptionGenerator.Any
            from third in OptionGenerator.Any
            select (first, second, third);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, second, third) = tuple;

            // Act
            var path1 = first.IfNone(() => second).IfNone(() => third);
            var path2 = first.IfNone(() => second.IfNone(() => third));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Mapping_over_choice_is_equivalent_to_choosing_mapped_options()
    {
        var gen =
            from first in OptionGenerator.Any
            from second in OptionGenerator.Any
            from f in MapperGenerator.ObjectToObject
            select (first, second, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, second, f) = tuple;

            // Act
            var path1 = first.IfNone(() => second).Map(f);
            var path2 = first.Map(f).IfNone(() => second.Map(f));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Some_returns_original_option()
    {
        var gen =
            from value in Generator.Object
            from fallback in
                from option in OptionGenerator.Any
                select new Func<Option<object>>(() => option)
            select (value, fallback);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, fallback) = tuple;

            // Act
            var result = Option.Some(value).IfNone(fallback);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}

public class Option_IfNoneTask_WithValueFallback_Tests
{
    [Test]
    public async Task Matches_effectful_elimination()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in
                from obj in Generator.Object
                select new Func<ValueTask<object>>(async () =>
                {
                    await Task.Yield();
                    return obj;
                })
            select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var path1 = await option.IfNoneTask(f);
            var path2 = await option.Match(ValueTask.FromResult, f);

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }
}

public class Option_IfNoneTask_WithOptionFallback_Tests
{
    [Test]
    public async Task Satisfies_monoid_left_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            var none = Option<object>.None;

            async ValueTask<Option<object>> f()
            {
                await Task.Yield();
                return option;
            }

            // Act
            var result = await none.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monoid_right_identity_law()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            async ValueTask<Option<object>> f()
            {
                await Task.Yield();
                return Option<object>.None;
            }

            // Act
            var result = await option.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monoid_associativity_law()
    {
        var gen =
            from first in OptionGenerator.Any
            from second in OptionGenerator.Any
            from third in
                from option in OptionGenerator.Any
                select new Func<ValueTask<Option<object>>>(async () =>
                {
                    await Task.Yield();
                    return option;
                })
            select (first, second, third);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (first, second, third) = tuple;

            async ValueTask<Option<object>> secondAsync()
            {
                await Task.Yield();
                return second;
            }

            // Act
            var intermediate = await first.IfNoneTask(secondAsync);
            var path1 = await intermediate.IfNoneTask(third);

            var path2 = await first.IfNoneTask(async () => await second.IfNoneTask(third));

            // Assert
            await Assert.That(path1)
                        .IsEqualTo(path2);
        });
    }

    [Test]
    public async Task Some_returns_original_option()
    {
        var gen =
            from value in Generator.Object
            from fallback in
                from option in OptionGenerator.Any
                select new Func<ValueTask<Option<object>>>(async () =>
                {
                    await Task.Yield();
                    return option;
                })
            select (value, fallback);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, fallback) = tuple;

            // Act
            var result = await Option.Some(value).IfNoneTask(fallback);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }
}

public class Option_IfNoneNull_Tests
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            // Act
            var result = option.IfNoneNull();

            // Assert
            await Assert.That(result)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_returns_null()
    {
        // Arrange
        var option = Option<object>.None;

        // Act
        var result = option.IfNoneNull();

        // Assert
        await Assert.That(result)
                    .IsNull();
    }
}

public class Option_IfNoneNullable_Tests
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            // Act
            var result = option.IfNoneNullable();

            // Assert
            await Assert.That(result)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_returns_null()
    {
        // Arrange
        var option = Option<int>.None;

        // Act
        var result = option.IfNoneNullable();

        // Assert
        await Assert.That(result)
                    .IsNull();
    }
}

public class Option_IfNoneThrow_Tests
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);
            Exception f() => throw new UnreachableException();

            // Act
            var result = option.IfNoneThrow(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_throws_exception()
    {
        var gen = ExceptionGenerator.Any;

        await gen.SampleAsync(async exception =>
        {
            // Arrange
            var option = Option<object>.None;
            var action = () => option.IfNoneThrow(() => exception);

            // Assert
            await Assert.That(action)
                        .Throws<Exception>()
                        .WithMessage(exception.Message, StringComparison.Ordinal);
        });
    }
}

public class Option_Tap_Tests
{
    [Test]
    public async Task Some_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);
            object? observedPayload = null;

            // Act
            option.Tap(payload => observedPayload = payload);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public void None_does_not_execute_callback()
    {
        // Arrange
        var option = Option<object>.None;
        void f(object _) => throw new UnreachableException();

        // Act
        option.Tap(f);
    }

    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            void f(object _) { }

            // Act
            var result = option.Tap(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }
}

public class Option_TapTask_Tests
{
    [Test]
    public async Task Some_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            object? observedPayload = null;

            async ValueTask f(object payload)
            {
                await Task.Yield();
                observedPayload = payload;
            }

            // Act
            await option.TapTask(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_does_not_execute_callback()
    {
        // Arrange
        var option = Option<object>.None;

        ValueTask f(object _) => throw new UnreachableException();

        // Act
        await option.TapTask(f);
    }

    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            ValueTask f(object _) => ValueTask.CompletedTask;

            // Act
            var result = await option.TapTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }
}

public class Option_TapNone_Tests
{
    [Test]
    public void Some_does_not_execute_callback()
    {
        var gen = Generator.Object;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);
            void f() => throw new UnreachableException();

            // Act
            option.TapNone(f);
        });
    }

    [Test]
    public async Task None_executes_callback()
    {
        // Arrange
        var option = Option<object>.None;
        var executed = false;

        // Act
        option.TapNone(() => executed = true);

        // Assert
        await Assert.That(executed)
                    .IsTrue();
    }

    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            void f() { }

            // Act
            var result = option.TapNone(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }
}

public class Option_TapNoneTask_Tests
{
    [Test]
    public async Task Some_does_not_execute_callback()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            ValueTask f() => throw new UnreachableException();

            // Act
            await option.TapNoneTask(f);
        });
    }

    [Test]
    public async Task None_executes_callback()
    {
        // Arrange
        var option = Option<object>.None;

        var executed = false;

        async ValueTask f()
        {
            await Task.Yield();
            executed = true;
        }

        // Act
        await option.TapNoneTask(f);

        // Assert
        await Assert.That(executed)
                    .IsTrue();
    }

    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            ValueTask f() => ValueTask.CompletedTask;

            // Act
            var result = await option.TapNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }
}

public class Option_Iter_Tests
{
    [Test]
    public async Task Some_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);
            object? observedPayload = null;

            // Act
            option.Iter(payload => observedPayload = payload);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public void None_does_not_execute_callback()
    {
        // Arrange
        var option = Option<object>.None;
        void f(object _) => throw new UnreachableException();

        // Act
        option.Iter(f);
    }
}

public class Option_IterTask_Tests
{
    [Test]
    public async Task Some_observes_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            object? observedPayload = null;

            async ValueTask f(object payload)
            {
                await Task.Yield();
                observedPayload = payload;
            }

            // Act
            await option.IterTask(f);

            // Assert
            await Assert.That(observedPayload)
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_does_not_execute_callback()
    {
        // Arrange
        var option = Option<object>.None;

        ValueTask f(object _) => throw new UnreachableException();

        // Act
        await option.IterTask(f);
    }
}

public class Option_IterNone_Tests
{
    [Test]
    public void Some_does_not_execute_callback()
    {
        var gen = Generator.Object;

        gen.Sample(value =>
        {
            // Arrange
            var option = Option.Some(value);
            void f() => throw new UnreachableException();

            // Act
            option.IterNone(f);
        });
    }

    [Test]
    public async Task None_executes_callback()
    {
        // Arrange
        var option = Option<object>.None;
        var executed = false;

        // Act
        option.IterNone(() => executed = true);

        // Assert
        await Assert.That(executed)
                    .IsTrue();
    }
}

public class Option_IterNoneTask_Tests
{
    [Test]
    public async Task Some_does_not_execute_callback()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async value =>
        {
            // Arrange
            var option = Option.Some(value);

            ValueTask f() => throw new UnreachableException();

            // Act
            await option.IterNoneTask(f);
        });
    }

    [Test]
    public async Task None_executes_callback()
    {
        // Arrange
        var option = Option<object>.None;

        var executed = false;

        async ValueTask f()
        {
            await Task.Yield();
            executed = true;
        }

        // Act
        await option.IterNoneTask(f);

        // Assert
        await Assert.That(executed)
                    .IsTrue();
    }
}

public class Option_OptionGenerator_Any_Tests
{
    [Test]
    public async Task Can_generate_some_option()
    {
        // Arrange
        var gen = OptionGenerator.Any;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(option => option.IsSome);
    }

    [Test]
    public async Task Can_generate_none_option()
    {
        // Arrange
        var gen = OptionGenerator.Any;

        // Assert
        await Assert.That(gen)
                    .GeneratesValueSatisfying(option => option.IsNone);
    }
}

public class Option_ToResult_Tests
{
    [Test]
    public async Task Some_returns_success()
    {
        var gen =
            from value in Generator.Object
            from errorFactory in
                from error in ErrorGenerator.Any
                select new Func<Error>(() => error)
            select (value, errorFactory);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (value, errorFactory) = tuple;
            var option = Option.Some(value);

            // Act
            var result = option.ToResult(errorFactory);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(value);
        });
    }

    [Test]
    public async Task None_returns_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            var option = Option<object>.None;

            // Act
            var result = option.ToResult(() => error);

            // Assert
            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}