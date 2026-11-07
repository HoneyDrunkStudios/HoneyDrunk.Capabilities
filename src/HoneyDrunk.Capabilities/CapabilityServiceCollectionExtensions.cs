using HoneyDrunk.Capabilities.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Capabilities;

/// <summary>Registers the capability runtime and declared capabilities.</summary>
public static class CapabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-process registry (singleton, as itself and as <see cref="ICapabilityRegistry"/>), the invoker and
    /// the Auth-backed guard (both scoped, because Kernel's <c>ITelemetryActivityFactory</c> is scoped). The host
    /// composes <c>IAuthorizationPolicy</c>, Kernel telemetry and logging; this method registers none of them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optionally configures the guard options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddHoneyDrunkCapabilities(this IServiceCollection services, Action<CapabilityGuardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<CapabilityGuardOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton(CreateRegistry);
        services.TryAddSingleton<ICapabilityRegistry>(provider => provider.GetRequiredService<InProcessCapabilityRegistry>());
        services.TryAddScoped<ICapabilityGuard, AuthPolicyCapabilityGuard>();
        services.TryAddScoped<ICapabilityInvoker, CapabilityInvoker>();
        return services;
    }

    /// <summary>
    /// Declares a capability and its handler. The handler is a singleton. The registry registers every declared
    /// capability when it is first resolved, so a duplicate key fails at startup.
    /// </summary>
    /// <typeparam name="THandler">The handler type, implemented by the owning node.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="descriptor">The capability descriptor.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCapability<THandler>(this IServiceCollection services, CapabilityDescriptor descriptor)
        where THandler : class, ICapabilityHandler
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptor);

        services.TryAddSingleton<THandler>();
        services.AddSingleton(new CapabilityDeclaration(descriptor, typeof(THandler)));
        return services;
    }

    private static InProcessCapabilityRegistry CreateRegistry(IServiceProvider provider)
    {
        var registry = new InProcessCapabilityRegistry(provider.GetRequiredService<ILogger<InProcessCapabilityRegistry>>());
        foreach (var declaration in provider.GetServices<CapabilityDeclaration>())
        {
            registry.Register(declaration.Descriptor, (ICapabilityHandler)provider.GetRequiredService(declaration.HandlerType));
        }

        return registry;
    }

    private sealed record CapabilityDeclaration(CapabilityDescriptor Descriptor, Type HandlerType);
}
