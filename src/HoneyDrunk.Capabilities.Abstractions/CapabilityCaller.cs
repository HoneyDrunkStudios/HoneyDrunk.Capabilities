namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// Who is invoking a capability, as asserted by the host composition. The guard turns it into an identity for
/// Auth to evaluate; it is not a validated token, so the trust boundary is the host process.
/// </summary>
/// <param name="SubjectId">The caller's subject identifier.</param>
/// <param name="Scopes">The scopes the caller holds.</param>
/// <param name="Roles">The roles the caller holds.</param>
public sealed record CapabilityCaller(string SubjectId, IReadOnlyList<string> Scopes, IReadOnlyList<string> Roles)
{
    /// <summary>Gets the caller's subject identifier.</summary>
    public string SubjectId { get; } = string.IsNullOrWhiteSpace(SubjectId)
        ? throw new ArgumentException("A caller needs a subject identifier.", nameof(SubjectId))
        : SubjectId;

    /// <summary>Gets the scopes the caller holds.</summary>
    public IReadOnlyList<string> Scopes { get; } = CapabilityValidation.NonBlankList(Scopes, nameof(Scopes));

    /// <summary>Gets the roles the caller holds.</summary>
    public IReadOnlyList<string> Roles { get; } = CapabilityValidation.NonBlankList(Roles, nameof(Roles));
}
