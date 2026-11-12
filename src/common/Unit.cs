using System;
using System.Threading.Tasks;

namespace common;

/// <summary>
/// Represents the absence of a meaningful value. Methods can return this type instead of void.
/// </summary>
public readonly record struct Unit
{
    public static Unit Instance { get; }

    public override string ToString() => "()";

    public override int GetHashCode() => 0;
    
    public static Unit From(Action action)
    {
        action();
        return Instance;
    }

    public static async ValueTask<Unit> From(Func<ValueTask> action)
    {
        await action();
        return Instance;
    }
}
