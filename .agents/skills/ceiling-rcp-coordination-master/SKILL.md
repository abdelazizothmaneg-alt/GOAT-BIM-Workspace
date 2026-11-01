---
name: ceiling-rcp-coordination-master
description: Coordinates multi-disciplinary Reflected Ceiling Plans (RCP), aligning architectural lighting grids with HVAC linear slot diffusers, NFPA 13 fire sprinklers, smoke detectors, and concealed access hatches.
---

# Ceiling & Reflected Ceiling Plan (RCP) Coordination Master

Interior architectural engineering skill for masterminding coordinated Reflected Ceiling Plans (RCP), resolving multi-trade collisions between architectural lighting, mechanical air distribution, fire protection, and maintenance access.

---

## 1. The RCP Coordination Hierarchy

The ceiling is the most congested visual surface in an interior. Lack of coordination results in randomly scattered diffusers, off-center downlights, and unsightly plastic access hatches.

```
Priority 1: Life Safety (NFPA 13 Fire Sprinklers & Smoke Detectors)
   └── Priority 2: Architectural Datum & Lighting Grid (Downlights, Pendants, Coves)
         └── Priority 3: Mechanical HVAC (Linear slot diffusers, Return grilles)
               └── Priority 4: Maintenance Access Panels (Concealed flush frameless hatches)
```

---

## 2. Multi-Trade Integration Rules

### A. Lighting & HVAC Integration
- **Linear Slot Diffusers (LSD)**:
  - Integrate continuous linear supply diffusers inside perimeter curtain pockets or architectural ceiling steps to keep central gypsum ceilings clean.
  - Paint interior plenum duct boxes matte black (`#000000`) so internal sheet metal is invisible from below.

### B. Fire Sprinkler Clearances (NFPA 13)
- Maintain standard coverage spacing: Maximum distance between sprinkler heads $\le 4.6\text{ m}$ ($15\text{ ft}$).
- **Obstruction Rule**: Sprinkler heads must maintain minimum **$450\text{ mm}$ clear horizontal distance** from projecting suspended light fixtures or ceiling bulkheads to prevent spray pattern shadowing.
- Always center sprinkler heads within ceiling grid tiles or align axially with recessed downlight centerlines.

### C. Concealed Access Hatches (فتحات الصيانة المخفية)
- Never use surface-mounted plastic or visible-flange aluminum hatches in luxury projects.
- Specify **frameless recessed gypsum access panels** with touch-latch magnetic push releases, clad in the exact same paint/veneer finish as the surrounding ceiling plane.

---

## 3. Ceiling Heights, Pockets & Coves (Typologies)

| Feature | Standard Dimensions | Functional Detail |
| :--- | :--- | :--- |
| **Drapery / Curtain Pocket** | $200\text{ mm}$ wide $\times 150 - 200\text{ mm}$ recessed | Accommodates dual motorized tracks (sheer + blackout) + 24V power feed. |
| **Perimeter Light Cove** | $150\text{ mm}$ drop, $150\text{ mm}$ recess lip | LED strip tilted at $45^\circ$ facing ceiling to cast soft gradient wash without hotspot. |
| **Minimum Corridor Clear Height** | $\ge 2400\text{ mm}$ | Houses congested main MEP horizontal distribution trunks above. |
| **Executive Office Clear Height** | $2800\text{ mm} - 3200\text{ mm}$ | Ceiling dropped only where necessary to maximize perceived volume. |

---

## 4. Revit API RCP Coordination Auditor (C#)

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RCPCoordinator
{
    public static class CeilingElementAuditor
    {
        /// <summary>
        /// Audits lighting fixtures to verify they are properly hosted to ceiling planes.
        /// </summary>
        public static List<string> CheckUnattachedLights(Document doc)
        {
            List<string> unhostedWarnings = new List<string>();
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .WhereElementIsNotElementType();

            foreach (FamilyInstance light in collector)
            {
                if (light.Host == null || !(light.Host is Ceiling))
                {
                    unhostedWarnings.Add($"Light Fixture [ID: {light.Id}] is not hosted to a valid Ceiling!");
                }
            }
            return unhostedWarnings;
        }
    }
}
```

---

## 5. Deliverables Checklist
1. **Fully Coordinated Reflected Ceiling Plan (1:50 scale)**: Layered drawing with distinct symbols for lighting (keyed to circuit switches), HVAC grilles, sprinklers, and access panels.
2. **Ceiling Section Details (1:10 / 1:5 scale)**: Sections through curtain pockets, perimeter shadow reveals, and recessed lighting troffers.
