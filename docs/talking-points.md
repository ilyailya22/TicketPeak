# Talking points

One entry per non-obvious decision: the problem, the options, the choice, the trade-off, and what
would change at ten times the scale. Written as the work happens, per CLAUDE.md §9.

---

## Warnings as errors caught a CVE on the first build (Phase 0)

- **Problem.** The `webapi` template pinned `Microsoft.AspNetCore.OpenApi` 10.0.4, which resolves
  `Microsoft.OpenApi` 2.0.0, subject to a high-severity advisory.
- **Options.** Pin the transitive package; bump the parent; suppress `NU1903`.
- **Choice.** Bump the parent to 10.0.11, keeping the dependency graph coherent.
- **Trade-off.** Any newly published advisory can turn a green build red with no code change.
  That is correct, and it will be inconvenient at least once.
- **At 10×.** A dependency-update bot plus a documented response time for advisories.

## Liveness and readiness are different questions (Phase 0)

- **Problem.** A sick database must not make the orchestrator restart a healthy process.
- **Choice.** `/health/live` counts only `live`-tagged checks; `/health/ready` requires all of them.
  The AppHost gates on the same readiness probe the service exposes.
- **At 10×.** Probes with timeouts tuned per dependency, and readiness that degrades
  instead of flapping.

## Breaking every architecture rule once found a real bug (Phase 1)

- **Problem.** An architecture test that has never failed is not known to work.
- **Choice.** Plant one violation per rule and require exactly those tests to fail.
- **What it found.** With `ArchLoader.LoadAssemblies`, a Domain type exposing MediatR and a kernel
  type exposing Autofac both *passed*. Dependencies on unloaded assemblies were not flagged. Fixed
  with `LoadAssembliesIncludingDependencies`.
- **Trade-off.** Loading dependencies makes the architecture tests slower.
- **At 10×.** The same approach; the rules are cheap compared with an unenforced boundary.

## The monolith has one consistency boundary (Phase 1, ADR 0004)

- **Problem.** The plan wanted one transaction for checkout; the working agreement wanted events
  for every cross-module change. Both cannot be true.
- **Choice.** Synchronous `I<Module>Api` calls inside the checkout boundary, events outside it.
- **Trade-off.** Catalog ← Inventory ← Ordering become compile-time dependencies, so those three
  cannot be extracted independently without first replacing the calls.
- **At 10×.** Inventory is the likely extraction, behind a reservation API with its own store.

## Expiry is a function of the clock, not a state (Phase 1)

- **Problem.** Seat holds lapse after ten minutes. A background job to release them can run late,
  fail, or release twice.
- **Choice.** `Hold.IsActiveAt(now)` compares the clock. Nothing writes "expired"; a lapsed hold
  frees its seats the instant it lapses.
- **Trade-off.** Expired holds stay in memory until something prunes them.
- **At 10×.** Redis keys with TTLs (Phase 4) give the same semantics, with eviction for free.

## MediatR 12.5.0 is pinned for licensing (Phase 1, ADR 0005)

- **Problem.** MediatR 13+ is RPL-1.5 or commercial.
- **Choice.** Pin the last Apache-2.0 release; depend only on four interfaces so a hand-rolled
  dispatcher is a mechanical swap.
- **Trade-off.** A frozen dependency receives no fixes; NuGet audit failing the build is the net.

## `next()` silently dropped cancellation (Phase 1)

- **Problem.** In MediatR 12.5.0, `RequestHandlerDelegate` takes an optional `CancellationToken`.
  `next()` compiles and drops it at every behaviour.
- **How it was caught.** CA2016, with warnings as errors.
- **Choice.** `next(cancellationToken)` everywhere, plus a test asserting the token arrives, so the
  fix survives someone suppressing the analyzer.

## Results become HTTP in exactly one place (Phase 1)

- **Problem.** Three modules each mapping `ErrorType` to status codes would drift apart.
- **Choice.** Endpoints return the `Result`; one endpoint filter on the host's `/api` group maps it
  to 200, 204 or an RFC 9457 problem carrying the stable error code.
- **Trade-off.** Reading `Result<T>.Value` in the filter uses reflection per response.
- **At 10×.** Cache the property lookup per type, or generate the mapping at compile time.

## What Phase 1 does not prove (Phase 1)

The in-memory stores are not thread-safe, so the no-double-sale invariant is proven only by
single-threaded domain tests. Proving it under fifty parallel buyers against SQL is Phase 2. Saying
so plainly is better than a green test suite that implies more than it checks.
