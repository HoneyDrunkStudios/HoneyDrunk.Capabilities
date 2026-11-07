using HoneyDrunk.Auth.Abstractions;

namespace HoneyDrunk.Capabilities.Tests;

/// <summary>Records the identity and request it receives, then returns a scripted decision or throws.</summary>
internal sealed class FakeAuthorizationPolicy : IAuthorizationPolicy
{
    public AuthorizationDecision Decision { get; set; } = AuthorizationDecision.Allow();

    public Exception? Error { get; set; }

    public AuthenticatedIdentity? Identity { get; private set; }

    public AuthorizationRequest? Request { get; private set; }

    public int Calls { get; private set; }

    public string PolicyName => "Fake";

    public Task<AuthorizationDecision> EvaluateAsync(AuthenticatedIdentity? identity, AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        this.Calls++;
        this.Identity = identity;
        this.Request = request;
        cancellationToken.ThrowIfCancellationRequested();
        return this.Error is null ? Task.FromResult(this.Decision) : Task.FromException<AuthorizationDecision>(this.Error);
    }
}
