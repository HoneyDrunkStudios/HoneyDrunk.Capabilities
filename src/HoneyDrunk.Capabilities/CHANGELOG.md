# Changelog

## [0.1.0] - 2026-11-07

### Added

- `InProcessCapabilityRegistry` and `CapabilityInvoker`: resolve by exact key, guard once, then dispatch.
- `AuthPolicyCapabilityGuard`: authorization through HoneyDrunk.Auth policy, failing closed.
- `AddHoneyDrunkCapabilities` and `AddCapability<THandler>` service registration.
