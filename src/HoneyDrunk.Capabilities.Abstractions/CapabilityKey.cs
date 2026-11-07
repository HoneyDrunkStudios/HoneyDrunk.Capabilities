namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// The registry key of a capability: its name and its version. Lookup is exact on both; there is no
/// "latest" (ADR-0017 D6, Grid invariant 128).
/// </summary>
/// <param name="Name">The capability name: a lowercase letter, then up to 63 lowercase letters, digits, '_' or '-'.</param>
/// <param name="Version">The capability version: MAJOR.MINOR.PATCH, digits only, no leading zeros, no pre-release.</param>
public readonly record struct CapabilityKey(string Name, string Version)
{
    /// <summary>Gets the capability name.</summary>
    public string Name { get; } = CapabilityValidation.Name(Name, nameof(Name));

    /// <summary>Gets the capability version.</summary>
    public string Version { get; } = CapabilityValidation.Version(Version, nameof(Version));

    /// <summary>Formats the key as <c>name@version</c>.</summary>
    /// <returns>The formatted key.</returns>
    public override string ToString() => $"{this.Name}@{this.Version}";
}
