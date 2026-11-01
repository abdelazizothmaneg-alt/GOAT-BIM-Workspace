---
name: architectural-detailing-tectonics
description: Architectural detailing and tectonics master skill inspired by Carlo Scarpa and Peter Zumthor, covering shadow gaps (reveals), flush material transitions, brass expansion joints, recessed skirtings, and concealed joinery detailing.
---

# Architectural Detailing & Tectonics

Master-level architectural detailing and tectonic craft skill. Specializes in the poetry of construction joints, material transition interfaces, shadow gaps (reveals), recessed skirtings, and concealed connections.

---

## 1. The Tectonic Philosophy: "Separating to Connect"

In master architecture (from **Carlo Scarpa** to **Peter Zumthor** and **Mies van der Rohe**), two dissimilar materials (e.g., concrete and oak, stone and plaster) must never collide directly without a deliberate joint. Because materials expand, contract, and age at different rates, forcing a flush joint results in cracking, uneven gaps, or messy silicone caulk.

```
Messy Direct Joint (Slapped together):       Tectonic Shadow Gap (Articulated void):
  [ Plaster Wall ]                            [ Plaster Wall ]
         │ (Cracks & uneven line)                   │
  ───────┴────────                              ────┘  ┌──────── (Crisp 10x10mm reveal)
  [ Travertine Floor ]                                 │
                                                ───────┴────────
                                                [ Travertine Floor ]
```

---

## 2. Core Architectural Detail Typologies

### A. The Shadow Gap / Reveal (فواصل الظل - Reglet Joint)
- **Dimensions**: Uniform $10\text{ mm} \times 10\text{ mm}$ (or $12\text{ mm} \times 12\text{ mm}$) negative recess.
- **Applications**:
  - **Ceiling Perimeter**: Extruded aluminum plaster bead creates a floating ceiling effect, preventing corner hairline cracks.
  - **Recessed Baseboard (النعال الغاطس)**: Wall sits flush above an aluminum Z-profile, creating a clean recess instead of protruding wooden skirtings.
  - **Frameless Doors**: Secret door frames flush with plaster, separated by a crisp $4\text{ mm}$ perimeter reveal.

### B. Material Transitions: The Flush Metal Inlay
- When luxury hardwood meets large-format marble/porcelain:
  - Never use cheap plastic T-moldings.
  - Install a **$3\text{ mm}$ or $5\text{ mm}$ solid brushed brass (or blackened steel) flat bar**, anchored flush to the subfloor with structural epoxy.
  - Provides a durable expansion cushion while celebrating the threshold between zones.

### C. Façade Weathering & Drip Edges (مزاريب تنقيط المياه)
- Every stone/metal coping, window sill, and projecting slab must incorporate a **recessed drip groove (Reglet)**:
  - Width: $6\text{ mm}$, Depth: $6\text{ mm}$, located $20 - 25\text{ mm}$ behind the outer projecting edge.
  - Surface tension breaks rainwater drops, preventing dirty runoff from running down and staining the building facade.

---

## 3. Detail Tolerances & Fastener Concealment

| Component | Standard Tolerance | Craft Execution Rule |
| :--- | :--- | :--- |
| **Shadow Gap Width** | $\pm 1.0\text{ mm}$ | Use laser alignment and rigid extruded aluminum profiles (Fry Reglet / Pittcon). |
| **Tile Grout Joints** | $1.5 - 2.0\text{ mm}$ | Rectified porcelain with epoxy color-matched grout (Mapei Kerapoxy). |
| **Stone Cladding Joint** | $6.0 - 8.0\text{ mm}$ | Open ventilated rainscreen joints or recessed silicone with foam backer rod. |
| **Joinery Hardware** | Concealed | 3D-adjustable concealed hinges (Tectus / SOSS) and magnetic flush latches (Bonaiti). |

---

## 4. Revit / CAD Detail Section Generator (C# Revit API)

```csharp
using System;
using Autodesk.Revit.DB;

namespace ArchitecturalTectonics
{
    public static class ShadowGapDetailer
    {
        /// <summary>
        /// Embeds a parametric reveal (Sweep/Reveal) along wall base to model recessed skirting.
        /// </summary>
        public static void CreateRecessedSkirtingReveal(Document doc, Wall wall, ElementId revealTypeId)
        {
            using (Transaction t = new Transaction(doc, "Add Recessed Baseboard Reveal"))
            {
                t.Start();
                
                // WallReveal is typically created via WallSweep API or family instance placement
                WallSweepInfo sweepInfo = new WallSweepInfo(WallSweepType.Reveal, false);
                sweepInfo.Distance = UnitUtils.ConvertToInternalUnits(10.0, UnitTypeId.Millimeters); // 10mm from bottom
                sweepInfo.DistanceMeasuredFrom = DistanceMeasuredFrom.Base;
                
                WallSweep reveal = WallSweep.Create(wall, revealTypeId, sweepInfo);
                
                t.Commit();
            }
        }
    }
}
```

---

## 5. Architectural Detailing QA/QC Protocol
1. **Material Interface Audit**: Check that every boundary between two materials has an explicit detail drawing at 1:5 or 1:2 scale.
2. **Expansion Allowance**: Ensure that aluminum/glass assemblies have $\ge 3\text{ mm}$ thermal movement allowance per $3\text{ m}$ run.
3. **Hardware Invisibility**: Verify that no screw heads, hinge knuckles, or raw fasteners are visible on finished architectural surfaces.
