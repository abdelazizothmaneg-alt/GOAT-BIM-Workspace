---
name: abstracting-building-models
description: >-
  Simplifies and abstracts dense architectural building models (BIM, IFC, Archicad/Revit) into lightweight analytical schemas,
  space syntax graphs, thermal zoning meshes, structural wireframes, and topological connectivity representations.
---

# Abstracting Building Models

Algorithmic abstraction framework to distill complex multi-gigabyte BIM geometry into lightweight, task-specific analytical graphs.

## 1. Abstraction Typologies

### A. Topological & Space Syntax Graphs
* **Node-Edge Representation:**
  * Nodes = Rooms / Functional Zones / Thermal Spaces.
  * Edges = Doors / Physical Apertures / Direct Circulation Connections.
* **Spatial Adjacency & Depth Calculation:**
  * Calculate Mean Depth, Integration Value, and Choice Value to predict human movement flows, evacuation bottlenecks, and visual connectivity.

### B. Analytical Energy & Thermal Zoning Mesh (BEM)
* Convert complex composite curved walls into planar 2D thermal boundary surfaces.
* Extract Second-Level Space Boundaries (inner surface, outer surface, adjacent zone surface, sunlit exterior roof/glazing) for EnergyPlus / OpenStudio building performance simulation.

### C. Structural Wireframe & Centerline Abstraction
* Collapse 3D volumetric structural elements (beams, columns, shear walls, slabs) into 1D analytical axes and 2D finite element plates.
* Export standardized structural analytical models directly into structural FEA engines (SAP2000, ETABS, Robot).

## 2. Multi-LOD Geometric Decimation
* **LOD 100/200 Bounding Box & Massing:** Simplified convex hulls for sunlight shading, zoning envelope verification, and GIS urban scale models.
* **Semantic Filtering:** Strip non-essential interior decorative detail (screws, complex family geometry, MEP brackets) while strictly preserving volumetric boundaries and thermal U-values.
