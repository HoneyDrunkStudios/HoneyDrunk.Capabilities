namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// What a caller must hold to invoke a capability. Auth decides: every required scope must be present, and any
/// one required role is enough. At least one scope or role is required, so a capability is never open by default.
/// </summary>
/// <param name="RequiredScopes">Scopes the caller must all hold. Each value is 1 to 128 characters with no whitespace.</param>
/// <param name="RequiredRoles">Roles of which the caller must hold one. Each value is 1 to 128 characters with no whitespace.</param>
public sealed record CapabilityPermissions(IReadOnlyList<string> RequiredScopes, IReadOnlyList<string> RequiredRoles)
{
    /// <summary>Gets the scopes the caller must all hold.</summary>
    public IReadOnlyList<string> RequiredScopes { get; } = CapabilityValidation.Permissions(RequiredScopes, nameof(RequiredScopes));

    /// <summary>Gets the roles of which the caller must hold one.</summary>
    public IReadOnlyList<string> RequiredRoles { get; } = RequireAny(RequiredScopes, CapabilityValidation.Permissions(RequiredRoles, nameof(RequiredRoles)), nameof(RequiredRoles));

    private static IReadOnlyList<string> RequireAny(IReadOnlyList<string> scopes, IReadOnlyList<string> roles, string parameterName) =>
        scopes.Count == 0 && roles.Count == 0
            ? throw new ArgumentException("A capability requires at least one scope or role.", parameterName)
            : roles;
}
