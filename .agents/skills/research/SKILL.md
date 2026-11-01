---
name: research
description: >-
  Conducts rigorous primary-source engineering and technical research on software libraries, APIs, SDKs, and engineering standards.
  Inspects official documentation, official source repositories, and release notes, outputting cited markdown reports directly to the project.
---

# Technical Research (Primary-Source Engineering Investigator)

Engineering research workflow designed to gather authoritative facts, verify API contracts, and document architectural options without relying on unverified forum summaries.

## 1. Core Research Principles
* **Primary Sources First:**
  * Prioritize official vendor documentation (e.g., Autodesk Developer Network, Microsoft Learn, official GitHub source code).
  * Check exact version compatibility, deprecation notices, and breaking changes.
* **No Speculation:**
  * If an API behavior or code constraint is undocumented or ambiguous, explicitly mark it as an assumption or run a minimal verification probe.
* **Persistent Markdown Deliverable:**
  * Never leave research findings solely in chat logs.
  * Always synthesize findings into a structured report saved to `docs/research/<topic-slug>.md`.

---

## 2. Research Report Template Structure

```markdown
# Research: [TOPIC / API / LIBRARY NAME]

**Date:** YYYY-MM-DD  
**Status:** [Complete | In Progress | Blocked]  
**Target Platform:** [e.g., Revit 2026 / .NET 10 / AutoCAD 2025]

## 1. Executive Summary
High-level architectural findings and direct recommendation.

## 2. Primary Technical Findings
* **Classes / Methods / Endpoints:** Exact namespaces, signatures, and interfaces.
* **Code Example (Minimal Working Snippet):** Tested or verified usage pattern.
* **Constraints & Gotchas:** Threading rules, licensing prerequisites, memory footprint.

## 3. Evaluated Alternatives & Trade-offs
| Option | Pros | Cons | Verdict |
| :--- | :--- | :--- | :--- |
| Approach A | Native performance | Verbose boilerplate | Recommended |
| Approach B | Fast implementation | Deprecated in next release | Avoid |

## 4. Authoritative Citations & References
* [Official Documentation Link](https://...)
* [GitHub Repository Source File](https://...)
```
