namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// Machine-readable metadata for one capability (an agent-callable tool). Every rule is checked at construction,
/// so a descriptor that exists is valid; the version is a required argument (ADR-0017 D3 and D6, Grid invariant 128).
/// </summary>
/// <param name="Name">The capability name: a lowercase letter, then up to 63 lowercase letters, digits, '_' or '-'.</param>
/// <param name="Version">The capability version: MAJOR.MINOR.PATCH, digits only, no leading zeros, no pre-release.</param>
/// <param name="Description">What the capability does, 1 to 1024 characters.</param>
/// <param name="OwningNode">The Grid node that implements the capability, 1 to 64 characters.</param>
/// <param name="ParameterSchema">JSON Schema text for the arguments; the root is an object with <c>"type": "object"</c>; at most 32,768 characters.</param>
/// <param name="ReturnSchema">Optional JSON Schema text for the output, with the same rules as <paramref name="ParameterSchema"/>.</param>
/// <param name="Permissions">The scopes and roles a caller must hold.</param>
public sealed record CapabilityDescriptor(
    string Name,
    string Version,
    string Description,
    string OwningNode,
    string ParameterSchema,
    string? ReturnSchema,
    CapabilityPermissions Permissions)
{
    /// <summary>Gets the capability name.</summary>
    public string Name { get; } = CapabilityValidation.Name(Name, nameof(Name));

    /// <summary>Gets the capability version.</summary>
    public string Version { get; } = CapabilityValidation.Version(Version, nameof(Version));

    /// <summary>Gets what the capability does.</summary>
    public string Description { get; } = CapabilityValidation.Description(Description, nameof(Description));

    /// <summary>Gets the Grid node that implements the capability.</summary>
    public string OwningNode { get; } = CapabilityValidation.OwningNode(OwningNode, nameof(OwningNode));

    /// <summary>Gets the JSON Schema text for the arguments.</summary>
    public string ParameterSchema { get; } = CapabilityValidation.Schema(ParameterSchema, nameof(ParameterSchema));

    /// <summary>Gets the optional JSON Schema text for the output.</summary>
    public string? ReturnSchema { get; } = ReturnSchema is null ? null : CapabilityValidation.Schema(ReturnSchema, nameof(ReturnSchema));

    /// <summary>Gets the scopes and roles a caller must hold.</summary>
    public CapabilityPermissions Permissions { get; } = Permissions ?? throw new ArgumentNullException(nameof(Permissions));

    /// <summary>Gets the registry key, <c>(Name, Version)</c>.</summary>
    public CapabilityKey Key { get; } = new(Name, Version);
}
