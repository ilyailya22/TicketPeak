# 5. Handle cross-cutting concerns in a MediatR pipeline, pinned to 12.5.0

- **Status:** Accepted
- **Date:** 2026-09-17

## Context

Every command and query in the monolith needs the same treatment: logging with its outcome, a
latency signal, input validation, and committing the unit of work only when the handler succeeded.
Later phases add caching and idempotency. With roughly sixty handlers expected, repeating that in each
one would be sixty chances to forget it.

Commands will not only arrive over HTTP. From Phase 6 they also come from message consumers and
scheduled jobs, where there is no HTTP request and no middleware.

MediatR re-licensed in 2025. The last Apache-2.0 release is **12.5.0**; 13.0.0 onward is RPL-1.5,
a reciprocal licence, or a commercial one. This was verified from the `LICENSE.md` inside the
14.2.0 package itself, not from secondary sources.

## Decision

**MediatR 12.5.0, pinned permanently, in the monolith only.** `Directory.Packages.props` carries a
do-not-bump comment. The services extracted from Phase 7 onward use plain handlers: five to ten
endpoints do not need a mediator.

**Four behaviours, outermost first:**

1. **Logging.** Every request and its outcome. A refused business rule is a warning with its error
   code, not an error.
2. **Performance.** Warns past 500 ms, timed through `TimeProvider`.
3. **Validation.** FluentValidation, returning a `Result` failure instead of throwing.
4. **Unit of work.** Commits every module's `IUnitOfWork` after success, none after failure.

**Routing by generic constraint, not runtime checks.** Validation requires a `Result` response and
the unit of work requires an `ICommand` request. Autofac honours both constraints when closing the
open generics. Container-level tests send real requests through real Autofac and MediatR and prove
that an invalid command never reaches its handler and that a query never touches the unit of work.

**Logging is source-generated** through `[LoggerMessage]`, so a disabled level costs nothing on a
path every request takes. CA1848 now enforces this repo-wide.

## Alternatives considered

**ASP.NET Core middleware or endpoint filters.** Middleware sees HTTP requests, not commands. It
cannot tell a command from a query, and it does not run for a command sent from a message consumer.
Endpoint filters are still used, but only to turn Results into HTTP responses.

**MediatR 14 under RPL-1.5 or a commercial licence.** A public repository arguably complies with
RPL-1.5, but anyone forking this for closed use would inherit the obligation. That is the wrong
lesson for a project meant to show commercial judgement.

**Wolverine.** A capable mediator and message bus in one. Rejected here because messaging is Phase
6's decision (MassTransit is planned), and adopting a second framework's conventions now would
decide that early.

**A hand-rolled dispatcher.** About 150 lines, with no licence question at all. It remains the exit
plan below; rejected for now because MediatR 12.5.0 already does this and is widely recognised.

## Consequences

**Good.** Cross-cutting behaviour exists once, and commands work the same from HTTP, consumers and
jobs. Handlers contain only their use case.

**Bad.** Indirection: "go to definition" on `Send` lands on an interface, not the handler. The
dependency is frozen, so it will receive no fixes. NuGet audit failing the build on any advisory
(ADR 0003) is the safety net. `ResultFailure<T>` uses reflection to build a failed `Result<T>`,
done once per response type and cached.

**Exit plan.** Application code depends only on `IRequest`, `IRequestHandler`, `IPipelineBehavior`
and `ISender`. A hand-rolled dispatcher implementing those four shapes is a mechanical replacement
that no handler would notice.

**A finding worth remembering.** In 12.5.0, `RequestHandlerDelegate` takes an optional
`CancellationToken`. Calling `next()` compiles but silently drops cancellation at every behaviour.
The analyzer (CA2016) caught it; all behaviours now call `next(cancellationToken)`, and a test
asserts the handler receives the caller's token, so the fix does not depend on the analyzer staying
enabled.
