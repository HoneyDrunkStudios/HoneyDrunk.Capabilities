namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// Registers, discovers and resolves capabilities by their exact <c>(name, version)</c> key (ADR-0017 D3). No member
/// returns a handler; running a capability goes through <see cref="ICapabilityInvoker"/>.
/// </summary>
public interface ICapabilityRegistry
{
    /// <summary>Registers a capability and its handler.</summary>
    /// <param name="descriptor">The capability descriptor.</param>
    /// <param name="handler">The handler that implements it.</param>
    void Register(CapabilityDescriptor descriptor, ICapabilityHandler handler);

    /// <summary>Resolves the descriptor registered under the exact key; there is no fallback to another version.</summary>
    /// <param name="key">The capability key.</param>
    /// <returns>The descriptor, or <see langword="null"/> when nothing is registered under the key.</returns>
    CapabilityDescriptor? Resolve(CapabilityKey key);

    /// <summary>Lists every registered descriptor.</summary>
    /// <returns>The descriptors.</returns>
    IReadOnlyList<CapabilityDescriptor> List();
}
