---
name: cad-skill
description: >-
  Architectural and engineering Computer-Aided Design (CAD) drafting skill covering layer standards (AIA CAD layer guidelines),
  DXF/DWG geometry manipulation, linetypes, scale factors, hatch patterns, blocks/symbols, and plot style configurations.
---

# CAD Drafting & Geometry Skill

Engineering drafting standards, layer conventions, and geometric precision for architectural and civil CAD systems (AutoCAD, BricsCAD, LibreCAD, DXF processing).

## 1. AIA CAD Layer Standards Hierarchy
Follow the standard discipline-major-minor-status naming convention:
* **`A-WALL-FULL`:** Architectural interior & exterior full-height walls (Lineweight: $0.50\text{mm}$, Continuous).
* **`A-WALL-PRTN`:** Partial-height drywall partitions (Lineweight: $0.35\text{mm}$).
* **`A-DOOR`:** Doors, frames, swings, and hardware (Lineweight: $0.25\text{mm}$).
* **`A-GLAZ`:** Windows, curtain walls, and glazed storefronts (Lineweight: $0.25\text{mm}$).
* **`A-FLOR-LEVL`:** Finished floor level marks, steps, ramps (Lineweight: $0.18\text{mm}$).
* **`A-FLOR-PATT`:** Floor hatch patterns, tile grid lines (Lineweight: $0.09\text{mm} - 0.13\text{mm}$, Muted color).
* **`A-ANNO-DIMS`:** Dimension lines, tick marks, and dimension text (Lineweight: $0.18\text{mm}$).
* **`A-ANNO-TEXT`:** Room labels, notes, title blocks (Lineweight: $0.25\text{mm} - 0.35\text{mm}$).

## 2. Drafting Invariants & Geometric Rules
* **Closed Polylines for Area Boundaries:** All rooms and gross floor areas must be strictly closed polylines (`LWPOLYLINE`) with zero vertex gaps to enable instant area extraction (`BPOLY` / `AREA`).
* **Orthogonal Snapping & Tolerance:** Maintain strictly orthogonal geometry ($0^\circ, 90^\circ, 180^\circ, 270^\circ$) unless an intentional radial/skewed axis is defined. Snap tolerances must not exceed $0.001\text{ units}$.
* **Annotation Scaling:** Use Annotative text and dimensions matching drawing scale ($1:100$ for general plans, $1:50$ for enlarged zones, $1:20 / 1:10$ for construction details).
* **Plot Styles:** Use CTB (Color-Dependent Plot Styles) where color number maps to lineweight, or STB (Named Plot Styles) for monochrome presentation with defined screening percentages.
