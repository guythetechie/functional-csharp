using CsCheck;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace common.tests;

public class None_ToString_Tests
{
    [Test]
    public async Task Returns_None()
    {
        // Arrange
        var none = new None();

        // Act
        var result = none.ToString();

        // Assert
        await Assert.That(result)
                    .IsEqualTo("None");
    }
}

public class None_GetHashCode_Tests
{
    [Test]
    public async Task Returns_zero()
    {
        // Arrange
        var none = new None();

        // Act
        var hashCode = none.GetHashCode();

        // Assert
        await Assert.That(hashCode)
                    .IsEqualTo(0);
    }
}

public class OptionT_Match_Tests()
{
    [Test]
    public async Task Some_returns_some_function_result()
    {
        var gen = from x in Generator.Object
                  from f in MapperGenerator.ObjectToObject
                  from y in Generator.Object
                  select (x, f, y);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f, y) = tuple;
            var option = Option.Some(x);

            // Act
            var result = option.Match(f, () => y);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(f(x));
        });
    }

    [Test]
    public async Task None_returns_none_function_result()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Act
            var result = Option<object>.None.Match(value => value, () => x);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
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

public class OptionT_Match_WithAction_Tests()
{
    [Test]
    public async Task Some_executes_some_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var option = Option.Some(x);
            object? observed = null;
            var noneExecuted = false;

            // Act
            option.Match(value => observed = value,
                         () => noneExecuted = true);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);
            await Assert.That(noneExecuted)
                        .IsFalse();
        });
    }

    [Test]
    public async Task None_executes_none_action()
    {
        // Arrange
        object? observed = null;
        var noneExecuted = false;

        // Act
        Option<object>.None
                      .Match(value => observed = value,
                             () => noneExecuted = true);

        // Assert
        await Assert.That(observed)
                    .IsNull();
        await Assert.That(noneExecuted)
                    .IsTrue();
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

public class OptionT_None_ImplicitOperator_Tests
{
    [Test]
    public async Task Implicitly_converts_None_to_an_option_in_the_none_state()
    {
        var option = Option<object>.None;

        await Assert.That(option).IsNone();
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
            var some = new Option<object>.Some(value);
            var option = new Option<object>(some);

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
        var none = new None();
        var option = new Option<object>(none);

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
    public async Task Equality_is_reflexive()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Assert
            await Assert.That(option.Equals(option))
                        .IsTrue();

#pragma warning disable CS1718 // Comparison made to same variable
            await Assert.That(option == option)
#pragma warning restore CS1718 // Comparison made to same variable
                        .IsTrue();
        });
    }

    [Test]
    public async Task Equality_is_symmetric()
    {
        var gen = from x1 in Generator.Object
                  from x2 in Generator.Object
                  let optionGenerator =
                    Gen.OneOfConst(Option.Some(x1),
                                   Option.Some(x2),
                                   Option.None)
                  from option1 in optionGenerator
                  from option2 in optionGenerator
                  select (option1, option2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2) = tuple;

            // Assert
            await Assert.That(option1.Equals(option2))
                        .IsEqualTo(option2.Equals(option1));

            await Assert.That(option1 == option2)
                        .IsEqualTo(option2 == option1);
        });
    }

    [Test]
    public async Task Equality_is_transitive()
    {
        var gen = from x1 in Generator.Object
                  from x2 in Generator.Object
                  let optionGenerator =
                    Gen.OneOfConst(Option.Some(x1),
                                   Option.Some(x2),
                                   Option.None)
                  from option1 in optionGenerator
                  from option2 in optionGenerator
                  from option3 in optionGenerator
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

                await Assert.That(option1 == option3)
                            .IsTrue();
            }
        });
    }
}

public class Option_Some_Tests
{
    [Test]
    public async Task Returns_an_option_in_the_some_state()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Act
            var option = Option.Some(x);

            // Assert
            await Assert.That(option)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }
}

public class Option_Where_Tests()
{
    [Test]
    public async Task Satisfies_predicate_conjunction()
    {
        var gen = from option in OptionGenerator.Any
                  from predicate1 in MapperGenerator.ObjectPredicate
                  from predicate2 in MapperGenerator.ObjectPredicate
                  select (option, predicate1, predicate2);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, predicate1, predicate2) = tuple;

