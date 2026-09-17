# CLAUDE.md — TicketPeak

Instructions for Claude Code working in this repository.
The full roadmap lives in [PORTFOLIO_PLAN.md](./PORTFOLIO_PLAN.md) — read the relevant phase before starting work.

---

## 1. What this is

TicketPeak is an event ticketing platform built as a **portfolio project** demonstrating Strong Middle .NET Developer skills.

It is a **modular monolith** (core transactional domain) plus **three deliberately extracted services** (Search, Ticketing, Notifications) behind a YARP gateway, with Blazor web and MAUI mobile clients sharing one Razor Class Library.

**This being a portfolio changes the priorities.** Clarity, justification and explainability outrank cleverness and speed. Code that works but cannot be explained in an interview is a failure here.

### Current phase

> **Phase 2 — Persistence: MSSQL + EF Core** (update this line when a phase is merged)
>
> Phase 1 is complete: see [PR #2](https://github.com/ilyailya22/TicketPeak/pull/2) and ADRs 0004–0006.
> Phase 0: [PR #1](https://github.com/ilyailya22/TicketPeak/pull/1) and ADRs 0001–0003.

Work only on the current phase. Do not scaffold things belonging to later phases "while we're here."

---

## 2. Non-negotiable rules

1. **Ask before adding any NuGet package.** State what it does, what the alternative is, and why it wins here. Wait for approval.
2. **Ask before crossing a module boundary** or changing anything in `Shared.Kernel` or `Contracts`.
3. **Every architectural decision gets an ADR** in `docs/adr/` before or with the code that implements it. Format: Context / Decision / Alternatives considered / Consequences. One page.
4. **No technology without a two-sentence justification.** If it cannot be justified, remove it. Unjustified tech is a negative signal in this repo.
5. **Never write a secret into a tracked file.** Use `dotnet user-secrets` locally, Key Vault in Azure.
6. **Never invent domain rules.** If the spec is ambiguous (pricing, refunds, seat rules), ask.
7. **Stay in scope.** Do not refactor unrelated code, reformat untouched files, or "improve" other modules while implementing a task.
8. **Do not create files that were not asked for** — no extra README/summary/notes files unless the phase deliverables list them.

---

## 3. Commands

```bash
# run everything (dashboard + API today; SQL, Redis, Mongo, RabbitMQ join in later phases)
aspire run
dotnet run --project src/Aspire/TicketPeak.AppHost   # same, without the Aspire CLI

# build / test — --solution or --project is required under Microsoft.Testing.Platform
dotnet build TicketPeak.slnx
dotnet test --solution TicketPeak.slnx                     # all
dotnet test --project tests/TicketPeak.UnitTests           # fast loop, no containers
dotnet test --project tests/TicketPeak.ArchitectureTests   # boundary rules

# formatting — CI fails if this is not clean
dotnet format TicketPeak.slnx --verify-no-changes

# migrations (from Phase 2: per module, each has its own DbContext + history table)
dotnet ef migrations add <Name> \
  --project src/Monolith/TicketPeak.Modules.<Module> \
  --startup-project src/Monolith/TicketPeak.Api \
  --context <Module>DbContext
```

Integration tests use **Testcontainers** from Phase 2 — Docker must be running. They are slower; run unit tests during the inner loop.

---

## 4. Repository map

```
src/Aspire/          AppHost (orchestration) + ServiceDefaults (telemetry, health, resilience)
src/Monolith/        TicketPeak.Api (host) + one project per bounded context + Shared.Kernel
src/Services/        Search (MongoDB) · Ticketing (worker) · Notifications · Gateway (YARP)
src/Contracts/       Integration event records ONLY — no logic, no dependencies
src/Clients/         Client.Shared (RCL) · Web (Blazor) · Mobile (MAUI Blazor Hybrid)
tests/               Unit · Integration · Architecture · Contract · load/ (k6)
infra/bicep/         Azure infrastructure as code
docs/adr/            Architecture decision records
```

### Module internals — vertical slices, not layer folders

```
TicketPeak.Modules.Ordering/
├─ Domain/              aggregates, value objects, domain events, repository interfaces
├─ Application/         one folder per use case: Command + Handler + Validator + Response
├─ Infrastructure/      persistence and external clients (in-memory in Phase 1, EF Core from Phase 2)
├─ Endpoints/           request bodies whose other half comes from the route
├─ OrderingModule.cs    Autofac module — the host registers this and nothing else
├─ OrderingEndpoints.cs maps the module's routes; endpoints return Results, the host maps them to HTTP
└─ IOrderingApi.cs      what other modules may call, with the records it returns
```

**Boundary rules (enforced by architecture tests, not by discipline — see ADR 0004):**
- `Domain` depends only on the BCL, `Shared.Kernel` and its own module's Domain. `Shared.Kernel` depends only on the BCL.
- A module may depend on another module **only** through that module's root namespace (`I<Module>Api` and the records it returns), never its Domain, Application, Infrastructure or Endpoints.
- Types in those four layer namespaces are `internal`; only the root namespace is public.
- No module references `TicketPeak.Api`. That would be a circular project reference, so MSBuild enforces it.
- **Inside the checkout consistency boundary** (Catalog, Inventory, Ordering, Payments), modules call each other synchronously through `I<Module>Api`. **Everything outside it** goes through integration events from Phase 6.

---

## 5. Stack and pinned choices

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 (LTS), C# 14 | `nullable` and `TreatWarningsAsErrors` on everywhere |
| Packages | Central Package Management | NuGet versions live **only** in `Directory.Packages.props`; MSBuild SDK versions in `global.json` |
| API | Minimal APIs | module endpoints return `Result`; one host endpoint filter maps it to 200/204 or RFC 9457 problems |
| Docs | `Microsoft.AspNetCore.OpenApi` + Scalar | not Swashbuckle |
| DI | **Autofac** in the monolith, **MS.DI** in the services | deliberate contrast — see ADR 0006 |
| Mediator | **MediatR 12.5.0**, pinned, in the monolith only | last Apache-2.0 release — **never bump**; services use plain handlers — see ADR 0005 |
| ORM | EF Core 10; **Dapper** for hot read paths | from Phase 2; mark each Dapper query with a comment saying why |
| Mapping | **Mapperly** (source-generated) | not AutoMapper |
| Validation | FluentValidation, invoked by a MediatR behaviour | shape only; aggregates still guard their own rules |
| Messaging | MassTransit over RabbitMQ (Azure Service Bus in cloud, swapped by config) | from Phase 6 |
| Caching | HybridCache (L1 memory + L2 Redis) | explicit invalidation on write |
| Resilience | `Microsoft.Extensions.Resilience` / Polly v8 pipelines | on every outbound HTTP call |
| Logging | `[LoggerMessage]` source generation now; Serilog, structured, JSON to console from Phase 5 | CA1848 is enforced |
| Telemetry | OpenTelemetry traces + metrics + logs | wired in ServiceDefaults |
| Tests | xUnit v3 on Microsoft.Testing.Platform, Shouldly, FakeTimeProvider, ArchUnitNET; Testcontainers from Phase 2 | NetArchTest rejected as unmaintained — see ADR 0003 |

**Licensing:** several of these re-licensed in 2025 (MediatR, AutoMapper, MassTransit v9, FluentAssertions v8). Before adding one, check the licence of the exact version being pinned and record it in `docs/adr/0003-third-party-licensing.md`.

---

## 6. Code conventions

- **`TimeProvider`, never `DateTime.Now`/`UtcNow`.** Inject it; tests use `FakeTimeProvider`. Generate ids with `Guid.CreateVersion7(time.GetUtcNow())`.
- **Strongly-typed IDs**: `readonly record struct OrderId(Guid Value)` with EF Core value converters. Each module owns its own id types.
- **`Result<T>` for expected failures**; exceptions only for genuinely exceptional cases. Never use exceptions for control flow.
- **RFC 9457 `ProblemDetails`** at the API edge, including the trace id (from Phase 5).
- **Aggregates protect their invariants.** No public setters on entities; state changes go through methods that can refuse.
- **No repository interface per entity** — one per aggregate root.
- `async`/`await` all the way down; pass `CancellationToken` through every layer — including MediatR's `next(cancellationToken)`; never `.Result` or `.Wait()`.
- `sealed` by default; `record` for DTOs and events; `readonly record struct` for value objects.
- Files: one public type per file, named after the type.
- Comments explain **why**, never what. No commented-out code, no `// TODO` without an issue reference.
- No `#region`. No `var` when the type isn't obvious from the right-hand side.

---

## 7. Testing expectations

- **Unit tests** cover domain invariants with zero infrastructure. Every aggregate rule has a test that proves it *refuses* the invalid case.
- **Integration tests** use Testcontainers against real MSSQL/Redis/Mongo/RabbitMQ — never in-memory providers for persistence behaviour.
- **Architecture tests** must fail loudly if a boundary is violated. When adding a rule, prove it by breaking it once.
- **Concurrency matters here**: the seat-allocation path must have a test running parallel purchases and asserting exactly one succeeds.
- Test names: `MethodOrScenario_Condition_ExpectedResult`.
- Arrange/Act/Assert, no logic in tests, no shared mutable state between tests.
- Do not chase a coverage number; cover behaviour that would embarrass you in production if broken.
- A test run after a failed build uses stale binaries and proves nothing — gate test runs on the build's exit code.

---

## 8. Workflow per phase

1. Read the phase section in `PORTFOLIO_PLAN.md`.
2. **Plan first** — propose files to create, packages to add, ADRs to write. Do not write code until the plan is approved.
3. Implement **one step at a time**, stopping after each for review.
4. Write the tests named in the phase's acceptance criteria and make them pass.
5. Write the ADRs.
6. Update `README.md` and the progress tracker table in `PORTFOLIO_PLAN.md`.
7. Verify: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and the app actually runs.

**Definition of done for a phase:** build green, tests green, format clean, `aspire run` works end to end, ADRs written, README updated, acceptance criteria demonstrably met.

### Commits and branches

- One branch per phase: `phase-NN-<slug>`. One PR, squash-merged, with a real description.
- **Conventional Commits**: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `perf:`.
- Small commits with meaningful messages — the git history is part of the portfolio and will be read.
- Commit only when asked.

---

## 9. Explain as you go

After implementing something non-obvious, add a short note to `docs/talking-points.md`: the problem, the options considered, the choice, the tradeoff, and what would change at 10× scale.

If a piece of code cannot be explained out loud in an interview, it does not belong in this repository — say so rather than leaving it in.
