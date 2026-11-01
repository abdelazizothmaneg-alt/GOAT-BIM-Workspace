---
name: axonometric-diagram-architect
description: Generates exploded axonometric diagrams, programmatic zoning massing, circulation flows, and bioclimatic strategy diagrams from 3D Revit models, Rhino geometry, and vector drafting tools.
---

# Axonometric Diagram Architect (توليد الدياجرامات الكتلية والمعمارية المفككة)

A visual architectural storytelling skill dedicated to creating world-class concept diagrams, exploded axonometrics, programmatic stacking matrices, and dynamic circulation schemes (in the distinctive stylistic language of leading global practices like BIG, OMA, and MVRDV) directly from 3D Revit and Rhino/Grasshopper geometry.

---

## 1. Diagram Typologies

```
┌─────────────────────────────────┬─────────────────────────────────┐
│ 1. Form-Finding Evolution       │ 2. Exploded Axonometric         │
│ (Box -> Carve -> Twist -> Terr) │ (Vertical Separation of Floors) │
├─────────────────────────────────┼─────────────────────────────────┤
│ 3. Programmatic Stacking Matrix │ 4. Bioclimatic Strategy Vector  │
│ (Color-coded functional zoning) │ (Sun angles, prevailing winds)  │
└─────────────────────────────────┴─────────────────────────────────┘
```

---

## 2. Programmatic Revit Displaced Views (.NET C#)

Snippet illustrating how to programmatically separate floor levels vertically in an axonometric 3D view using the Revit API:

```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace DiagramAutomation
{
    public static class ExplodedAxoCreator
    {
        public static void ExplodeFloorLevels(Document doc, View3D isoView, double zSeparationFeet)
        {
            using (Transaction t = new Transaction(doc, "Create Exploded Axonometric View"))
            {
                t.Start();

                // Group elements by Level
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                int multiplier = 0;
                foreach (var lvl in levels)
                {
                    // Collect elements hosted on this level
                    ElementLevelFilter lvlFilter = new ElementLevelFilter(lvl.Id);
                    var elementIds = new FilteredElementCollector(doc, isoView.Id)
                        .WherePasses(lvlFilter)
                        .WhereElementIsNotElementType()
                        .ToElementIds();

                    if (elementIds.Count > 0)
                    {
                        XYZ displacementVector = new XYZ(0, 0, multiplier * zSeparationFeet);
                        
                        // Create Revit DisplacementElement
                        DisplacementElement.Create(doc, isoView, elementIds, displacementVector);
                    }
                    multiplier++;
                }

                t.Commit();
            }
        }
    }
}
```

---

## 3. Graphic Style Rules (The "BIG" Diagram Aesthetics)
1. **Line Weight**: Use crisp black outlines ($0.35\text{ mm}$ for silhouette edges, $0.18\text{ mm}$ for inner plane intersections).
2. **Color Palette**: Muted pastel / flat tints with subtle $10 - 15\%$ shadows (ambient occlusion style).
   - Residential: Warm Ochre / Terracotta (`#E07A5F`)
   - Commercial / Retail: Vivid Teal (`#3D405B` / `#81B29A`)
   - Public / Cultural: Saffron Yellow (`#F2CC8F`)
   - Landscape / Greenery: Sage Green (`#84A98C`)
3. **3D Arrows & Vectors**: Use bold isometric arrows with drop shadows to illustrate pedestrian access, vehicle loops, and natural ventilation cross-drafts.
