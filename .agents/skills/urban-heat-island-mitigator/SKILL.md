---
name: urban-heat-island-mitigator
description: Analyzes and mitigates Urban Heat Island (UHI) effects in master plans, computing Solar Reflectance Index (SRI >= 78), tree canopy shading fractions, sky view factors (SVF), and bioclimatic wind corridors.
---

# Urban Heat Island Mitigator

Advanced computational urban climatology skill for evaluating, modeling, and mitigating Urban Heat Island (UHI) effects, optimizing surface albedo, microclimate vegetation canopies, and thermal comfort (PET / UTCI) in master plans.

---

## 1. Urban Heat Island Fundamentals & Metrics

### A. Surface Radiation & Solar Reflectance Index (SRI)
- **SRI Definition (ASTM E1980)**:
  Measures the relative steady-state surface temperature of a material under solar irradiance compared to standard black ($\text{SRI} = 0$, solar reflectance $0.05$, emittance $0.90$) and standard white ($\text{SRI} = 100$, solar reflectance $0.80$, emittance $0.90$).
- **Mandatory Requirements**:
  - Non-roof hardscape pavements: $\text{SRI} \ge 29$ (or $50\%$ shaded by canopies/structures).
  - Low-slope roofs ($\le 2:12$): Initial $\text{SRI} \ge 82$, 3-year aged $\text{SRI} \ge 64$.
  - Steep-slope roofs ($> 2:12$): Initial $\text{SRI} \ge 39$, 3-year aged $\text{SRI} \ge 32$.

### B. Sky View Factor (SVF)
- Ratio of radiation received by a planar surface to radiation emitted by the entire hemispherical sky vault.
- Dense urban street canyons ($H/W > 2.0$) trap longwave radiation, causing $\text{SVF} < 0.30$ and raising nocturnal temperatures by $4^\circ\text{C} - 8^\circ\text{C}$.
- **Target Canyon Ratio**: Maintain $H/W \le 1.2$ or incorporate step-backs and podium courtyards to maintain $\text{SVF} \ge 0.45$.

---

## 2. Physiological Thermal Comfort Indices (UTCI & PET)

- **Universal Thermal Climate Index (UTCI)**:
  Derived from Fiala multi-node human thermoregulation model combining:
  1. Dry-bulb air temperature ($T_a$)
  2. Mean Radiant Temperature ($T_{mrt}$)
  3. Wind velocity at $10\text{ m}$ ($v_{10}$)
  4. Relative humidity ($RH$)
- **Urban Vegetation Impact**:
  - A mature canopy tree ($10\text{ m}$ crown) reduces $T_{mrt}$ by $15^\circ\text{C} - 25^\circ\text{C}$ directly underneath, reducing pedestrian UTCI by up to $6^\circ\text{C}$ to $10^\circ\text{C}$ (from "Very Strong Heat Stress" down to "Moderate Stress").

---

## 3. Python Microclimate Calculation Engine

```python
"""
Urban Surface Heat & Albedo Heat Island Calculator
Based on ASTM E1980 Solar Reflectance Index (SRI)
"""

def calculate_surface_temperature_delta(solar_radiation_w_m2: float, albedo: float, emittance: float, h_c: float = 12.0) -> float:
    """
    Approximates steady-state surface temperature rise above ambient (delta T in Kelvin).
    h_c: Convective heat transfer coefficient (W/m^2*K), typical 12.0 for low wind.
    """
    alpha = 1.0 - albedo # Solar absorptance
    sigma = 5.67e-8     # Stefan-Boltzmann constant
    
    # Linearized energy balance approximation:
    # alpha * I = h_c * (Ts - Ta) + 4 * sigma * emittance * Ta^3 * (Ts - Ta)
    ta_kelvin = 313.15 # 40 deg C ambient
    radiative_coeff = 4.0 * sigma * emittance * (ta_kelvin ** 3)
    total_transfer = h_c + radiative_coeff
    
    delta_t = (alpha * solar_radiation_w_m2) / total_transfer
    return round(delta_t, 2)

if __name__ == "__main__":
    i_sun = 950.0 # W/m2 intense desert noon sun
    
    # Asphalt Pavement (Albedo 0.10, Emittance 0.90)
    delta_asphalt = calculate_surface_temperature_delta(i_sun, albedo=0.10, emittance=0.90)
    
    # Cool High-Albedo Concrete Paver (Albedo 0.65, Emittance 0.90, SRI > 78)
    delta_cool_paver = calculate_surface_temperature_delta(i_sun, albedo=0.65, emittance=0.90)
    
    print(f"Asphalt surface temp rise above ambient: +{delta_asphalt} K (approx +{delta_asphalt} C)")
    print(f"Cool Paver surface temp rise above ambient: +{delta_cool_paver} K (approx +{delta_cool_paver} C)")
    print(f"Heat Island Mitigation Difference: {round(delta_asphalt - delta_cool_paver, 2)} C cooler!")
```

---

## 4. Mitigation Strategies & Deliverables
1. **Paving Material Selection Matrix**:
   - Permeable interlocking pavers with light-reflective limestone aggregate.
   - Grass-crete / porous turf blocks for low-traffic parking aisles.
2. **Bioclimatic Shading Plan**:
   - Native arid trees (*Prosopis cineraria*, *Acacia tortilis*, *Phoenix dactylifera*) planted along east-west pedestrian spines.
   - Tensioned PTFE fabric tensile canopies over critical transit nodes.
3. **Masterplan UHI Compliance Report**:
   - Zonal calculation showing $> 50\%$ shaded or $\text{SRI} \ge 29$ compliance per plot.
