namespace common;

/// <summary>
/// Represents the absence of a meaningful value. Methods can return this type instead of void.
/// </summary>
public readonly record struct Unit
{
    public static Unit Instance { get; }

    public override string ToString() => "()";

    public override int GetHashCode() => 0;
}
