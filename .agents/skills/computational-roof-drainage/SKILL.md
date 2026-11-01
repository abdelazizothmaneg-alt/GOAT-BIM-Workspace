---
name: computational-roof-drainage
description: Calculates and models siphonic and gravity roof drainage systems, parapet overflow scuppers, gutter sizing, and detention storage capacity conforming to EN 12056-3, ASPE Data Book 2, and International Plumbing Code (IPC).
---

# Computational Roof Drainage

Specialized engineering skill for calculating, sizing, and modeling high-capacity gravity and siphonic roof drainage networks, secondary emergency overflow scuppers, flat roof ponding prevention, and rainfall runoff management.

---

## 1. Rainfall Runoff & Design Standards

### Rational Runoff Formula (EN 12056-3 / IPC Chapter 11)
$$Q = \frac{C \cdot I \cdot A}{3600}$$
- $Q$: Total stormwater runoff rate ($\text{L}/\text{s}$).
- $C$: Runoff coefficient (Flat concrete / PVC membrane = $0.90 - 0.95$; Extensive green roof = $0.30 - 0.50$).
- $I$: Design rainfall intensity ($\text{mm}/\text{h}$):
  - Primary Gravity Drainage: 10-year or 25-year return period ($I_{25}$).
  - Secondary Emergency Overflow: 100-year return period, 15-minute duration ($I_{100, 15\text{min}}$).
- $A$: Effective horizontal catchment area ($\text{m}^2$), including 50% of wind-driven vertical wall projection:
  $$A = A_{\text{roof}} + 0.5 \sum A_{\text{vertical\_wall}}$$

---

## 2. Gravity vs. Siphonic System Comparison

| Parameter | Gravity Drainage (نظام الانسياب بالجاذبية) | Siphonic Drainage (نظام السحب السيفوني) |
| :--- | :--- | :--- |
| **Air/Water Ratio** | Air core inside pipe (fill ratio $h/d \approx 0.33 - 0.50$) | $100\%$ full-bore liquid water flow without air entrainment |
| **Pipe Diameter** | Large diameter pipes ($150\text{ mm} - 300\text{ mm}$) | Compact pipes ($40\text{ mm} - 110\text{ mm}$) |
| **Pipe Slope** | Requires continuous slope ($1.0\% - 2.0\%$) | $0\%$ slope (completely horizontal collector manifolds) |
| **Outlets** | Standard dome/flat strainer | Anti-vortex baffle plate preventing air cavitation |
| **Negative Pressure** | Atmospheric | High suction ($-\Delta P$ up to $-0.8\text{ bar}$) |

### Siphonic Energy Equation (Bernoulli Principle)
$$\Delta H_{\text{total}} = \sum h_f + \sum h_m + \frac{v^2}{2g}$$
- Driving head $\Delta H_{\text{total}}$ equals height from roof outlet to discharge tail-pipe break.
- Head losses $\sum h_f$ calculated using Darcy-Weisbach or Colebrook-White equation.

---

## 3. Emergency Parapet Overflow Scupper Sizing

Per IBC / SBC Plumbing codes, overflow scuppers must handle $100\%$ of the $I_{100}$ rainfall if all primary drains are completely blocked.

### Broad-Crested Rectangular Weir Formula
$$Q_w = \frac{2}{3} \cdot C_d \cdot B \cdot \sqrt{2g} \cdot H^{1.5}$$
- $Q_w$: Scupper discharge capacity ($\text{m}^3/\text{s}$).
- $C_d$: Discharge coefficient ($\approx 0.60 - 0.62$).
- $B$: Clear width of scupper opening ($\text{m}$).
- $H$: Hydraulic head above scupper invert ($\text{m}$).
- **Freeboard**: Minimum $50\text{ mm}$ clearance between maximum water ponding depth and roof waterproof membrane top termination.

---

## 4. Python Automation: Roof Catchment & Scupper Sizing

```python
"""
Roof Drainage & Overflow Scupper Sizing Engine
Compliant with EN 12056-3 and IPC Chapter 11
"""

import math

def calculate_roof_runoff(area_sqm: float, wall_vertical_area_sqm: float, intensity_mm_hr: float, c_factor: float = 0.95) -> float:
    effective_area = area_sqm + (0.5 * wall_vertical_area_sqm)
    # Q in L/s
    q_liters_sec = (c_factor * intensity_mm_hr * effective_area) / 3600.0
    return round(q_liters_sec, 2)

def size_rectangular_scupper(q_liters_sec: float, max_head_mm: float = 75.0, c_d: float = 0.62) -> float:
    """
    Calculates minimum scupper width (B in meters) for broad-crested weir.
    """
    q_m3_s = q_liters_sec / 1000.0
    h_m = max_head_mm / 1000.0
    g = 9.81
    
    # Q = (2/3) * Cd * B * sqrt(2g) * H^1.5
    constant_factor = (2.0 / 3.0) * c_d * math.sqrt(2 * g) * (h_m ** 1.5)
    width_m = q_m3_s / constant_factor
    return round(width_m * 1000.0, 1) # Return in mm

if __name__ == "__main__":
    roof_area = 1500.0 # m^2
    surrounding_walls = 200.0 # m^2
    rainfall_i100 = 120.0 # mm/hr (Saudi / GCC flash storm peak)
    
    q_total = calculate_roof_runoff(roof_area, surrounding_walls, rainfall_i100)
    scupper_width = size_rectangular_scupper(q_total, max_head_mm=60.0)
    
    print(f"Total Peak Runoff Rate: {q_total} L/s")
    print(f"Required Total Scupper Clear Width at 60mm Head: {scupper_width} mm")
```

---

## 5. Deliverables Checklist
1. **Hydraulic Calculation Report**: Primary outlet counts, collector pipe diameters, flow velocities ($\ge 1.0\text{ m}/\text{s}$ for self-cleansing).
2. **Revit MEP Piping Model**: Sloped/horizontal drainage runs, roof drain families with MEP connector flow parameters.
3. **Parapet Detail Sheets**: 1:10 scale section showing primary drain bowl, clamping ring, vapor barrier seal, and overflow scupper invert elevation.
