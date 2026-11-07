# Changelog

## [0.1.0] - 2026-11-07

### Added

- `HoneyDrunk.Capabilities.Abstractions` with `CapabilityDescriptor`, `CapabilityKey` and `CapabilityPermissions` (ADR-0017 D3, D6).
- Invocation shapes (`CapabilityCaller`, `CapabilityInvocation`, `CapabilityResult`, `CapabilityResultStatus`, `CapabilityGuardDecision`) and the contracts `ICapabilityRegistry`, `ICapabilityInvoker`, `ICapabilityGuard` and `ICapabilityHandler`.
- A contract-shape canary over `ICapabilityRegistry`, `CapabilityDescriptor`, `ICapabilityInvoker` and `ICapabilityGuard` and every public type their signatures reach (ADR-0017 D8, Grid invariant 130). The committed baseline is `api/HoneyDrunk.Capabilities.Abstractions.contract-shape.txt`.
- `HoneyDrunk.Capabilities` with `InProcessCapabilityRegistry` (thread-safe, exact `(name, version)` lookup) and `CapabilityInvoker` (resolve, guard once, dispatch; handler failures become a fixed `handler_error` result; telemetry through Kernel with an allowlist of tags).
- `AuthPolicyCapabilityGuard` and `CapabilityGuardOptions`: the default guard maps the caller to an Auth `AuthenticatedIdentity` and the descriptor to an `AuthorizationRequest` (`capability.invoke` on `capability/{name}/{version}`), lets `IAuthorizationPolicy` decide, and fails closed with `guard_error` (ADR-0017 D5, D10; Grid invariant 129).
- `AddHoneyDrunkCapabilities` and `AddCapability<THandler>`: the registry is a singleton that registers every declared capability on first resolve; the invoker and guard are scoped; the host composes `IAuthorizationPolicy`.
- `HoneyDrunk.Capabilities.Testing`: `CapabilityTestHarness` over the real registry and invoker, `ScriptedCapabilityGuard` (`AllowAll`, `DenyAll`, `RequireScopes`), `RecordingCapabilityHandler`, `NullTelemetryActivityFactory` and `CapabilityFixtures`.
- An Auth canary test runs the invoker and guard against the real HoneyDrunk.Auth `DefaultAuthorizationPolicy`.
- The publish workflow releases `HoneyDrunk.Capabilities.Abstractions`, `HoneyDrunk.Capabilities` and `HoneyDrunk.Capabilities.Testing`.
