using System.Text.Json;

namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>One request to run a capability.</summary>
/// <param name="Key">The exact capability to run.</param>
/// <param name="Arguments">The arguments, a JSON object. The invocation keeps its own copy.</param>
/// <param name="Caller">Who is invoking the capability.</param>
public sealed record CapabilityInvocation(CapabilityKey Key, JsonElement Arguments, CapabilityCaller Caller)
{
    /// <summary>Gets the exact capability to run.</summary>
    public CapabilityKey Key { get; } = Key.Name is null
        ? throw new ArgumentException("An invocation needs a constructed capability key.", nameof(Key))
        : Key;

    /// <summary>Gets the arguments, a JSON object that does not depend on the caller's document.</summary>
    public JsonElement Arguments { get; } = Arguments.ValueKind == JsonValueKind.Object
        ? Arguments.Clone()
        : throw new ArgumentException("Capability arguments must be a JSON object.", nameof(Arguments));

    /// <summary>Gets who is invoking the capability.</summary>
    public CapabilityCaller Caller { get; } = Caller ?? throw new ArgumentNullException(nameof(Caller));
}
