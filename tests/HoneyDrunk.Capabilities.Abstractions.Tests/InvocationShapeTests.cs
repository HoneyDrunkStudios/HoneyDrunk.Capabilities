using System.Reflection;
using System.Text.Json;
using Xunit;

namespace HoneyDrunk.Capabilities.Abstractions.Tests;

/// <summary>Rules for the invocation, caller, result and guard decision shapes.</summary>
public sealed class InvocationShapeTests
{
    private static readonly CapabilityKey Key = new("test-tool", "1.0.0");
    private static readonly CapabilityCaller Caller = new("test-caller", ["test.invoke"], []);

    /// <summary>Arguments must be a JSON object.</summary>
    /// <param name="json">The rejected arguments.</param>
    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    public void Invocation_rejects_non_object_arguments(string json)
    {
        using var document = JsonDocument.Parse(json);

        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityInvocation(Key, document.RootElement, Caller));
        Assert.Equal("Arguments", error.ParamName);
    }

    /// <summary>A default (undefined) argument element is rejected.</summary>
    [Fact]
    public void Invocation_rejects_undefined_arguments()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityInvocation(Key, default, Caller));
        Assert.Equal("Arguments", error.ParamName);
    }

    /// <summary>The invocation keeps its own copy of the arguments.</summary>
    [Fact]
    public void Invocation_arguments_survive_disposal_of_the_source_document()
    {
        CapabilityInvocation invocation;
        using (var document = JsonDocument.Parse("""{"text":"hello"}"""))
        {
            invocation = new CapabilityInvocation(Key, document.RootElement, Caller);
        }

        Assert.Equal("hello", invocation.Arguments.GetProperty("text").GetString());
    }

    /// <summary>An invocation needs a caller and a constructed key.</summary>
    [Fact]
    public void Invocation_rejects_missing_caller_and_default_key()
    {
        using var document = JsonDocument.Parse("{}");

        Assert.Equal("Caller", Assert.ThrowsAny<ArgumentException>(() => new CapabilityInvocation(Key, document.RootElement, null!)).ParamName);
        Assert.Equal("Key", Assert.ThrowsAny<ArgumentException>(() => new CapabilityInvocation(default, document.RootElement, Caller)).ParamName);
    }

    /// <summary>A caller needs a subject.</summary>
    /// <param name="subject">The rejected subject.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Caller_rejects_blank_subject(string subject)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new CapabilityCaller(subject, [], []));
        Assert.Equal("SubjectId", error.ParamName);
    }

    /// <summary>A caller copies its scope and role lists.</summary>
    [Fact]
    public void Caller_copies_its_lists()
    {
        var scopes = new List<string> { "test.invoke" };
        var caller = new CapabilityCaller("test-caller", scopes, []);

        scopes.Clear();

        Assert.Equal(["test.invoke"], caller.Scopes);
    }

    /// <summary>Each factory sets its status and error code.</summary>
    /// <param name="factory">Which factory to call.</param>
    /// <param name="status">The expected status.</param>
    /// <param name="errorCode">The expected error code.</param>
    [Theory]
    [InlineData("success", CapabilityResultStatus.Succeeded, null)]
    [InlineData("not_found", CapabilityResultStatus.NotFound, "not_found")]
    [InlineData("denied", CapabilityResultStatus.Denied, "denied")]
    [InlineData("invalid_arguments", CapabilityResultStatus.InvalidArguments, "invalid_arguments")]
    [InlineData("failed", CapabilityResultStatus.Failed, "test_error")]
    public void Result_factories_set_status_and_codes(string factory, CapabilityResultStatus status, string? errorCode)
    {
        var result = factory switch
        {
            "success" => CapabilityResult.Success(null),
            "not_found" => CapabilityResult.NotFound(Key),
            "denied" => CapabilityResult.Denied(["MissingScope"]),
            "invalid_arguments" => CapabilityResult.InvalidArguments("The text argument is required."),
            _ => CapabilityResult.Failed("test_error", "The test failed."),
        };

        Assert.Equal(status, result.Status);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(status == CapabilityResultStatus.Succeeded, result.ErrorMessage is null);
        Assert.Equal(status == CapabilityResultStatus.Denied ? ["MissingScope"] : [], result.DenyCodes);
    }

    /// <summary>A not-found result names the key it missed.</summary>
    [Fact]
    public void Not_found_names_the_key()
    {
        Assert.Contains("test-tool@1.0.0", CapabilityResult.NotFound(Key).ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>A success result keeps its own copy of the output.</summary>
    [Fact]
    public void Success_output_survives_disposal_of_the_source_document()
    {
        CapabilityResult result;
        using (var document = JsonDocument.Parse("""{"echo":"hello"}"""))
        {
            result = CapabilityResult.Success(document.RootElement);
        }

        Assert.Equal("hello", result.Output!.Value.GetProperty("echo").GetString());
    }

    /// <summary>Guard decisions carry deny codes only when they deny.</summary>
    [Fact]
    public void Guard_decision_factories()
    {
        var allow = CapabilityGuardDecision.Allow();
        var deny = CapabilityGuardDecision.Deny(["MissingScope", "MissingRole"]);

        Assert.True(allow.IsAllowed);
        Assert.Empty(allow.DenyCodes);
        Assert.False(deny.IsAllowed);
        Assert.Equal(["MissingScope", "MissingRole"], deny.DenyCodes);
    }

    /// <summary>The public registry never hands out a handler, so the invoker is the only way to run a capability.</summary>
    [Fact]
    public void Registry_exposes_no_handler()
    {
        var members = typeof(ICapabilityRegistry).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var member in members)
        {
            var type = member switch
            {
                MethodInfo method => method.ReturnType,
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null,
            };

            Assert.False(type is not null && Mentions(type, typeof(ICapabilityHandler)), $"{member.Name} exposes a handler.");
        }
    }

    private static bool Mentions(Type type, Type target) =>
        type == target
        || (type.HasElementType && Mentions(type.GetElementType()!, target))
        || (type.IsGenericType && type.GetGenericArguments().Any(argument => Mentions(argument, target)));
}
