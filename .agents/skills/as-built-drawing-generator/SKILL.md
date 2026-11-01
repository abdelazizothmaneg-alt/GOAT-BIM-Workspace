---
name: as-built-drawing-generator
description: Converts site redline markups, surveyed point clouds, and field deviation logs into official contractual As-Built AutoCAD DWG drawing sets and updated Revit BIM models for project handover.
---

# As-Built Drawing Generator

Construction engineering and drafting skill for reconciling field redline markups, total station surveyed coordinates, and 3D laser scan data into certified As-Built drawings and record BIM models.

---

## 1. As-Built Reconciliation Standards (ISO 19650-3 / AIA Standards)

```
Field Survey / Site Redlines (Red/Green pen) ──> CAD/BIM Reconciliation ──> Delta Stamp & Title Block Update ──> Certified As-Built Handover
```

### Redline Drawing Color Conventions:
- **Red**: Additions, actual installed positions, new runs, or dimension changes.
- **Green**: Deletions, omitted elements, demolished partitions.
- **Blue**: Engineer notes, explanatory comments, or spec cross-references.
- **Black / Graphite**: Original design drawings (unchanged base geometry).

---

## 2. Drawing Title Block & Record Revisions

Every As-Built drawing must feature an official **As-Built Certification Stamp**:

```
┌────────────────────────────────────────────────────────┐
│               AS-BUILT RECORD DRAWING                  │
│  I hereby certify that this drawing represents an      │
│  accurate record of the actual conditions and work     │
│  constructed on site.                                  │
│                                                        │
│  CONTRACTOR: [Contractor Name]                         │
│  PROJECT MANAGER SIGNATURE: __________________________ │
│  SURVEYOR LICENSE NO: [XXXX-XXXX]                      │
│  DATE CERTIFIED: [YYYY-MM-DD]                          │
└────────────────────────────────────────────────────────┘
```

- **Revision Clouding**: In final As-Built packages, previous design revision clouds are wiped clean, or final delta symbol `[AB]` is assigned with status code `CR` (As-Built Record per ISO 19650).

---

## 3. Revit API As-Built Parameter & Revision Automation (C#)

```csharp
using System;
using Autodesk.Revit.DB;

namespace AsBuiltEngine
{
    public static class AsBuiltUpdater
    {
        /// <summary>
        /// Updates sheet parameters to reflect certified As-Built status.
        /// </summary>
        public static void ConvertSheetToAsBuilt(Document doc, ViewSheet sheet, string certifiedDate)
        {
            using (Transaction t = new Transaction(doc, "Convert Sheet to As-Built"))
            {
                t.Start();

                Parameter statusParam = sheet.LookupParameter("Sheet Status");
                if (statusParam != null && !statusParam.IsReadOnly)
                {
                    statusParam.Set("AS-BUILT RECORD");
                }

                Parameter certDateParam = sheet.LookupParameter("AsBuilt_Certified_Date");
                if (certDateParam != null && !certDateParam.IsReadOnly)
                {
                    certDateParam.Set(certifiedDate);
                }

                // Append AB suffix to Sheet Number if required by BEP
                // e.g. A-101 becomes A-101-AB
                if (!sheet.SheetNumber.EndsWith("-AB"))
                {
                    sheet.SheetNumber = sheet.SheetNumber + "-AB";
                }

                t.Commit();
            }
        }
    }
}
```

---

## 4. Deliverables Checklist
1. **Clean DWG As-Built Package**: Purged of unused layers, blocks, and external references bound as local inserts.
2. **Revit Record Model**: Updated to reflect actual MEP pipe invert levels and relocated partition walls.
3. **Surveyor Verification Dossier**: Total station point comparison report verifying structural column plumbness and slab levels.
