---
name: execution-planning
description: >-
  Builds comprehensive execution roadmaps, milestone schedules, Critical Path Method (CPM) analyses,
  resource allocation plans, and risk mitigation registers for complex architectural and software engineering projects.
---

# Execution Planning

Strategic planning framework to translate technical scopes into executable, phased roadmaps with clear dependencies and timelines.

## Planning Framework

### 1. Phasing & Milestone Definition
* **Phase 0: Inception & Environment Setup:** Tooling, baseline requirements, repository scaffolds.
* **Phase 1: Proof of Concept / Alpha:** Core algorithms, mock interfaces, architectural geometry verification.
* **Phase 2: Beta / Production Implementation:** Feature completeness, database schemas, full BIM workflows.
* **Phase 3: QA, Hardening & Commissioning:** Load testing, stress testing, documentation, client handover.

### 2. Critical Path Analysis (CPM)
* Identify tasks where any delay directly delays the target delivery date.
* Calculate Earliest Start (ES), Latest Start (LS), and Float / Slack time for secondary work streams.
* Sequence concurrent engineering paths to maximize team throughput without introducing circular blockers.

### 3. Risk Assessment & Mitigation Register
* Map risks by **Probability ($P$)** and **Impact ($I$)** on a $1-5$ scale:
  $$\text{Risk Score} = P \times I$$
* Establish explicit contingency protocols (e.g., fallback API endpoints, manual override modes, version rollback scripts).