            // Act
            var result1 = option.Where(predicate1)
                                .Where(predicate2);

            var result2 = option.Where(x => predicate1(x) && predicate2(x));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monadic_guard()
    {
        var gen = from option in OptionGenerator.Any
                  from predicate in MapperGenerator.ObjectPredicate
                  select (option, predicate);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, predicate) = tuple;

            // Act
            var result1 = option.Where(predicate);
            var result2 = option.Bind(x => predicate(x) ? Option.Some(x) : Option.None);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Option_Map_Tests()
{
    [Test]
    public async Task Satisfies_functor_identity()
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
    public async Task Satisfies_functor_composition()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  from g in MapperGenerator.ObjectToObject
                  select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var result1 = option.Map(x => g(f(x)));

            var result2 = option.Map(f)
                                .Map(g);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_bind_then_return()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var result1 = option.Map(f);
            var result2 = option.Bind(x => Option.Some(f(x)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Option_MapTask_Tests()
{
    [Test]
    public async Task Satisfies_traverse_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask<object> f(object x)
            {
                await Task.Yield();
                return x;
            }

            // Act
            var result = await option.MapTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_traverse_naturality()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in
                from f in MapperGenerator.ObjectToObject
                select new Func<object, ValueTask<object>>(async x =>
                {
                    await Task.Yield();
                    return f(x);
                })
            from g in MapperGenerator.ObjectToObject
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var r1 = await option.MapTask(f);
            var result1 = r1.Map(g);

            var result2 = await option.MapTask(async x =>
            {
                var r2 = await f(x);
                return g(r2);
            });

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_traverse_composition()
    {
        var gen =
            from option in OptionGenerator.Any
            from f in
                from f in MapperGenerator.ObjectToObject
                select new Func<object, ValueTask<object>>(async x =>
                {
                    await Task.Yield();
                    return f(x);
                })
            from g in
                from g in MapperGenerator.ObjectToObject
                select new Func<object, ValueTask<object>>(async x =>
                {
                    await Task.Yield();
                    return g(x);
                })
            select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var r1 = await option.MapTask(f);
            var result1 = await r1.MapTask(g);

            var result2 = await option.MapTask(async x =>
            {
                var r2 = await f(x);
                return await g(r2);
            });

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_BindTask_then_return()
    {
        var gen = from option in OptionGenerator.Any
                  from f in
                      from f in MapperGenerator.ObjectToObject
                      select new Func<object, ValueTask<object>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var result1 = await option.MapTask(f);

            var result2 = await option.BindTask(async x => Option.Some(await f(x)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Option_Bind_Tests()
{
    [Test]
    public async Task Satisfies_monad_left_identity()
    {
        var gen = from x in Generator.Object
                  from f in MapperGenerator.ObjectToOption
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var option = Option.Some(x);

            // Act
            var result1 = option.Bind(f);
            var result2 = f(x);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity()
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
    public async Task Satisfies_monad_associativity()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToOption
                  from g in MapperGenerator.ObjectToOption
                  select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var result1 = option.Bind(f)
                                .Bind(g);

            var result2 = option.Bind(x => f(x).Bind(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_left_zero()
    {
        var gen = MapperGenerator.ObjectToOption;

        await gen.SampleAsync(async f =>
        {
            // Act
            var result = Option<object>.None.Bind(f);

            // Assert
            await Assert.That(result)
                        .IsNone();
        });
    }

    [Test]
    public async Task Satisfies_monad_zero_right_zero()
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

public class Option_BindTask_Tests()
{
    [Test]
    public async Task Satisfies_monad_left_identity()
    {
        var gen = from x in Generator.Object
                  from f in
                      from f in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var option = Option.Some(x);

            // Act
            var result1 = await option.BindTask(f);
            var result2 = await f(x);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_monad_right_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask<Option<object>> f(object x)
            {
                await Task.Yield();
                return Option.Some(x);
            }

            // Act
            var result = await option.BindTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_monad_associativity()
    {
        var gen = from option in OptionGenerator.Any
                  from f in
                      from f in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return f(x);
                      })
                  from g in
                      from g in MapperGenerator.ObjectToOption
                      select new Func<object, ValueTask<Option<object>>>(async x =>
                      {
                          await Task.Yield();
                          return g(x);
                      })
                  select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var result1 = await (await option.BindTask(f)).BindTask(g);

            var result2 = await option.BindTask(async x => await (await f(x)).BindTask(g));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }
}

public class Option_Select_Tests()
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_map()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var result1 = from x in option
                          select f(x);

            var result2 = option.Select(f);

            var result3 = option.Map(f);

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2)
                        .And
                        .IsEqualTo(result3);
        });
    }
}

public class Option_SelectMany_Tests()
{
    [Test]
    public async Task LINQ_is_syntactic_sugar_for_bind()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToOption
                  from g1 in MapperGenerator.ObjectToObject
                  from g2 in MapperGenerator.ObjectToObject
                      // Nothing special, just a repeatable function with two parameters
                  let g = new Func<object, object, object>((x, y) => g2(g1(x).ToString() + y.ToString()))
                  select (option, f, g);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f, g) = tuple;

            // Act
            var result1 = from x in option
                          from y in f(x)
                          select g(x, y);

            var result2 = option.SelectMany(f, g);

            var result3 = option.Bind(x => f(x).Map(y => g(x, y)));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2)
                        .And
                        .IsEqualTo(result3);
        });
    }
}

public class Option_IfNone_WithValueFallback_Tests()
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = from x in Generator.Object
                  from f in
                      from y in Generator.Object
                      select new Func<object>(() => y)
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var some = Option.Some(x);

            // Act
            var result = some.IfNone(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_returns_the_fallback()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            object f() => x;

            // Act
            var result = Option<object>.None.IfNone(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }
}

public class Option_IfNone_WithOptionFallback_Tests()
{
    [Test]
    public async Task Satisfies_alternative_left_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Act
            var result = Option<object>.None.IfNone(() => option);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_alternative_right_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static Option<object> f() => Option<object>.None;

            // Act
            var result = option.IfNone(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_alternative_associativity()
    {
        var gen = from option1 in OptionGenerator.Any
                  from option2 in OptionGenerator.Any
                  from option3 in OptionGenerator.Any
                  select (option1, option2, option3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2, option3) = tuple;

            // Act
            var result1 = option1.IfNone(() => option2)
                                 .IfNone(() => option3);

            var result2 = option1.IfNone(() => option2.IfNone(() => option3));

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_left_bias()
    {
        var gen = from x in Generator.Object
                  from f in
                      from y in OptionGenerator.Any
                      select new Func<Option<object>>(() => y)
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var option = Option.Some(x);

            // Act
            var result = option.IfNone(f);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }
}

public class Option_IfNoneThrow_Tests()
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            // Act
            var result = some.IfNoneThrow(() => new InvalidOperationException());

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_throws_exception()
    {
        // Act
        static object f() => Option<object>.None.IfNoneThrow(() => new InvalidOperationException());

        // Assert
        await Assert.That(f)
                    .Throws<InvalidOperationException>();
    }
}

public class Option_IfNoneTask_WithValueFallback__Tests()
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = from x in Generator.Object
                  from f in
                      from y in Generator.Object
                      select new Func<ValueTask<object>>(async () =>
                      {
                          await Task.Yield();
                          return y;
                      })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var some = Option.Some(x);

            // Act
            var result = await some.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_returns_the_fallback()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            async ValueTask<object> f()
            {
                await Task.Yield();
                return x;
            }

            // Act
            var result = await Option<object>.None.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }
}

public class Option_IfNoneTask_WithOptionFallback_Tests()
{
    [Test]
    public async Task Satisfies_alternative_left_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            async ValueTask<Option<object>> f()
            {
                await Task.Yield();
                return option;
            }

