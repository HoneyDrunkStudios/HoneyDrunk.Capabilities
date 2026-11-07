using HoneyDrunk.Capabilities.Abstractions;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>A handler that returns its arguments as its output. Public so dependency injection can build it.</summary>
public sealed class EchoHandler : ICapabilityHandler
{
    /// <inheritdoc />
    public Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken) =>
        Task.FromResult(CapabilityResult.Success(invocation.Arguments));
}
