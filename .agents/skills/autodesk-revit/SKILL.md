---
name: autodesk-revit
description: >-
  Expert Autodesk Revit and BIM automation skill covering BIM Execution Planning (BEP), parametric family creation,
  shared parameter management, Level of Development (LOD 100-500), worksharing/worksets, and sheet production workflows.
---

# Autodesk Revit & BIM Workflows

Expert guidelines for architects, BIM managers, and computational designers working with Autodesk Revit and digital twin environments.

## Core Revit Architectural & Data Principles

### 1. Revit Data Hierarchy
Always respect and maintain Revit's strict structural taxonomy:
* **Category:** Built-in system definition (e.g., `OST_Walls`, `OST_Doors`, `OST_Rooms`).
* **Family:**
  * *System Families:* Built directly into project templates (Walls, Floors, Roofs, Stairs).
  * *Component / Loadable Families:* Defined in `.rfa` files (Doors, Windows, Furniture, MEP Equipment).
  * *In-Place Families:* Custom project-specific geometry.
* **FamilySymbol (Type):** Distinct parametric variations containing type parameters.
* **FamilyInstance (Instance):** Individual placed 3D instances in the model hosting instance parameters.

### 2. Parameters & Data Standards
* **Shared Parameters:** Use GUID-based `.txt` shared parameter definitions for parameters meant to appear across schedules and multi-category tags.
* **Project Parameters:** Assigned to categories for high-level project metadata.
* **Global Parameters:** Direct driving of geometric relationships and constraints across unrelated elements.

### 3. Worksharing & Multi-Disciplinary Coordination
* **Worksets Organization:** Partition model elements by discipline, core/shell, interior fit-out, and linked CAD/IFC files.
* **Phasing & Design Options:** Clear demarcation of Existing, Demolition, and New Construction phases.
* **Clash Detection & Quality Audits:** Pre-export cleanup of unplaced rooms, duplicated marks, orphaned elements, and unresolved warnings.
