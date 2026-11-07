namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// Runs a capability: resolves the exact key, asks the guard, then dispatches to the handler (ADR-0017 D3). It is the
/// only way to run a capability, so every invocation passes the guard.
/// </summary>
public interface ICapabilityInvoker
{
    /// <summary>Invokes a capability.</summary>
    /// <param name="invocation">The invocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result; failures are results, cancellation is an exception.</returns>
    Task<CapabilityResult> InvokeAsync(CapabilityInvocation invocation, CancellationToken cancellationToken = default);
}
