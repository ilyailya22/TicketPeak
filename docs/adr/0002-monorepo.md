# 2. Use a monorepo

- **Status:** Accepted
- **Date:** 2026-09-07

## Context

TicketPeak is not one deployable. By the end of the roadmap it is a modular monolith, three
extracted services, a YARP gateway, a shared contracts assembly, a Razor class library, a
Blazor web app, a MAUI mobile app, and Bicep infrastructure — roughly twenty projects that
ship on different cadences.

Two facts drive the decision:

1. **One person works on all of it.** There are no team boundaries to encode, no independent
   release trains to protect, and no access-control story that a repository split would serve.
2. **A reviewer must be able to run the whole system.** The plan's success criterion for
   Phase 15 is that a stranger clones the repository and has it running without asking a
   question. Aspire's AppHost boots every service and data store from one process — but only
   if it can reference every project.

The services also share code that changes together: `TicketPeak.Contracts` defines the
integration events that the monolith publishes and the services consume. A change to an event
schema is inherently a change to both sides.

## Decision

One repository containing everything: all source, tests, infrastructure and documentation,
built by one CI pipeline and orchestrated by one Aspire AppHost.

## Alternatives considered

**A repository per service (polyrepo).** This is what a real organisation with separate teams
would do, and the honest reason to reject it is that this project has no teams. Concretely it
would cost: a package feed and a release cycle for `TicketPeak.Contracts` before the first
integration event can be consumed; an atomic contract change becoming a coordinated
multi-repository sequence; the AppHost losing the project references that make `aspire run`
work, forcing container images even in the inner loop; nine CI pipelines to maintain; and a
reviewer needing nine clones to see the system.

**Hybrid — monolith plus clients in one repository, services in another.** Inherits the
contract-versioning problem, which is the expensive part, while giving up the single-clone
property. The worst of both.

**Monorepo with a build-graph tool (Nx, Bazel, Moon).** Solves incremental builds across many
projects. At twenty .NET projects with a sub-minute CI, it solves a problem this repository
does not have, and adds a toolchain a reviewer must learn before they can build. Revisit only
if CI time becomes a real constraint.

## Consequences

**Good.** An integration event and both sides of its contract change in one atomic commit and
one reviewable pull request. `aspire run` boots the entire system from project references,
with no image builds or package feeds in the inner loop. One CI pipeline, one set of
conventions, one `Directory.Packages.props` — version drift across twenty projects is
structurally impossible. One clone shows a reviewer everything.

**Bad.** CI builds and tests everything on every change; this is cheap now and will not stay
cheap, and path filters or affected-project detection will eventually be needed. Nothing
structurally prevents an inappropriate project reference from a service into the monolith's
internals — that boundary is enforced by architecture tests (Phase 1), not by the repository
layout, and those tests are load-bearing precisely because of this decision. There is also no
per-directory access control, which is irrelevant here and would not be in a company.

**Honest caveat for interviews.** The correct answer to "monorepo or polyrepo?" is "it depends
on team topology, not on the architecture." Independently deployable services do not require
independent repositories. This project chose a monorepo because one person maintains it and
demonstrability matters more than deployment independence; with four teams owning these
services, the coordination cost of shared ownership would likely flip the decision.
