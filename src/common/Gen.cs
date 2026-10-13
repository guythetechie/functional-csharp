using CsCheck;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace common;

public static class Generator
{
#pragma warning disable CA1720 // Identifier contains type name
    public static Gen<object> Object { get; } =
#pragma warning restore CA1720 // Identifier contains type name
        Gen.OneOf(from x in Gen.Bool
                  select (object)x,
                  from x in Gen.Byte
                  select (object)x,
                  from x in Gen.Char
                  select (object)x,
                  from x in Gen.Date
                  select (object)x,
                  from x in Gen.DateOnly
                  select (object)x,
                  from x in Gen.DateTime
                  select (object)x,
                  from x in Gen.DateTimeOffset
                  select (object)x,
                  from x in Gen.Decimal
                  select (object)x,
                  from x in Gen.Double
                  select (object)x,
                  from x in Gen.Float
                  select (object)x,
                  from x in Gen.Guid
                  select (object)x,
                  from x in Gen.Int
                  select (object)x,
                  from x in Gen.Long
                  select (object)x,
                  from x in Gen.SByte
                  select (object)x,
                  from x in Gen.Short
                  select (object)x,
                  Gen.String,
                  from x in Gen.TimeOnly
                  select (object)x,
                  from x in Gen.TimeSpan
                  select (object)x,
                  from x in Gen.UInt
                  select (object)x,
                  from x in Gen.ULong
                  select (object)x,
                  from x in Gen.UShort
                  select (object)x);

    public static Gen<ImmutableArray<T>> SubArrayOf<T>(ICollection<T> collection) =>
        SubArrayOf(collection, minimumLength: 0, maximumLength: collection.Count);

    public static Gen<ImmutableArray<T>> SubArrayOf<T>(ICollection<T> collection, int length) =>
        SubArrayOf(collection, minimumLength: length, maximumLength: length);

    public static Gen<ImmutableArray<T>> SubArrayOf<T>(ICollection<T> collection, int minimumLength, int maximumLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumLength, collection.Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, minimumLength);

        return from length in Gen.Int[minimumLength, Math.Min(collection.Count, maximumLength)]
               from subArray in Gen.Shuffle(constants: [.. collection], length)
               select subArray.ToImmutableArray();
    }

    public static Gen<ImmutableHashSet<T>> SubSetOf<T>(ICollection<T> collection, IEqualityComparer<T>? comparer = default) =>
        SubSetOf(collection, minimumLength: 0, maximumLength: collection.Count, comparer);

    public static Gen<ImmutableHashSet<T>> SubSetOf<T>(ICollection<T> collection, int length, IEqualityComparer<T>? comparer = default) =>
        SubSetOf(collection, minimumLength: length, maximumLength: length, comparer);

    public static Gen<ImmutableHashSet<T>> SubSetOf<T>(ICollection<T> collection, int minimumLength, int maximumLength, IEqualityComparer<T>? comparer = default)
    {
        var collectionSet = collection.ToImmutableHashSet(comparer);

        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumLength, collectionSet.Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, minimumLength);

        return from length in Gen.Int[minimumLength, Math.Min(collectionSet.Count, maximumLength)]
               from set in length switch
               {
                   0 => Gen.Const(ImmutableHashSet.Create(comparer)),
                   _ => from list in Gen.Shuffle(constants: [.. collectionSet], length)
                        select list.ToImmutableHashSet(comparer)
               }
               select set;
    }

    public static Gen<ImmutableHashSet<T>> HashSetOf<T>(this Gen<T> gen, IEqualityComparer<T>? comparer = default) =>
        gen.HashSetOf(minimumLength: 0, maximumLength: 10, comparer);

    public static Gen<ImmutableHashSet<T>> HashSetOf<T>(this Gen<T> gen, int length, IEqualityComparer<T>? comparer = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        return gen.HashSetOf(minimumLength: length, maximumLength: length, comparer);
    }

    public static Gen<ImmutableHashSet<T>> HashSetOf<T>(this Gen<T> gen, int minimumLength, int maximumLength, IEqualityComparer<T>? comparer = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, minimumLength);

        return from items in gen.Array[minimumLength, maximumLength]
               let set = comparer is null
                   ? [with(comparer), .. items]
                   : items.ToImmutableHashSet(comparer)
               where set.Count >= minimumLength && set.Count <= maximumLength
               select set;
    }

    public static Gen<ImmutableArray<T>> ArrayOf<T>(this Gen<T> gen) =>
        gen.ArrayOf(minimumLength: 0, maximumLength: 10);

    public static Gen<ImmutableArray<T>> ArrayOf<T>(this Gen<T> gen, int length) =>
        gen.ArrayOf(minimumLength: length, maximumLength: length);

    public static Gen<ImmutableArray<T>> ArrayOf<T>(this Gen<T> gen, int minimumLength, int maximumLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, minimumLength);

        return from items in gen.Array[minimumLength, maximumLength]
               select items.ToImmutableArray();
    }

    public static Gen<ImmutableArray<T2>> Traverse<T1, T2>(IEnumerable<T1> source, Func<T1, Gen<T2>> f) =>
        source.Aggregate(Gen.Const(ImmutableArray.Create<T2>()),
                         (arrayGen, t1) => from array in arrayGen
                                           from t2 in f(t1)
                                           select array.Add(t2));
}