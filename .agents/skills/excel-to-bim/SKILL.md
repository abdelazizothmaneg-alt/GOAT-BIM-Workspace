---
name: excel-to-bim
description: >-
  Automates the ingestion and parsing of structured Excel/CSV spreadsheets into native BIM elements, parameter values,
  room finish matrices, spatial coordinates, and quantity registers in Autodesk Revit and IFC models.
---

# Excel to BIM Automation

ETL (Extract, Transform, Load) pipeline for synchronizing tabular spreadsheet data into parametric Building Information Models.

## 1. Tabular Ingestion & Data Validation
* **Schema Matching:**
  * Map Excel column headers to native or Shared Parameter names (e.g., `Room_Number` $\rightarrow$ `ROOM_NUMBER`, `Floor_Finish` $\rightarrow$ `ROOM_FINISH_FLOOR`).
  * Enforce strict type conversions: Strings, Integers, Booleans (`1/0` or `Yes/No`), Lengths (convert metric millimeters to Revit decimal feet).
* **Identifier Keys:**
  * Use stable primary keys to bind rows to elements: `ElementId`, `UniqueId` (GUID), or compound functional keys (e.g., `Level-RoomNumber` or `Mark`).

## 2. Parameter Population & Element Creation
* **Batch Parameter Updating:**
  * Group updates into a single transaction to optimize document performance and avoid UI flickering.
  * Check `.IsReadOnly` status on parameters before calling `.Set()`.
* **Automated Element Placement from Coordinates:**
  * Ingest XYZ columns (e.g., pile foundations, lighting fixtures, MEP equipment, site survey points).
  * Instantiate family symbols at point coordinates: `doc.Create.NewFamilyInstance(xyzPoint, symbol, level, StructuralType.NonStructural)`.

## 3. Room & Space Finish Synchronization
* Automated reading of room finish schedules:
  * Extract Baseboard finish, Wall paint code (RAL), Ceiling type, and Acoustic panel specs.
  * Create or update Revit `Room` parameters to populate schedule sheets automatically without manual data entry.
