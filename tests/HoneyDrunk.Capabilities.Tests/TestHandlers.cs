using HoneyDrunk.Capabilities.Abstractions;
using System.Text.Json;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Handlers, a guard and fixtures for the runtime tests.</summary>
internal static class TestHandlers
{
    public const string ObjectSchema = """{"type":"object"}""";

    public static CapabilityDescriptor Descriptor(string name = "test-tool", string version = "1.0.0") =>
        new(name, version, "A test capability.", "test-node", ObjectSchema, null, new CapabilityPermissions(["test.invoke"], []));

    public static CapabilityCaller Caller() => new("test-caller", ["test.invoke"], []);

    public static CapabilityInvocation Invocation(string json = "{}", string name = "test-tool", string version = "1.0.0")
    {
        using var document = JsonDocument.Parse(json);
        return new CapabilityInvocation(new CapabilityKey(name, version), document.RootElement, Caller());
    }

    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>A handler that runs a delegate and counts its calls.</summary>
    /// <param name="handle">What the handler does.</param>
    internal sealed class CountingHandler(Func<CapabilityInvocation, CancellationToken, Task<CapabilityResult>> handle) : ICapabilityHandler
    {
        private int calls;

        public int Calls => this.calls;

        public static CountingHandler Returning(CapabilityResult result) => new((_, _) => Task.FromResult(result));

        public static CountingHandler Throwing(Exception error) => new((_, _) => Task.FromException<CapabilityResult>(error));

        public Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.calls);
            return handle(invocation, cancellationToken);
        }
    }

    /// <summary>A guard that returns a fixed decision and counts its calls.</summary>
    /// <param name="decide">What the guard decides.</param>
    internal sealed class CountingGuard(Func<CapabilityDescriptor, CapabilityCaller, CancellationToken, Task<CapabilityGuardDecision>> decide) : ICapabilityGuard
    {
        private int calls;

        public int Calls => this.calls;

        public static CountingGuard Allowing() => new((_, _, _) => Task.FromResult(CapabilityGuardDecision.Allow()));

        public static CountingGuard Denying(params string[] codes) => new((_, _, _) => Task.FromResult(CapabilityGuardDecision.Deny(codes)));

        public Task<CapabilityGuardDecision> AuthorizeAsync(CapabilityDescriptor descriptor, CapabilityCaller caller, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref this.calls);
            return decide(descriptor, caller, cancellationToken);
        }
    }
}
