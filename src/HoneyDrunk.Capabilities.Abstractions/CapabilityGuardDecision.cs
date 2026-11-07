namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>The guard's answer for one invocation.</summary>
/// <param name="IsAllowed">Whether the caller may invoke the capability.</param>
/// <param name="DenyCodes">Why the caller was denied; empty when allowed.</param>
public sealed record CapabilityGuardDecision(bool IsAllowed, IReadOnlyList<string> DenyCodes)
{
    /// <summary>Gets why the caller was denied; empty when allowed.</summary>
    public IReadOnlyList<string> DenyCodes { get; } = CapabilityValidation.NonBlankList(DenyCodes, nameof(DenyCodes));

    /// <summary>Creates an allow decision.</summary>
    /// <returns>The decision.</returns>
    public static CapabilityGuardDecision Allow() => new(true, []);

    /// <summary>Creates a deny decision.</summary>
    /// <param name="codes">Why the caller was denied.</param>
    /// <returns>The decision.</returns>
    public static CapabilityGuardDecision Deny(IReadOnlyList<string> codes) => new(false, codes);
}
