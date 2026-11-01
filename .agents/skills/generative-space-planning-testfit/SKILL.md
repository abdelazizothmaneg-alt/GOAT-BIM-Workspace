---
name: generative-space-planning-testfit
description: Generates algorithmic architectural floor plans, apartment unit test-fits, and commercial office layouts from boundary polygons using topological adjacency graphs, daylight optimization, and corridor circulation minimization.
---

# Generative Space Planning Test-Fit (التوزيع الفراغي التوليدي وتخطيط المساقط)

A computational architectural design and generative layout skill. It automates apartment unit partitioning, office test-fits, and residential villa zoning from boundary boundary polylines. Utilizing **Topological Adjacency Graphs**, Voronoi relaxation, and genetic optimization algorithms, it maximizes daylight exposure for living areas while restricting circulation corridor loss to under $12\%$.

---

## 1. Generative Space Planning Pipeline

```
[Building Outer Boundary Polygon & Structural Core]
                        │
                        ▼
   [1. Spatial Program Matrix & Adjacency Weights]
   - Room Typologies: Living, Kitchen, Master Bed, Secondary Beds, Baths
   - Adjacency Graph: Living <-> Balcony (1.0), Kitchen <-> Dining (0.9)
   - Privacy Buffer: Master Bed <-> Guest Majlis (Acoustic Separation)
                        │
                        ▼
   [2. Boundary Subdivision & Topological Graph Solver]
   - Place central spine circulation corridor (Minimizing travel path)
   - Partition perimeter envelope for maximum fenestration daylight
   - Treemap rectangular partitioning / Voronoi cell relaxation
                        │
                        ▼
   [3. Architectural Regularization & Orthogonal Snapping]
   - Snap diagonal partition walls to strict 90° orthogonal angles
   - Enforce minimum room widths: Master Bed >= 3.6m, Bath >= 1.5m
                        │
                        ▼
   [4. Native Revit Instantiation (.NET C#)]
   - Place interior partition walls: Wall.Create(doc, curve, wallTypeId...)
   - Instantiate Rooms and Room Tags: doc.Create.NewRoom(level, UV)
```

---

## 2. Program Adjacency Matrix & Layout Solver (Python)

```python
import numpy as np

class SpaceProgramSolver:
    def __init__(self, total_area_sqm: float):
        self.total_area = total_area_sqm
        # Program distribution for a typical 3-Bedroom Luxury Apartment
        self.target_program = {
            "Living_Dining": {"ratio": 0.32, "needs_daylight": True, "min_width_m": 4.5},
            "Master_Bedroom": {"ratio": 0.20, "needs_daylight": True, "min_width_m": 3.8},
            "Bedroom_02": {"ratio": 0.14, "needs_daylight": True, "min_width_m": 3.4},
            "Bedroom_03": {"ratio": 0.12, "needs_daylight": True, "min_width_m": 3.2},
            "Kitchen": {"ratio": 0.10, "needs_daylight": True, "min_width_m": 2.8},
            "Bathrooms_WC": {"ratio": 0.08, "needs_daylight": False, "min_width_m": 1.6},
            "Corridor_Circulation": {"ratio": 0.04, "needs_daylight": False, "min_width_m": 1.2}
        }

    def evaluate_layout_efficiency(self, generated_rooms: list, boundary_area: float) -> dict:
        """
        Computes Net-to-Gross efficiency and circulation ratio.
        """
        net_usable_area = sum(r["area_sqm"] for r in generated_rooms if "Corridor" not in r["name"])
        circulation_area = sum(r["area_sqm"] for r in generated_rooms if "Corridor" in r["name"])

        efficiency_ratio = net_usable_area / boundary_area
        circ_percentage = (circulation_area / boundary_area) * 100.0

        return {
            "net_usable_area_sqm": round(net_usable_area, 2),
            "circulation_area_sqm": round(circulation_area, 2),
            "efficiency_ratio": round(efficiency_ratio, 3),
            "circulation_percent": round(circ_percentage, 1),
            "is_efficient": circ_percentage <= 12.0 # Target: <= 12% lost in circulation
        }
```

---

## 3. Direct Revit API Generation (.NET C#)

Snippet illustrating how to take the solved room boundary curves and spawn native Revit partition walls and rooms:

```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace GenerativeLayoutAutomation
{
    public static class LayoutBuilder
    {
        public static void BuildPartitionWalls(
            Document doc, 
            Level level, 
            WallType interiorWallType, 
            IEnumerable<Curve> wallCurves)
        {
            using (Transaction t = new Transaction(doc, "Generate Architectural Layout"))
            {
                t.Start();
                double ceilingHeightFeet = 3000.0 / 304.8; // 3.0 meters

                foreach (Curve curve in wallCurves)
                {
                    Wall.Create(
                        doc,
                        curve,
                        interiorWallType.Id,
                        level.Id,
                        ceilingHeightFeet,
                        0.0,
                        false,
                        false
                    );
                }

                t.Commit();
            }
        }
    }
}
```

---

## 4. Architectural Rules of Thumb
- **Wet Area Grouping (Plumbing Stacks)**: Bathrooms and kitchens must cluster along shared vertical MEP plumbing shafts to avoid horizontal drainage runs across living room ceilings.
- **Aspect Ratio Limits**: Rooms should maintain aspect ratios between $1.0:1.0$ and $1.0:1.6$ to prevent tunnel-like, unlivable spaces.
