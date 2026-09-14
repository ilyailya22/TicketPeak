# Strong Middle .NET Developer — Portfolio Build Plan (2026)

> A staged, executable plan for building a portfolio that proves **depth**, not just breadth.
> Written to be consumed by Claude Code one phase at a time.

**Created:** 2026-09-07
**Target role:** Strong Middle / Middle+ .NET Developer (backend-leaning full-stack)

---

## Table of contents

1. [Goals and rules of the game](#1-goals-and-rules-of-the-game)
2. [Portfolio composition](#2-portfolio-composition)
3. [Flagship product: TicketPeak](#3-flagship-product-ticketpeak)
4. [Architecture decisions and their justification](#4-architecture-decisions-and-their-justification)
5. [Data store justification matrix](#5-data-store-justification-matrix)
6. [Technology stack (2026)](#6-technology-stack-2026)
7. [Repository layout](#7-repository-layout)
8. [Engineering conventions](#8-engineering-conventions)
9. [How to work through this plan with Claude Code](#9-how-to-work-through-this-plan-with-claude-code)
10. [Phases 0–15](#10-phases)
11. [Secondary project: OSS library](#11-secondary-project--oss-library)
12. [Minimum viable portfolio](#12-minimum-viable-portfolio-if-time-is-short)
13. [Risks and anti-patterns](#13-risks-and-anti-patterns)
14. [Interview talking points to prepare](#14-interview-talking-points-to-prepare)

---

## 1. Goals and rules of the game

### What this portfolio must prove

| Claim a Middle+ dev must back up | How this plan proves it |
|---|---|
| I can model a non-trivial domain | Bounded contexts, aggregates, invariants that actually matter (seat inventory) |
| I choose architecture, I don't cargo-cult it | Monolith **and** microservices in one product, each with a written ADR |
| I know why each database is picked | MSSQL + MongoDB + Redis, each doing what only it does well |
| I can run async, distributed workflows | Outbox, saga, idempotency, DLQ, retries |
| My code is observable and operable | OpenTelemetry, structured logs, health checks, dashboards |
| I test what matters | Testcontainers, architecture tests, contract tests, load tests |
| I can ship to cloud | IaC + CI/CD to Azure with federated identity, no secrets in the repo |
| I can build a UI when needed | Blazor web + MAUI mobile from a shared component library |

### Hard rules

1. **Every architectural decision gets an ADR.** No ADR → the decision does not exist. A reviewer reads `docs/adr/` before reading code.
2. **Nothing is added "to show I know it."** If MediatR, RabbitMQ or a given microservice cannot be justified in two sentences in an ADR, it does not go in. Unjustified technology is a *negative* signal at middle+ level.
3. **Every phase ends green**: build passes, tests pass, `aspire run` / `docker compose up` works, README updated.
4. **Small, conventional commits, one branch per phase.** Git history is part of the portfolio.
5. **Verify library versions and licences at the moment you add them.** The .NET ecosystem re-licensed heavily in 2025 (see §6.4) — awareness of this is itself an interview signal.
6. **Seed realistic demo data.** A reviewer must be able to click through a working system in under 2 minutes.

---

## 2. Portfolio composition

| # | Project | Type | Share of effort | Purpose |
|---|---|---|---|---|
| **A** | **TicketPeak** | Flagship: modular monolith + extracted services + web + mobile | ~85% | Depth: architecture, data, messaging, cloud, clients |
| **B** | **One small OSS library** | NuGet package with benchmarks and docs | ~15% | Breadth: library authoring, source generators, BenchmarkDotNet, public API design |

Two finished artifacts beat five half-finished CRUD apps every time.

---

## 3. Flagship product: TicketPeak

**TicketPeak** — an event ticketing platform (concerts, conferences, local venues).

Chosen because its workloads are genuinely heterogeneous, which is what makes the architectural decisions *honest* rather than decorative:

- **Purchase path** needs strict transactional consistency — seat 14B must never be sold twice. → monolith, one relational DB, one transaction.
- **Public browsing/search** is read-heavy, spiky and schema-flexible (a concert, a conference and a workshop have completely different attributes). → separate read service on a document DB.
- **Seat holds** are short-lived, high-churn, TTL-based state. → Redis, not a table.
- **Ticket rendering** (PDF + QR) is CPU-bursty and may lag by seconds. → async worker, scale-to-zero.
- **Notifications** fan out massively at the on-sale minute. → separate service with its own retry/DLQ policy.
- **Mobile has a real reason to exist**: the attendee wallet works offline at a venue with no signal, and door staff scan QR codes with the camera and validate offline, syncing later. This is not "the website in a WebView."

### Bounded contexts

| Context | Home | Responsibility |
|---|---|---|
| Identity & Access | Monolith module | Users, roles, organiser accounts, device API keys |
| Catalog (write) | Monolith module | Events, venues, seat maps, price tiers, on-sale schedule |
| Inventory | Monolith module | Seat/quota allocation, holds — the core invariant |
| Ordering | Monolith module | Cart, order lifecycle, idempotent checkout |
| Payments | Monolith module | Payment intents against a fake provider, refunds |
| **Search & Discovery** | **Service** | Public read model, faceted + geo search |
| **Ticketing** | **Service (worker)** | PDF/QR issuing, blob storage, wallet passes |
| **Notifications** | **Service** | Email/push fan-out, templates, preferences |
| **Gateway / BFF** | **Service** | YARP routing, auth edge, aggregation for clients |

---

## 4. Architecture decisions and their justification

### 4.1 Why a modular monolith for the core

- The purchase flow spans Inventory → Ordering → Payments. Splitting them means distributed transactions for an invariant (*no double-sold seat*) that a single ACID transaction solves for free.
- One deployable, one DB, one transaction = lower latency and radically lower operational cost.
- Modules are enforced boundaries (separate `DbContext`, `internal` types, architecture tests), so extraction later is cheap — and you demonstrate exactly that in Phase 7.

**Interview line:** *"I started as a modular monolith because the core has a single consistency boundary. I extracted only the parts whose scaling profile, availability requirement or data shape genuinely differed."*

### 4.2 Why exactly these three services are extracted

| Service | Driver | Would a module have worked? |
|---|---|---|
| Search & Discovery | 50–100× read traffic vs. writes; independent scaling; different data model (documents, not rows); public traffic must survive an overloaded checkout | Yes — but it would couple public availability to the transactional core |
| Ticketing worker | CPU-bursty rendering; scale-to-zero between events; failure must not block checkout | Only with an in-process queue that dies with the host |
| Notifications | Fan-out spikes at on-sale; third-party providers are slow and flaky; needs its own retry/DLQ policy and rate limits | It would put provider timeouts on the core's request threads |

**Explicitly NOT extracted:** Ordering, Payments, Inventory, Identity. Document *why* in an ADR — restraint is the senior signal here.

### 4.3 Communication rules

- **Sync** (HTTP through the gateway) only for query paths a client is actively waiting on.
- **Async** (RabbitMQ locally / Azure Service Bus in cloud) for everything cross-service and state-changing.
- Integration events are published through a **transactional outbox** — never `SaveChanges()` followed by `Publish()`.
- No service ever reads another service's database.
- Every consumer is **idempotent** (inbox table / dedup key) — at-least-once is the only delivery guarantee you get.

### 4.4 Why MediatR and Autofac (held to the same rule)

- **MediatR in the monolith:** pipeline behaviours give one place for validation, transaction/unit-of-work scoping, correlated logging, caching and idempotency across ~60 handlers. That is a real cross-cutting-concern problem, not indirection for its own sake.
  In the **microservices, deliberately skip MediatR** — 5–10 endpoints do not need a mediator. Showing you can tell the difference is worth more than using it everywhere.
- **Autofac in the monolith:** `Autofac.Module` per bounded context, assembly scanning, **decorators** (caching/logging/retry wrappers) and nested lifetime scopes for message consumers — things the built-in container does not do cleanly.
  In the **microservices, use `Microsoft.Extensions.DependencyInjection`** (+ `Scrutor` if needed). Write the ADR comparing the two.

---

## 5. Data store justification matrix

| Store | Used for | Why *this* store | Why not the others |
|---|---|---|---|
| **MSSQL** (Azure SQL) | Orders, payments, seat inventory, users, outbox | ACID, `rowversion` optimistic concurrency, range locks for the seat invariant, mature EF Core support, reporting joins | Mongo lacks the transactional ergonomics for this; Redis is not durability-first |
| **MongoDB** (Cosmos DB for MongoDB vCore in Azure) | Public event/search read model, denormalised event documents, audit trail | Heterogeneous attributes per event category, single-document reads with no joins, geo indexes, cheap read scaling, projection rebuildable from events | Modelling per-category attributes in SQL means EAV or 30 nullable columns, and searching them means joins on the hot path |
| **Redis** (Azure Cache for Redis) | Seat holds with TTL, distributed lock, HybridCache L2, idempotency keys, rate limiting, SignalR backplane, waiting-room queue | Native TTL, atomic Lua ops, sub-ms latency, pub/sub. Holds are ephemeral 10-minute state — writing 100k of them to SQL is waste | SQL rows for holds create hot-page contention plus a cleanup job; Mongo adds nothing here |
| **Blob Storage** | Rendered ticket PDFs, event images | Cheap, CDN-frontable, SAS-scoped access | Storing PDFs in SQL is an anti-pattern you should be able to argue against |
| **SQLite** (mobile) | Offline wallet + offline scan queue | Embedded, transactional, syncs later | — |

Write `docs/adr/0007-polyglot-persistence.md` covering exactly this table.

---

## 6. Technology stack (2026)

### 6.1 Platform

| Area | Choice | Notes |
|---|---|---|
| Runtime | **.NET 10 (LTS)**, C# 14 | LTS through 2028; do not build a portfolio on a STS release |
| Web | ASP.NET Core — Minimal APIs for services, Controllers where model binding/filters earn their keep | Use both; be able to argue the tradeoff |
| API docs | `Microsoft.AspNetCore.OpenApi` + **Scalar** UI | Swashbuckle is no longer the default; know why |
| Local orchestration | **Aspire** AppHost | One `aspire run` boots SQL, Redis, Mongo, RabbitMQ, all services, dashboard |
| Containers | Docker + `docker compose` fallback | Reviewers without Aspire must still run it |

### 6.2 Backend libraries

| Concern | Choice | Alternative to mention in the ADR |
|---|---|---|
| Mediator / pipeline | **MediatR** (monolith only) | Wolverine; hand-rolled dispatcher |
| DI container | **Autofac** (monolith) / MS.DI (services) | Scrutor, Lamar |
| ORM | **EF Core 10** + **Dapper** on hot read paths | — |
| Mapping | **Mapperly** (source-generated) | AutoMapper (now commercial) |
| Validation | **FluentValidation** | DataAnnotations, minimal-API filters |
| Messaging | **MassTransit** over RabbitMQ / Azure Service Bus | Rebus, raw `RabbitMQ.Client`, Wolverine |
| Resilience | `Microsoft.Extensions.Resilience` / **Polly v8** pipelines | — |
| Caching | **HybridCache** (L1 memory + L2 Redis) + `StackExchange.Redis` | — |
| Background jobs | **Quartz.NET** (or Hangfire for the dashboard) | Hosted services for simple cases |
| Logging | **Serilog** structured logging → console JSON + Seq + App Insights | — |
| Telemetry | **OpenTelemetry** traces/metrics/logs | — |
| Auth | **OpenIddict** or **Keycloak** (OIDC) + JWT bearer; Entra ID in the Azure story | Duende IdentityServer (commercial) |
| Gateway | **YARP** | Azure API Management, Ocelot |
| Realtime | **SignalR** + Redis backplane | — |
| Document DB | **MongoDB.Driver** | Cosmos DB SQL API, Marten on Postgres |
| Feature flags | **Azure App Configuration** + `Microsoft.FeatureManagement` | — |

### 6.3 Testing and quality

| Concern | Choice |
|---|---|
| Unit/integration | **xUnit v3**, **NSubstitute**, **Shouldly** (or AwesomeAssertions) |
| Real infrastructure in tests | **Testcontainers** (MSSQL, Redis, Mongo, RabbitMQ) |
| API tests | `WebApplicationFactory` + Testcontainers |
| Architecture rules | **NetArchTest** / ArchUnitNET — enforce module boundaries in CI |
| Snapshot | **Verify** |
| Contract tests | **PactNet** between gateway/clients and services |
| Mutation testing | **Stryker.NET** on the domain layer only |
| Load testing | **k6** or **NBomber** — on-sale spike scenario, with a committed report |
| Micro-benchmarks | **BenchmarkDotNet** on one real hot path |
| Static analysis | Roslyn analyzers, `TreatWarningsAsErrors`, **SonarAnalyzer**, CodeQL, Trivy image scan |

### 6.4 Licensing awareness (a 2026 interview question)

Several formerly-free libraries moved to commercial licences during 2025 — among them **MediatR**, **AutoMapper**, **MassTransit v9** and **FluentAssertions v8**. Before adding each one:

- check the current licence for the version you are pinning;
- pin the last permissively licensed version *or* pick the free alternative;
- record the choice in `docs/adr/0003-third-party-licensing.md`.

Being able to say *"I know MediatR's licence changed, here's how I'd handle it in a commercial product"* is worth more than the library itself.

### 6.5 Cloud (Azure)

Container Apps · Azure SQL · Cosmos DB for MongoDB · Azure Cache for Redis · Service Bus · Blob Storage · Key Vault · App Configuration · Application Insights · Container Registry · Front Door.
IaC with **Bicep** (primary) — optionally mirror one module in **Terraform** to show both.

### 6.6 Clients

- **Shared Razor Class Library** — components, view models, typed API clients generated from OpenAPI (Kiota or NSwag).
- **Blazor Web App** (Auto render mode) — public site + organiser admin.
- **.NET MAUI Blazor Hybrid** — attendee wallet + staff scanner, reusing the RCL. This is the "both in one project" option, and it is the strongest one for a .NET portfolio.
- *Optional stretch:* a small React/TypeScript front-end for one page, purely to show you can work outside .NET.

---

## 7. Repository layout

A **monorepo**. Justify it in an ADR: one Aspire AppHost can boot the whole system, one CI pipeline, and a reviewer sees everything in one clone. (Polyrepo would be more realistic for separate teams — say so.)

```
ticketpeak/
├─ AGENTS.md / CLAUDE.md          # instructions for Claude Code
├─ PORTFOLIO_PLAN.md              # this file
├─ Directory.Build.props          # shared MSBuild settings
├─ Directory.Packages.props       # central package management
├─ .editorconfig
├─ TicketPeak.slnx
├─ docs/
│  ├─ adr/                        # 0001-…, one file per decision
│  ├─ architecture/               # C4 diagrams (Mermaid / Structurizr)
│  ├─ runbook.md
│  └─ talking-points.md           # your interview crib sheet
├─ src/
│  ├─ Aspire/
│  │  ├─ TicketPeak.AppHost/
│  │  └─ TicketPeak.ServiceDefaults/
│  ├─ Monolith/
│  │  ├─ TicketPeak.Api/                       # host: endpoints, DI, middleware
│  │  ├─ TicketPeak.Modules.Identity/
│  │  ├─ TicketPeak.Modules.Catalog/
│  │  ├─ TicketPeak.Modules.Inventory/
│  │  ├─ TicketPeak.Modules.Ordering/
│  │  ├─ TicketPeak.Modules.Payments/
│  │  └─ TicketPeak.Shared.Kernel/             # value objects, result types, abstractions
│  ├─ Services/
│  │  ├─ TicketPeak.Search/                    # MongoDB read model
│  │  ├─ TicketPeak.Ticketing/                 # worker: PDF/QR
│  │  ├─ TicketPeak.Notifications/
│  │  └─ TicketPeak.Gateway/                   # YARP + BFF
│  ├─ Contracts/
│  │  └─ TicketPeak.Contracts/                 # integration event schemas ONLY
│  └─ Clients/
│     ├─ TicketPeak.Client.Shared/             # Razor Class Library
│     ├─ TicketPeak.Web/                       # Blazor Web App
│     └─ TicketPeak.Mobile/                    # MAUI Blazor Hybrid
├─ tests/
│  ├─ *.UnitTests/  *.IntegrationTests/  *.ArchitectureTests/  *.ContractTests/
│  └─ load/                                    # k6 scripts + results
├─ infra/
│  ├─ bicep/
│  └─ terraform/                               # optional mirror
└─ .github/workflows/
```

**Module internal structure** (vertical slices, not layer folders):

```
TicketPeak.Modules.Ordering/
├─ Domain/            # aggregates, value objects, domain events — no dependencies
├─ Application/       # one folder per use case: Command, Handler, Validator, Response
├─ Infrastructure/    # DbContext, configurations, repositories, external clients
├─ Endpoints/         # minimal API endpoint group
├─ OrderingModule.cs  # Autofac module + service registration
└─ IOrderingApi.cs    # the ONLY public surface other modules may call
```

---

## 8. Engineering conventions

- **Nullable enabled, warnings as errors**, latest analyzers, `.editorconfig` enforced in CI (`dotnet format --verify-no-changes`).
- **Central Package Management** (`Directory.Packages.props`) — no version drift across 20 projects.
- **Result pattern** for expected failures; exceptions only for exceptional cases. RFC 9457 `ProblemDetails` at the API edge.
- **No `DateTime.Now`** — inject `TimeProvider` (this alone reads as a 2026-current developer).
- **Strongly-typed IDs** (`readonly record struct OrderId`) with EF Core value converters.
- **Domain layer references nothing** — enforced by an architecture test, not by discipline.
- **Conventional Commits** (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`) and one PR per phase, merged with a squash and a written description. Review your own PRs as if a stranger will read them — one will.
- **ADR format:** Context / Decision / Alternatives considered / Consequences. Keep them one page.
- **Secrets:** user-secrets locally, Key Vault in Azure. Never a connection string in `appsettings.json`.

---

## 9. How to work through this plan with Claude Code

### 9.1 First, create `CLAUDE.md` at the repo root

It should contain: the stack and pinned versions, the folder conventions from §7, the coding conventions from §8, the build/test commands, and the rule *"before adding a dependency or crossing a module boundary, propose it and wait."*

### 9.2 Per-phase workflow

```
1. git checkout -b phase-NN-<slug>
2. Open Claude Code, use plan mode first:
   "Read PORTFOLIO_PLAN.md §Phase NN and CLAUDE.md. Propose an implementation
    plan for this phase only. List files to create, packages to add, and the
    ADRs to write. Do not write code yet."
3. Review and correct the plan yourself — this is where you learn.
4. "Implement step 1 of the plan." … iterate step by step, never the whole phase at once.
5. "Write the tests listed in the acceptance criteria and make them pass."
6. "Write docs/adr/NNNN-*.md for the decisions we made."
7. Run it yourself, break it deliberately, fix it.
8. Commit, open a PR, squash-merge.
```

### 9.3 Rules for using the assistant well

- **Never accept code you cannot explain in an interview.** After each phase, close the editor and explain the phase out loud. If you cannot, redo it.
- Ask *"what are the alternatives and why is this one better here?"* before each new library — that conversation is what you will be asked in an interview.
- Ask Claude to review your own diff before you commit (`/code-review`).
- Keep phases to one branch each; a 4000-line PR teaches you nothing.

---

## 10. Phases

Estimates assume ~10 focused hours/week. Total: roughly 5–7 months part-time; the [MVP path](#12-minimum-viable-portfolio-if-time-is-short) is ~10 weeks.

---

### Phase 0 — Foundation and tooling
**Estimate:** 1 week · **Branch:** `phase-00-foundation`

**Deliverables**
- Monorepo, solution, `Directory.Build.props` + `Directory.Packages.props`, `.editorconfig`, `.gitignore`.
- `CLAUDE.md`, `README.md` skeleton, `docs/adr/0001-record-architecture-decisions.md`, `docs/adr/0002-monorepo.md`.
- Aspire AppHost + ServiceDefaults with a single "hello" API wired to the dashboard.
- GitHub Actions: restore → build → test → format check, on every PR.

**Acceptance**
- `aspire run` opens the dashboard and the API responds.
- CI is green on a PR and fails on a deliberate formatting error.

**Proves:** tooling discipline, CI from day one.

---

### Phase 1 — Domain model and monolith skeleton
**Estimate:** 2 weeks · **Branch:** `phase-01-modular-monolith`

**Deliverables**
- Six module projects per §7, each with `Domain / Application / Infrastructure / Endpoints`.
- Shared kernel: `Result<T>`, strongly-typed IDs, `Entity`, `AggregateRoot`, domain events, `TimeProvider` usage.
- Real domain logic, not anaemic classes: `SeatMap`, `PriceTier`, `Order` state machine, `Hold` with expiry, invariants enforced inside aggregates.
- **Autofac**: one `Autofac.Module` per bounded context, assembly-scanned handler registration, `ILifetimeScope` per request.
- **MediatR** pipeline behaviours: `LoggingBehavior`, `ValidationBehavior`, `UnitOfWorkBehavior`, `PerformanceBehavior`.
- **Architecture tests**: Domain references nothing; no module references another module's internals; only `I<Module>Api` is public.

**Acceptance**
- Architecture tests fail when you deliberately add a cross-module reference.
- Unit tests cover every aggregate invariant, with no infrastructure involved.
- `docs/adr/0004-modular-monolith.md`, `0005-mediatr-pipeline.md`, `0006-autofac-vs-msdi.md`.

**Proves:** DDD, boundaries, DI mastery, cross-cutting concerns.

---

### Phase 2 — Persistence: MSSQL + EF Core
**Estimate:** 1.5 weeks · **Branch:** `phase-02-sql-persistence`

**Deliverables**
- One `DbContext` **per module**, one schema per module, in a single database. Separate migration histories.
- Entity configurations in separate classes; owned types; strongly-typed ID converters; `rowversion` concurrency tokens.
- Interceptors: auditing (`CreatedAt/By`), soft delete query filters, domain-event dispatch on `SaveChanges`.
- **Dapper** for two or three read-heavy queries; explain the choice in the code and the README.
- Seat allocation implemented correctly under concurrency (optimistic concurrency + retry, or a serializable transaction — benchmark and document which you chose).
- Testcontainers-based integration tests with MSSQL.
- Realistic seed data: 3 venues, 30 events, seat maps, users.

**Acceptance**
- A concurrency test spawning 50 parallel purchases of the same seat sells it **exactly once**.
- `dotnet ef migrations` works per module; a fresh DB is created on startup in dev.

**Proves:** EF Core beyond CRUD, concurrency, transaction control, testing against real infrastructure.

---

### Phase 3 — Authentication and authorisation
**Estimate:** 1 week · **Branch:** `phase-03-auth`

**Deliverables**
- OIDC provider (Keycloak in a container, or OpenIddict hosted in the Identity module) — pick one, ADR the choice.
- JWT bearer auth, refresh tokens, role claims (attendee / organiser / door-staff / admin).
- **Policy-based** and **resource-based** authorisation (an organiser may edit only their own events).
- API keys for scanner devices; separate scheme.
- ASP.NET Core rate limiting per user and per IP on the checkout endpoint.

**Acceptance**
- Integration tests assert 401 vs 403 vs 200 for each role on each endpoint.
- A cross-organiser edit attempt returns 403.

**Proves:** real-world auth, not `[Authorize]` sprinkled on controllers.

---

### Phase 4 — Redis
**Estimate:** 1 week · **Branch:** `phase-04-redis`

**Deliverables**
- Seat holds as Redis keys with TTL + a Lua script for atomic multi-seat hold.
- Distributed lock (RedLock) around the on-sale opening path.
- **HybridCache** (L1 memory + L2 Redis) for the event/seat-map read path, with explicit invalidation on write.
- Idempotency-key store for `POST /orders` (replay returns the original response).
- Output caching for public endpoints.

**Acceptance**
- Holds expire automatically; a released hold returns inventory without any background job.
- Sending the same `Idempotency-Key` twice creates exactly one order.
- Cache hit/miss counters visible in the Aspire dashboard.
- `docs/adr/0007-polyglot-persistence.md` explains Redis vs. SQL for holds.

**Proves:** you use Redis for what it's *for*, not as "a cache someone told me to add."

---

### Phase 5 — Observability and resilience
**Estimate:** 1 week · **Branch:** `phase-05-observability`

**Deliverables**
- **Serilog** structured logging, JSON to console, enrichers (correlation id, user id, module), request logging with duration and status.
- **OpenTelemetry**: traces (ASP.NET Core, EF Core, HTTP, Redis, MassTransit), metrics, log correlation → Aspire dashboard locally, App Insights in Azure.
- Custom metrics: holds created, checkout latency, seats sold per minute.
- Health checks (`/health/live`, `/health/ready`) covering SQL, Redis, RabbitMQ.
- **Polly / Microsoft.Extensions.Resilience** pipelines on all outbound HTTP: retry with jitter, circuit breaker, timeout, fallback.
- Global exception handling → RFC 9457 `ProblemDetails`, with a trace id the user can quote.

**Acceptance**
- One checkout produces a single distributed trace spanning API → SQL → Redis → broker.
- Killing Redis degrades the system gracefully instead of returning 500s.

**Proves:** you have operated software, not just written it.

---

### Phase 6 — Async messaging and the transactional outbox
**Estimate:** 1.5 weeks · **Branch:** `phase-06-messaging`

**Deliverables**
- RabbitMQ via Aspire; **MassTransit** configured with a transport abstraction so Azure Service Bus is a one-line swap.
- `TicketPeak.Contracts` — versioned integration event records (`OrderPlaced`, `PaymentAuthorized`, `TicketsIssued`, `EventPublished`).
- **Transactional outbox** (EF Core outbox + dispatcher) — events and state commit atomically.
- **Inbox / dedup** for consumers, with a documented idempotency strategy.
- Retry policy, exponential backoff, dead-letter queue and a way to inspect and replay it.
- Scheduled jobs (Quartz): open on-sale at time T, expire abandoned carts, rebuild projections.

**Acceptance**
- A test that kills the process between `SaveChanges` and publish proves no event is lost and none is duplicated in effect.
- Poison messages land in the DLQ and can be replayed from a documented command.

**Proves:** the single most-asked distributed-systems topic at middle+ level.

---

### Phase 7 — First extraction: Search & Discovery on MongoDB
**Estimate:** 2 weeks · **Branch:** `phase-07-search-service`

**Deliverables**
- New service consuming catalog integration events and building a **denormalised MongoDB read model** (event document with category-specific attributes, venue, price range, tags, geo point).
- Faceted search, full-text, geo-radius, pagination, sorting; indexes designed deliberately and documented with `explain` output in the README.
- Projection rebuild command (replay from the event log) — proves the read model is disposable.
- **YARP gateway** introduced in front: `/api/search/*` → Search service, everything else → monolith.
- Search service uses **MS.DI, no MediatR** — and the ADR explains why that is a considered decision.

**Acceptance**
- Publishing an event in the monolith makes it searchable within ~1 s.
- Dropping the Mongo database and running the rebuild restores search fully.
- A load test shows search scaling independently of the monolith.
- `docs/adr/0008-extract-search-service.md`, `0009-document-db-read-model.md`.

**Proves:** CQRS across services, eventual consistency, document modelling, gateway routing — and the *justified* transition from monolith to services.

---

### Phase 8 — Ticketing worker and the checkout saga
**Estimate:** 2 weeks · **Branch:** `phase-08-ticketing-saga`

**Deliverables**
- Worker service: on `PaymentAuthorized`, render ticket PDF + QR (signed payload, not a raw id), upload to Blob Storage, publish `TicketsIssued`.
- **MassTransit saga / state machine** for the checkout process: `OrderPlaced → PaymentAuthorized → TicketsIssued → Completed`, with timeouts and **compensation** (release seats and refund on failure).
- Saga state persisted (EF Core or Mongo — choose and justify).
- QR signature verification endpoint used later by the scanner.

**Acceptance**
- A simulated payment failure releases the held seats and leaves the system consistent.
- A simulated renderer crash mid-saga resumes correctly after restart.
- Saga state transitions are visible in traces.

**Proves:** long-running distributed workflows and failure compensation — the topic that separates middle from junior.

---

### Phase 9 — Notifications service
**Estimate:** 1 week · **Branch:** `phase-09-notifications`

**Deliverables**
- Consumes `TicketsIssued`, `EventCancelled`, `OnSaleStarting`; templated email (MJML/Razor) via a pluggable provider abstraction with a local dev sink (Mailpit/Papercut).
- Mobile push via Firebase or Azure Notification Hubs.
- Per-user notification preferences, quiet hours, per-provider rate limits.
- Outbound idempotency: the same event never sends twice.

**Acceptance**
- 1000 fan-out notifications process without blocking the core; failures retry then dead-letter.
- Preferences are respected, proven by tests.

**Proves:** integration with unreliable third parties, fan-out patterns.

---

### Phase 10 — Realtime, gateway hardening, API maturity
**Estimate:** 1 week · **Branch:** `phase-10-realtime-gateway`

**Deliverables**
- **SignalR** hub with Redis backplane: live seat availability on the seat map, plus a virtual waiting room (Redis sorted-set queue) for on-sale spikes.
- Gateway: rate limiting, request aggregation (BFF endpoints for mobile), header propagation of correlation ids, auth at the edge.
- **API versioning** (`Asp.Versioning`), OpenAPI documents per version, generated clients committed or generated in CI.

**Acceptance**
- Two browsers on the same seat map see each other's holds in real time.
- The waiting room admits users at a fixed rate under load.

**Proves:** realtime, edge concerns, API lifecycle thinking.

---

### Phase 11 — Clients: web + mobile from a shared library
**Estimate:** 3 weeks · **Branch:** `phase-11-clients`

**Deliverables**
- **`TicketPeak.Client.Shared`** RCL: typed API clients generated from OpenAPI (Kiota/NSwag), view models, shared Razor components (event card, seat map, checkout steps), auth token handling.
- **Blazor Web App** (Auto render mode): browse/search, seat picking, checkout, order history, plus an organiser admin area (create event, seat map editor, live sales dashboard over SignalR).
- **MAUI Blazor Hybrid** mobile, reusing the RCL:
  - *Attendee:* wallet with tickets cached in **SQLite**, works fully offline, QR rendered locally, push notifications, deep links.
  - *Door staff:* camera QR scanner, **offline validation** against a downloaded signed manifest, queued check-ins synced when connectivity returns with conflict handling for double scans.
- Accessible, responsive UI; loading/error/empty states everywhere.

**Acceptance**
- One component change is visibly reflected in both web and mobile.
- Airplane mode: wallet shows tickets, scanner validates and queues, sync reconciles on reconnect.

**Proves:** you can deliver a product end to end, and that mobile exists for a reason.

---

### Phase 12 — Azure infrastructure as code
**Estimate:** 1.5 weeks · **Branch:** `phase-12-infra`

**Deliverables**
- **Bicep** modules: Container Apps environment, Azure SQL, Cosmos DB for MongoDB, Azure Cache for Redis, Service Bus, Storage, Key Vault, App Configuration, Application Insights, Container Registry, Front Door.
- Managed identities everywhere; **zero secrets in config** — Key Vault references and RBAC.
- Environment parameter files (dev / prod), naming conventions, cost notes per resource.
- MassTransit transport swapped to **Azure Service Bus** by configuration, not code — demonstrate the abstraction paying off.
- Optionally mirror one module in Terraform to show both toolchains.

**Acceptance**
- `az deployment` from scratch produces a working environment; teardown script included.
- A documented monthly cost estimate and a "how to keep it under $X" note (portfolio realism).

**Proves:** cloud fluency and cost awareness.

---

### Phase 13 — CI/CD
**Estimate:** 1 week · **Branch:** `phase-13-cicd`

**Deliverables**
- GitHub Actions: build → test (with Testcontainers) → coverage → analyzers → container build → Trivy scan → push to ACR → deploy to Container Apps.
- **OIDC federated credentials** to Azure — no stored secrets.
- Environments with approval gates, revision-based blue/green with rollback, EF migrations applied as a gated pre-deploy job (never automatically on app start in prod — explain why).
- Dependabot/Renovate, CodeQL, release notes from conventional commits.

**Acceptance**
- A push to `main` deploys automatically; a rollback is one documented command.
- A failing test blocks deployment, proven by a deliberate red build.

**Proves:** you can own the delivery pipeline.

---

### Phase 14 — Quality proof
**Estimate:** 1.5 weeks · **Branch:** `phase-14-quality`

**Deliverables**
- Test pyramid documented with actual numbers (unit / integration / contract / e2e counts and runtime).
- **Contract tests** (Pact) between clients and services.
- **Mutation testing** on the domain layer with the score in the README badge.
- **k6 load test** of the on-sale spike (5000 virtual users hitting one event), with before/after results after you optimise something real.
- **BenchmarkDotNet** on the seat-availability query; document the optimisation you made (e.g. Dapper + covering index vs. EF Core).
- Profiling notes: one memory/allocation issue found and fixed, with numbers.

**Acceptance**
- README contains a performance section with real measurements, not adjectives.

**Proves:** you optimise with data. Almost nobody's portfolio has this — it is the strongest differentiator on this list.

---

### Phase 15 — Portfolio packaging
**Estimate:** 1 week · **Branch:** `phase-15-packaging`

**Deliverables**
- Top-level README: one-paragraph pitch, **C4 diagrams** (Mermaid), screenshots/GIF, quick-start (`aspire run` — under 5 minutes to a working system), tech table, link list to every ADR.
- Per-service READMEs.
- `docs/talking-points.md`: for each decision — the problem, the options, the choice, the tradeoff, what you would do differently at 10× scale.
- Optional 5-minute demo video.
- A live demo environment if the cost is acceptable, otherwise a scripted local demo.

**Acceptance**
- A stranger clones the repo and has it running without asking you a question.

---

## 11. Secondary project — OSS library

Pick one small, genuinely useful library and do it properly. Suggestions, in order of signal value:

1. **A source-generated idempotency / outbox helper** for EF Core — extracted from TicketPeak, which makes the story cohesive.
2. **A typed Redis key/TTL abstraction** with a Roslyn analyzer that catches key-format mistakes at compile time.
3. **A minimal-API endpoint discovery + versioning package** with source generation.

**Required extras** (this is where the signal is): XML docs on all public API, README with examples, BenchmarkDotNet results, >90% coverage, semantic versioning, CI publishing to NuGet, `PublicAPI.Shipped.txt` analyzer, multi-targeting `net8.0;net10.0`.

**Estimate:** 1 week.

---

## 12. Minimum viable portfolio (if time is short)

If you need something presentable in ~10 weeks, run: **Phases 0, 1, 2, 4, 5, 6, 7, 11 (web only), 13, 15.**

That still delivers a modular monolith + one justified microservice + MSSQL + Redis + MongoDB + RabbitMQ + outbox + observability + a web client + CI/CD — every requirement in the brief. Add mobile, the saga, notifications and the quality phase afterwards, each as its own PR, so the repo visibly keeps growing.

---

## 13. Risks and anti-patterns

| Risk | Mitigation |
|---|---|
| **Scope creep** — the classic portfolio killer | Ship a working system at the end of every phase. Never start Phase N+1 with Phase N red. |
| **Microservices theatre** — 9 services for a to-do app | Only three extractions, each with a written justification. Say "no" loudly in the ADR to the ones you rejected. |
| **Tech-name bingo** — libraries with no purpose | The two-sentence justification rule. Delete anything that fails it. |
| **Code you cannot explain** | Explain each phase out loud before merging. If you can't, redo it. |
| **Cloud bill surprise** | Scale-to-zero Container Apps, serverless tiers, teardown script, budget alert. |
| **Perfectionism on UI** | Use a component library; a clean, plain UI is fine. Backend is what you are being hired for. |
| **The repo looks AI-generated** | Meaningful commit messages, real ADRs with rejected options, at least one documented refactor where you changed your mind. |

---

## 14. Interview talking points to prepare

Write each of these out in `docs/talking-points.md` as you build. These are the questions this portfolio is engineered to attract:

1. Why a modular monolith, and how would you know when to split it?
2. Walk me through preventing a double-sold seat under 500 concurrent buyers.
3. Why Redis for holds instead of a SQL table? What breaks if Redis dies?
4. Why MongoDB for search and not just SQL full-text?
5. Explain the transactional outbox. What problem does it solve that a try/catch doesn't?
6. How do you make a consumer idempotent? What's your dedup key and how long do you keep it?
7. Saga vs. two-phase commit — what did you implement and why?
8. What do MediatR pipeline behaviours give you that middleware doesn't?
9. Autofac vs. the built-in container — when is the extra dependency worth it?
10. Show me a trace of one checkout. Where would you look first if p99 doubled?
11. How does your system behave when the payment provider hangs for 30 seconds?
12. What did you measure, and what did you change as a result?
13. What would you do differently at 100× traffic?
14. What in this codebase are you least happy with? *(Have a real, thoughtful answer.)*

---

## Progress tracker

| Phase | Branch | Status | PR | ADRs written |
|---|---|---|---|---|
| 0 Foundation | `phase-00-foundation` | ✅ | [#1](https://github.com/ilyailya22/TicketPeak/pull/1) | 0001, 0002, 0003 |
| 1 Modular monolith | `phase-01-modular-monolith` | ☐ | | 0004, 0005, 0006 |
| 2 SQL persistence | `phase-02-sql-persistence` | ☐ | | |
| 3 Auth | `phase-03-auth` | ☐ | | |
| 4 Redis | `phase-04-redis` | ☐ | | 0007 |
| 5 Observability | `phase-05-observability` | ☐ | | |
| 6 Messaging + outbox | `phase-06-messaging` | ☐ | | |
| 7 Search service | `phase-07-search-service` | ☐ | | 0008, 0009 |
| 8 Ticketing saga | `phase-08-ticketing-saga` | ☐ | | |
| 9 Notifications | `phase-09-notifications` | ☐ | | |
| 10 Realtime + gateway | `phase-10-realtime-gateway` | ☐ | | |
| 11 Clients | `phase-11-clients` | ☐ | | |
| 12 Infra | `phase-12-infra` | ☐ | | |
| 13 CI/CD | `phase-13-cicd` | ☐ | | |
| 14 Quality | `phase-14-quality` | ☐ | | |
| 15 Packaging | `phase-15-packaging` | ☐ | | |
| B OSS library | separate repo | ☐ | | |
