namespace HoneyDrunk.Capabilities;

/// <summary>Options for <see cref="AuthPolicyCapabilityGuard"/>.</summary>
public sealed class CapabilityGuardOptions
{
    /// <summary>Gets or sets the authentication scheme stamped on the identity built from a capability caller.</summary>
    public string IdentityScheme { get; set; } = "honeydrunk-capability-caller";
}
