---
name: sustainability-energy-auditor
description: Audits building thermal envelopes, fenestration ratios, and electrical power densities against ASHRAE 90.1, SBC 1001, and LEED v4.1 standards, calculating U-values, WWR limits, Lighting Power Density (LPD), and Embodied Carbon.
---

# Sustainability & Energy Auditor (تدقيق كفاءة الطاقة والاستدامة)

An environmental engineering and green building compliance skill that evaluates architectural building envelopes and MEP systems against **ASHRAE 90.1 (Energy Standard for Buildings)**, the **Saudi Energy Conservation Code (SBC 1001)**, and **LEED v4.1**. It audits thermal transmittance ($U$-values), Window-to-Wall Ratios ($WWR$), Lighting Power Density ($LPD$), and Whole Life Embodied Carbon ($A1-A3$).

---

## 1. Prescriptive Energy Performance Framework

```
[Architectural & MEP BIM Model Data]
                   │
                   ▼
  [1. Climate Zone Classification (ASHRAE Climate Zone 1 through 8)]
  - GCC / Middle East: Predominantly Zone 1A (Very Hot - Humid) & 2B (Dry)
                   │
                   ▼
  ┌────────────────────────────────────────────────────────┐
  │ 2. Thermal Envelope Transmittance Audit (U-Values)    │
  │ - Roof U-value: Max <= 0.20 W/m2.K (SBC 1001)          │
  │ - Exterior Wall U-value: Max <= 0.34 W/m2.K            │
  │ - Glazing U-value: Max <= 1.80 W/m2.K                  │
  │ - Glazing SHGC: Max <= 0.25 (Solar Heat Gain Coeff)    │
  ├────────────────────────────────────────────────────────┤
  │ 3. Fenestration Limit (Window-to-Wall Ratio - WWR)     │
  │ - Prescriptive Maximum: WWR <= 40%                     │
  │ - Enhanced High-Performance Target: WWR <= 28-30%      │
  ├────────────────────────────────────────────────────────┤
  │ 4. Electrical Lighting Power Density (LPD)             │
  │ - Office Buildings: Max <= 6.5 W/m2 (0.61 W/sq.ft)     │
  │ - Retail / Commercial: Max <= 9.0 W/m2                 │
  └────────────────────────────────────────────────────────┘
                   │
                   ▼
  [3. Embodied Carbon Life Cycle Assessment (EN 15978)]
  - Quantify Concrete, Steel, Glass, and Insulation Volumes
  - Calculate kg CO2e / m2 GFA (LEED Carbon Reduction Credit)
```

---

## 2. Automated Thermal Envelope U-Value Calculator (Python)

```python
def calculate_assembly_u_value(layers: list, r_indoor_film: float = 0.13, r_outdoor_film: float = 0.04):
    """
    Computes total thermal transmittance U-value from material layers.
    layers = [{"material": "Limestone", "thickness_m": 0.03, "conductivity_w_mk": 1.3}, ...]
    """
    total_r = r_indoor_film + r_outdoor_film

    layer_breakdown = []
    for l in layers:
        r_layer = l["thickness_m"] / l["conductivity_w_mk"]
        total_r += r_layer
        layer_breakdown.append({
            "material": l["material"],
            "r_value": round(r_layer, 3)
        })

    u_value = 1.0 / total_r
    return {
        "total_r_value_m2k_w": round(total_r, 3),
        "overall_u_value_w_m2k": round(u_value, 3),
        "layers": layer_breakdown
    }

def audit_building_wwr(total_glazing_area_sqm: float, total_gross_wall_area_sqm: float, max_allowable_wwr: float = 0.40):
    """
    Audits fenestration against ASHRAE 90.1 Section 5.5.4.2 maximum WWR limit.
    """
    wwr = total_glazing_area_sqm / total_gross_wall_area_sqm
    is_compliant = wwr <= max_allowable_wwr

    return {
        "calculated_wwr_percent": round(wwr * 100.0, 1),
        "max_allowable_percent": round(max_allowable_wwr * 100.0, 1),
        "is_compliant": is_compliant,
        "recommendation": "Compliant with prescriptive path" if is_compliant else "Excessive glazing area. Reduce WWR or require whole-building energy cost simulation (Appendix G)."
    }
```

---

## 3. High-Value Sustainability Checklist
- **Solar Shading Coefficient**: For south and west-facing facades with $WWR > 25\%$, require horizontal overhangs ($PF \ge 0.5$) or kinetic timber mashrabiya screens.
- **Cool Roof Solar Reflectance Index (SRI)**: Low-slope roofs must achieve minimum $\text{SRI} \ge 82$ (aged 3-year value $\ge 64$) to combat Urban Heat Island effects.
