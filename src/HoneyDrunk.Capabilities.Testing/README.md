# HoneyDrunk.Capabilities.Testing

Test fixtures for the HoneyDrunk Grid capability registry. `CapabilityTestHarness` composes the real `InProcessCapabilityRegistry` and `CapabilityInvoker` with a `ScriptedCapabilityGuard`, so tests get the runtime's exact resolution, guard and failure semantics. Reference it from test projects only, never from production composition (ADR-0017 D9).
