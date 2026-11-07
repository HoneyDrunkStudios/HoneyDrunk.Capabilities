using HoneyDrunk.Capabilities.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Capabilities.Testing;

/// <summary>
/// Composes the real <see cref="InProcessCapabilityRegistry"/> and <see cref="CapabilityInvoker"/> with a
/// <see cref="ScriptedCapabilityGuard"/> and no telemetry, so tests share the runtime's exact semantics instead of
/// copying them.
/// </summary>
public sealed class CapabilityTestHarness
{
    private readonly InProcessCapabilityRegistry registry;

    /// <summary>Initializes a new instance of the <see cref="CapabilityTestHarness"/> class.</summary>
    /// <param name="guard">The guard to use; allows every caller when omitted.</param>
    public CapabilityTestHarness(ScriptedCapabilityGuard? guard = null)
    {
        this.registry = new InProcessCapabilityRegistry(NullLogger<InProcessCapabilityRegistry>.Instance);
        this.Guard = guard ?? ScriptedCapabilityGuard.AllowAll();
        this.Invoker = new CapabilityInvoker(this.registry, this.Guard, new NullTelemetryActivityFactory(), NullLogger<CapabilityInvoker>.Instance);
    }

    /// <summary>Gets the registry.</summary>
    public ICapabilityRegistry Registry => this.registry;

    /// <summary>Gets the invoker.</summary>
    public ICapabilityInvoker Invoker { get; }

    /// <summary>Gets the guard.</summary>
    public ScriptedCapabilityGuard Guard { get; }

    /// <summary>Registers a capability and its handler.</summary>
    /// <param name="descriptor">The capability descriptor.</param>
    /// <param name="handler">The handler.</param>
    public void Register(CapabilityDescriptor descriptor, ICapabilityHandler handler) => this.registry.Register(descriptor, handler);
}
