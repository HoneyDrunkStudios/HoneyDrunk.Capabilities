using HoneyDrunk.Auth.Abstractions;
using HoneyDrunk.Capabilities.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Capabilities;

/// <summary>
/// The default guard: it projects the caller and the descriptor into Auth's types and lets the host's
/// <see cref="IAuthorizationPolicy"/> decide (ADR-0017 D5 and D10, Grid invariant 129). It keeps no permission
/// model of its own. A policy failure denies with <c>guard_error</c> and logs only the exception type name and the key.
/// </summary>
public sealed class AuthPolicyCapabilityGuard : ICapabilityGuard
{
    private const string InvokeAction = "capability.invoke";

    private const string GuardErrorCode = "guard_error";

    private readonly IAuthorizationPolicy policy;
    private readonly string identityScheme;
    private readonly ILogger logger;

    /// <summary>Initializes a new instance of the <see cref="AuthPolicyCapabilityGuard"/> class.</summary>
    /// <param name="policy">The Auth policy the host composed.</param>
    /// <param name="options">The guard options.</param>
    /// <param name="logger">The logger.</param>
    public AuthPolicyCapabilityGuard(IAuthorizationPolicy policy, IOptions<CapabilityGuardOptions> options, ILogger<AuthPolicyCapabilityGuard> logger)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        var scheme = options.Value?.IdentityScheme;
        if (string.IsNullOrWhiteSpace(scheme))
        {
            throw new ArgumentException("CapabilityGuardOptions.IdentityScheme must not be blank.", nameof(options));
        }

        this.policy = policy;
        this.identityScheme = scheme;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<CapabilityGuardDecision> AuthorizeAsync(CapabilityDescriptor descriptor, CapabilityCaller caller, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(caller);

        var identity = new AuthenticatedIdentity(caller.SubjectId, this.identityScheme, displayName: null, claims: Claims(caller));
        var request = new AuthorizationRequest(
            InvokeAction,
            $"capability/{descriptor.Name}/{descriptor.Version}",
            descriptor.Permissions.RequiredScopes,
            descriptor.Permissions.RequiredRoles);

        AuthorizationDecision decision;
        try
        {
            decision = await this.policy.EvaluateAsync(identity, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (!(error is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            CapabilityLog.PolicyFailed(this.logger, descriptor.Key.ToString(), error.GetType().FullName ?? error.GetType().Name);
            return CapabilityGuardDecision.Deny([GuardErrorCode]);
        }

        if (decision is null)
        {
            return CapabilityGuardDecision.Deny([GuardErrorCode]);
        }

        if (decision.IsAllowed)
        {
            return CapabilityGuardDecision.Allow();
        }

        string[] codes = [.. decision.DenyReasons.Select(reason => reason.Code.ToString()).Distinct(StringComparer.Ordinal)];
        return CapabilityGuardDecision.Deny(codes.Length > 0 ? codes : [GuardErrorCode]);
    }

    private static Dictionary<string, IReadOnlyList<string>> Claims(CapabilityCaller caller)
    {
        var claims = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (caller.Scopes.Count > 0)
        {
            claims[AuthClaimTypes.Scope] = caller.Scopes;
        }

        if (caller.Roles.Count > 0)
        {
            claims[AuthClaimTypes.Role] = caller.Roles;
        }

        return claims;
    }
}
