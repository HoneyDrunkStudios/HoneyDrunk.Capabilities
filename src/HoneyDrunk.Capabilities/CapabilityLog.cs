using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Capabilities;

/// <summary>
/// Log messages for the capability runtime. They carry the capability key, deny codes and exception type names
/// only; arguments, output and exception messages are never logged (Grid invariant 8).
/// </summary>
internal static partial class CapabilityLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Registered capability {CapabilityKey} owned by {OwningNode}")]
    public static partial void Registered(ILogger logger, string capabilityKey, string owningNode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No capability is registered as {CapabilityKey}")]
    public static partial void NotFound(ILogger logger, string capabilityKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Capability {CapabilityKey} denied: {DenyCodes}")]
    public static partial void Denied(ILogger logger, string capabilityKey, string denyCodes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Capability {CapabilityKey} handler failed with {ExceptionType}")]
    public static partial void HandlerFailed(ILogger logger, string capabilityKey, string exceptionType);
}
