using HoneyDrunk.Audit.Abstractions;
using HoneyDrunk.Auth.Authorization;
using HoneyDrunk.Capabilities.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;
using Xunit;

namespace HoneyDrunk.Capabilities.Canary;

/// <summary>
/// Runs the invoker and guard against the real HoneyDrunk.Auth <see cref="DefaultAuthorizationPolicy"/>, so a
/// change in Auth's contract or rules shows up here (Grid invariant 129).
/// </summary>
public sealed class AuthCanaryTests
{
    private readonly RecordingAuditLog audit = new();
    private readonly CountingHandler handler = new();
    private readonly CapabilityInvoker invoker;

    /// <summary>Initializes a new instance of the <see cref="AuthCanaryTests"/> class.</summary>
    public AuthCanaryTests()
    {
        var telemetry = Substitute.For<ITelemetryActivityFactory>();
        var gridContext = Substitute.For<IGridContextAccessor>();
        gridContext.GridContext.Returns(Substitute.For<IGridContext>());
        var policy = new DefaultAuthorizationPolicy(telemetry, this.audit, gridContext, NullLogger<DefaultAuthorizationPolicy>.Instance);

        var registry = new InProcessCapabilityRegistry(NullLogger<InProcessCapabilityRegistry>.Instance);
        registry.Register(
            new CapabilityDescriptor("test-tool", "1.0.0", "A test capability.", "test-node", """{"type":"object"}""", null, new CapabilityPermissions(["test.invoke"], [])),
            this.handler);
        var guard = new AuthPolicyCapabilityGuard(policy, Options.Create(new CapabilityGuardOptions()));
        this.invoker = new CapabilityInvoker(registry, guard, telemetry, NullLogger<CapabilityInvoker>.Instance);
    }

    /// <summary>The real policy denies a caller without the scope; the handler never runs and one denial is audited.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Real_auth_policy_denies_a_missing_scope()
    {
        var result = await this.invoker.InvokeAsync(Invocation(new CapabilityCaller("test-caller", ["test.other"], [])));

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Contains("MissingScope", result.DenyCodes);
        Assert.Equal(0, this.handler.Calls);
        var entry = Assert.Single(this.audit.Entries);
        Assert.Equal(AuditOutcome.Denied, entry.Outcome);
        Assert.Equal("capability/test-tool/1.0.0", entry.Target.Id);
    }

    /// <summary>The real policy allows a caller with the scope; the handler runs once and the success is audited.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Real_auth_policy_allows_a_granted_scope()
    {
        var result = await this.invoker.InvokeAsync(Invocation(new CapabilityCaller("test-caller", ["test.invoke"], [])));

        Assert.Equal(CapabilityResultStatus.Succeeded, result.Status);
        Assert.Equal(1, this.handler.Calls);
        Assert.Equal(AuditOutcome.Succeeded, Assert.Single(this.audit.Entries).Outcome);
    }

    private static CapabilityInvocation Invocation(CapabilityCaller caller)
    {
        using var document = JsonDocument.Parse("{}");
        return new CapabilityInvocation(new CapabilityKey("test-tool", "1.0.0"), document.RootElement, caller);
    }

    private sealed class CountingHandler : ICapabilityHandler
    {
        public int Calls { get; private set; }

        public Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken)
        {
            this.Calls++;
            return Task.FromResult(CapabilityResult.Success(null));
        }
    }
}
