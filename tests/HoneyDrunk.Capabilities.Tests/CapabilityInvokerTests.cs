using HoneyDrunk.Capabilities.Abstractions;
using Xunit;
using static HoneyDrunk.Capabilities.Tests.TestHandlers;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Behavior of <see cref="CapabilityInvoker"/>: resolve, guard once, then dispatch.</summary>
public sealed class CapabilityInvokerTests
{
    private static readonly string[] AllowedTags = ["capability.name", "capability.version", "capability.status", "capability.deny_code"];

    private readonly InProcessCapabilityRegistry registry = new(new ListLogger<InProcessCapabilityRegistry>());
    private readonly RecordingTelemetryActivityFactory telemetry = new();
    private readonly ListLogger<CapabilityInvoker> logger = new();

    /// <summary>An unknown key is not found, and the guard is never asked.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Unknown_key_returns_not_found_without_guard()
    {
        var guard = CountingGuard.Allowing();
        var handler = CountingHandler.Returning(CapabilityResult.Success(null));
        this.registry.Register(Descriptor(version: "1.0.0"), handler);

        var result = await this.Invoker(guard).InvokeAsync(Invocation(version: "2.0.0"));

        Assert.Equal(CapabilityResultStatus.NotFound, result.Status);
        Assert.Equal("not_found", result.ErrorCode);
        Assert.Equal(0, guard.Calls);
        Assert.Equal(0, handler.Calls);
        Assert.Contains(this.telemetry.Tags, tag => tag.Key == "capability.status" && Equals(tag.Value, "not_found"));
    }

    /// <summary>A denied caller gets the guard's codes, and the handler never runs.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Denied_never_reaches_the_handler()
    {
        var guard = CountingGuard.Denying("MissingScope");
        var handler = CountingHandler.Returning(CapabilityResult.Success(null));
        this.registry.Register(Descriptor(), handler);

        var result = await this.Invoker(guard).InvokeAsync(Invocation());

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Equal("denied", result.ErrorCode);
        Assert.Equal(["MissingScope"], result.DenyCodes);
        Assert.Equal(0, handler.Calls);
        Assert.Contains(this.telemetry.Tags, tag => tag.Key == "capability.deny_code" && Equals(tag.Value, "MissingScope"));
    }

    /// <summary>An allowed call returns the handler's result as is.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Allowed_returns_the_handler_result()
    {
        var expected = CapabilityResult.Success(Json("""{"echo":"hello"}"""));
        this.registry.Register(Descriptor(), CountingHandler.Returning(expected));

        var result = await this.Invoker(CountingGuard.Allowing()).InvokeAsync(Invocation());

        Assert.Same(expected, result);
        Assert.Contains(this.telemetry.Tags, tag => tag.Key == "capability.status" && Equals(tag.Value, "succeeded"));
    }

    /// <summary>A handler's own non-success result is returned as is, too.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Handler_invalid_arguments_result_is_returned()
    {
        var expected = CapabilityResult.InvalidArguments("The text argument is required.");
        this.registry.Register(Descriptor(), CountingHandler.Returning(expected));

        Assert.Same(expected, await this.Invoker(CountingGuard.Allowing()).InvokeAsync(Invocation()));
    }

    /// <summary>The guard runs exactly once per invocation, with the resolved descriptor and the caller.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Guard_runs_exactly_once()
    {
        var descriptor = Descriptor();
        CapabilityDescriptor? seenDescriptor = null;
        CapabilityCaller? seenCaller = null;
        var guard = new CountingGuard((d, c, _) =>
        {
            seenDescriptor = d;
            seenCaller = c;
            return Task.FromResult(CapabilityGuardDecision.Allow());
        });
        this.registry.Register(descriptor, CountingHandler.Returning(CapabilityResult.Success(null)));
        var invocation = Invocation();

        await this.Invoker(guard).InvokeAsync(invocation);

        Assert.Equal(1, guard.Calls);
        Assert.Same(descriptor, seenDescriptor);
        Assert.Same(invocation.Caller, seenCaller);
    }

    /// <summary>A handler exception becomes a fixed failure; its message reaches no result, log or tag.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Handler_exception_is_reported_without_its_message()
    {
        this.registry.Register(Descriptor(), CountingHandler.Throwing(new InvalidOperationException("token sk-secret-value rejected")));

        var result = await this.Invoker(CountingGuard.Allowing()).InvokeAsync(Invocation());

        Assert.Equal(CapabilityResultStatus.Failed, result.Status);
        Assert.Equal("handler_error", result.ErrorCode);
        Assert.Equal("The capability handler failed.", result.ErrorMessage);
        Assert.DoesNotContain("sk-secret-value", result.ToString(), StringComparison.Ordinal);
        var entry = Assert.Single(this.logger.Entries);
        Assert.Contains("InvalidOperationException", entry.message, StringComparison.Ordinal);
        Assert.Contains("test-tool@1.0.0", entry.message, StringComparison.Ordinal);
        Assert.Null(entry.exception);
        this.AssertNowhere("sk-secret-value");
    }

