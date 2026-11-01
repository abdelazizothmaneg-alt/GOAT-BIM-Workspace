---
name: autodesk-cloud-platform
description: >-
  Architects and automates Autodesk Platform Services (APS - formerly Forge) cloud workflows, including Model Derivative API
  translation (SVF/SVF2), Data Management API (Hubs, Projects, Folders, Items), Design Automation API (cloud batch Revit/AutoCAD), and Webhooks.
---

# Autodesk Platform Services (APS / Forge)

Cloud-native architecture and API automation for Autodesk cloud platforms.

## 1. Authentication (OAuth 2.0)
* **2-Legged OAuth:** Server-to-server automation (`data:read`, `data:write`, `bucket:create`, `code:all`).
* **3-Legged OAuth:** User-delegated access with authorization code flow to interact with a user's Autodesk Construction Cloud (ACC) or BIM 360 hubs.

## 2. Core APS Services

### A. Model Derivative API
* Translate native design files (`.rvt`, `.dwg`, `.nwd`, `.ifc`, `.step`) into lightweight cloud-streamable format (`SVF` / `SVF2`) for web browser rendering in the Autodesk Viewer.
* Extract metadata trees, property hierarchies, and 2D/3D geometry bounds without installing desktop software.

### B. Data Management API
* Hierarchy navigation: `Hubs` $\rightarrow$ `Projects` $\rightarrow$ `TopFolders` $\rightarrow$ `Folders` $\rightarrow$ `Items` $\rightarrow$ `Versions`.
* Direct upload and download of multi-part files using signed S3 URLs.

### C. Design Automation API (Cloud Engines)
* Execute headless batch jobs running actual AutoCAD (`accoreconsole.exe`), Revit, or Inventor engines in cloud containers:
  * Run custom `.dll` or script to batch purge 100 Revit models overnight.
  * Automated generation of IFC and PDF deliverables upon commit.
