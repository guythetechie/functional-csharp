using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace common;

#pragma warning disable CA1716 // Identifiers should not match keywords
public record Error
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    private readonly ImmutableHashSet<string> messages;

    protected Error(IEnumerable<string> messages)
    {
        this.messages = [.. messages];
    }

    public ImmutableHashSet<string> Messages => messages;

    public static Error From(params string[] messages) =>
        new(messages);

    public static Error From(Exception exception) =>
        new Exceptional(exception);

    public virtual Exception ToException() =>
        messages.ToArray() switch
        {
            [var message] => new InvalidOperationException(message),
            _ => new AggregateException(messages.Select(message => new InvalidOperationException(message)))
        };

    public override string ToString() =>
        messages.ToArray() switch
        {
            [var message] => message,
            _ => string.Join("; ", messages)
        };

    public static implicit operator Error(string message) =>
        From(message);

    public static implicit operator Error(Exception exception) =>
        From(exception);

    public static Error operator +(Error left, Error right) =>
        (left.messages, right.messages) switch
        {
            ({ Count: 0 }, _) => right,
            (_, { Count: 0 }) => left,
            _ => new(left.messages.Union(right.messages))
        };

    public virtual bool Equals(Error? other) =>
        (this, other) switch
        {
            (_, null) => false,
            ({ messages.Count: 0 }, { messages.Count: 0 }) => true,
            _ => messages.SetEquals(other.messages)
        };

    public override int GetHashCode() =>
        messages.Count switch
        {
            0 => 0,
            _ => messages.Aggregate(0, (hash, message) => hash ^ message.GetHashCode())
        };

    public sealed record Exceptional : Error
    {
        private readonly Exception exception;

        internal Exceptional(Exception exception) : base([exception.Message])
        {
            this.exception = exception;
        }

        public override Exception ToException() => exception;

        public bool Equals(Error.Exceptional? other) =>
            (this, other) switch
            {
                (_, null) => false,
                _ => exception.Equals(other.exception)
            };

        public override int GetHashCode() =>
            exception.GetHashCode();
    }
}
