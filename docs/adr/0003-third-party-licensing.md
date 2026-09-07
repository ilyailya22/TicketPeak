# 3. Check and record third-party licences before pinning them

- **Status:** Accepted
- **Date:** 2026-09-07

## Context

The .NET ecosystem re-licensed heavily during 2025. Libraries that were free and permissive
for a decade moved to commercial or source-available terms, usually at a major version
boundary, and usually without breaking the build — a routine `dotnet add package` or a
Dependabot bump can silently change what a codebase is legally allowed to do. Among the
affected: **MediatR**, **AutoMapper**, **MassTransit** (v9), and **FluentAssertions** (v8).

This is not hypothetical for this repository. The plan calls for MediatR in the monolith and
MassTransit for messaging, so the question arrives in Phase 1 and again in Phase 6 — not at
some indefinite future date.

There is a second, related failure mode with the same shape: a pinned version that is
permissively licensed but *vulnerable*. Phase 0 hit exactly this. The `webapi` template emits
`Microsoft.AspNetCore.OpenApi` 10.0.4, which resolves `Microsoft.OpenApi` 2.0.0 — subject to
advisory GHSA-v5pm-xwqc-g5wc, high severity. It was caught only because `TreatWarningsAsErrors`
promoted NuGet's `NU1903` to a failed restore.

## Decision

Before any package is added to `Directory.Packages.props`:

1. **Read the licence of the exact version being pinned**, from the package's own `.nuspec` —
   not from memory, a blog post, or the licence the previous major version had.
2. **Record it in the table below**, with the version it was checked against.
3. If the licence is not permissive, either **pin the last permissively licensed version** and
   note why, or **choose a free alternative** — and write the reasoning into the ADR for that
   phase.

`TreatWarningsAsErrors` stays on repository-wide so that `NU1901`–`NU1904` (vulnerable and
deprecated packages) fail the build rather than scrolling past. A restore that fails on a CVE
is the cheapest security control available.

## Alternatives considered

**Check licences only at release.** Too late. By then the library is woven through the code
and the cost of removing it is what decides the outcome, not the licence terms.

**Avoid every library that has ever re-licensed.** Overcorrection. MediatR's pipeline
behaviours solve a real cross-cutting-concern problem across ~60 handlers; discarding a good
tool because its licence *might* change is not judgement, it is superstition.

**Automate with a licence-scanning tool in CI.** Genuinely better at scale and worth adding
later. Rejected for now because the value here is the *habit and the reasoning*, and a green
check mark tempts you to skip both. A tool tells you a licence changed; it does not tell you
what to do about it.

**Trust Dependabot/Renovate.** They track versions and vulnerabilities, not licence terms. A
minor-version bump across a licence change would sail through.

## Package licences as pinned

Verified by reading `<license type="expression">` from each package's `.nuspec` in the local
NuGet cache on 2026-09-07.

| Package | Version | Licence |
|---|---|---|
| Aspire.Hosting.AppHost (implicit, via Aspire.AppHost.Sdk) | 13.5.3 | MIT |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.11 | MIT |
| Microsoft.Extensions.Http.Resilience | 10.8.0 | MIT |
| Microsoft.Extensions.ServiceDiscovery | 10.8.0 | MIT |
| Microsoft.OpenApi (transitive) | 2.7.5 | MIT |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.3 | Apache-2.0 |
| OpenTelemetry.Extensions.Hosting | 1.15.3 | Apache-2.0 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.2 | Apache-2.0 |
| OpenTelemetry.Instrumentation.Http | 1.15.1 | Apache-2.0 |
| OpenTelemetry.Instrumentation.Runtime | 1.15.1 | Apache-2.0 |
| Scalar.AspNetCore | 2.17.3 | MIT |
| Shouldly | 4.3.0 | BSD-3-Clause |
| xunit.v3 | 4.0.0 | Apache-2.0 |

All permissive. Nothing in Phase 0 required a compromise.

## Decisions deferred to the phase that needs them

| Package | Phase | Question to answer then |
|---|---|---|
| MediatR | 1 | Licence of the version pinned; if commercial, pin the last permissive version or hand-roll the dispatcher — and say which in ADR 0005 |
| MassTransit | 6 | v9 licence terms; alternatives are Rebus, Wolverine, or raw `RabbitMQ.Client` |
| AutoMapper | — | Already avoided. Mapperly (source-generated) is chosen for performance reasons independent of licensing |
| FluentAssertions | — | Already avoided. Shouldly (BSD-3-Clause) is the assertion library |

## Consequences

**Good.** No licence surprise can arrive silently. The table is a standing answer to a
question this portfolio is designed to attract, backed by evidence rather than recollection.
`NU1903` as a build error already caught one real vulnerability before it reached `main`.

**Bad.** Every package addition costs a few minutes and an ADR edit, and the table needs
maintaining as versions move — a stale table is worse than none because it implies a check
that did not happen. Treating NuGet audit warnings as errors also means a newly published
advisory against an already-pinned package can turn a previously green build red without any
change on this side. That is the correct trade — a build that fails for a reason you did not
cause is still telling you something true — but it will be inconvenient at least once.

**Neutral.** Some of these libraries may re-license again. The mitigation is not prediction;
it is keeping the abstraction seam that makes replacement cheap, which is a design property
this repository wants for other reasons anyway.
