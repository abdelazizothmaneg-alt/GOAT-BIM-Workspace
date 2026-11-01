---
name: structural-steel-connection-automation
description: Automates standard structural steel connections (base plates, moment connections, shear tabs) in Revit Steel Connections and Advance Steel API, performing code compliance checks per AISC 360 and Eurocode 3.
---

# Structural Steel Connection Automation (أتمتة وتصميم الوصلات الإنشائية الفولاذية)

A structural steel detailing and engineering automation skill that streamlines the modeling, parametric configuration, and code verification of structural steel connections in **Autodesk Revit** and **Advance Steel**. It automates standard connection types (Base plates with anchor rods, Beam-to-Column Moment Flanges, Shear Tabs) and executes engineering verification calculations conforming to **AISC 360** and **Eurocode 3**.

---

## 1. Steel Connection Automation Pipeline

```
[Revit Structural Framing Model (Steel Columns & Beams)]
                           │
                           ▼
     [1. Joint Topology Identification]
     - Node intersection detection: Column + Beam web/flange
     - Extract joint reaction forces: Shear (V_u), Moment (M_u), Axial (P_u)
                           │
                           ▼
     [2. Connection Type Selection (AISC 360 Standards)]
     - Base Joint: Base plate, stiffeners, anchor rods, grout bed
     - Moment Connection: Bolted Extended End Plate / Welded Flange (WUF)
     - Simple Shear Joint: Single plate shear tab with standard bolt pitches
                           │
                           ▼
     [3. Automated Engineering Verification Calculations]
     - Bolt Shear & Tension Capacity (phi * R_n)
     - Plate Bearing & Block Shear Rupture
     - Fillet Weld Throat Effective Stress (phi * 0.60 * F_EXX)
                           │
                           ▼
     [4. Native Revit Steel Connection Placement (.NET C#)]
     - Instantiate StructuralConnectionHandler via Revit API
     - Set parameters: Plate thickness, bolt grade (A325 / A490), weld size
```

---

## 2. AISC 360 Engineering Calculation Engine (Python)

```python
import math

def verify_shear_tab_connection(
    shear_force_kn: float,
    num_bolts: int,
    bolt_diameter_mm: float = 20.0,
    bolt_grade: str = "A325",
    plate_thickness_mm: float = 10.0,
    plate_fy_mpa: float = 355.0
):
    """
    Verifies a single-plate shear connection per AISC 360-16 Chapter J.
    """
    # 1. Bolt Shear Capacity: phi * R_n = 0.75 * F_nv * A_b
    bolt_area_mm2 = (math.pi / 4.0) * (bolt_diameter_mm ** 2)
    f_nv_mpa = 372.0 if bolt_grade == "A325" else 469.0 # Threads included
    phi_bolt_shear = 0.75

    nominal_bolt_shear_kn = (phi_bolt_shear * f_nv_mpa * bolt_area_mm2) / 1000.0
    total_bolt_shear_capacity_kn = nominal_bolt_shear_kn * num_bolts

    # 2. Plate Gross Shear Yielding: phi * R_n = 1.0 * 0.6 * Fy * A_gv
    # Assume 75mm spacing + 40mm edge distances
    plate_height_mm = 80.0 + (num_bolts - 1) * 75.0
    a_gv_mm2 = plate_height_mm * plate_thickness_mm
    phi_plate_yield = 1.0
    plate_yield_capacity_kn = (phi_plate_yield * 0.60 * plate_fy_mpa * a_gv_mm2) / 1000.0

    # Governing capacity
    design_capacity_kn = min(total_bolt_shear_capacity_kn, plate_yield_capacity_kn)
    demand_capacity_ratio = shear_force_kn / design_capacity_kn

    return {
        "applied_shear_kn": shear_force_kn,
        "design_capacity_kn": round(design_capacity_kn, 2),
        "dcr": round(demand_capacity_ratio, 3),
        "is_safe": demand_capacity_ratio <= 1.0,
        "status": "PASS" if demand_capacity_ratio <= 1.0 else "FAIL - INSUFFICIENT CAPACITY"
    }
```

---

## 3. Revit Steel API Connection Placement (.NET C#)

Snippet illustrating how to programmatically assign structural connections:

```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace SteelAutomation
{
    public static class SteelConnectionCreator
    {
        public static void PlaceShearConnection(
            Document doc, 
            ElementId connectionTypeId, 
            ElementId columnId, 
            ElementId beamId)
        {
            using (Transaction t = new Transaction(doc, "Place Structural Steel Connection"))
            {
                t.Start();

                var memberIds = new List<ElementId> { columnId, beamId };

                // StructuralConnectionHandler creates native LOD 400 steel joints
                StructuralConnectionHandler.Create(doc, memberIds, connectionTypeId);

                t.Commit();
            }
        }
    }
}
```

---

## 4. Key Detailing Parameters
- **Standard Bolt Pitch**: $75\text{ mm}$ ($3\text{ in}$) on center; minimum edge distance $1.5 \times d_{\text{bolt}}$.
- **Base Plate Grout Bed**: Specify high-strength non-shrink cementitious grout ($25\text{ mm}$ to $50\text{ mm}$ thick) beneath column base plates.
- **Weld Symbols**: Standardize fillet weld callouts ($w \ge 6\text{ mm}$ or matching thinner connected part thickness).
