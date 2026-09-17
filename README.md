# TicketPeak

[![CI](https://github.com/ilyailya22/TicketPeak/actions/workflows/ci.yml/badge.svg)](https://github.com/ilyailya22/TicketPeak/actions/workflows/ci.yml)

An event ticketing platform built as a portfolio project: a **modular monolith** for the
transactional core, plus three deliberately extracted services behind a YARP gateway, with
Blazor web and MAUI mobile clients sharing one Razor Class Library.

The interesting part is not that it sells tickets — it is *why each piece is shaped the way it
is*. Every architectural decision is recorded in [`docs/adr/`](docs/adr/); read those first.

> **Status:** Phase 1 complete — domain model and modular monolith, with a working checkout path over HTTP.
> The full roadmap is in [PORTFOLIO_PLAN.md](./PORTFOLIO_PLAN.md).

---

## Quick start

```bash
git clone https://github.com/ilyailya22/TicketPeak.git
cd TicketPeak
aspire run
```

The Aspire dashboard opens with the API, its logs, traces and metrics. The API serves OpenAPI at
`/openapi/v1.json`, a Scalar UI at `/scalar`, and health probes at `/health/live` and `/health/ready`.

**Prerequisites**

| | |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Version pinned in `global.json`; `setup-dotnet` reads the same file in CI |
| Aspire CLI | `dotnet tool install -g aspire.cli` — or skip it and use `dotnet run --project src/Aspire/TicketPeak.AppHost` |
| Docker Desktop | **Not needed yet.** Becomes a hard requirement in Phase 2, when Testcontainers and the containerised data stores arrive |

### Walk through a checkout

With the API running on `http://localhost:5206`:

```bash
API=http://localhost:5206/api

# A venue with 10 reserved seats in row A of STALLS, and 100 standing places on the FLOOR
VENUE=$(curl -s -X POST $API/venues -H 'Content-Type: application/json' \
  -d '{"name":"Roundhouse","sections":[{"code":"STALLS","rows":[{"label":"A","seats":10}]},{"code":"FLOOR","standingCapacity":100}]}' | tr -d '"')

# An event there: price every section, open sales, publish, release the tickets
EVENT=$(curl -s -X POST $API/events -H 'Content-Type: application/json' \
  -d "{\"organiserId\":\"$(uuidgen)\",\"venueId\":\"$VENUE\",\"title\":\"Autumn Tour\",\"startsAt\":\"2027-06-01T19:00:00Z\",\"currency\":\"EUR\"}" | tr -d '"')
curl -s -X PUT $API/events/$EVENT/prices/STALLS -H 'Content-Type: application/json' -d '{"amountMinor":4500,"currency":"EUR"}'
curl -s -X PUT $API/events/$EVENT/prices/FLOOR  -H 'Content-Type: application/json' -d '{"amountMinor":3000,"currency":"EUR"}'
curl -s -X PUT $API/events/$EVENT/on-sale -H 'Content-Type: application/json' -d '{"opensAt":"2026-01-01T00:00:00Z","closesAt":"2027-06-01T19:00:00Z"}'
curl -s -X POST $API/events/$EVENT/publish
curl -s -X POST $API/events/$EVENT/inventory

# Hold two seats and a standing place for ten minutes, then order them
curl -s -X POST $API/events/$EVENT/holds -H 'Content-Type: application/json' \
  -d '{"seats":[{"section":"STALLS","row":"A","number":1},{"section":"STALLS","row":"A","number":2}],"standing":[{"section":"FLOOR","quantity":1}]}'
```

Holding a seat someone else holds returns `409` with the rule's code in an RFC 9457 problem. The
same flow runs as an automated test in [`CheckoutFlowTests`](tests/TicketPeak.Api.Tests/CheckoutFlowTests.cs).

## Build and test

```bash
dotnet build TicketPeak.slnx
dotnet test --solution TicketPeak.slnx          # --solution is required under Microsoft.Testing.Platform
dotnet format TicketPeak.slnx --verify-no-changes
```

CI runs exactly these steps on every pull request, in that order — formatting last, so a stray
blank line can never mask a real build or test failure.

| Suite | What it proves |
|---|---|
| `TicketPeak.UnitTests` | Every aggregate rule refuses what it should, with no infrastructure |
| `TicketPeak.ArchitectureTests` | Module boundaries hold. Each rule was proven by breaking it once |
| `TicketPeak.Api.Tests` | The host composes correctly, the pipeline behaves, and checkout works end to end over HTTP |

## Repository layout

```
src/Aspire/      AppHost (orchestration) + ServiceDefaults (telemetry, health, resilience)
src/Monolith/    API host + one project per bounded context + Shared.Kernel
src/Services/    Search (MongoDB) · Ticketing (worker) · Notifications · Gateway (YARP)
src/Contracts/   Integration event records only
src/Clients/     Client.Shared (RCL) · Web (Blazor) · Mobile (MAUI Blazor Hybrid)
tests/           Unit · Integration · Architecture · Contract · load (k6)
infra/bicep/     Azure infrastructure as code
docs/adr/        Architecture decision records
```

Directories appear as the phase that needs them lands. Today the monolith has five modules:
**Identity, Catalog, Inventory, Ordering, Payments**. Each has `Domain/`, `Application/` (one
folder per use case), `Infrastructure/` and `Endpoints/`; only its root namespace is public.

## Architecture decisions

| ADR | Decision |
|---|---|
| [0001](docs/adr/0001-record-architecture-decisions.md) | Record architecture decisions |
| [0002](docs/adr/0002-monorepo.md) | Use a monorepo |
| [0003](docs/adr/0003-third-party-licensing.md) | Check and record third-party licences before pinning them |
| [0004](docs/adr/0004-modular-monolith.md) | Build the core as a modular monolith with enforced boundaries |
| [0005](docs/adr/0005-mediatr-pipeline.md) | Handle cross-cutting concerns in a MediatR pipeline, pinned to 12.5.0 |
| [0006](docs/adr/0006-autofac-vs-msdi.md) | Use Autofac in the monolith and the built-in container in services |

The reasoning behind the less obvious choices, and what would change at ten times the scale, is in
[`docs/talking-points.md`](docs/talking-points.md).

## Tech stack

Grows as each phase lands; see [PORTFOLIO_PLAN.md §6](./PORTFOLIO_PLAN.md#6-technology-stack-2026)
for the full intended stack and the reasoning behind each choice.

| Area | Choice |
|---|---|
| Runtime | .NET 10 (LTS), C# 14 |
| Architecture | Modular monolith: five bounded contexts, boundaries enforced by ArchUnitNET |
| Orchestration | Aspire 13.5 — `aspire run` boots everything |
| API | ASP.NET Core Minimal APIs, `Microsoft.AspNetCore.OpenApi` + Scalar, RFC 9457 problems |
| Composition | Autofac — one module per bounded context |
| Pipeline | MediatR 12.5.0 (pinned for licensing): logging, timing, validation, unit of work |
| Validation | FluentValidation |
| Persistence | In-memory in Phase 1; EF Core 10 + MSSQL from Phase 2 |
| Telemetry | OpenTelemetry traces, metrics and logs over OTLP; source-generated logging |
| Resilience | `Microsoft.Extensions.Http.Resilience` (Polly v8) |
| Testing | xUnit v3 on Microsoft.Testing.Platform, Shouldly, FakeTimeProvider, ArchUnitNET |
| Packages | Central Package Management — versions live only in `Directory.Packages.props` |
| CI | GitHub Actions: restore → build → test → format |

## What Phase 1 does not prove yet

Persistence is in memory and **not safe under concurrent requests**, so the no-double-sale
invariant is proven by single-threaded domain tests only. Proving it with fifty parallel buyers
against SQL is Phase 2. It is stated here so a green test suite does not imply more than it checks.

## Engineering conventions

Nullable enabled and **warnings as errors** everywhere, `.editorconfig` enforced in CI, and
central package management so twenty projects cannot drift apart. `TreatWarningsAsErrors` is
not decoration: it caught a high-severity advisory in a transitive dependency on the very
first build (see [ADR 0003](docs/adr/0003-third-party-licensing.md)), and an analyzer caught
MediatR behaviours silently dropping cancellation (see [ADR 0005](docs/adr/0005-mediatr-pipeline.md)).

The working agreement this repository is built to is in [CLAUDE.md](./CLAUDE.md).

## Licence

Not yet chosen.
