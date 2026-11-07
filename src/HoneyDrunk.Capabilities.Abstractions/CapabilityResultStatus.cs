namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>How a capability invocation ended.</summary>
public enum CapabilityResultStatus
{
    /// <summary>The handler ran and succeeded.</summary>
    Succeeded = 0,

    /// <summary>No capability is registered under the exact key.</summary>
    NotFound = 1,

    /// <summary>The guard denied the caller; the handler did not run.</summary>
    Denied = 2,

    /// <summary>The handler rejected the arguments.</summary>
    InvalidArguments = 3,

    /// <summary>The handler failed.</summary>
    Failed = 4,
}