            // Act
            var result = await Option<object>.None.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Satisfies_alternative_right_identity()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask<Option<object>> f()
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
    public async Task Satisfies_alternative_associativity()
    {
        var gen = from option1 in OptionGenerator.Any
                  from option2 in OptionGenerator.Any
                  from option3 in OptionGenerator.Any
                  select (option1, option2, option3);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option1, option2, option3) = tuple;

            // Act
            var result1 =
                await (await option1.IfNoneTask(async () =>
                                     {
                                         await Task.Yield();
                                         return option2;
                                     }))
                                     .IfNoneTask(async () =>
                                     {
                                         await Task.Yield();
                                         return option3;
                                     });

            var result2 =
                await option1.IfNoneTask(async () =>
                {
                    await Task.Yield();

                    return await option2.IfNoneTask(async () =>
                    {
                        await Task.Yield();
                        return option3;
                    });
                });

            // Assert
            await Assert.That(result1)
                        .IsEqualTo(result2);
        });
    }

    [Test]
    public async Task Satisfies_left_bias()
    {
        var gen = from x in Generator.Object
                  from f in
                      from y in OptionGenerator.Any
                      select new Func<ValueTask<Option<object>>>(async () =>
                      {
                          await Task.Yield();
                          return y;
                      })
                  select (x, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (x, f) = tuple;
            var option = Option.Some(x);

            // Act
            var result = await option.IfNoneTask(f);

            // Assert
            await Assert.That(result)
                        .IsSome()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }
}

