---
name: bim-coordination
description: >-
  Lead multi-disciplinary BIM coordination, automated clash detection (Navisworks/Solibri/Revizto), clearance tolerances,
  coordination meeting leadership, and BCF (BIM Collaboration Format) issue lifecycle management.
---

# BIM Coordination & Clash Management

Comprehensive multi-disciplinary coordination framework for Architectural, Structural, and MEP systems.

## 1. Clash Matrix & Tolerance Standards
Define clear clash test batches with specific clearance envelopes:
* **Batch 01: Structure vs. Gravity MEP (Zero Tolerance):**
  * Concrete beams/columns intersecting drainage pipes, ductwork, sewer lines. ($\text{Tolerance} = 0\text{mm}$).
* **Batch 02: Structure vs. Pressurized Services:**
  * Fire protection pipes, domestic water pipes passing through structural slabs without penetration sleeves.
* **Batch 03: Architectural Ceilings vs. MEP Services:**
  * Minimum clearance between finished ceiling grid and lowest duct/pipe soffit ($\ge 150\text{mm} - 250\text{mm}$ for maintenance access).
* **Batch 04: Maintenance & Operational Clearances (Soft Clashes):**
  * Electrical distribution boards ($900\text{mm}$ clear frontal working space).
  * VAV boxes and damper access doors ($600\text{mm} \times 600\text{mm}$ clear envelope below access panels).

## 2. BCF (BIM Collaboration Format) Issue Lifecycle
Every detected clash must follow a strict issue state machine:
```text
Identified -> Assigned -> In Progress -> Resolved -> Verified / Closed
```
* **Required BCF Attributes:**
  * Issue GUID, Title, Priority (Critical / High / Medium / Low).
  * Discipline Assigned (Arch, Struct, Mech, Elec, Plumb).
  * Camera viewpoint coordinates ($XYZ$) and affected element GUIDs.
  * Snapshot with clear redlining and action statement (e.g., "Reroute 400x200 supply duct below beam B-12").

## 3. Weekly Coordination Meeting (ICE Sessions)
* Review clash burndown chart.
* Resolve multidisciplinary pinch points in real-time.
* Freeze coordinated model milestones before generating approved-for-construction (AFC) shop drawings.
