---
name: dwg-to-excel
description: >-
  Extracts architectural and engineering quantities from AutoCAD DWG/DXF drawings directly into structured Excel spreadsheets,
  aggregating block counts, attribute tags (ATTRIB), polyline areas, linear pipe/duct runs, and text annotations for QTO and BOQ.
---

# DWG to Excel Data Extraction (QTO & Takeoffs)

Automated Quantity Takeoff (QTO) pipeline for parsing CAD geometry and attributes into structured Microsoft Excel spreadsheets.

## 1. Extraction Pipeline Architecture
```text
[AutoCAD DWG/DXF] ──> [Filter Layers/Entities] ──> [Extract Attributes & Metrics] ──> [Aggregate Data] ──> [Formatted Excel (.xlsx)]
```

## 2. Entity Extraction Rules

### A. Block References & Attribute Tags (Block Count Schedules)
* Extract block insertion name (e.g., `DOOR_SINGLE_900`, `VALVE_GATE_100`, `DIFFUSER_4WAY`).
* Extract dynamic block properties and embedded attribute tags (`MARK`, `WIDTH`, `HEIGHT`, `FIRE_RATING`, `MANUFACTURER`).
* Aggregate identical blocks by layer and floor to generate automated furniture, door, and equipment schedules.

### B. Closed Polylines (Area Schedules)
* Scan layers designated for room finishes or zoning (e.g., `A-AREA`, `A-FLOR-PATT`).
* Calculate true planar area:
  $$\text{Area (m}^2\text{)} = \frac{\text{Polyline Area (mm}^2\text{)}}{1,000,000}$$
* Correlate polyline boundaries with interior text labels to pair Room Name with exact measured area.

### C. Linear Elements (Pipe, Cable Tray & Duct Run Takeoffs)
* Sum total length of centerlines per layer (`M-DUCT-SPLY`, `E-CABL-TRAY`, `P-PIPE-DOMW`).
* Automatically calculate fitting allowances ($10 - 15\%$ standard multiplier) and export detailed material takeoffs.

## 3. Structured Excel Output Formatting
* Generate multi-tab workbooks:
  * Tab 1: **Summary & KPIs** (Total Gross Area, Total Door Count, Total Linear MEP Runs).
  * Tab 2: **Door & Window Schedule** (Mark, Block Name, Dimensions, Count).
  * Tab 3: **Spatial Area Takeoff** (Level, Room Tag, Calculated Area $m^2$, Perimeter $m$).
  * Tab 4: **Linear MEP Takeoff** (Service Type, Layer Name, Total Length $m$).
