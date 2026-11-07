# HoneyDrunk.Capabilities agent instructions

This repository holds the Grid capability registry (ADR-0017): `HoneyDrunk.Capabilities.Abstractions` (descriptors and the four frozen contracts), `HoneyDrunk.Capabilities` (in-process registry, guarded invoker, Auth-backed guard, service registration) and `HoneyDrunk.Capabilities.Testing` (test harness and fixtures). Inspect [README.md](README.md), the tracked files and open implementation PRs before making claims; an open PR is not merged behavior. Tool implementations belong to the node that owns them, never to this repository. Do not invent node ownership or new responsibilities from the repository name.

## Build and test

The solution is `HoneyDrunk.Capabilities.slnx`.

```bash
dotnet build HoneyDrunk.Capabilities.slnx -c Release
dotnet test HoneyDrunk.Capabilities.slnx -c Release
```

The contract-shape canary (`Category=ContractShape`) freezes `ICapabilityRegistry`, `CapabilityDescriptor`, `ICapabilityInvoker`, `ICapabilityGuard` and every public type their signatures reach in `api/HoneyDrunk.Capabilities.Abstractions.contract-shape.txt` (Grid invariant 130). To accept an intended shape change, raise `Version` in `Directory.Build.props` first, then regenerate and commit the baseline:

```bash
HONEYDRUNK_UPDATE_CONTRACT_SHAPE=1 dotnet test tests/HoneyDrunk.Capabilities.Abstractions.Tests --filter Category=ContractShape
```

`tests/HoneyDrunk.Capabilities.Canary` runs the invoker and guard against the real HoneyDrunk.Auth `DefaultAuthorizationPolicy`.

## Boundaries

- `Abstractions` references no HoneyDrunk package; the runtime references only `*.Abstractions` packages. Consumers compile against `Abstractions`; `Testing` is for test projects only (Grid invariant 127).
- Every invocation passes the guard; the registry never exposes a handler. Authorization is decided by Auth's `IAuthorizationPolicy`; do not add a permission model here (Grid invariant 129).
- Descriptor versions are required and lookup is exact on `(name, version)` (Grid invariant 128).
- Arguments, output and exception messages never go into logs, traces or result messages.

## Working rules

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Code Review Rules

Apply the [shared review criteria](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md#code-review) to changed behavior, using the repository boundaries above. Report actionable findings with the failing path, concrete impact and a small corrective action; disclose unavailable evidence. These rules grant no cross-repository access or merge authority.

- Review against the selected responsibility and actual tracked files. A repository name, catalog entry or open PR is not an implemented contract; do not invent ownership, public APIs or a runtime framework beyond what is tracked.
- If implementation is introduced, check real existing shared capabilities and dependency direction before accepting duplicate mechanics. Trace trust/tenant boundaries, failure/retry/cancellation and bounded work for the behavior actually added; keep product policy in its selected owner.
- Require verification that exists and fits the change: observable behavior and contract tests, the contract-shape canary for Abstractions changes, and relevant security/concurrency tests for new code. Do not fabricate build commands, benchmark results, analysis coverage or deployment readiness.
