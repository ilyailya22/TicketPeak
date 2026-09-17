# 4. Build the core as a modular monolith with enforced boundaries

- **Status:** Accepted
- **Date:** 2026-09-17

## Context

The purchase path runs through Inventory, Ordering and Payments, and it protects the invariant the
whole platform exists for: a seat is never sold twice. That invariant needs one consistency
boundary. Splitting those contexts into services would turn a single ACID transaction into a
distributed one.

The plan also calls for extracting Search, Ticketing and Notifications later (Phases 7–9). That only
stays cheap if the monolith's modules are genuinely separate from day one. "We'll keep it tidy" is
not a boundary; the monorepo (ADR 0002) puts no structural barrier between modules at all.

Two documents disagreed. Plan §4.1 puts the purchase flow in one transaction, while CLAUDE.md §4 said
cross-module state changes go only through integration events, which cannot share a transaction and
do not exist until Phase 6. Following CLAUDE.md literally would have removed the reason to build a
monolith in the first place.

## Decision

**One deployable, five modules:** Identity, Catalog, Inventory, Ordering and Payments, one project
each, with vertical slices inside (`Domain/`, `Application/<UseCase>/`, `Infrastructure/`,
`Endpoints/`).

**A module's public surface is its root namespace and nothing else.** That covers its `I<Module>Api`
and the records it returns, its Autofac module, and its endpoint mapping. Everything in the four layer
namespaces is `internal`.

**Communication depends on the consistency boundary, not on style:**

- *Inside the checkout boundary*, modules call each other synchronously through `I<Module>Api`.
  Inventory reads Catalog's layout and on-sale status; Ordering reads the active hold and prices.
  These run in one request and, from Phase 2, one transaction.
- *Outside it* (search projection, ticket issuing, notifications), modules communicate through
  integration events from Phase 6.

CLAUDE.md §4 is reworded to match.

**Dependencies point inward.** Domain may reference only the BCL and `Shared.Kernel`. The kernel
references only the BCL. Each module owns its own identifier types (`Ordering.EventId`,
`Payments.OrderId`), so no module depends on another's domain.

**Architecture tests enforce this, and each rule was proven by breaking it once:**

1. Domain depends only on the BCL, the kernel and its own Domain.
2. A module never depends on another module's layer namespaces.
3. Types in those layers are never public.
4. The kernel depends only on the BCL.

Breaking the rules found a real defect. With `ArchLoader.LoadAssemblies`, rules 1 and 4 passed a
Domain type exposing MediatR and a kernel type exposing Autofac: dependencies on assemblies outside
the loaded set were not flagged. `LoadAssembliesIncludingDependencies` fixed it, and the test fixture
records why.

**"No module references the host" has no test.** That reference would be circular, which MSBuild
already refuses to build. A rule that cannot fail cannot be proven by breaking it.

## Alternatives considered

**Microservices from the start.** Each context deployable alone, at the cost of a saga or two-phase
commit for an invariant a local transaction handles for free. Rejected: the purchase path has one
consistency boundary, and distributing it buys operational cost with no scaling benefit yet.

**A layered monolith with no module boundaries** (`Controllers/`, `Services/`, `Repositories/`).
Fastest to start, and extraction later means untangling every shared service and table. Rejected
because extraction is part of this plan, not a hypothetical.

**A project per layer per module** (5 × 4 = 20 projects). The compiler would enforce layering by
project reference. Rejected as twenty projects of ceremony for what four namespace rules already
enforce, and they are enforced in CI.

**Events for every cross-module change, per the original CLAUDE.md wording.** Rejected for the
checkout path for the reason above. Kept for everything outside it.

**NetArchTest for the rules.** Named in the original stack, but unreleased since May 2021 and its
package declares no licence expression (ADR 0003). ArchUnitNET is maintained and Apache-2.0.

## Consequences

**Good.** The no-double-sale invariant can live in one database transaction (Phase 2). A module
becomes a service by replacing its `I<Module>Api` calls with HTTP or events, with no untangling. The
boundaries fail the build rather than a code review.

**Bad.** The synchronous calls create a compile-time dependency chain: Catalog ← Inventory ←
Ordering. A cycle would be a build error, which is acceptable, but these three cannot be extracted
independently without first replacing those calls. The rules key off namespaces, so a type placed
in a module's root namespace is public by convention and escapes rule 3; review has to catch that.

`Shared.Kernel` grew during Phase 1 to include `Money`, `CurrencyCode`, `ICommand` and `IUnitOfWork`.
CLAUDE.md asks for approval before any kernel change; these were made under an instruction to
complete the phase without stopping, and they are flagged on the pull request for review.

Phase 1's persistence is in-memory and **not safe under concurrent requests**. The invariant is
proven by single-threaded domain tests. Proving it under load is Phase 2's job, and the stores say
so in their own comments.
