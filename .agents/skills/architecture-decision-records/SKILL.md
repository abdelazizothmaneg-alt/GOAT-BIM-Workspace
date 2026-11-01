---
name: architecture-decision-records
description: >-
  Drafts, manages, and evaluates Architecture Decision Records (ADRs) to capture significant architectural choices,
  context, options considered, trade-offs, and downstream consequences.
---

# Architecture Decision Records (ADRs)

Standardized format for documenting architectural and engineering decisions to maintain historical context and align engineering teams.

## Standard ADR Template (Michael Nygard Format)

```markdown
# ADR-[NUMBER]: [SHORT TITLE OF THE DECISION]

## Status
[Proposed | Accepted | Superseded | Deprecated] by [ADR-XXX if applicable]
Date: YYYY-MM-DD

## Context & Problem Statement
What is the specific architectural, technical, or business problem we are facing?
What constraints, dependencies, and business requirements must be satisfied?

## Considered Options
1. Option A: [Brief description]
2. Option B: [Brief description]
3. Option C: [Brief description]

## Decision Outcome
Chosen Option: "[Option A]", because [Primary compelling reason based on requirements].

### Positive Consequences
* [Benefit 1: e.g., Improves memory efficiency by 40%]
* [Benefit 2: e.g., Native compatibility with Revit 2024-2026]

### Negative Consequences / Trade-offs
* [Drawback 1: e.g., Requires manual dependency registration]
* [Drawback 2: e.g., Additional learning curve for junior engineers]

## Pros and Cons of the Options

### Option A
* Good, because [argument]
* Bad, because [argument]

### Option B
* Good, because [argument]
* Bad, because [argument]

## Validation & Compliance
How will we verify that this decision is followed? (e.g., automated architectural linter, CI pipeline, code review checklist).
```
