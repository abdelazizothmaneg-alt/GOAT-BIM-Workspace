---
name: autodesk-bim-360
description: >-
  Manages and automates Autodesk BIM 360 & Autodesk Construction Cloud (ACC) workflows, including Docs document control,
  Model Coordination, Issues API lifecycle, RFIs, submittals, and Desktop Connector file synchronization.
---

# Autodesk BIM 360 & Construction Cloud (ACC)

Enterprise administration, document control, and automated coordination on Autodesk BIM 360 and ACC platforms.

## 1. Document Control & Folder Security (Docs)
* **Folder Permissions:** Enforce role-based access control (View only, View+Download, Edit, Full Admin) across discipline folders (e.g., `01_WIP`, `02_SHARED`, `03_PUBLISHED`, `04_ARCHIVED` in accordance with ISO 19650).
* **Automated Publishing & Cloud Worksharing:**
  * Coordinate Revit Cloud Worksharing (`Revit.CloudWorksharing`) publishing schedules to ensure live link models sync cleanly across teams.
* **Autodesk Desktop Connector Integration:**
  * Virtual drive mapping (`ACCDocs://`) for seamless CAD/DWG xrefs and linked IFC models.

## 2. Issues & RFIs API Integration
* **Automated Issue Tracking:**
  * Programmatically create and update coordination issues with Clash pinpoints, element handles, assigned contractors, root cause classification, and due dates.
* **Review Workflows & Approval Gates:**
  * Multi-step formal review workflows (1-step, 2-step, or 3-step approval) with automated email notifications and audit logs for tender submissions.
