namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// The implementation of one capability, supplied by the node that owns it. The handler is registered with the
/// registry and is only ever called by the invoker, after the guard has allowed the caller.
/// </summary>
public interface ICapabilityHandler
{
    /// <summary>Runs the capability. The handler validates its own arguments and returns an invalid-arguments result when they are wrong.</summary>
    /// <param name="invocation">The invocation, already authorized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result.</returns>
    Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken);
}
