---
name: architectural-cfd-stack-ventilation
description: Models indoor Computational Fluid Dynamics (CFD), thermal stack ventilation in atriums, windcatcher (Badgir) cooling towers, and buoyancy-driven airflow for zero-energy bioclimatic architecture.
---

# Architectural Indoor CFD & Stack Ventilation

Advanced bioclimatic engineering and Computational Fluid Dynamics (CFD) skill for indoor building performance. Models thermal buoyancy, atrium stack effect ventilation, traditional desert windcatchers (Badgir / Barjeel), and smoke evacuation compliant with NFPA 92.

---

## 1. Buoyancy-Driven Stack Effect Equations

When an indoor-outdoor temperature difference ($\Delta T$) exists, warmer indoor air rises, creating vertical pressure differentials that draw in cool fresh air from lower openings:

$$Q = C_d \cdot A \cdot \sqrt{\frac{2 \cdot g \cdot h \cdot (T_i - T_o)}{T_{avg}}}$$
- $Q$: Volumetric stack ventilation airflow rate ($\text{m}^3/\text{s}$).
- $C_d$: Discharge coefficient of openings ($0.60 - 0.65$ for clear louvers).
- $A$: Free effective area of lower/upper openings ($\text{m}^2$).
- $h$: Vertical distance between lower air intake and upper exhaust louvers ($\text{m}$).
- $T_i, T_o$: Absolute indoor and outdoor temperatures ($\text{K}$).
- $T_{avg}$: Average absolute temperature ($\text{K}$).

### Neutral Pressure Level (NPL):
The height inside the atrium where indoor static pressure equals outdoor atmospheric pressure:
$$h_{NPL} = \frac{h}{1 + \left( \frac{A_{\text{lower}}}{A_{\text{upper}}} \right)^2 \cdot \left( \frac{T_i}{T_o} \right)}$$

---

## 2. Desert Windcatcher (Badgir / Barjeel) Aerodynamics

Traditional vernacular Persian and Gulf windcatchers operate on two complementary physical principles:
1. **Positive Windward Pressure Catching**: Catches ambient desert breezes at roof level, directing cool air down through vertical dividing shafts.
2. **Negative Leeward Suction & Evaporative Cooling**: As wind passes over the leeward side of the tower, negative pressure (suction) pulls warm indoor air up and out. Incorporating ceramic water jars or water spray misters drops supply air temperature by **$8^\circ\text{C} - 12^\circ\text{C}$** via latent heat of vaporization.

---

## 3. Python Atrium Stack Flow & Air Exchange Calculator

```python
"""
Atrium Stack Ventilation & Air Exchange Rate (ACH) Calculator
Based on ASHRAE Fundamentals Chapter 16
"""

import math

def calculate_stack_ventilation(atrium_volume_m3: float, 
                                vertical_height_m: float, 
                                lower_opening_area_sqm: float, 
                                upper_opening_area_sqm: float, 
                                indoor_temp_c: float, 
                                outdoor_temp_c: float, 
                                c_d: float = 0.62) -> dict:
    
    t_in_k = indoor_temp_c + 273.15
    t_out_k = outdoor_temp_c + 273.15
    t_avg = (t_in_k + t_out_k) / 2.0
    delta_t = abs(t_in_k - t_out_k)
    
    if delta_t < 0.5:
        return {"error": "Temperature differential too low to induce significant buoyancy stack effect."}
        
    # Effective opening area A_eff: 1/A_eff^2 = 1/A_low^2 + 1/A_high^2
    a_eff = (lower_opening_area_sqm * upper_opening_area_sqm) / math.sqrt(lower_opening_area_sqm**2 + upper_opening_area_sqm**2)
    
    g = 9.81
    # Q = Cd * A_eff * sqrt(2 * g * h * delta_T / T_avg)
    q_m3_s = c_d * a_eff * math.sqrt((2.0 * g * vertical_height_m * delta_t) / t_avg)
    q_m3_h = q_m3_s * 3600.0
    
    # Air Changes per Hour (ACH)
    ach = q_m3_h / atrium_volume_m3
    
    return {
        "stack_airflow_rate_m3_s": round(q_m3_s, 2),
        "hourly_airflow_m3_h": round(q_m3_h, 1),
        "atrium_volume_m3": atrium_volume_m3,
        "ach_air_changes_per_hour": round(ach, 2),
        "is_natural_ventilation_sufficient": ach >= 4.0 # 4 ACH standard for good natural flushing
    }

if __name__ == "__main__":
    # 24m tall central atrium, 8000 m3 volume, 6 m2 lower and upper louvers
    res = calculate_stack_ventilation(
        atrium_volume_m3=8000.0,
        vertical_height_m=24.0,
        lower_opening_area_sqm=6.0,
        upper_opening_area_sqm=6.0,
        indoor_temp_c=25.0,
        outdoor_temp_c=18.0 # Cool evening
    )
    print("Atrium Stack Ventilation Performance:")
    for k, v in res.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **CFD Simulation Vector Map**: 3D slices showing air velocity vectors ($\text{m/s}$) and temperature contours ($^\circ\text{C}$).
2. **Atrium Louver Sizing Specification**: Certified motorized damper schedules with automated BMS temperature threshold triggers.
