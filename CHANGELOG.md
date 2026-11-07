# Changelog

## [Unreleased]

### Added

- `HoneyDrunk.Capabilities.Abstractions` with `CapabilityDescriptor`, `CapabilityKey` and `CapabilityPermissions` (ADR-0017 D3, D6).
- Invocation shapes (`CapabilityCaller`, `CapabilityInvocation`, `CapabilityResult`, `CapabilityResultStatus`, `CapabilityGuardDecision`) and the contracts `ICapabilityRegistry`, `ICapabilityInvoker`, `ICapabilityGuard` and `ICapabilityHandler`.
- A contract-shape canary over `ICapabilityRegistry`, `CapabilityDescriptor`, `ICapabilityInvoker` and `ICapabilityGuard` and every public type their signatures reach (ADR-0017 D8, Grid invariant 130). The committed baseline is `api/HoneyDrunk.Capabilities.Abstractions.contract-shape.txt`.
- `HoneyDrunk.Capabilities` with `InProcessCapabilityRegistry` (thread-safe, exact `(name, version)` lookup) and `CapabilityInvoker` (resolve, guard once, dispatch; handler failures become a fixed `handler_error` result; telemetry through Kernel with an allowlist of tags).
