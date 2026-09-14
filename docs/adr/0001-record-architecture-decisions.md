# 1. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-07

## Context

This repository exists to demonstrate engineering judgement, not just working software. The
code shows *what* was built; it cannot show what else was considered, what was rejected, or
what the choice cost. Six months from now — and in an interview — the reasoning is the part
that matters, and it is the part that evaporates first.

A reviewer landing on this repository needs to answer "why is it like this?" before "how does
it work?". Without a written record, every non-obvious decision looks either arbitrary or
cargo-culted, and both readings are worse than the truth.

## Decision

Every architectural decision is recorded as a numbered Markdown file in `docs/adr/`, in the
lightweight format popularised by Michael Nygard: **Context / Decision / Alternatives
considered / Consequences**, one page.

Rules:

- **No ADR, no decision.** If a choice is not written down, it did not happen, and the code
  implementing it is not finished.
- ADRs are **immutable once accepted**. A decision that changes gets a *new* ADR that
  supersedes the old one; the old file stays, marked `Superseded by ADR-NNNN`. The history of
  changing your mind is more instructive than a tidy final state.
- **Alternatives considered is not optional.** An ADR listing only the chosen option records
  nothing — the rejected options and the reason for rejecting them are the actual content.
- **Consequences must include the bad ones.** A decision with no downsides was not a decision.

Numbering is sequential and never reused.

## Alternatives considered

**Nothing written down; rely on code review and memory.** The default, and the reason most
codebases cannot explain themselves. Fails the primary purpose of this repository.

**A single long ARCHITECTURE.md.** Easier to skim, but it describes only the current state.
It has nowhere to put "we chose X over Y because Z", it grows into a document nobody updates,
and edits destroy the history of *when* and *why* something changed.

**Full RFC process with review and sign-off.** Appropriate for a team; pure ceremony for one
person. The cost would guarantee it gets skipped, and a process that gets skipped is worse
than a light one that gets followed.

**ADRs in an external wiki or issue tracker.** Splits the reasoning from the code it
justifies, so the two drift. Keeping ADRs in the repository means they are versioned with the
change that implements them and appear in the same pull request.

## Consequences

**Good.** Every decision has a durable, reviewable justification. The `docs/adr/` index
becomes the fastest way to understand the system. Writing "Alternatives considered" forces a
real comparison before the code is written, which has already changed decisions rather than
merely documented them. Interview preparation becomes a matter of re-reading, not recalling.

**Bad.** Every non-trivial change costs an extra 20–30 minutes of writing. There is a
standing temptation to write the ADR after the fact to rationalise a decision already made,
which produces documentation that is worse than none because it looks authoritative. The
discipline only holds if the ADR is written *with* the change, in the same pull request.

**Neutral.** Some ADRs will be proven wrong. That is the point — a superseding ADR that says
"we were wrong about this, here is what we learned" is the most valuable document in the
directory.
