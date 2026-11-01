---
name: bim-5d-qto-boq-engine
description: Automates 5D BIM Quantity Takeoff (QTO) and Bill of Quantities (BOQ) extraction from Revit elements conforming to RICS NRM2, POMI, and CSI MasterFormat measurement rules, calculating net volumes, compound layer splits, and priced cost tables.
---

# BIM 5D QTO & BOQ Engine (أتمتة حصر الكميات والتسعير 5D طبقاً لـ NRM2)

A cost-engineering and 5D BIM automation skill that transforms 3D design models into rigorous, audited Bills of Quantities (BOQ). It implements international rules of measurement—including the **RICS New Rules of Measurement (NRM2)** and the **Principles of Measurement International (POMI)**—computing net quantities, executing opening deduction logic, decomposing layered compound structures, and binding **CSI MasterFormat** / **Uniclass 2015** cost items.

---

## 1. 5D QTO & BOQ Pipeline

```
[Autodesk Revit 3D BIM Model]
              │
              ▼
  [1. Element Classification & Cost Parameter Binding]
  - Map CSI MasterFormat / Uniclass codes (e.g. 03 30 00 Cast-in-Place Concrete)
  - Verify element phasing (Existing, New Construction, Demolished)
              │
              ▼
  [2. Layer Decomposition & Contact Surface Extraction]
  - Compound Walls: Separate Brickwork, Insulation, and Plaster volumes
  - Formwork Calculation: Net contact area subtracting beam/slab overlaps
              │
              ▼
  [3. NRM2 Deductions & Cessation Rules]
  - Small openings (< 0.5 m2): No deduction (measured solid)
  - Medium/Large openings (>= 1.0 m2): Fully deducted from masonry/concrete
              │
              ▼
  [4. Priced Bill of Quantities (BOQ) Assembly]
  - Apply Unit Rates (SAR / USD per m3, m2, linear meter, or item)
  - Export structured Excel BOQ workbooks with hierarchical WBS
```

---

## 2. Core Revit API Extraction Engine (.NET C#)

Snippet illustrating how to extract material layer quantities and decompose compound wall structures:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BimCostAutomation
{
    public class Nrm2QuantityExtractor
    {
        public static List<BoqItem> ExtractCompoundWallLayers(Document doc)
        {
            var boqItems = new List<BoqItem>();

            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Wall wall in walls)
            {
                WallType wType = doc.GetElement(wall.GetTypeId()) as WallType;
                if (wType == null) continue;

                // Query individual material layers
                ICollection<ElementId> materialIds = wall.GetMaterialIds(false);
                foreach (ElementId matId in materialIds)
                {
                    Material mat = doc.GetElement(matId) as Material;
                    if (mat == null) continue;

                    // Get precise computed net area and volume in metric units
                    double areaSqm = wall.GetMaterialArea(matId, false) * 0.092903;
                    double volumeM3 = wall.GetMaterialVolume(matId) * 0.0283168;

                    // Read custom cost parameters
                    string masterformatCode = mat.LookupParameter("MasterFormat_Code")?.AsString() ?? "09 00 00";
                    double unitRate = mat.LookupParameter("Unit_Rate_SAR")?.AsDouble() ?? 0.0;

                    boqItems.Add(new BoqItem
                    {
                        ElementId = wall.Id.Value,
                        Category = "Wall Finishes & Substrates",
                        MaterialName = mat.Name,
                        ClassificationCode = masterformatCode,
                        Quantity = Math.Round(areaSqm, 2),
                        Unit = "m2",
                        UnitRateSAR = unitRate,
                        TotalCostSAR = Math.Round(areaSqm * unitRate, 2)
                    });
                }
            }

            return boqItems;
        }
    }

    public class BoqItem
    {
        public long ElementId { get; set; }
        public string Category { get; set; }
        public string MaterialName { get; set; }
        public string ClassificationCode { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public double UnitRateSAR { get; set; }
        public double TotalCostSAR { get; set; }
    }
}
```

---

## 3. Standard Deduction Rules (NRM2 vs POMI)

| Building Element | Void Size $< 0.5\text{ m}^2$ | Void Size $0.5 - 1.0\text{ m}^2$ | Void Size $> 1.0\text{ m}^2$ |
| :--- | :--- | :--- | :--- |
| **Cast-in-Place Concrete** | Do not deduct | Deduct void volume | Deduct void volume |
| **Formwork Contact Area** | Do not deduct | Deduct contact face | Deduct contact face |
| **Blockwork Masonry** | Do not deduct | Do not deduct | Deduct net opening |
| **Wall Plaster & Paint** | Do not deduct | Do not deduct | Deduct net opening |

---

## 4. Key 5D Auditing Rules
1. **Uniclass / MasterFormat Coverage**: Flag any family or material instance with missing classification (`Unassigned_Code`).
2. **Net vs Gross Discrepancies**: Ensure drywall quantities deduct door and window punch-outs exceeding $1.0\text{ m}^2$.
3. **Formwork Intersections**: Subtract column top surfaces from slab soffits to prevent double-counting formwork area.
