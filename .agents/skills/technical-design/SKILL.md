---
name: technical-design
description: >-
  Generates rigorous technical design specifications, architecture breakdown structures, system interfaces,
  data flow models, and engineering specifications for complex technical and AEC software projects.
---

# Technical Design

Standardized technical design specification generator for engineering, software architecture, and integrated digital systems.

## Design Document Structure

### 1. Context & Objectives
* **Problem Statement:** What engineering, operational, or architectural challenge is being resolved?
* **Scope & Boundaries:** Explicit in-scope deliverables versus out-of-scope elements.
* **Success Criteria & KPIs:** Measurable technical benchmarks (performance, throughput, latency, geometry accuracy, concurrency).

### 2. System Architecture & Component Model
* **Subsystem Decomposition:** Clear boundary definitions for individual modules, services, or add-in components.
* **Component Diagram:** Data contracts, API endpoints, and event loops.
* **Data Flow & State Lifecycle:**
  * Ingestion / Input processing
  * Transformation / Core domain logic
  * Output persistence / Export / Rendering

### 3. Engineering Constraints & Invariants
* **Dependencies & Interfaces:** Third-party APIs, SDKs (e.g., Revit API, .NET CLR), and host platform constraints.
* **Non-Functional Requirements (NFRs):**
  * Reliability & Graceful Degradation: Handling host application crashes or null geometry.
  * Performance: CPU/Memory bounds, caching strategies, and asynchronous execution.
  * Security & Data Privacy: API key management, local disk permissions, and audit logs.