public class Option_IfNoneNull_Tests()
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            // Act
            var result = some.IfNoneNull();

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_returns_null()
    {
        // Act
        var result = Option<object>.None.IfNoneNull();

        // Assert
        await Assert.That(result)
                    .IsNull();
    }
}

public class Option_IfNoneNullable_Tests()
{
    [Test]
    public async Task Some_returns_its_value()
    {
        var gen = Gen.Int;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            // Act
            var result = some.IfNoneNullable();

            // Assert
            await Assert.That(result)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_returns_null()
    {
        // Arrange
        var none = new Option<int>(Option.None);

        // Act
        var result = none.IfNoneNullable();

        // Assert
        await Assert.That(result)
                    .IsNull();
    }
}

public class Option_Iter_Tests()
{
    [Test]
    public async Task Some_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            object? passedValue = null;
            void f(object value) => passedValue = value;

            // Act
            some.Iter(f);

            // Assert
            await Assert.That(passedValue)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_does_not_execute_action()
    {
        // Arrange
        var none = Option<object>.None;

        var actionExecuted = false;
        void f(object _) => actionExecuted = true;

        // Act
        none.Iter(f);

        // Assert
        await Assert.That(actionExecuted)
                    .IsFalse();
    }
}

public class Option_IterTask_Tests()
{
    [Test]
    public async Task Some_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            object? passedValue = null;
            async ValueTask f(object value)
            {
                await Task.Yield();
                passedValue = value;
            }

            // Act
            await some.IterTask(f);

            // Assert
            await Assert.That(passedValue)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_does_not_execute_action()
    {
        // Arrange
        var none = Option<object>.None;

        var actionExecuted = false;
        async ValueTask f(object _)
        {
            await Task.Yield();
            actionExecuted = true;
        }

        // Act
        await none.IterTask(f);

        // Assert
        await Assert.That(actionExecuted)
                    .IsFalse();
    }
}

public class Option_IterNone_Tests()
{
    [Test]
    public async Task Executes_action_if_none()
    {
        // Arrange
        var actionExecuted = false;

        // Act
        Option<object>.None.IterNone(() => actionExecuted = true);

        // Assert
        await Assert.That(actionExecuted)
                    .IsTrue();
    }

    [Test]
    public async Task Does_not_execute_action_if_some()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);

            var actionExecuted = false;
            void f() => actionExecuted = true;

