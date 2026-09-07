# TicketPeak

[![CI](https://github.com/ilyailya22/TicketPeak/actions/workflows/ci.yml/badge.svg)](https://github.com/ilyailya22/TicketPeak/actions/workflows/ci.yml)

An event ticketing platform built as a portfolio project: a **modular monolith** for the
transactional core, plus three deliberately extracted services behind a YARP gateway, with
Blazor web and MAUI mobile clients sharing one Razor Class Library.

The interesting part is not that it sells tickets — it is *why each piece is shaped the way it
is*. Every architectural decision is recorded in [`docs/adr/`](docs/adr/); read those first.

> **Status:** Phase 0 complete — foundation and tooling.
> The full roadmap is in [PORTFOLIO_PLAN.md](./PORTFOLIO_PLAN.md).

---

## Quick start

```bash
git clone https://github.com/ilyailya22/TicketPeak.git
cd TicketPeak
aspire run
```

The Aspire dashboard opens with every service, its logs, traces and metrics. The API answers
on `/hello`, `/health/live` and `/health/ready`, with OpenAPI at `/openapi/v1.json` and a
Scalar UI at `/scalar`.

**Prerequisites**

| | |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Version pinned in `global.json`; `setup-dotnet` reads the same file in CI |
| Aspire CLI | `dotnet tool install -g aspire.cli` — or skip it and use `dotnet run --project src/Aspire/TicketPeak.AppHost` |
| Docker Desktop | **Not needed yet.** Becomes a hard requirement in Phase 2, when Testcontainers and the containerised data stores arrive |

## Build and test

```bash
dotnet build TicketPeak.slnx
dotnet test --solution TicketPeak.slnx          # --solution is required under Microsoft.Testing.Platform
dotnet format TicketPeak.slnx --verify-no-changes
```

CI runs exactly these four steps on every pull request, in that order — formatting last, so a
stray blank line can never mask a real build or test failure.

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

Directories appear as the phase that needs them lands.

## Architecture decisions

| ADR | Decision |
|---|---|
| [0001](docs/adr/0001-record-architecture-decisions.md) | Record architecture decisions |
| [0002](docs/adr/0002-monorepo.md) | Use a monorepo |
| [0003](docs/adr/0003-third-party-licensing.md) | Check and record third-party licences before pinning them |

## Tech stack

Grows as each phase lands; see [PORTFOLIO_PLAN.md §6](./PORTFOLIO_PLAN.md#6-technology-stack-2026)
for the full intended stack and the reasoning behind each choice.

| Area | Choice |
|---|---|
| Runtime | .NET 10 (LTS), C# 14 |
| Orchestration | Aspire 13.5 — `aspire run` boots everything |
| API | ASP.NET Core Minimal APIs, `Microsoft.AspNetCore.OpenApi` + Scalar |
| Telemetry | OpenTelemetry traces, metrics and logs over OTLP |
| Resilience | `Microsoft.Extensions.Http.Resilience` (Polly v8) |
| Testing | xUnit v3 on Microsoft.Testing.Platform, Shouldly |
| Packages | Central Package Management — versions live only in `Directory.Packages.props` |
| CI | GitHub Actions: restore → build → test → format |

## Engineering conventions

Nullable enabled and **warnings as errors** everywhere, `.editorconfig` enforced in CI, and
central package management so twenty projects cannot drift apart. `TreatWarningsAsErrors` is
not decoration: it caught a high-severity advisory in a transitive dependency on the very
first build (see [ADR 0003](docs/adr/0003-third-party-licensing.md)).

The working agreement this repository is built to is in [CLAUDE.md](./CLAUDE.md).

## Licence

Not yet chosen.
