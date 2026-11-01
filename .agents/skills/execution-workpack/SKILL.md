---
name: execution-workpack
description: >-
  Structures engineering and implementation deliverables into granular work packages (Work Breakdown Structure - WBS),
  specifying inputs, execution tasks, acceptance criteria, and deliverable checklists.
---

# Execution Workpack

Production-grade work package decomposition framework for engineering, architectural delivery, and software builds.

## Workpack Anatomy & Lifecycle

### 1. Work Package Definition
Every workpack must have:
* **Package ID & Title:** Unique identifier (e.g., `WP-01-BIM-LOD300`, `WP-04-API-INTEGRATION`).
* **Objective:** Direct technical outcome upon completion.
* **Owner & Role:** Specific discipline lead or agent role responsible.

### 2. Task Breakdown (Granular Work Items)
* **Atomic Tasks:** Step-by-step action items with exact file paths, schemas, or models modified.
* **Prerequisites & Inputs:** Required upstream assets (e.g., verified site surveyor CAD, architectural floor plans, NuGet packages).
* **Dependencies:** Blocked by / Blocks relationships with other work packages.

### 3. Verification & Acceptance Criteria
* **Definition of Done (DoD):**
  * Passes automated unit/integration test suites.
  * Code adheres to linter and style guidelines.
  * Architectural/BIM model is purged of clashes and unplaced rooms/families.
* **Sign-off Artifacts:** Generated reports, documentation, test logs, or compiled binaries.
