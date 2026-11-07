using HoneyDrunk.Capabilities.Abstractions;

namespace HoneyDrunk.Capabilities.Testing;

/// <summary>Descriptor and caller fixtures with made-up defaults: <c>test-tool</c>, <c>1.0.0</c>, scope <c>test.invoke</c>.</summary>
public static class CapabilityFixtures
{
    /// <summary>The default capability name.</summary>
    public const string Name = "test-tool";

    /// <summary>The default capability version.</summary>
    public const string Version = "1.0.0";

    /// <summary>The default scope.</summary>
    public const string Scope = "test.invoke";

    /// <summary>Creates a valid descriptor.</summary>
    /// <param name="name">The name.</param>
    /// <param name="version">The version.</param>
    /// <param name="requiredScopes">The required scopes; <c>test.invoke</c> when omitted.</param>
    /// <param name="requiredRoles">The required roles; none when omitted.</param>
    /// <param name="parameterSchema">The parameter schema; an empty object schema when omitted.</param>
    /// <returns>The descriptor.</returns>
    public static CapabilityDescriptor Descriptor(
        string name = Name,
        string version = Version,
        IReadOnlyList<string>? requiredScopes = null,
        IReadOnlyList<string>? requiredRoles = null,
        string parameterSchema = """{"type":"object"}""") =>
        new(name, version, "A test capability.", "test-node", parameterSchema, null, new CapabilityPermissions(requiredScopes ?? [Scope], requiredRoles ?? []));

    /// <summary>Creates a caller.</summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="scopes">The scopes held; <c>test.invoke</c> when omitted.</param>
    /// <param name="roles">The roles held; none when omitted.</param>
    /// <returns>The caller.</returns>
    public static CapabilityCaller Caller(string subjectId = "test-caller", IReadOnlyList<string>? scopes = null, IReadOnlyList<string>? roles = null) =>
        new(subjectId, scopes ?? [Scope], roles ?? []);
}
