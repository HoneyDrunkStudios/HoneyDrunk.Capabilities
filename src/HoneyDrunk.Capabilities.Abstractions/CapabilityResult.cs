using System.Text.Json;

namespace HoneyDrunk.Capabilities.Abstractions;

/// <summary>
/// The outcome of a capability invocation. Error messages are fixed text chosen by the code that produced the
/// result; they never carry arguments, output or exception messages.
/// </summary>
/// <param name="Status">How the invocation ended.</param>
/// <param name="Output">The output, when the handler produced one. The result keeps its own copy.</param>
/// <param name="ErrorCode">A stable error code, or <see langword="null"/> on success.</param>
/// <param name="ErrorMessage">A short, fixed error message, or <see langword="null"/> on success.</param>
public sealed record CapabilityResult(CapabilityResultStatus Status, JsonElement? Output, string? ErrorCode, string? ErrorMessage)
{
    /// <summary>The error code of a <see cref="CapabilityResultStatus.NotFound"/> result.</summary>
    private const string NotFoundCode = "not_found";

    /// <summary>The error code of a <see cref="CapabilityResultStatus.Denied"/> result.</summary>
    private const string DeniedCode = "denied";

    /// <summary>The error code of a <see cref="CapabilityResultStatus.InvalidArguments"/> result.</summary>
    private const string InvalidArgumentsCode = "invalid_arguments";

    /// <summary>Gets the output, when the handler produced one.</summary>
    public JsonElement? Output { get; } = Output is { ValueKind: not JsonValueKind.Undefined } output ? output.Clone() : Output;

    /// <summary>Gets the guard's deny codes on a <see cref="CapabilityResultStatus.Denied"/> result; otherwise empty.</summary>
    public IReadOnlyList<string> DenyCodes { get; private init; } = [];

    /// <summary>Creates a success result.</summary>
    /// <param name="output">The output, if any.</param>
    /// <returns>The result.</returns>
    public static CapabilityResult Success(JsonElement? output) => new(CapabilityResultStatus.Succeeded, output, null, null);

    /// <summary>Creates a not-found result for a key with no registration.</summary>
    /// <param name="key">The key that was not found.</param>
    /// <returns>The result.</returns>
    public static CapabilityResult NotFound(CapabilityKey key) =>
        new(CapabilityResultStatus.NotFound, null, NotFoundCode, $"No capability is registered as {key}.");

    /// <summary>Creates a denied result carrying the guard's deny codes.</summary>
    /// <param name="denyCodes">The deny codes.</param>
    /// <returns>The result.</returns>
    public static CapabilityResult Denied(IReadOnlyList<string> denyCodes) =>
        new(CapabilityResultStatus.Denied, null, DeniedCode, "The caller is not authorized to invoke this capability.")
        {
            DenyCodes = CapabilityValidation.NonBlankList(denyCodes, nameof(denyCodes)),
        };

    /// <summary>Creates an invalid-arguments result. The message must not quote the arguments.</summary>
    /// <param name="message">A short message naming what is wrong.</param>
    /// <returns>The result.</returns>
    public static CapabilityResult InvalidArguments(string message) =>
        new(CapabilityResultStatus.InvalidArguments, null, InvalidArgumentsCode, CapabilityValidation.NonBlank(message, nameof(message)));

    /// <summary>Creates a failed result. The message must not carry exception text or arguments.</summary>
    /// <param name="errorCode">A stable error code.</param>
    /// <param name="message">A short, fixed message.</param>
    /// <returns>The result.</returns>
    public static CapabilityResult Failed(string errorCode, string message) =>
        new(CapabilityResultStatus.Failed, null, CapabilityValidation.NonBlank(errorCode, nameof(errorCode)), CapabilityValidation.NonBlank(message, nameof(message)));
}
