---
name: standards-compliance-checker
description: >-
  Automated construction and BIM data compliance engine checking model integrity against ISO 19650 naming conventions,
  IFC4 schema standards, Level of Information Need (LOIN), and national construction classification systems (OmniClass/UniFormat).
---

# Construction & BIM Standards Compliance Checker

Automated data auditing and compliance verification framework for digital construction, BIM models, and technical specifications.

## 1. ISO 19650 Naming Convention Validator
Validate file and model container metadata against the standard formula:
```text
[PROJECT]-[ORIGINATOR]-[VOLUME/SYSTEM]-[LEVEL]-[TYPE]-[ROLE]-[NUMBER]
Example: PRJ01-HMF-ZZ-01-M3-A-0001.rvt
```
* **Project:** 2 to 6 character project code.
* **Originator:** 3 to 6 character company code.
* **Volume/Zone:** `ZZ` (All zones) or specific zone code (`Z1`, `Z2`).
* **Level:** `00` (Ground), `01` (First floor), `M1` (Mezzanine), `B1` (Basement), `ZZ` (Multiple).
* **Type:** `M3` (3D Model), `DR` (Drawing), `SP` (Specification), `BO` (Bill of Quantities).
* **Role:** `A` (Architect), `S` (Structural), `M` (Mechanical), `E` (Electrical), `C` (Civil).
* **Number:** 4 to 6 digit sequential numbering.

## 2. Model Data & Parameter Auditing
* **Unassigned Parameters:** Ensure mandatory attributes (`OmniClassTitle`, `UniFormatNumber`, `Manufacturer`, `FireRating`) are not null or whitespace.
* **Level & Grid Association:** Elements must be hosted to verified structural levels; flag orphaned elements hosted to invalid reference planes.
* **Geometric Clashes & Tolerances:**
  * Hard Clash: Structural column intersecting MEP ductwork ($> 0\text{mm}$).
  * Clearance / Soft Clash: Electrical panel maintenance clear zone ($900\text{mm}$ deep envelope).

## 3. IFC OpenBIM Export Compliance
* Verify entity mapping: Architectural walls mapped to `IfcWall` / `IfcWallStandardCase`, doors to `IfcDoor`.
* Ensure Psets (Property Sets) adhere to `Pset_WallCommon`, `Pset_DoorCommon`, and custom client-specified Information Delivery Manuals (IDM).
