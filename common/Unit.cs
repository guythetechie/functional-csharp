namespace common;

/// <summary>
/// Represents the absence of a meaningful value. Unit is used in functional programming
/// to indicate that a function performs side effects but doesn't return a meaningful value.
/// It's the functional equivalent of void, but as a proper type that can be used in generic contexts.
/// </summary>
public readonly record struct Unit
{
    public static Unit Instance { get; }
    
    public override string ToString() => "()";

    public override int GetHashCode() => 0;
}
