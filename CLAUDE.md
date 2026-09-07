# CLAUDE.md — TicketPeak

Instructions for Claude Code working in this repository.
The full roadmap lives in [PORTFOLIO_PLAN.md](./PORTFOLIO_PLAN.md) — read the relevant phase before starting work.

---

## 1. What this is

TicketPeak is an event ticketing platform built as a **portfolio project** demonstrating Strong Middle .NET Developer skills.

It is a **modular monolith** (core transactional domain) plus **three deliberately extracted services** (Search, Ticketing, Notifications) behind a YARP gateway, with Blazor web and MAUI mobile clients sharing one Razor Class Library.

**This being a portfolio changes the priorities.** Clarity, justification and explainability outrank cleverness and speed. Code that works but cannot be explained in an interview is a failure here.

### Current phase

> **Phase 0 — Foundation and tooling** (update this line when a phase is merged)

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
# run everything (SQL, Redis, Mongo, RabbitMQ, all services, dashboard)
dotnet run --project src/Aspire/TicketPeak.AppHost

# build / test
dotnet build TicketPeak.slnx
dotnet test                                    # all
dotnet test tests/TicketPeak.UnitTests         # fast loop, no containers
dotnet test tests/TicketPeak.ArchitectureTests # boundary rules

# formatting — CI fails if this is not clean
dotnet format --verify-no-changes

# migrations (per module, each has its own DbContext + history table)
dotnet ef migrations add <Name> \
  --project src/Monolith/TicketPeak.Modules.<Module> \
  --startup-project src/Monolith/TicketPeak.Api \
  --context <Module>DbContext
```

Integration tests use **Testcontainers** — Docker must be running. They are slower; run unit tests during the inner loop.

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
├─ Domain/            aggregates, value objects, domain events — references NOTHING
├─ Application/       one folder per use case: Command + Handler + Validator + Response
├─ Infrastructure/    DbContext, EF configurations, repositories, external clients
├─ Endpoints/         minimal API endpoint group
├─ OrderingModule.cs  Autofac module + registration
└─ IOrderingApi.cs    the ONLY public type other modules may reference
```

**Boundary rules (enforced by architecture tests, not by discipline):**
- `Domain` has no project references and no framework dependencies beyond BCL.
- A module may reference another module **only** through its `I<Module>Api` interface.
- Everything else in a module is `internal`.
- No module references `TicketPeak.Api`.
- Cross-module *state changes* go through integration events, not direct calls.

---

## 5. Stack and pinned choices

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 (LTS), C# 14 | `nullable` and `TreatWarningsAsErrors` on everywhere |
| Packages | Central Package Management | versions live **only** in `Directory.Packages.props` |
| API | Minimal APIs in services; Controllers in the monolith where filters/model binding earn it | |
| Docs | `Microsoft.AspNetCore.OpenApi` + Scalar | not Swashbuckle |
| DI | **Autofac** in the monolith, **MS.DI** in the services | deliberate contrast — see ADR 0006 |
| Mediator | **MediatR** in the monolith only | services use plain handlers — see ADR 0005 |
| ORM | EF Core 10; **Dapper** for hot read paths | mark each Dapper query with a comment saying why |
| Mapping | **Mapperly** (source-generated) | not AutoMapper |
| Validation | FluentValidation, invoked by a MediatR behaviour | |
| Messaging | MassTransit over RabbitMQ (Azure Service Bus in cloud, swapped by config) | |
| Caching | HybridCache (L1 memory + L2 Redis) | explicit invalidation on write |
| Resilience | `Microsoft.Extensions.Resilience` / Polly v8 pipelines | on every outbound HTTP call |
| Logging | Serilog, structured, JSON to console | |
| Telemetry | OpenTelemetry traces + metrics + logs | wired in ServiceDefaults |
| Tests | xUnit v3, NSubstitute, Shouldly, Testcontainers, NetArchTest | |

**Licensing:** several of these re-licensed in 2025 (MediatR, AutoMapper, MassTransit v9, FluentAssertions v8). Before adding one, check the licence of the exact version being pinned and record it in `docs/adr/0003-third-party-licensing.md`.

---

## 6. Code conventions

- **`TimeProvider`, never `DateTime.Now`/`UtcNow`.** Inject it; tests use `FakeTimeProvider`.
- **Strongly-typed IDs**: `readonly record struct OrderId(Guid Value)` with EF Core value converters.
- **`Result<T>` for expected failures**; exceptions only for genuinely exceptional cases. Never use exceptions for control flow.
- **RFC 9457 `ProblemDetails`** at the API edge, including the trace id.
- **Aggregates protect their invariants.** No public setters on entities; state changes go through methods that can refuse.
- **No repository interface per entity** — one per aggregate root.
- `async`/`await` all the way down; pass `CancellationToken` through every layer; never `.Result` or `.Wait()`.
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
