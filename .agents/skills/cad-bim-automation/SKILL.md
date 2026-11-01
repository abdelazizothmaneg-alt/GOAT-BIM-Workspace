---
name: cad-bim-automation
description: >-
  Automates the bidirectional translation between 2D CAD drawings (DWG/DXF) and parametric BIM models (Revit/IFC),
  converting layer-based vectors into native walls, slabs, columns, and room spatial boundaries with automated parameter binding.
---

# CAD to BIM Automation

Engineering and automation pipeline for bridging legacy 2D CAD drawings with intelligent 3D Building Information Models.

## 1. Vector-to-BIM Reconstruction Pipeline
1. **Layer Sanitization & Filtering:**
   * Isolate boundary lines from `A-WALL-FULL`, `A-COLS`, `A-DOOR`, `S-COLS`.
   * Purge text, hatching, dimensions, and furniture vectors before extraction.
2. **Geometric Normalization & Snapping:**
   * Join fragmented line segments into continuous polylines (`PEDIT` / `JOIN`).
   * Clean up micro-gaps ($< 5\text{mm}$) and collinear overlaps using vertex snapping algorithms.
3. **Parametric Wall Generation:**
   * Detect parallel vector pairs to calculate wall thickness ($100\text{mm}, 150\text{mm}, 200\text{mm}, 300\text{mm}$).
   * Compute centerline trajectories and generate native `Wall.Create(document, centerlineCurve, wallTypeId, levelId, height)`.
4. **Automated Room & Space Generation:**
   * Extract room boundary tags and text labels (`A-ANNO-TEXT`).
   * Place Revit `Room` objects at text insertion points and synchronize Room Name and Number parameters.

## 2. Batch Export & Parameter Synchronization
* Automated generation of standardized DWG sheets from 3D BIM views with custom export layer mapping tables (`ExportDWGSettings`).
* Mapping Revit Shared Parameters into CAD block attributes (`ATTRIB`) for downstream facility management.
