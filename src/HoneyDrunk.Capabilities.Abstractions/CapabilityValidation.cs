using System.Text.Json;

namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>The construction rules shared by the capability records.</summary>
internal static class CapabilityValidation
{
    private const int MaxNameLength = 64;
    private const int MaxDescriptionLength = 1024;
    private const int MaxOwningNodeLength = 64;
    private const int MaxSchemaLength = 32768;
    private const int MaxPermissionLength = 128;

    /// <summary>Checks a capability name: a lowercase letter, then up to 63 lowercase letters, digits, '_' or '-'.</summary>
    /// <param name="name">The name.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked name.</returns>
    public static string Name(string name, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(name, parameterName);
        if (name.Length is 0 or > MaxNameLength || !char.IsAsciiLetterLower(name[0]))
        {
            throw new ArgumentException("A capability name is 1 to 64 characters and starts with a lowercase letter.", parameterName);
        }

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character is not '_' and not '-')
            {
                throw new ArgumentException("A capability name holds only lowercase letters, digits, '_' and '-'.", parameterName);
            }
        }

        return name;
    }

    /// <summary>Checks a capability version: MAJOR.MINOR.PATCH, digits only, no leading zeros, no pre-release.</summary>
    /// <param name="version">The version.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked version.</returns>
    public static string Version(string version, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(version, parameterName);
        var parts = version.Split('.');
        if (parts.Length != 3 || !Array.TrueForAll(parts, IsVersionNumber))
        {
            throw new ArgumentException("A capability version is MAJOR.MINOR.PATCH with digits only and no leading zeros.", parameterName);
        }

        return version;
    }

    /// <summary>Checks a required text value with a maximum length.</summary>
    /// <param name="value">The value.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked value.</returns>
    public static string Text(string value, int maxLength, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            throw new ArgumentException($"The value must be 1 to {maxLength} characters and not blank.", parameterName);
        }

        return value;
    }

    /// <summary>Checks a description (1 to 1024 characters).</summary>
    /// <param name="description">The description.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked description.</returns>
    public static string Description(string description, string parameterName) => Text(description, MaxDescriptionLength, parameterName);

    /// <summary>Checks an owning node (1 to 64 characters).</summary>
    /// <param name="owningNode">The owning node.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked owning node.</returns>
    public static string OwningNode(string owningNode, string parameterName) => Text(owningNode, MaxOwningNodeLength, parameterName);

    /// <summary>Checks a schema: JSON text whose root is an object with <c>"type": "object"</c>, at most 32,768 characters.</summary>
    /// <param name="schema">The schema text.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>The checked schema text.</returns>
    public static string Schema(string schema, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(schema, parameterName);
        if (schema.Length is 0 or > MaxSchemaLength)
        {
            throw new ArgumentException("A schema is 1 to 32768 characters.", parameterName);
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String
                || !type.ValueEquals("object"))
            {
                throw new ArgumentException("A schema's root must be an object with \"type\": \"object\".", parameterName);
            }
        }
        catch (JsonException)
        {
            throw new ArgumentException("A schema must be valid JSON.", parameterName);
        }

        return schema;
    }

    /// <summary>Copies a permission list; each value is 1 to 128 characters with no whitespace.</summary>
    /// <param name="values">The values.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>A read-only copy.</returns>
    public static IReadOnlyList<string> Permissions(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copy = values.ToArray();
        foreach (var value in copy)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxPermissionLength || value.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException("A permission value is 1 to 128 characters with no whitespace.", parameterName);
            }
        }

        return Array.AsReadOnly(copy);
    }

    /// <summary>Copies a list of non-blank values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="parameterName">The parameter to name in the exception.</param>
    /// <returns>A read-only copy.</returns>
    public static IReadOnlyList<string> NonBlankList(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copy = values.ToArray();
        if (Array.Exists(copy, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("The list must not hold a blank value.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }

    private static bool IsVersionNumber(string part) =>
        part.Length > 0
        && part.All(char.IsAsciiDigit)
        && (part.Length == 1 || part[0] != '0');
}