            // Act
            some.IterNone(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Option_IterNoneTask_Tests()
{
    [Test]
    public async Task Executes_action_if_none()
    {
        // Arrange
        var actionExecuted = false;

        async ValueTask f()
        {
            await Task.Yield();
            actionExecuted = true;
        }

        // Act
        await Option<object>.None.IterNoneTask(f);

        // Assert
        await Assert.That(actionExecuted)
                    .IsTrue();
    }

    [Test]
    public async Task Does_not_execute_action_if_some()
    {
        var gen = OptionGenerator.Some;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            var actionExecuted = false;
            async ValueTask f()
            {
                await Task.Yield();
                actionExecuted = true;
            }

            // Act
            await option.IterNoneTask(f);

            // Assert
            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Option_Tap_Tests
{
    [Test]
    public async Task Returns_original_option()
    {
        var gen = from option in OptionGenerator.Any
                  from f in MapperGenerator.ObjectToObject
                  select (option, f);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange
            var (option, f) = tuple;

            // Act
            var result = option.Tap(x => { _ = f(x); });

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Some_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);
            object? observed = null;

            // Act
            some.Tap(value => observed = value);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_does_not_execute_action()
    {
        // Arrange
        var actionExecuted = false;

        // Act
        Option<object>.None.Tap(_ => actionExecuted = true);

        // Assert
        await Assert.That(actionExecuted)
                    .IsFalse();
    }
}

public class Option_TapTask_Tests
{
    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            // Arrange
            static async ValueTask f(object x) => await Task.Yield();

            // Act
            var result = await option.TapTask(f);

            // Assert
            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task Some_executes_action()
    {
        var gen = Generator.Object;

        await gen.SampleAsync(async x =>
        {
            // Arrange
            var some = Option.Some(x);
            object? observed = null;

            async ValueTask f(object value)
            {
                await Task.Yield();
                observed = value;
            }

            // Act
            await some.TapTask(f);

            // Assert
            await Assert.That(observed)
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_does_not_execute_action()
    {
        // Arrange
        var actionExecuted = false;
        async ValueTask f(object _)
        {
            await Task.Yield();
            actionExecuted = true;
        }

        // Act
        await Option<object>.None.TapTask(f);

        // Assert
        await Assert.That(actionExecuted)
                    .IsFalse();
    }
}

public class Option_TapNone_Tests
{
    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            var result = option.TapNone(() => { });

            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task None_executes_action()
    {
        var actionExecuted = false;

        Option<object>.None.TapNone(() => actionExecuted = true);

        await Assert.That(actionExecuted)
                    .IsTrue();
    }

    [Test]
    public async Task Some_does_not_execute_action()
    {
        var gen = OptionGenerator.Some;

        await gen.SampleAsync(async option =>
        {
            var actionExecuted = false;

            option.TapNone(() => actionExecuted = true);

            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Option_TapNoneTask_Tests
{
    [Test]
    public async Task Returns_original_option()
    {
        var gen = OptionGenerator.Any;

        await gen.SampleAsync(async option =>
        {
            var result = await option.TapNoneTask(async () =>
            {
                await Task.Yield();
            });

            await Assert.That(result)
                        .IsEqualTo(option);
        });
    }

    [Test]
    public async Task None_executes_action()
    {
        var actionExecuted = false;

        await Option<object>.None.TapNoneTask(async () =>
        {
            await Task.Yield();
            actionExecuted = true;
        });

        await Assert.That(actionExecuted)
                    .IsTrue();
    }

    [Test]
    public async Task Some_does_not_execute_action()
    {
        var gen = OptionGenerator.Some;

        await gen.SampleAsync(async option =>
        {
            var actionExecuted = false;

            await option.TapNoneTask(async () =>
            {
                await Task.Yield();
                actionExecuted = true;
            });

            await Assert.That(actionExecuted)
                        .IsFalse();
        });
    }
}

public class Option_ToResult_Tests
{
    [Test]
    public async Task Some_returns_success()
    {
        var gen =
            from x in Generator.Object
            from error in ErrorGenerator.Any
            select (x, error);

        await gen.SampleAsync(async tuple =>
        {
            // Arrange  
            var (x, error) = tuple;
            var option = Option.Some(x);
            Error errorFactory() => error;

            // Act
            var result = option.ToResult(errorFactory);

            // Assert
            await Assert.That(result)
                        .IsSuccess()
                        .WhoseValue
                        .IsEqualTo(x);
        });
    }

    [Test]
    public async Task None_returns_error()
    {
        var gen = ErrorGenerator.Any;

        await gen.SampleAsync(async error =>
        {
            // Arrange
            Error errorFactory() => error;

            // Act
            var result = Option<object>.None.ToResult(errorFactory);

            await Assert.That(result)
                        .IsError()
                        .Which
                        .IsEqualTo(error);
        });
    }
}