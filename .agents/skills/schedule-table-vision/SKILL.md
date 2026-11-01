---
name: schedule-table-vision
description: >-
  Extracts architectural and structural schedule tables from images and screenshots (Wall Type Legends, Room Finish Schedules,
  Bar Bending Schedules). Decomposes layered wall compositions (materials, thicknesses in mm, structural functions) into structured JSON & Revit CompoundStructure data models.
---

# Schedule Table Vision & System Family Generator

Computer vision engine specialized in extracting structured layer hierarchies, material specifications, and thicknesses from architectural schedule images and legends (inspired by the BIM Pure AI-to-Revit workflow).

## 1. Wall Legend & Composite Structure Extraction

When provided with a screenshot or crop of a wall type schedule:
1. **Header & Key Identification:**
   * Extract Wall Type Code / Name (e.g., `EW-01`, `INT-WALL-150`, `PARTITION-TYPE-B`).
   * Identify overall wall thickness (e.g., $280\text{mm}$, $150\text{mm}$).
2. **Layer Deconstruction (Exterior $\rightarrow$ Interior):**
   * Parse individual material layers from exterior face to interior face.
   * Determine layer function based on standard Revit `MaterialFunctionAssignment`:
     * `Structure [1]`: Concrete, CMU block, light-gauge steel stud, CLT.
     * `Substrate [2]`: Plywood sheathing, cement board.
     * `Thermal/Air [3]`: Rigid insulation, mineral wool, air cavity/gap.
     * `Membrane [0]`: Vapor barrier, damp-proof membrane (zero thickness).
     * `Finish 1 [4]`: Exterior render, stone cladding, facade panels.
     * `Finish 2 [5]`: Interior gypsum board, plaster, paint, wall tiles.
3. **Dimensional Normalization:**
   * Extract layer thickness in millimeters and convert to Revit internal feet:
     $$\text{Thickness (ft)} = \frac{\text{Thickness (mm)}}{304.8}$$

---

## 2. Structured JSON Output Schema

```json
{
  "wall_type_name": "EXT_WALL_INSULATED_300MM",
  "total_thickness_mm": 300,
  "structural_material": "Concrete Masonry Units",
  "layers": [
    {
      "sequence": 1,
      "layer_side": "Exterior",
      "function": "Finish1",
      "function_id": 4,
      "material_name": "Exterior Cement Stucco",
      "thickness_mm": 20.0,
      "thickness_feet": 0.0656
    },
    {
      "sequence": 2,
      "layer_side": "Exterior",
      "function": "ThermalOrAir",
      "function_id": 3,
      "material_name": "Rigid Polyisocyanurate Insulation",
      "thickness_mm": 80.0,
      "thickness_feet": 0.2625
    },
    {
      "sequence": 3,
      "layer_side": "Core",
      "function": "Structure",
      "function_id": 1,
      "material_name": "Concrete Masonry Units",
      "thickness_mm": 180.0,
      "thickness_feet": 0.5906,
      "is_core": true
    },
    {
      "sequence": 4,
      "layer_side": "Interior",
      "function": "Finish2",
      "function_id": 5,
      "material_name": "Gypsum Plaster with Matte Paint",
      "thickness_mm": 20.0,
      "thickness_feet": 0.0656
    }
  ]
}
```

---

## 3. Direct Revit API C# Injection Pattern

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ORTools.Vision
{
    public static class WallTypeFromVisionBuilder
    {
        public static WallType CreateCompositeWallType(Document doc, string wallTypeName, List<CompoundStructureLayer> layers)
        {
            // Find existing basic wall type to duplicate
            WallType baseType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Find(wt => wt.Kind == WallKind.Basic);

            using (Transaction tx = new Transaction(doc, $"Create Wall Type: {wallTypeName}"))
            {
                tx.Start();
                WallType newType = baseType.Duplicate(wallTypeName) as WallType;
                CompoundStructure cs = newType.GetCompoundStructure();
                
                // Assign extracted layers
                cs.SetLayers(layers);
                newType.SetCompoundStructure(cs);
                
                tx.Commit();
                return newType;
            }
        }
    }
}
```
