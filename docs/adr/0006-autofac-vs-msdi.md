# 6. Use Autofac in the monolith and the built-in container in services

- **Status:** Accepted
- **Date:** 2026-09-17

## Context

The monolith composes five bounded contexts. Each should own its own wiring, so the host never
needs to know which handlers, validators or repositories a module contains, and adding a use case
needs no registration code anywhere.

The pipeline (ADR 0005) needs open-generic behaviours that apply only to requests meeting their
generic constraints. Later phases want decorators for caching and retries.

The services extracted from Phase 7 onward are small, with a handful of endpoints each.

## Decision

**Autofac in the monolith**, through `AutofacServiceProviderFactory`:

- One `Autofac.Module` per bounded context. The host registers five modules and nothing else from
  them.
- Assembly scanning registers handlers and validators, which therefore stay `internal`.
- Open-generic pipeline behaviours are registered once. Autofac skips any whose generic constraints
  a request does not satisfy.
- A lifetime scope per HTTP request, so each module's unit of work is shared by the handler and the
  pipeline within that request.

**`Microsoft.Extensions.DependencyInjection` in the extracted services.** The contrast is
deliberate: the right container depends on the size of the thing being composed.

What the tests prove rather than assume: the host's service provider really is
`AutofacServiceProvider`, and the constraints really do keep a query out of the unit of work. The
end-to-end tests also override `TimeProvider` through `ConfigureTestServices`, which confirms that
test-time replacement works through the Autofac integration.

## Alternatives considered

**The built-in container plus Scrutor.** Scrutor adds assembly scanning and decorators, which
covers most of the need. Module encapsulation would be hand-written `AddCatalogModule()` extension
methods, which is workable. I did not verify how the built-in container treats open generics with
unsatisfied constraints when resolving a collection, so I make no claim about it. Rejected for the
monolith because Autofac provides all of this natively, and the plan asks for the comparison to be
demonstrated.

**Lamar or DryIoc.** Both capable. Rejected as less common in the .NET teams this portfolio targets,
with nothing to gain here that Autofac lacks.

**The built-in container everywhere, for consistency.** Honest and simpler. Rejected for the
monolith because the module-per-context composition and constrained open generics are exactly the
monolith's needs, and one repository showing both containers used where each fits is part of this
project's point.

## Consequences

**Good.** Modules own their wiring, and the host's composition root is five lines. Constraint-based
routing removes a class of forgotten runtime checks.

**Bad.** An extra dependency, and a second DI dialect in one repository. Autofac scanning has a
sharp edge: chained `AsClosedTypesOf` calls filter *cumulatively*, so one chain registers only types
closing every interface at once, which is none. Each module scans separately, with a comment
explaining why.

**Neutral.** Registration mistakes surface at resolve time, not build time, as with any
container. The composition and pipeline tests exist to catch them in CI rather than in production.
