using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace common;

#pragma warning disable CA1716 // Identifiers should not match keywords
/// <summary>
/// Represents an error containing one or more messages.
/// </summary>
public record Error
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    private readonly ImmutableHashSet<string> messages;

    protected Error(IEnumerable<string> messages)
    {
        this.messages = [.. messages];
    }

    /// <summary>
    /// Gets all error messages as an immutable set.
    /// </summary>
    public ImmutableHashSet<string> Messages => messages;

    /// <summary>
    /// Creates an error from one or more messages.
    /// </summary>
    /// <param name="messages">The error messages.</param>
    /// <returns>An error containing the specified messages.</returns>
    public static Error From(params string[] messages) =>
        new(messages);

    /// <summary>
    /// Creates an error from an exception.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <returns>An exceptional error containing the exception.</returns>
    public static Error From(Exception exception) =>
        new Exceptional(exception);

    /// <summary>
    /// Converts the error to an appropriate exception.
    /// </summary>
    /// <returns>An exception representing this error.</returns>
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

    /// <summary>
    /// Implicitly converts a string to an error.
    /// </summary>
    public static implicit operator Error(string message) =>
        From(message);

    /// <summary>
    /// Implicitly converts an exception to an error.
    /// </summary>
    public static implicit operator Error(Exception exception) =>
        From(exception);

    /// <summary>
    /// Combines two errors into a single error.
    /// </summary>
    /// <param name="left">The first error.</param>
    /// <param name="right">The second error.</param>
    /// <returns>An error containing messages from both errors.</returns>
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
            _ => messages.Aggregate(0, (hash, message) => HashCode.Combine(hash, message.GetHashCode()))
        };

    /// <summary>
    /// Represents an error that wraps an exception.
    /// </summary>
    public sealed record Exceptional : Error
    {
        internal Exceptional(Exception exception) : base([exception.Message])
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets the wrapped exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Returns the original wrapped exception.
        /// </summary>
        /// <returns>The original exception.</returns>
        public override Exception ToException() => Exception;

        public bool Equals(Error.Exceptional? other) =>
            (this, other) switch
            {
                (_, null) => false,
                _ => Exception.Equals(other.Exception)
            };

        public override int GetHashCode() =>
            Exception.GetHashCode();
    }
}