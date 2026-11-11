# HoneyDrunk.Capabilities

The HoneyDrunk Grid's capability registry: how an agent-callable tool is described, registered, found, authorized and run. Tool implementations live in the node that owns them; this repository owns the registry, the descriptor, dispatch and the authorization gate (ADR-0017; see [Decisions](#decisions)).

## SDK requirement

Build and test with the .NET 11 SDK selected by `global.json`. Runtime packages target `net11.0`; consuming applications must also target .NET 11 or later. The contracts-only Abstractions package targets both `net10.0` and `net11.0` to preserve existing contract consumers.

## Packages

| Package | Use it from | Holds |
|---|---|---|
| `HoneyDrunk.Capabilities.Abstractions` | Any node that exposes or calls a capability | `CapabilityDescriptor`, `CapabilityKey`, `CapabilityPermissions`, the invocation and result records, and the contracts `ICapabilityRegistry`, `ICapabilityInvoker`, `ICapabilityGuard` and `ICapabilityHandler`. It references no other HoneyDrunk package. |
| `HoneyDrunk.Capabilities` | The host's composition root only | `InProcessCapabilityRegistry`, `CapabilityInvoker`, `AuthPolicyCapabilityGuard` and `AddHoneyDrunkCapabilities` / `AddCapability<THandler>`. |
| `HoneyDrunk.Capabilities.Testing` | Test projects only | `CapabilityTestHarness` over the real registry and invoker, `ScriptedCapabilityGuard`, `RecordingCapabilityHandler`, `NullTelemetryActivityFactory` and `CapabilityFixtures`. |

Nodes compile against `Abstractions` alone; the host picks the runtime and composes Auth (Grid invariant 127).

## Descriptors and versions

A `CapabilityDescriptor` is checked when it is constructed, so an invalid one never reaches the registry:

| Field | Rule |
|---|---|
| `Name` | `^[a-z][a-z0-9_-]{0,63}$` |
| `Version` | `MAJOR.MINOR.PATCH`, digits only, no leading zeros, no pre-release |
| `Description` | 1 to 1024 characters |
| `OwningNode` | 1 to 64 characters |
| `ParameterSchema` | JSON text whose root is an object with `"type": "object"`, at most 32,768 characters |
| `ReturnSchema` | Optional; the same rules when present |
| `Permissions` | At least one required scope or role; each value 1 to 128 characters with no whitespace |

The registry key is `(Name, Version)`, written `name@version`. Lookup is exact: there is no "latest" and no fallback to another version (Grid invariant 128). An incompatible schema change ships under a new version, and callers move when they choose.

## Invocation

`ICapabilityInvoker` is the only way to run a capability; the registry never hands out a handler. For each call it:

1. Resolves the exact key. A miss returns `NotFound` without asking the guard.
2. Asks the guard exactly once. A denial returns `Denied` with the deny codes, and the handler does not run. A guard that throws, or denies without a code, gives `Denied` with `guard_error`; only the exception type name and the key are logged.
3. Runs the handler and returns its result. A handler exception returns `Failed` with `handler_error` and a fixed message; only the exception type name and the key are logged. The caller's cancellation propagates as `OperationCanceledException`.

Arguments, output and exception messages never reach logs, traces or result messages. Activities are `capabilities.resolve` and `capabilities.invoke`, through Kernel's `ITelemetryActivityFactory`, with only the tags `capability.name`, `capability.version`, `capability.status` and `capability.deny_code`.

Result error codes are `not_found`, `denied`, `invalid_arguments` and `handler_error`. Handlers validate their own arguments and return `CapabilityResult.InvalidArguments` when they are wrong.

## Authorization through Auth

`AuthPolicyCapabilityGuard` keeps no permission model; HoneyDrunk.Auth's `IAuthorizationPolicy` decides (Grid invariant 129). It maps:

| Capabilities | Auth |
|---|---|
| `CapabilityCaller.SubjectId` | `AuthenticatedIdentity.SubjectId` |
| `CapabilityGuardOptions.IdentityScheme` (default `honeydrunk-capability-caller`) | `AuthenticatedIdentity.Scheme` |
| `CapabilityCaller.Scopes` and `Roles` | claims `AuthClaimTypes.Scope` and `AuthClaimTypes.Role` |
| invoking a capability | action `capability.invoke` |
| the descriptor's key | resource `capability/{name}/{version}` |
| `CapabilityPermissions.RequiredScopes` and `RequiredRoles` | `AuthorizationRequest.RequiredScopes` and `RequiredRoles` |

With Auth's default policy, every required scope must be held and any one required role is enough. Deny codes are Auth's `AuthorizationDenyCode` names (for example `MissingScope`); a policy that throws, or a denial with no reasons, denies with `guard_error`, and a policy failure is logged with its exception type name and the key only. The caller is asserted by the host composition, not by a validated token, so the trust boundary is the host process.

## Registering a capability

The owning node implements `ICapabilityHandler` against `Abstractions`:

```csharp
public sealed class EchoHandler : ICapabilityHandler
{
    public Task<CapabilityResult> HandleAsync(CapabilityInvocation invocation, CancellationToken cancellationToken) =>
        Task.FromResult(CapabilityResult.Success(invocation.Arguments));
}
```

The host composes the runtime, an `IAuthorizationPolicy`, Kernel telemetry and logging, then declares each capability:

```csharp
services.AddHoneyDrunkCapabilities();
services.AddCapability<EchoHandler>(new CapabilityDescriptor(
    "echo",
    "1.0.0",
    "Returns its arguments.",
    "HoneyDrunk.Example",
    """{"type":"object","properties":{"text":{"type":"string"}}}""",
    null,
    new CapabilityPermissions(["example.echo"], [])));
```

The registry is a singleton that registers every declared capability when it is first resolved, so a duplicate key throws `InvalidOperationException` on that first resolve, not when the container is built. To fail during startup, resolve the registry once while the host starts:

```csharp
_ = provider.GetRequiredService<ICapabilityRegistry>();
```

Handlers are singletons. The invoker and the guard are scoped, because Kernel's telemetry factory is scoped:

```csharp
using var scope = provider.CreateScope();
var invoker = scope.ServiceProvider.GetRequiredService<ICapabilityInvoker>();
var result = await invoker.InvokeAsync(new CapabilityInvocation(
    new CapabilityKey("echo", "1.0.0"),
    arguments,
    new CapabilityCaller("service:example", ["example.echo"], [])));
```

## Contract-shape canary

`ICapabilityRegistry`, `CapabilityDescriptor`, `ICapabilityInvoker` and `ICapabilityGuard`, and every public type their signatures reach, are frozen in `api/HoneyDrunk.Capabilities.Abstractions.contract-shape.txt` (Grid invariant 130). A shape change without a version bump fails the build. To accept an intended change, raise `Version` in `Directory.Build.props`, then regenerate the baseline:

```bash
HONEYDRUNK_UPDATE_CONTRACT_SHAPE=1 dotnet test tests/HoneyDrunk.Capabilities.Abstractions.Tests --filter Category=ContractShape
```

## Build and test

```bash
dotnet build HoneyDrunk.Capabilities.slnx -c Release
dotnet test HoneyDrunk.Capabilities.slnx -c Release
```

## Decisions

The node's stand-up decision is ADR-0017 (accepted 2026-11-07); its Grid invariants are 127 to 130.
