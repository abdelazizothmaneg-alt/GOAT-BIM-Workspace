---
name: cad-dwg-reader
description: >-
  Inspects, queries, and analyzes AutoCAD DWG and DXF files directly using natural language.
  Extracts layer structures, block references, embedded attributes (ATTRIB), text notes, and calculates planar distances and areas.
---

# CAD DWG Reader & Interactive Inspector

Natural language query engine and inspector for binary AutoCAD DWG and ASCII DXF drawings.

## 1. Natural Language Querying & Inspection Loop

Users can upload or reference a DWG/DXF file and ask targeted diagnostic questions:
* **Layer Audits:**
  * Extract all defined layers, current visibility states (On/Off/Frozen), colors (ACI), and linetypes.
  * Identify non-standard or external consultant layers.
* **Block & Asset Counting:**
  * Enumerate dynamic and static block references (e.g., doors, windows, structural columns, electrical fixtures).
  * Read embedded attribute tag key-value pairs (`MARK`, `SIZE`, `FIRE_RATING`, `MANUFACTURER`).
* **Text & Annotation Extraction:**
  * Harvest single-line text (`TEXT`) and multiline text (`MTEXT`) within specified coordinates or layers (e.g., room names, notes, title block revisions).
* **Dimensional & Area Calculations:**
  * Query point-to-point distances between coordinate vertices $(X_1, Y_1) \rightarrow (X_2, Y_2)$.
  * Calculate closed polyline area metrics in square meters.

---

## 2. Drawing Diagnostics & Health Checks
* **Proxy Entities & Third-Party Objects:**
  * Detect unhandled proxy entities (e.g., Civil 3D surfaces, Archicad/Revit exports, or Tianzheng/T20 architectural elements) that may cause corruption during import into Revit.
* **Coordinate Basepoint & Extents:**
  * Check ModelSpace bounding box (`EXTMIN` to `EXTMAX`) to identify elements placed at cosmic coordinates ($> 100,000\text{ m}$ from origin) that trigger Revit coordinate warnings.
