using HoneyDrunk.Capabilities.Abstractions;
using System.Text.Json;
using Xunit;

namespace HoneyDrunk.Capabilities.Testing.Tests;

/// <summary>Behavior of the Testing package: the harness shares the runtime's semantics.</summary>
public sealed class CapabilityTestHarnessTests
{
    /// <summary>The harness runs the real invoker: exact lookup, the guard once, then the handler.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Harness_dispatches_through_the_real_invoker()
    {
        var harness = new CapabilityTestHarness();
        var handler = new RecordingCapabilityHandler(CapabilityResult.Success(null));
        harness.Register(CapabilityFixtures.Descriptor(), handler);

        var result = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "1.0.0"), CapabilityFixtures.Caller()));
        var missing = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "2.0.0"), CapabilityFixtures.Caller()));

        Assert.Equal(CapabilityResultStatus.Succeeded, result.Status);
        Assert.Equal(CapabilityResultStatus.NotFound, missing.Status);
        Assert.Single(handler.Invocations);
        Assert.Single(harness.Guard.Requests);
        Assert.NotNull(harness.Registry.Resolve(new CapabilityKey("test-tool", "1.0.0")));
    }

    /// <summary>DenyAll denies with its code, and the handler never runs.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task DenyAll_denies_with_its_code()
    {
        var harness = new CapabilityTestHarness(ScriptedCapabilityGuard.DenyAll("test_denied"));
        var handler = new RecordingCapabilityHandler(CapabilityResult.Success(null));
        harness.Register(CapabilityFixtures.Descriptor(), handler);

        var result = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "1.0.0"), CapabilityFixtures.Caller()));

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Equal(["test_denied"], result.DenyCodes);
        Assert.Empty(handler.Invocations);
    }

    /// <summary>RequireScopes needs every required scope, as Auth does.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RequireScopes_denies_a_missing_scope()
    {
        var harness = new CapabilityTestHarness(ScriptedCapabilityGuard.RequireScopes());
        var handler = new RecordingCapabilityHandler(CapabilityResult.Success(null));
        harness.Register(CapabilityFixtures.Descriptor(requiredScopes: ["test.invoke", "test.write"]), handler);

        var result = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "1.0.0"), CapabilityFixtures.Caller(scopes: ["test.invoke"])));

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Equal(["MissingScope"], result.DenyCodes);
        Assert.Empty(handler.Invocations);
    }

    /// <summary>RequireScopes accepts any one of the required roles, as Auth does.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RequireScopes_allows_any_one_role()
    {
        var harness = new CapabilityTestHarness(ScriptedCapabilityGuard.RequireScopes());
        var handler = new RecordingCapabilityHandler(CapabilityResult.Success(null));
        harness.Register(CapabilityFixtures.Descriptor(requiredScopes: [], requiredRoles: ["test-reader", "test-writer"]), handler);

        var allowed = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "1.0.0"), CapabilityFixtures.Caller(scopes: [], roles: ["test-writer"])));
        var denied = await harness.Invoker.InvokeAsync(Invocation(new CapabilityKey("test-tool", "1.0.0"), CapabilityFixtures.Caller(scopes: [], roles: ["test-other"])));

        Assert.Equal(CapabilityResultStatus.Succeeded, allowed.Status);
        Assert.Equal(["MissingRole"], denied.DenyCodes);
        Assert.Single(handler.Invocations);
    }

    /// <summary>The fixtures use the documented defaults.</summary>
    [Fact]
    public void Fixtures_use_the_defaults()
    {
        var descriptor = CapabilityFixtures.Descriptor();
        var caller = CapabilityFixtures.Caller();

        Assert.Equal(new CapabilityKey("test-tool", "1.0.0"), descriptor.Key);
        Assert.Equal(["test.invoke"], descriptor.Permissions.RequiredScopes);
        Assert.Equal(["test.invoke"], caller.Scopes);
    }

    private static CapabilityInvocation Invocation(CapabilityKey key, CapabilityCaller caller)
    {
        using var document = JsonDocument.Parse("{}");
        return new CapabilityInvocation(key, document.RootElement, caller);
    }
}
