# TicketPeak

An event ticketing platform built as a portfolio project: a **modular monolith** for the
transactional core, plus three deliberately extracted services behind a YARP gateway, with
Blazor web and MAUI mobile clients sharing one Razor Class Library.

The interesting part is not that it sells tickets — it is *why each piece is shaped the way it
is*. Every architectural decision is recorded in [`docs/adr/`](docs/adr/); read those first.

> **Status:** Phase 0 — foundation and tooling.
> The full roadmap is in [PORTFOLIO_PLAN.md](./PORTFOLIO_PLAN.md).

---

## Quick start

```bash
git clone <repo> && cd ticketpeak
aspire run
```

The Aspire dashboard opens with every service, its logs, traces and metrics.

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
and the Aspire CLI (`dotnet tool install -g aspire.cli`). Docker Desktop is required from
Phase 2 onward, once the containerised data stores arrive.

Without the Aspire CLI:

```bash
dotnet run --project src/Aspire/TicketPeak.AppHost
```

## Build and test

```bash
dotnet build TicketPeak.slnx
dotnet test
dotnet format --verify-no-changes    # CI fails if this is not clean
```

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

## Architecture decisions

| ADR | Decision |
|---|---|
| _(populated as they are written)_ | |

## Tech stack

Filled in as each phase lands. See [PORTFOLIO_PLAN.md §6](./PORTFOLIO_PLAN.md#6-technology-stack-2026)
for the full intended stack and the reasoning behind each choice.

## Licence

Not yet chosen.
