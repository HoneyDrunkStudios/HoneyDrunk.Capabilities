# HoneyDrunk.Capabilities

Runtime for the HoneyDrunk Grid capability registry: an in-process registry keyed by `(name, version)` and an invoker that resolves, runs the guard, then dispatches to the handler. Hosts compose this package; tool-exposing nodes reference `HoneyDrunk.Capabilities.Abstractions` only (ADR-0017 D9).
