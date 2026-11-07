using HoneyDrunk.Auth.Abstractions;
using HoneyDrunk.Capabilities.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static HoneyDrunk.Capabilities.Tests.TestHandlers;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Service registration through <see cref="CapabilityServiceCollectionExtensions"/>.</summary>
public sealed class RegistrationTests
{
    private static readonly ServiceProviderOptions Strict = new() { ValidateScopes = true, ValidateOnBuild = true };

    /// <summary>The registry is a singleton under both types; the invoker and guard are scoped and resolvable.</summary>
    [Fact]
    public void Registers_resolvable_services()
    {
        using var provider = Host().AddHoneyDrunkCapabilities(options => options.IdentityScheme = "test-scheme").BuildServiceProvider(Strict);
        using var scope = provider.CreateScope();

        var registry = provider.GetRequiredService<InProcessCapabilityRegistry>();
        Assert.Same(registry, provider.GetRequiredService<ICapabilityRegistry>());
        Assert.IsType<CapabilityInvoker>(scope.ServiceProvider.GetRequiredService<ICapabilityInvoker>());
        Assert.IsType<AuthPolicyCapabilityGuard>(scope.ServiceProvider.GetRequiredService<ICapabilityGuard>());
        Assert.Equal("test-scheme", provider.GetRequiredService<IOptions<CapabilityGuardOptions>>().Value.IdentityScheme);
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICapabilityInvoker>());
    }

    /// <summary>The Auth policy is the host's to compose; the extension never registers one.</summary>
    [Fact]
    public void Does_not_register_an_authorization_policy()
    {
        var services = new ServiceCollection().AddHoneyDrunkCapabilities();

        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IAuthorizationPolicy));
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(ITelemetryActivityFactory));
    }

    /// <summary>Declared capabilities are in the registry when it is first resolved, and run through the invoker.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Declared_capabilities_are_registered_on_first_resolve()
    {
        using var provider = Host()
            .AddHoneyDrunkCapabilities()
            .AddCapability<EchoHandler>(Descriptor())
            .AddCapability<EchoHandler>(Descriptor(version: "2.0.0"))
            .BuildServiceProvider(Strict);

        var registry = provider.GetRequiredService<ICapabilityRegistry>();
        Assert.Equal(["test-tool@1.0.0", "test-tool@2.0.0"], registry.List().Select(descriptor => descriptor.Key.ToString()));

        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICapabilityInvoker>().InvokeAsync(Invocation("""{"text":"hello"}"""));
        Assert.Equal(CapabilityResultStatus.Succeeded, result.Status);
        Assert.Equal("hello", result.Output!.Value.GetProperty("text").GetString());
    }

    /// <summary>Two declarations of the same key fail when the registry is first resolved, at startup.</summary>
    [Fact]
    public void Duplicate_declaration_fails_at_startup()
    {
        using var provider = Host()
            .AddHoneyDrunkCapabilities()
            .AddCapability<EchoHandler>(Descriptor())
            .AddCapability<EchoHandler>(Descriptor())
            .BuildServiceProvider(Strict);

        var error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICapabilityRegistry>());
        Assert.Contains("test-tool@1.0.0", error.Message, StringComparison.Ordinal);
    }

    private static ServiceCollection Host()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationPolicy>(new FakeAuthorizationPolicy());
        services.AddScoped<ITelemetryActivityFactory, RecordingTelemetryActivityFactory>();
        return services;
    }
}