    /// <summary>A guard exception is a guard_error denial; the handler never runs and the message reaches no result, log or tag.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Guard_exception_is_denied_without_its_message()
    {
        var handler = CountingHandler.Returning(CapabilityResult.Success(null));
        this.registry.Register(Descriptor(), handler);
        var guard = new CountingGuard((_, _, _) => Task.FromException<CapabilityGuardDecision>(new InvalidOperationException("approval store sk-secret-value down")));

        var result = await this.Invoker(guard).InvokeAsync(Invocation());

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Equal(["guard_error"], result.DenyCodes);
        Assert.Equal(0, handler.Calls);
        Assert.DoesNotContain("sk-secret-value", result.ToString(), StringComparison.Ordinal);
        var entry = Assert.Single(this.logger.Entries);
        Assert.Contains("System.InvalidOperationException", entry.message, StringComparison.Ordinal);
        Assert.Contains("test-tool@1.0.0", entry.message, StringComparison.Ordinal);
        Assert.Null(entry.exception);
        Assert.Contains(this.telemetry.Tags, tag => tag.Key == "capability.status" && Equals(tag.Value, "denied"));
        Assert.Contains(this.telemetry.Tags, tag => tag.Key == "capability.deny_code" && Equals(tag.Value, "guard_error"));
        this.AssertNowhere("sk-secret-value");
    }

    /// <summary>A guard denial with no codes is reported with guard_error.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Denial_without_codes_carries_guard_error()
    {
        this.registry.Register(Descriptor(), CountingHandler.Returning(CapabilityResult.Success(null)));

        var result = await this.Invoker(CountingGuard.Denying()).InvokeAsync(Invocation());

        Assert.Equal(CapabilityResultStatus.Denied, result.Status);
        Assert.Equal(["guard_error"], result.DenyCodes);
    }

    /// <summary>Arguments reach neither telemetry nor logs, and only allowlisted tags are set.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Arguments_never_reach_telemetry_or_logs()
    {
        this.registry.Register(Descriptor(), CountingHandler.Throwing(new InvalidOperationException("failed")));
        this.registry.Register(Descriptor(version: "2.0.0"), CountingHandler.Returning(CapabilityResult.Success(null)));
        var invoker = this.Invoker(CountingGuard.Allowing());
        const string arguments = """{"text":"SENTINEL-ARG-7731"}""";

        await invoker.InvokeAsync(Invocation(arguments));
        await invoker.InvokeAsync(Invocation(arguments, version: "2.0.0"));
        await invoker.InvokeAsync(Invocation(arguments, version: "3.0.0"));
        await this.Invoker(CountingGuard.Denying("MissingScope")).InvokeAsync(Invocation(arguments));

        Assert.NotEmpty(this.telemetry.Tags);
        Assert.All(this.telemetry.Tags, tag => Assert.Contains(tag.Key, AllowedTags));
        Assert.Equal(["capabilities.invoke", "capabilities.resolve"], this.telemetry.Activities.Select(activity => activity.OperationName).Distinct().Order(StringComparer.Ordinal));
        this.AssertNowhere("SENTINEL-ARG-7731");
    }

    /// <summary>Cancellation by the caller propagates from the handler and from the guard.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Cancellation_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var handler = new CountingHandler((_, token) => Task.FromCanceled<CapabilityResult>(token));
        this.registry.Register(Descriptor(), handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Invoker(CountingGuard.Allowing()).InvokeAsync(Invocation(), cancellation.Token));

        var cancellingGuard = new CountingGuard((_, _, token) => Task.FromCanceled<CapabilityGuardDecision>(token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Invoker(cancellingGuard).InvokeAsync(Invocation(), cancellation.Token));
        Assert.Equal(1, handler.Calls);
    }

    /// <summary>A handler's own cancellation (for example a timeout), when the caller did not cancel, is a handler failure.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task Handler_cancellation_without_the_callers_token_is_a_failure()
    {
        this.registry.Register(Descriptor(), CountingHandler.Throwing(new TaskCanceledException("timed out")));

        var result = await this.Invoker(CountingGuard.Allowing()).InvokeAsync(Invocation());

        Assert.Equal(CapabilityResultStatus.Failed, result.Status);
        Assert.Equal("handler_error", result.ErrorCode);
    }

    private CapabilityInvoker Invoker(ICapabilityGuard guard) => new(this.registry, guard, this.telemetry, this.logger);

    private void AssertNowhere(string sentinel)
    {
        Assert.All(this.logger.Entries, entry => Assert.DoesNotContain(sentinel, entry.message, StringComparison.Ordinal));
        Assert.All(this.telemetry.Tags, tag => Assert.DoesNotContain(sentinel, tag.Value?.ToString() ?? string.Empty, StringComparison.Ordinal));
        Assert.All(this.telemetry.Activities, activity => Assert.DoesNotContain(sentinel, activity.DisplayName, StringComparison.Ordinal));
    }
}
