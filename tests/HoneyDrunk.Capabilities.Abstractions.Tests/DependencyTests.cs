using Xunit;

namespace HoneyDrunk.Capabilities.Abstractions.Tests;

/// <summary>Dependency rules for the Abstractions package (Grid invariant 1).</summary>
public sealed class DependencyTests
{
    /// <summary>The Abstractions assembly references no HoneyDrunk assembly.</summary>
    [Fact]
    public void Abstractions_reference_no_HoneyDrunk_assembly()
    {
        var references = typeof(CapabilityDescriptor).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => reference.Name!.StartsWith("HoneyDrunk.", StringComparison.Ordinal));
    }
}
