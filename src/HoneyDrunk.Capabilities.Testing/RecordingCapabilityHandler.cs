using HoneyDrunk.Capabilities.Abstractions;

namespace HoneyDrunk.Capabilities.Testing;

/// <summary>A handler that returns a fixed result and records every invocation it receives.</summary>
/// <param name="result">The result to return.</param>
public sealed class RecordingCapabilityHandler(CapabilityResult result) : ICapabilityHandler
{
    private readonly CapabilityResult result = result ?? throw new ArgumentNullException(nameof(result));
    private readonly List<CapabilityInvocation> invocations = [];

    /// <summary>Gets the invocations received so far, oldest first.</summary>
    public IReadOnlyList<CapabilityInvocation> Invocations
    {
        get
        {
            lock (this.invocations)
            {
                return [.. this.invocations];
            }
        }
    }

    /// <inheritdoc />
    public Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        cancellationToken.ThrowIfCancellationRequested();
        lock (this.invocations)
        {
            this.invocations.Add(invocation);
        }

        return Task.FromResult(this.result);
    }
}
