using HoneyDrunk.Capabilities.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Capabilities;

/// <summary>
/// A thread-safe, in-process registry keyed by the exact <c>(name, version)</c> pair. Handlers stay inside the
/// runtime: only <see cref="CapabilityInvoker"/> can reach them.
/// </summary>
/// <param name="logger">The logger.</param>
public sealed class InProcessCapabilityRegistry(ILogger<InProcessCapabilityRegistry> logger) : ICapabilityRegistry
{
    private readonly ConcurrentDictionary<CapabilityKey, Registration> registrations = new();
    private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A capability is already registered under the same key.</exception>
    public void Register(CapabilityDescriptor descriptor, ICapabilityHandler handler)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(handler);

        if (!this.registrations.TryAdd(descriptor.Key, new Registration(descriptor, handler)))
        {
            throw new InvalidOperationException($"A capability is already registered as {descriptor.Key}.");
        }

        CapabilityLog.Registered(this.logger, descriptor.Key.ToString(), descriptor.OwningNode);
    }

    /// <inheritdoc />
    public CapabilityDescriptor? Resolve(CapabilityKey key) =>
        this.registrations.TryGetValue(key, out var registration) ? registration.Descriptor : null;

    /// <inheritdoc />
    public IReadOnlyList<CapabilityDescriptor> List() =>
        Array.AsReadOnly(this.registrations.Values
            .Select(registration => registration.Descriptor)
            .OrderBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .ThenBy(descriptor => descriptor.Version, Comparer<string>.Create(CompareVersions))
            .ToArray());

    /// <summary>Gets the handler registered under the exact key. Used only by the invoker.</summary>
    /// <param name="key">The capability key.</param>
    /// <param name="handler">The handler, when found.</param>
    /// <returns>Whether a handler was found.</returns>
    internal bool TryGetHandler(CapabilityKey key, [NotNullWhen(true)] out ICapabilityHandler? handler)
    {
        handler = this.registrations.TryGetValue(key, out var registration) ? registration.Handler : null;
        return handler is not null;
    }

    private static int CompareVersions(string? left, string? right)
    {
        var leftParts = (left ?? string.Empty).Split('.');
        var rightParts = (right ?? string.Empty).Split('.');
        for (var index = 0; index < Math.Min(leftParts.Length, rightParts.Length); index++)
        {
            // Validated versions have no leading zeros, so a longer number is a larger one.
            var comparison = leftParts[index].Length != rightParts[index].Length
                ? leftParts[index].Length.CompareTo(rightParts[index].Length)
                : string.CompareOrdinal(leftParts[index], rightParts[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    private sealed record Registration(CapabilityDescriptor Descriptor, ICapabilityHandler Handler);
}
