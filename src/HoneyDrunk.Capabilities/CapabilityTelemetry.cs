using HoneyDrunk.Capabilities.Abstractions;
using System.Diagnostics;

namespace HoneyDrunk.Capabilities;

/// <summary>
/// Activity names and the tag allowlist for the capability runtime (ADR-0017 D7). Only these tags are ever set;
/// arguments and output are never tagged.
/// </summary>
internal static class CapabilityTelemetry
{
    /// <summary>The activity that covers resolving a key.</summary>
    public const string ResolveActivity = "capabilities.resolve";

    /// <summary>The activity that covers the guard and the handler.</summary>
    public const string InvokeActivity = "capabilities.invoke";

    /// <summary>The capability name tag.</summary>
    public const string NameTag = "capability.name";

    /// <summary>The capability version tag.</summary>
    public const string VersionTag = "capability.version";

    /// <summary>The outcome tag.</summary>
    public const string StatusTag = "capability.status";

    /// <summary>The deny codes tag.</summary>
    public const string DenyCodeTag = "capability.deny_code";

    /// <summary>Builds the starting tags for a key.</summary>
    /// <param name="key">The capability key.</param>
    /// <returns>The tags.</returns>
    public static Dictionary<string, object?> KeyTags(CapabilityKey key) => new(StringComparer.Ordinal)
    {
        [NameTag] = key.Name,
        [VersionTag] = key.Version,
    };

    /// <summary>Sets the outcome tag.</summary>
    /// <param name="activity">The activity, if one was started.</param>
    /// <param name="status">The outcome.</param>
    public static void SetStatus(Activity? activity, CapabilityResultStatus status) => activity?.SetTag(StatusTag, StatusName(status));

    /// <summary>Sets the deny codes tag.</summary>
    /// <param name="activity">The activity, if one was started.</param>
    /// <param name="denyCodes">The deny codes, joined with commas.</param>
    public static void SetDenyCodes(Activity? activity, string denyCodes) => activity?.SetTag(DenyCodeTag, denyCodes);

    /// <summary>Gets the tag value for an outcome.</summary>
    /// <param name="status">The outcome.</param>
    /// <returns>The snake_case name.</returns>
    public static string StatusName(CapabilityResultStatus status) => status switch
    {
        CapabilityResultStatus.Succeeded => "succeeded",
        CapabilityResultStatus.NotFound => "not_found",
        CapabilityResultStatus.Denied => "denied",
        CapabilityResultStatus.InvalidArguments => "invalid_arguments",
        CapabilityResultStatus.Failed => "failed",
        _ => "unknown",
    };
}
