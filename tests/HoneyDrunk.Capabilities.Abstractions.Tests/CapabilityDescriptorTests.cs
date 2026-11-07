using Xunit;

namespace HoneyDrunk.Capabilities.Abstractions.Tests;

/// <summary>Construction rules for <see cref="CapabilityDescriptor"/>, <see cref="CapabilityKey"/> and <see cref="CapabilityPermissions"/>.</summary>
public sealed class CapabilityDescriptorTests
{
    private const string ObjectSchema = """{"type":"object","properties":{"text":{"type":"string"}}}""";

    /// <summary>A descriptor that follows every rule is accepted and keeps its values.</summary>
    [Fact]
    public void Accepts_a_valid_descriptor()
    {
        var descriptor = Create(returnSchema: """{"type":"object"}""");

        Assert.Equal("test-tool", descriptor.Name);
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Equal("A test capability.", descriptor.Description);
        Assert.Equal("test-node", descriptor.OwningNode);
        Assert.Equal(ObjectSchema, descriptor.ParameterSchema);
        Assert.Equal("""{"type":"object"}""", descriptor.ReturnSchema);
        Assert.Equal(["test.invoke"], descriptor.Permissions.RequiredScopes);
        Assert.Empty(descriptor.Permissions.RequiredRoles);
        Assert.Equal(new CapabilityKey("test-tool", "1.0.0"), descriptor.Key);
    }

    /// <summary>A missing return schema is allowed.</summary>
    [Fact]
    public void Accepts_a_descriptor_without_a_return_schema()
    {
        Assert.Null(Create(returnSchema: null).ReturnSchema);
    }

    /// <summary>Only MAJOR.MINOR.PATCH with digits and no leading zeros is a version.</summary>
    /// <param name="version">The rejected version.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1.0")]
    [InlineData("v1")]
    [InlineData("01.0.0")]
    [InlineData("1.0.0-beta")]
    [InlineData("1.0.0.0")]
    [InlineData("1..0")]
    [InlineData("1.0.0\n")]
    public void Rejects_malformed_versions(string version)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => Create(version: version));
        Assert.Equal("Version", error.ParamName);
    }

    /// <summary>The key rejects the same versions as the descriptor.</summary>
    [Fact]
    public void Key_rejects_a_malformed_version()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityKey("test-tool", "1.0"));
        Assert.Equal("Version", error.ParamName);
    }

    /// <summary>Names are lowercase, start with a letter and are at most 64 characters.</summary>
    /// <param name="name">The rejected name.</param>
    [Theory]
    [InlineData("")]
    [InlineData("Test")]
    [InlineData("1tool")]
    [InlineData("has.dot")]
    [InlineData("has space")]
    [InlineData("test-tool\n")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Rejects_bad_names(string name)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => Create(name: name));
        Assert.Equal("Name", error.ParamName);
    }

    /// <summary>A 64-character name is the longest allowed.</summary>
    [Fact]
    public void Accepts_a_64_character_name()
    {
        var name = new string('a', 64);
        Assert.Equal(name, Create(name: name).Name);
    }

    /// <summary>The parameter schema must be JSON whose root is an object with type "object".</summary>
    /// <param name="schema">The rejected schema.</param>
    [Theory]
    [InlineData("[]")]
    [InlineData("""{"type":"string"}""")]
    [InlineData("""{"properties":{}}""")]
    [InlineData("{not json")]
    [InlineData("")]
    public void Rejects_schema_that_is_not_an_object(string schema)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => Create(parameterSchema: schema));
        Assert.Equal("ParameterSchema", error.ParamName);
    }

    /// <summary>A present return schema follows the same rules.</summary>
    [Fact]
    public void Rejects_a_return_schema_that_is_not_an_object()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => Create(returnSchema: "[]"));
        Assert.Equal("ReturnSchema", error.ParamName);
    }

    /// <summary>A schema longer than 32,768 characters is rejected.</summary>
    [Fact]
    public void Rejects_an_oversized_schema()
    {
        var schema = "{\"type\":\"object\",\"description\":\"" + new string('x', 32768) + "\"}";
        var error = Assert.ThrowsAny<ArgumentException>(() => Create(parameterSchema: schema));
        Assert.Equal("ParameterSchema", error.ParamName);
    }

    /// <summary>Description and owning node have length limits.</summary>
    [Fact]
    public void Rejects_bad_description_and_owning_node()
    {
        Assert.Equal("Description", Assert.ThrowsAny<ArgumentException>(() => Create(description: string.Empty)).ParamName);
        Assert.Equal("Description", Assert.ThrowsAny<ArgumentException>(() => Create(description: new string('d', 1025))).ParamName);
        Assert.Equal("OwningNode", Assert.ThrowsAny<ArgumentException>(() => Create(owningNode: " ")).ParamName);
        Assert.Equal("OwningNode", Assert.ThrowsAny<ArgumentException>(() => Create(owningNode: new string('n', 65))).ParamName);
    }

    /// <summary>A capability with no required scope or role is rejected (deny by default).</summary>
    [Fact]
    public void Rejects_empty_permissions()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityPermissions([], []));
        Assert.Equal("RequiredRoles", error.ParamName);
    }

    /// <summary>A required permission with whitespace is rejected.</summary>
    /// <param name="scope">The rejected scope.</param>
    [Theory]
    [InlineData("test invoke")]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData("test.invoke\t")]
    public void Rejects_whitespace_in_a_scope(string scope)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityPermissions([scope], []));
        Assert.Equal("RequiredScopes", error.ParamName);
    }

    /// <summary>A permission value longer than 128 characters is rejected.</summary>
    [Fact]
    public void Rejects_an_oversized_role()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityPermissions([], [new string('r', 129)]));
        Assert.Equal("RequiredRoles", error.ParamName);
    }

    /// <summary>Permissions copy their lists, so later changes to the source do not leak in.</summary>
    [Fact]
    public void Permissions_copy_their_lists()
    {
        var scopes = new List<string> { "test.invoke" };
        var permissions = new CapabilityPermissions(scopes, []);

        scopes.Add("test.other");

        Assert.Equal(["test.invoke"], permissions.RequiredScopes);
        Assert.IsNotType<List<string>>(permissions.RequiredScopes);
    }

    /// <summary>Roles alone are enough permissions.</summary>
    [Fact]
    public void Accepts_roles_without_scopes()
    {
        Assert.Equal(["test-role"], new CapabilityPermissions([], ["test-role"]).RequiredRoles);
    }

    /// <summary>The key prints as name@version.</summary>
    [Fact]
    public void Key_formats_as_name_at_version()
    {
        Assert.Equal("test-tool@1.0.0", new CapabilityKey("test-tool", "1.0.0").ToString());
        Assert.Equal("test-tool@2.10.3", Create(version: "2.10.3").Key.ToString());
    }

    /// <summary>Keys compare by exact name and version.</summary>
    [Fact]
    public void Keys_compare_exactly()
    {
        Assert.Equal(new CapabilityKey("test-tool", "1.0.0"), new CapabilityKey("test-tool", "1.0.0"));
        Assert.NotEqual(new CapabilityKey("test-tool", "1.0.0"), new CapabilityKey("test-tool", "1.0.1"));
    }

    private static CapabilityDescriptor Create(
        string name = "test-tool",
        string version = "1.0.0",
        string description = "A test capability.",
        string owningNode = "test-node",
        string parameterSchema = ObjectSchema,
        string? returnSchema = null) =>
        new(name, version, description, owningNode, parameterSchema, returnSchema, new CapabilityPermissions(["test.invoke"], []));
}
