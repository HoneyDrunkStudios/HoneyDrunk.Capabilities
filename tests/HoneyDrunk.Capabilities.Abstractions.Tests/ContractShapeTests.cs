using HoneyDrunk.Capabilities.Abstractions.Tests.ContractShape;
using System.Reflection;
using Xunit;

namespace HoneyDrunk.Capabilities.Abstractions.Tests;

/// <summary>
/// The contract-shape canary (ADR-0017 D8, Grid invariant 130). It fails when a frozen contract, or a public type
/// its signatures reach, changes shape without a version bump. To accept an intended change, raise <c>Version</c>
/// in <c>Directory.Build.props</c> and run the test with <c>HONEYDRUNK_UPDATE_CONTRACT_SHAPE=1</c>.
/// </summary>
public sealed class ContractShapeTests
{
    private const string BaselineFile = "HoneyDrunk.Capabilities.Abstractions.contract-shape.txt";
    private const string UpdateVariable = "HONEYDRUNK_UPDATE_CONTRACT_SHAPE";

    /// <summary>The four frozen contracts match the committed shape.</summary>
    [Fact]
    [Trait("Category", "ContractShape")]
    public void Frozen_contracts_match_the_committed_shape()
    {
        Type[] roots = [typeof(ICapabilityRegistry), typeof(CapabilityDescriptor), typeof(ICapabilityInvoker), typeof(ICapabilityGuard)];
        var shape = ContractShapeRenderer.Render(roots);
        var baseline = Path.Combine(FindRepositoryRoot(), "api", BaselineFile);
        var version = typeof(ICapabilityRegistry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var update = Environment.GetEnvironmentVariable(UpdateVariable) == "1";

        var (passed, message) = ContractShapeCheck.Verify(shape, baseline, version, update);

        Assert.True(passed, message);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (the folder holding the .slnx) was not found.");
    }
}
