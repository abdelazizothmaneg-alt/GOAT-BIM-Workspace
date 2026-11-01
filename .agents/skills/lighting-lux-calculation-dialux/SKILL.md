---
name: lighting-lux-calculation-dialux
description: Calculates architectural interior lighting lux levels using the Lumen Method and DIALux logic, verifying EN 12464-1 illuminance targets, Unified Glare Rating (UGR <= 19), CRI >= 90, and IES photometric distribution.
---

# Lighting Lux Calculation & DIALux Logic

Interior lighting engineering skill for calculating illuminance levels, luminaire counts, spacing criteria, glare ratings (UGR), and lighting power density (LPD) conforming to EN 12464-1 and ASHRAE 90.1.

---

## 1. Illuminance Standards (EN 12464-1 / CIBSE Code)

| Space Typology | Maintained Lux ($E_m$) | Glare Limit ($UGR_L$) | Uniformity ($U_0$) | Color Rendering ($R_a / CRI$) |
| :--- | :--- | :--- | :--- | :--- |
| **Open-Plan Offices** | $500\text{ lux}$ | $\le 19$ | $\ge 0.60$ | $\ge 80$ (Target $\ge 90$) |
| **Architectural / CAD Studios** | $750\text{ lux}$ | $\le 16$ | $\ge 0.70$ | $\ge 90$ |
| **Conference & Boardrooms** | $500\text{ lux}$ | $\le 19$ | $\ge 0.60$ | $\ge 85$ |
| **Corridors & Circulation** | $100\text{ lux}$ | $\le 28$ | $\ge 0.40$ | $\ge 80$ |
| **Hotel Guestrooms (General)** | $150\text{ lux}$ | $\le 22$ | $\ge 0.40$ | $\ge 90$ |
| **Healthcare Operating Theatres**| $10,000 - 100,000\text{ lux}$ | $\le 19$ | $\ge 0.80$ | $\ge 95$ |

---

## 2. The Lumen Method (Zonal Cavity Method)

$$E = \frac{N \cdot \Phi \cdot UF \cdot MF}{A}$$
- $E$: Maintained illuminance level on the working plane ($\text{lux} = \text{lm}/\text{m}^2$).
- $N$: Number of luminaires.
- $\Phi$: Luminous flux per luminaire ($\text{lumens}$).
- $UF$: Utilization Factor (depends on luminaire photometric curve and room reflectances $\rho_{ceiling} / \rho_{wall} / \rho_{floor} = 0.7 / 0.5 / 0.2$).
- $MF$: Maintenance Factor (typically $0.80$ for clean LED indoor commercial spaces).
- $A$: Room floor area ($\text{m}^2$).

### Number of Luminaires Required:
$$N = \frac{E_{\text{target}} \cdot A}{\Phi \cdot UF \cdot MF}$$

---

## 3. Glare Control (Unified Glare Rating - UGR)

$$UGR = 8 \log_{10} \left[ \frac{0.25}{L_b} \sum \left( \frac{L^2 \cdot \omega}{p^2} \right) \right]$$
- $L_b$: Background luminance ($\text{cd/m}^2$).
- $L$: Luminance of the luminous parts of each luminaire ($\text{cd/m}^2$).
- $\omega$: Solid angle of the luminous parts at observer eye ($\text{sr}$).
- $p$: Guth position index.
- *Strict Rule*: In computer workstations and architectural offices, **$UGR \le 19$** is mandatory to prevent eye fatigue.

---

## 4. Python Lux & Fixture Sizing Calculator

```python
"""
Lumen Method Indoor Lighting Sizing Engine
Compliant with EN 12464-1
"""

import math

def calculate_fixtures_needed(room_length_m: float, 
                              room_width_m: float, 
                              target_lux: float, 
                              lumens_per_fixture: float, 
                              utilization_factor: float = 0.65, 
                              maintenance_factor: float = 0.80) -> dict:
    
    room_area = room_length_m * room_width_m
    total_lumens_needed = (target_lux * room_area) / (utilization_factor * maintenance_factor)
    exact_fixtures = total_lumens_needed / lumens_per_fixture
    recommended_count = math.ceil(exact_fixtures)
    
    # Calculate grid layout
    aspect_ratio = room_length_m / room_width_m
    cols = round(math.sqrt(recommended_count * aspect_ratio))
    rows = math.ceil(recommended_count / cols)
    final_count = cols * rows
    
    achieved_lux = (final_count * lumens_per_fixture * utilization_factor * maintenance_factor) / room_area
    
    return {
        "room_area_sqm": room_area,
        "target_lux": target_lux,
        "recommended_fixtures": final_count,
        "grid_layout": f"{rows} rows x {cols} columns",
        "achieved_lux": round(achieved_lux, 1),
        "luminaire_flux_lm": lumens_per_fixture
    }

if __name__ == "__main__":
    # 12m x 8m open-plan office requiring 500 lux using 3600lm LED troffers
    calc = calculate_fixtures_needed(12.0, 8.0, target_lux=500.0, lumens_per_fixture=3600.0)
    print("Lighting Calculation Results:")
    for k, v in calc.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Photometric Schedule**: Luminaire tag, manufacturer catalog code, CCT (Kelvin), CRI, beam angle, and wattages.
2. **Pseudocolor Lux False-Color Map**: Visual 3D false-color gradient proving compliance with target lux and uniformity.
