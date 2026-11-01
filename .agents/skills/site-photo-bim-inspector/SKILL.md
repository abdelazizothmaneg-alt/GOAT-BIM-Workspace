---
name: site-photo-bim-inspector
description: Analyzes construction site photos, 360-degree camera captures, and drone progress imagery using computer vision to detect construction trades, track structural and MEP progress against scheduled 4D BIM milestones, verify material installations, and flag site deviations or safety non-conformances.
---

# Site Photo BIM Inspector (فحص صور الموقع والتحقق الإنشائي الميداني)

A specialized site inspection and computer vision skill that ingests construction progress photos (smartphone photos, 360-degree Ricoh Theta/Insta360 captures, drone survey orthophotos), recognizes active trade activities (formwork, rebar, blockwork, MEP ductwork/piping, drywall), and matches visual progress against 4D BIM schedules and Autodesk Revit model elements.

---

## 1. Inspection Framework

```
[Site Photo / 360° Panorama / Drone Capture]
                      │
                      ▼
   [1. Spatial Alignment & Pose Estimation]
   - Camera position & orientation matched to Revit 3D camera viewpoint
   - Grid line & structural column triangulation
                      │
                      ▼
   [2. Multi-Trade Semantic Segmentation]
   ┌──────────────────────────────────────────────┐
   │ Structural: Concrete columns, slabs, rebar   │
   │ Architecture: CMU blockwork, AAC, drywall    │
   │ MEP: Ductwork, cable trays, sprinkler pipes  │
   │ Finishes: Plaster, screed, tiles, ceilings   │
   └──────────────────────────────────────────────┘
                      │
                      ▼
   [3. 4D BIM Milestone Verification & QA/QC]
   - Compare physical installation % vs scheduled phase
   - Detect missing sleeves / penetrations before concrete pour
   - Flag out-of-tolerance deviations
                      │
                      ▼
   [4. Automated ACC / BIM 360 Issue Creation & Revit Parameter Sync]
   - Update Revit Element Parameters: "Site_Status = Installed"
   - Publish non-conformance issues to Autodesk Construction Cloud
```

---

## 2. Construction Trade Visual Detection Classes

| Category | Visual Detection Features | BIM Model Link |
| :--- | :--- | :--- |
| **Structural Frame** | Plywood formwork, steel rebar cages, exposed concrete columns/beams | `OST_StructuralColumns`, `OST_StructuralFraming` |
| **Masonry / Envelope** | Grey concrete masonry units (CMU), white AAC blocks, mortar bed joints | `OST_Walls` (Blockwork partitions) |
| **HVAC Mechanical** | Rectangular galvanized sheet metal ducts, circular spiral ducts, insulation wrap | `OST_DuctCurves`, `OST_DuctFitting` |
| **Electrical / Low Current** | Perforated metal cable trays, PVC conduit bundles, electrical DB panels | `OST_CableTray`, `OST_Conduit` |
| **Plumbing & Fire** | Red painted steel fire sprinkler pipes, orange/white PVC drainage pipes | `OST_PipeCurves` |
| **Interior Finishes** | Light-gauge steel C-studs, gypsum boards, acoustic ceiling T-grid | `OST_Ceilings`, `OST_Walls` (Drywall) |

---

## 3. Discrepancy & Non-Conformance Rules (QA/QC)

The inspector automatically detects critical construction discrepancies:
1. **Poured Concrete Without MEP Sleeves**: Slab or beam rebar installed, but required conduit/pipe sleeves shown in the BIM model are absent prior to pouring.
2. **Missing Expansion / Control Joints**: Long masonry walls ($> 6\text{ m}$) lacking vertical expansion foam joints according to SBC / ASTM C90.
3. **Fire-Stopping Omission**: MEP pipes or cable trays penetrating fire-rated partition walls without intumescent firestop collars or sealants.
4. **Site Safety (HSE)**: Unprotected floor openings, absent guardrails at slab perimeters, workers without mandatory PPE (hard hat, reflective vest).

---

## 4. Revit Parameter Synchronization (.NET C#)

Snippet illustrating how to update Revit element parameters based on vision inspection results:

```csharp
using System;
using Autodesk.Revit.DB;

namespace BimVisionAutomation
{
    public static class SiteInspectionUpdater
    {
        public static void UpdateElementSiteProgress(
            Document doc,
            ElementId elementId,
            string inspectionStatus, // e.g. "Verified_Installed", "Delayed", "Discrepancy_Found"
            double installedPercentage,
            string photoReferenceUrl)
        {
            Element elem = doc.GetElement(elementId);
            if (elem == null) return;

            using (Transaction trans = new Transaction(doc, "Update Site Progress Parameter"))
            {
                trans.Start();

                // Update custom project parameters
                Parameter statusParam = elem.LookupParameter("Site_Verification_Status");
                if (statusParam != null && !statusParam.IsReadOnly)
                {
                    statusParam.Set(inspectionStatus);
                }

                Parameter percentParam = elem.LookupParameter("Site_Progress_Percent");
                if (percentParam != null && !percentParam.IsReadOnly)
                {
                    percentParam.Set(installedPercentage);
                }

                Parameter photoParam = elem.LookupParameter("Site_Photo_URL");
                if (photoParam != null && !photoParam.IsReadOnly)
                {
                    photoParam.Set(photoReferenceUrl);
                }

                trans.Commit();
            }
        }
    }
}
```

---

## 5. Autodesk Construction Cloud (ACC) Issues API Dispatch

When a discrepancy is flagged, an issue payload is sent to ACC / BIM 360:

```json
{
  "title": "Missing MEP Wall Penetration Sleeve - Axis D-2",
  "description": "Site photo at Level 1 reveals CMU blockwork completed without 150mm PVC sleeve for rainwater downpipe indicated in Plumbing BIM Model.",
  "issueTypeId": "quality_non_conformance",
  "status": "open",
  "priority": "high",
  "assignedTo": "Subcontractor - MEP Division",
  "dueDate": "2026-11-05"
}
```
