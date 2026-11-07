using HoneyDrunk.Auth.Abstractions;
using HoneyDrunk.Capabilities.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Behavior of <see cref="AuthPolicyCapabilityGuard"/>: it maps to Auth's types and lets Auth decide.</summary>
public sealed class AuthPolicyCapabilityGuardTests
{
    private static readonly CapabilityDescriptor Descriptor = new(
        "test-tool",
        "1.2.3",
        "A test capability.",
        "test-node",
        """{"type":"object"}""",
        null,
        new CapabilityPermissions(["test.invoke", "test.read"], ["test-operator", "test-admin"]));

    private readonly FakeAuthorizationPolicy policy = new();

    /// <summary>The caller becomes an identity with its subject, the configured scheme and scope and role claims.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Maps_caller_to_identity_claims()
    {
        var guard = new AuthPolicyCapabilityGuard(this.policy, Options.Create(new CapabilityGuardOptions { IdentityScheme = "test-scheme" }));

        await guard.AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", ["test.invoke", "test.read"], ["test-operator"]));

        var identity = Assert.IsType<AuthenticatedIdentity>(this.policy.Identity);
        Assert.Equal("test-caller", identity.SubjectId);
        Assert.Equal("test-scheme", identity.Scheme);
        Assert.Equal(["test.invoke", "test.read"], identity.GetClaimValues(AuthClaimTypes.Scope));
        Assert.Equal(["test-operator"], identity.GetClaimValues(AuthClaimTypes.Role));
        Assert.Equal(2, identity.Claims.Count);
    }

    /// <summary>The default scheme is the pinned caller scheme, and empty claim lists are left out.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Uses_the_default_scheme_and_omits_empty_claims()
    {
        await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", ["test.invoke"], []));

        Assert.Equal("honeydrunk-capability-caller", this.policy.Identity!.Scheme);
        Assert.False(this.policy.Identity.HasClaim(AuthClaimTypes.Role));
    }

    /// <summary>The descriptor becomes an invoke request on the capability's resource, with its scopes and roles.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Maps_descriptor_to_authorization_request()
    {
        await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []));

        var request = Assert.IsType<AuthorizationRequest>(this.policy.Request);
        Assert.Equal("capability.invoke", request.Action);
        Assert.Equal("capability/test-tool/1.2.3", request.Resource);
        Assert.Equal(["test.invoke", "test.read"], request.RequiredScopes);
        Assert.Equal(["test-operator", "test-admin"], request.RequiredRoles);
        Assert.Null(request.ResourceOwnerId);
    }

    /// <summary>Auth's allow is the guard's allow.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Allow_maps_to_allow()
    {
        this.policy.Decision = AuthorizationDecision.Allow(["authenticated"]);

        var decision = await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.DenyCodes);
        Assert.Equal(1, this.policy.Calls);
    }

    /// <summary>Auth's deny carries Auth's deny code names, each once.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Deny_carries_auth_deny_codes()
    {
        this.policy.Decision = AuthorizationDecision.Deny(
        [
            new DenyReason(AuthorizationDenyCode.MissingScope, "Required scope 'test.invoke' is missing"),
            new DenyReason(AuthorizationDenyCode.MissingScope, "Required scope 'test.read' is missing"),
            new DenyReason(AuthorizationDenyCode.MissingRole, "None of the required roles are present"),
        ]);

        var decision = await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []));

        Assert.False(decision.IsAllowed);
        Assert.Equal(["MissingScope", "MissingRole"], decision.DenyCodes);
    }

    /// <summary>A policy that throws denies with guard_error (fail closed).</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Policy_exception_fails_closed()
    {
        this.policy.Error = new InvalidOperationException("policy store sk-secret-value unavailable");

        var decision = await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []));

        Assert.False(decision.IsAllowed);
        Assert.Equal(["guard_error"], decision.DenyCodes);
    }

    /// <summary>A policy cancellation that the caller did not ask for still fails closed.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Policy_cancellation_without_the_callers_token_fails_closed()
    {
        this.policy.Error = new TaskCanceledException("timed out");

        var decision = await this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []));

        Assert.Equal(["guard_error"], decision.DenyCodes);
    }

    /// <summary>The caller's cancellation propagates.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Cancellation_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            this.Guard().AuthorizeAsync(Descriptor, new CapabilityCaller("test-caller", [], []), cancellation.Token));
    }

    /// <summary>A blank identity scheme is rejected when the guard is built.</summary>
    [Fact]
    public void Blank_scheme_is_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AuthPolicyCapabilityGuard(this.policy, Options.Create(new CapabilityGuardOptions { IdentityScheme = " " })));
    }

    private AuthPolicyCapabilityGuard Guard() => new(this.policy, Options.Create(new CapabilityGuardOptions()));
}
