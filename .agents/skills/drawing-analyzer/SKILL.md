---
name: drawing-analyzer
description: >-
  Analyzes and extracts structured engineering data from architectural, structural, and MEP drawings (PDF, DWG, raster blueprints).
  Parses Title Blocks, dimensions, door/window schedules, keynotes, grids, and symbol legends for QTO and automated BIM validation.
---

# Construction Drawing Analyzer

Computer vision and optical document analysis engine for converting 2D drawing sets and blueprints into structured machine-readable datasets.

## 1. Drawing Ingestion & Feature Extraction Pipeline

```text
[Drawing PDF / Raster Scan] ──> [Region Segmentation] ──> [OCR & Vector Extraction] ──> [Structured BIM/QTO Schema]
```

### A. Title Block & Metadata Parsing
Extract the standard ISO/AIA drawing management parameters:
* **Project Name & Code:** Primary project reference identifier.
* **Drawing Number & Title:** E.g., `A-101 GROUND FLOOR PLAN`, `S-201 COLUMN DETAILS`.
* **Revision Index & Date:** Current approval state (`Rev 02`, `2026-10-30`).
* **Scale Factor:** Numeric or graphic bar scale (`1:100`, `1:50`, `1/4" = 1'-0"`).
* **Discipline & Phase:** Architecture, Structure, MEP; Tender, IFC, As-Built.

### B. Annotation & Dimension Harvesting
* Parse linear dimensions with measurement values and extension line bounding boxes.
* Detect level datum callouts (e.g., `F.F.L. +3.60 m`, `S.S.L. +3.45 m`).
* Extract room boundary text and link Room Name to Room Number.

### C. Schedule & Legend Extraction (Schedules to Data)
* Extract tabular data from Door/Window Legends, Wall Type Schedules, and Finish Tables:
  * Capture Mark (`D-01`, `W-03`), Width $\times$ Height, Fire Rating, Glazing Type.
  * Structure into JSON/CSV format for automated Revit family generation or Excel BOQ population.

---

## 2. Symbol & Quantity Takeoff (QTO) Recognition
* **Element Counting:** Detect and enumerate standardized architectural/MEP symbols:
  * Sanitaryware (toilets, sinks, floor drains).
  * Electrical fixtures (switches, duplex outlets, panelboards, luminaire symbols).
  * Fire protection (sprinkler heads, smoke detectors, fire hose reels).
* **Grid Coordinate Mapping:** Map element locations to the nearest structural grid intersection (e.g., `Grid B-4 + 1.2m North`).
