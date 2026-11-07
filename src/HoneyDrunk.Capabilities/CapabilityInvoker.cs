using HoneyDrunk.Capabilities.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Capabilities;

/// <summary>
/// Runs capabilities: resolves the exact key, asks the guard exactly once, then dispatches to the handler. A miss
/// never reaches the guard and a denial never reaches the handler. A guard exception is a <c>guard_error</c>
/// denial and a handler exception is a fixed <c>handler_error</c> result; in both cases only the exception type
/// name and the key are logged. Only the caller's cancellation propagates.
/// </summary>
/// <param name="registry">The registry that holds the handlers.</param>
/// <param name="guard">The authorization guard.</param>
/// <param name="telemetry">Kernel's activity factory.</param>
/// <param name="logger">The logger.</param>
public sealed class CapabilityInvoker(
    InProcessCapabilityRegistry registry,
    ICapabilityGuard guard,
    ITelemetryActivityFactory telemetry,
    ILogger<CapabilityInvoker> logger) : ICapabilityInvoker
{
    private const string HandlerErrorCode = "handler_error";
    private const string HandlerErrorMessage = "The capability handler failed.";
    private const string GuardErrorCode = "guard_error";

    private readonly InProcessCapabilityRegistry registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly ICapabilityGuard guard = guard ?? throw new ArgumentNullException(nameof(guard));
    private readonly ITelemetryActivityFactory telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<CapabilityResult> InvokeAsync(CapabilityInvocation invocation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        var key = invocation.Key;

        CapabilityDescriptor? descriptor;
        ICapabilityHandler? handler;
        using (var resolve = this.telemetry.Start(CapabilityTelemetry.ResolveActivity, CapabilityTelemetry.KeyTags(key)))
        {
            descriptor = this.registry.Resolve(key);
            if (descriptor is null || !this.registry.TryGetHandler(key, out handler))
            {
                CapabilityTelemetry.SetStatus(resolve, CapabilityResultStatus.NotFound);
                CapabilityLog.NotFound(this.logger, key.ToString());
                return CapabilityResult.NotFound(key);
            }

            CapabilityTelemetry.SetStatus(resolve, CapabilityResultStatus.Succeeded);
        }

        using var invoke = this.telemetry.Start(CapabilityTelemetry.InvokeActivity, CapabilityTelemetry.KeyTags(key));
        CapabilityGuardDecision? decision;
        try
        {
            decision = await this.guard.AuthorizeAsync(descriptor, invocation.Caller, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            CapabilityLog.GuardFailed(this.logger, key.ToString(), error.GetType().FullName ?? error.GetType().Name);
            CapabilityTelemetry.SetStatus(invoke, CapabilityResultStatus.Denied);
            CapabilityTelemetry.SetDenyCodes(invoke, GuardErrorCode);
            return CapabilityResult.Denied([GuardErrorCode]);
        }

        if (decision is not { IsAllowed: true })
        {
            IReadOnlyList<string> denyCodes = decision is { DenyCodes.Count: > 0 } ? decision.DenyCodes : [GuardErrorCode];
            var joined = string.Join(',', denyCodes);
            CapabilityTelemetry.SetStatus(invoke, CapabilityResultStatus.Denied);
            CapabilityTelemetry.SetDenyCodes(invoke, joined);
            CapabilityLog.Denied(this.logger, key.ToString(), joined);
            return CapabilityResult.Denied(denyCodes);
        }

        CapabilityResult? result;
        try
        {
            result = await handler.HandleAsync(invocation, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            CapabilityLog.HandlerFailed(this.logger, key.ToString(), error.GetType().FullName ?? error.GetType().Name);
            result = null;
        }

        result ??= CapabilityResult.Failed(HandlerErrorCode, HandlerErrorMessage);
        CapabilityTelemetry.SetStatus(invoke, result.Status);
        return result;
    }
}
