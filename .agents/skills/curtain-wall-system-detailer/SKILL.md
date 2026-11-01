---
name: curtain-wall-system-detailer
description: Architects and engineers high-performance curtain wall systems, unitized and stick facades, thermal break insulation, structural silicone glazing (SSG), pressure equalization, and wind-load deflection calculations compliant with ASTM E330, AAMA 501, and EN 13830.
---

# Curtain Wall System Detailer

Production-grade engineering skill for architectural curtain wall systems, unitized panels, stick-built assemblies, structural silicone glazing (SSG), thermal breaks (polyamide strips), pressure equalization chambers, and structural deflection checks.

---

## 1. Curtain Wall Typologies & Detailing Standards

### A. Stick-Built System (نظام الواجهات المستمرة الميداني)
- **Assembly Method**: Mullions (verticals) anchored to slabs $\rightarrow$ Transoms (horizontals) connected via shear blocks $\rightarrow$ Vision glass / Spandrel panels inserted from exterior with pressure plates and snap-on covers.
- **Application**: Low-to-mid rise facades, complex geometries with variable bay widths.
- **Deflection Limits** (ASTM E330 / AAMA TIR A11):
  - Under design wind pressure:
    $$\delta_{\max} \le \min\left(\frac{L}{175}, 20\text{ mm}\right) \quad \text{for } L \le 4.1\text{ m}$$
    $$\delta_{\max} \le \min\left(\frac{L}{240} + 6.35\text{ mm}, 25\text{ mm}\right) \quad \text{for } L > 4.1\text{ m}$$

### B. Unitized / Pre-glazed System (نظام الواجهات الموحدة مسبقة الصنع)
- **Assembly Method**: Off-site factory fabrication of interlocking units (Male/Female split mullions and transoms) with integrated EPDM gaskets.
- **Installation**: Hoisted by mini-cranes or floor monorails, hung on 3-axis adjustable cast-in channel brackets (Halfen/Hilti).
- **Expansion & Movement**:
  - Horizontal movement: $\pm 3\text{ mm}$ to $\pm 6\text{ mm}$ per split mullion joint.
  - Vertical floor live load deflection: Stack joint accommodates up to $\pm 15\text{ mm}$ to $\pm 25\text{ mm}$.

---

## 2. Structural & Thermal Engineering Principles

### Thermal Break & Condensation Resistance
- **Polyamide 6.6 with 25% Glass Fiber (PA66 GF25)**:
  - Typical thermal strut depths: $24\text{ mm}$, $34\text{ mm}$, or $54\text{ mm}$.
  - Target system overall $U$-value: $U_{cw} \le 1.4\text{ W}/(\text{m}^2\cdot\text{K})$ (GCC / SBC 1001 requirements).
- **Condensation Resistance Factor (CRF)**:
  $$\text{CRF} \ge 65 \quad (\text{per AAMA 1503})$$

### Structural Silicone Glazing (SSG) Bite & Thickness
Per ASTM C1401:
- **Structural Bite ($B$)**:
  $$B = \frac{W_p \cdot W_{dim}}{2 \cdot f_d}$$
  - $W_p$: Design wind pressure ($\text{Pa}$ or $\text{N}/\text{m}^2$).
  - $W_{dim}$: Shorter span dimension of glass lite ($\text{m}$).
  - $f_d$: Allowable design dynamic stress of structural silicone (typically $138\text{ kPa} = 0.138\text{ N}/\text{mm}^2$).
  - *Minimum Bite*: $B \ge 6\text{ mm}$ ($1/4''$).
- **Glue Thickness ($T$)**:
  $$T = \frac{\Delta L_{diff}}{2 \cdot \epsilon_{allowable}} \ge 6\text{ mm}$$
  - Accommodates differential thermal expansion between aluminum and glass.

---

## 3. Revit API Curtain Wall Automation (C#)

```csharp
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CurtainWallDetailer
{
    public static class CurtainGridAutomation
    {
        public static void SubdivideCurtainWall(Document doc, Wall curtainWall, double verticalSpacingMeters, double horizontalSpacingMeters)
        {
            if (curtainWall.CurtainGrid == null)
                throw new InvalidOperationException("Selected wall does not contain a valid CurtainGrid.");

            using (Transaction t = new Transaction(doc, "Divide Curtain Wall Grid"))
            {
                t.Start();
                CurtainGrid grid = curtainWall.CurtainGrid;

                BoundingBoxXYZ bbox = curtainWall.get_BoundingBox(null);
                double height = bbox.Max.Z - bbox.Min.Z;
                double vStepFeet = UnitUtils.ConvertToInternalUnits(verticalSpacingMeters, UnitTypeId.Meters);
                double hStepFeet = UnitUtils.ConvertToInternalUnits(horizontalSpacingMeters, UnitTypeId.Meters);

                // Add Horizontal Grid Lines
                double currentZ = bbox.Min.Z + hStepFeet;
                while (currentZ < bbox.Max.Z - 0.5)
                {
                    XYZ pt = new XYZ(bbox.Min.X, bbox.Min.Y, currentZ);
                    grid.AddGridLine(false, pt, false);
                    currentZ += hStepFeet;
                }

                t.Commit();
            }
        }
    }
}
```

---

## 4. Quality & Compliance Deliverables

1. **Facade Shop Drawing Package**:
   - 1:5 & 1:2 scale detail sections through Stack Joint, Transom, and Base/Roof Parapet flashing.
   - 3-Way anchor connection detail (cast-in anchor channel, serrated washer, slotted T-bracket).
2. **Structural Calculation Dossier**:
   - Mullion bending moment $M = \frac{w L^2}{8}$ or two-span continuous analysis.
   - Wind load suction checks at building corners ($+1.0 / -1.8$ pressure coefficients per ASCE 7).
3. **Water Penetration & Air Infiltration Protocols**:
   - Dynamic water test per AAMA 501.1 ($720\text{ Pa}$ air pressure with wind stream).
   - Cyclic seismic drift test per AAMA 501.4 ($1.5\%$ to $2.5\%$ inter-story drift).
