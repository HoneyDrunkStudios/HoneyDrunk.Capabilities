using HoneyDrunk.Auth.Abstractions;
using HoneyDrunk.Capabilities.Abstractions;

namespace HoneyDrunk.Capabilities.Testing;

/// <summary>A guard with a scripted answer that records every request it receives.</summary>
public sealed class ScriptedCapabilityGuard : ICapabilityGuard
{
    private readonly Func<CapabilityDescriptor, CapabilityCaller, CapabilityGuardDecision> decide;
    private readonly List<(CapabilityDescriptor descriptor, CapabilityCaller caller)> requests = [];

    private ScriptedCapabilityGuard(Func<CapabilityDescriptor, CapabilityCaller, CapabilityGuardDecision> decide) => this.decide = decide;

    /// <summary>Gets the requests received so far, oldest first.</summary>
    public IReadOnlyList<(CapabilityDescriptor descriptor, CapabilityCaller caller)> Requests
    {
        get
        {
            lock (this.requests)
            {
                return [.. this.requests];
            }
        }
    }

    /// <summary>Creates a guard that allows every caller.</summary>
    /// <returns>The guard.</returns>
    public static ScriptedCapabilityGuard AllowAll() => new((_, _) => CapabilityGuardDecision.Allow());

    /// <summary>Creates a guard that denies every caller with one code.</summary>
    /// <param name="code">The deny code.</param>
    /// <returns>The guard.</returns>
    public static ScriptedCapabilityGuard DenyAll(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new((_, _) => CapabilityGuardDecision.Deny([code]));
    }

    /// <summary>
    /// Creates a guard that applies Auth's default rule to the descriptor's permissions: every required scope must
    /// be held, and any one required role is enough. Deny codes are Auth's <see cref="AuthorizationDenyCode"/> names.
    /// </summary>
    /// <returns>The guard.</returns>
    public static ScriptedCapabilityGuard RequireScopes() => new((descriptor, caller) =>
    {
        var codes = new List<string>();
        if (descriptor.Permissions.RequiredScopes.Any(scope => !caller.Scopes.Contains(scope, StringComparer.Ordinal)))
        {
            codes.Add(nameof(AuthorizationDenyCode.MissingScope));
        }

        var roles = descriptor.Permissions.RequiredRoles;
        if (roles.Count > 0 && !roles.Any(role => caller.Roles.Contains(role, StringComparer.Ordinal)))
        {
            codes.Add(nameof(AuthorizationDenyCode.MissingRole));
        }

        return codes.Count == 0 ? CapabilityGuardDecision.Allow() : CapabilityGuardDecision.Deny(codes);
    });

    /// <inheritdoc />
    public Task<CapabilityGuardDecision> AuthorizeAsync(CapabilityDescriptor descriptor, CapabilityCaller caller, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(caller);
        cancellationToken.ThrowIfCancellationRequested();
        lock (this.requests)
        {
            this.requests.Add((descriptor, caller));
        }

        return Task.FromResult(this.decide(descriptor, caller));
    }
}
