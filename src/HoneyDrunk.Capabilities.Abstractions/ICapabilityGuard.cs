namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// The authorization gate the invoker checks before dispatch (ADR-0017 D3 and D5). The default implementation
/// delegates the decision to HoneyDrunk.Auth policy; Capabilities keeps no permission model of its own.
/// </summary>
public interface ICapabilityGuard
{
    /// <summary>Decides whether the caller may invoke the capability.</summary>
    /// <param name="descriptor">The resolved capability descriptor.</param>
    /// <param name="caller">The caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decision.</returns>
    Task<CapabilityGuardDecision> AuthorizeAsync(CapabilityDescriptor descriptor, CapabilityCaller caller, CancellationToken cancellationToken = default);
}
