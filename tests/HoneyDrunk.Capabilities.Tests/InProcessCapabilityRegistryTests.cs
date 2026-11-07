using HoneyDrunk.Capabilities.Abstractions;
using Xunit;
using static HoneyDrunk.Capabilities.Tests.TestHandlers;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Behavior of <see cref="InProcessCapabilityRegistry"/>.</summary>
public sealed class InProcessCapabilityRegistryTests
{
    /// <summary>A registered capability resolves by its exact key.</summary>
    [Fact]
    public void Registers_and_resolves_by_exact_key()
    {
        var registry = new InProcessCapabilityRegistry(new ListLogger<InProcessCapabilityRegistry>());
        var descriptor = Descriptor();
        var handler = CountingHandler.Returning(CapabilityResult.Success(null));

        registry.Register(descriptor, handler);

        Assert.Same(descriptor, registry.Resolve(new CapabilityKey("test-tool", "1.0.0")));
        Assert.True(registry.TryGetHandler(descriptor.Key, out var found));
        Assert.Same(handler, found);
    }

    /// <summary>Another version is never returned in place of the one asked for.</summary>
    [Fact]
    public void Resolve_does_not_fall_back_to_another_version()
    {
        var registry = new InProcessCapabilityRegistry(new ListLogger<InProcessCapabilityRegistry>());
        registry.Register(Descriptor(version: "1.0.0"), CountingHandler.Returning(CapabilityResult.Success(null)));

        Assert.Null(registry.Resolve(new CapabilityKey("test-tool", "2.0.0")));
        Assert.Null(registry.Resolve(new CapabilityKey("test-tool", "1.0.1")));
        Assert.Null(registry.Resolve(new CapabilityKey("other-tool", "1.0.0")));
        Assert.Null(registry.Resolve(default));
        Assert.False(registry.TryGetHandler(new CapabilityKey("test-tool", "2.0.0"), out _));
    }

    /// <summary>A second registration under the same key throws and names the key.</summary>
    [Fact]
    public void Duplicate_key_throws()
    {
        var registry = new InProcessCapabilityRegistry(new ListLogger<InProcessCapabilityRegistry>());
        var first = CountingHandler.Returning(CapabilityResult.Success(null));
        registry.Register(Descriptor(), first);

        var error = Assert.Throws<InvalidOperationException>(() => registry.Register(Descriptor(), CountingHandler.Returning(CapabilityResult.Success(null))));

        Assert.Contains("test-tool@1.0.0", error.Message, StringComparison.Ordinal);
        Assert.True(registry.TryGetHandler(Descriptor().Key, out var kept));
        Assert.Same(first, kept);
    }

    /// <summary>Listing is sorted by name, then by numeric version.</summary>
    [Fact]
    public void List_is_sorted()
    {
        var registry = new InProcessCapabilityRegistry(new ListLogger<InProcessCapabilityRegistry>());
        foreach (var (name, version) in new[] { ("zeta-tool", "1.0.0"), ("test-tool", "10.0.0"), ("test-tool", "2.0.0"), ("test-tool", "2.0.10"), ("test-tool", "2.0.9"), ("alpha-tool", "1.0.0") })
        {
            registry.Register(Descriptor(name, version), CountingHandler.Returning(CapabilityResult.Success(null)));
        }

        var keys = registry.List().Select(descriptor => descriptor.Key.ToString());

        Assert.Equal(["alpha-tool@1.0.0", "test-tool@2.0.0", "test-tool@2.0.9", "test-tool@2.0.10", "test-tool@10.0.0", "zeta-tool@1.0.0"], keys);
    }

    /// <summary>Registrations from many threads all land.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Parallel_registrations_of_distinct_keys_all_land()
    {
        var registry = new InProcessCapabilityRegistry(new ListLogger<InProcessCapabilityRegistry>());

        await Task.WhenAll(Enumerable.Range(0, 100).Select(index => Task.Run(() =>
            registry.Register(Descriptor(version: $"1.0.{index}"), CountingHandler.Returning(CapabilityResult.Success(null))))));

        Assert.Equal(100, registry.List().Count);
        Assert.All(Enumerable.Range(0, 100), index => Assert.NotNull(registry.Resolve(new CapabilityKey("test-tool", $"1.0.{index}"))));
    }

    /// <summary>A registration is logged with its key, at information level.</summary>
    [Fact]
    public void Registration_is_logged()
    {
        var logger = new ListLogger<InProcessCapabilityRegistry>();
        var registry = new InProcessCapabilityRegistry(logger);

        registry.Register(Descriptor(), CountingHandler.Returning(CapabilityResult.Success(null)));

        Assert.Contains("test-tool@1.0.0", Assert.Single(logger.Entries).message, StringComparison.Ordinal);
    }
}
