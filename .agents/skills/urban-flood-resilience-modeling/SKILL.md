---
name: urban-flood-resilience-modeling
description: Designs climate-adaptive coastal waterfronts, 100-year storm surge inundation models, terraced sponge promenades, and tidal backflow barriers conforming to ASCE 24.
---

# Urban Coastal & Riverine Flood Resilience

Climate adaptation and resilient urban waterfront design skill. Simulates 100-year storm surges, sea-level rise (SLR) scenarios, multi-tiered sponge seawalls, and nature-based hydrodynamic wave dissipation conforming to ASCE 24 and FEMA guidelines.

---

## 1. Multi-Tiered Coastal Edge Architecture

Traditional vertical concrete seawalls reflect wave energy, worsening erosion. Resilient urban design uses a **multi-tiered sponge edge**:

```
Open Sea / Harbor ──> [ Tier 1: Living Shoreline ] ──> [ Tier 2: Tidal Amphitheater ] ──> [ Tier 3: Core Resilient Dike ] ──> City Core
                      (Mangroves / Rip-rap reef        (Steps & boardwalks designed      (High promenade protecting           (Finished floor
                       dissipating 60% wave energy)     for safe periodic inundation)     against 100-year storm surge)        >= DFE + Freeboard)
```

---

## 2. Design Flood Elevation (DFE) Formulation (ASCE 24)

$$\text{DFE} = \text{BFE} + \text{SLR}_{2100} + \text{Freeboard}$$
- $\text{BFE}$: Base Flood Elevation (100-year flood level including wave crest height per FEMA maps).
- $\text{SLR}_{2100}$: Projected global and localized sea-level rise by 2100 (typically $+0.8\text{ m}$ to $+1.5\text{ m}$ under IPCC SSP5-8.5).
- $\text{Freeboard}$: Safety factor buffer ($\ge 0.6\text{ m}$ for residential; $\ge 1.0\text{ m}$ for critical infrastructure).
- **Rule**: All mechanical, electrical, and life-safety plant rooms must be located above the calculated DFE (or enclosed within watertight subterranean submarine-type flood doors).

---

## 3. Python Storm Surge & Freeboard Calculator

```python
"""
Coastal Design Flood Elevation (DFE) Calculator
Compliant with ASCE 24 and FEMA Coastal Construction Manual
"""

def calculate_coastal_dfe(base_flood_elevation_m: float, 
                         wave_runup_height_m: float, 
                         slr_scenario_m: float = 1.0, 
                         building_risk_category: int = 3) -> dict:
    
    # ASCE 24 Freeboard based on Risk Category
    # Cat II (Standard) = 0.6m; Cat III/IV (Hospitals, Emergency) = 0.9m - 1.2m
    freeboard_m = 0.6 if building_risk_category <= 2 else 1.0
    
    total_water_level = base_flood_elevation_m + wave_runup_height_m + slr_scenario_m
    dfe = total_water_level + freeboard_m
    
    return {
        "base_flood_elevation_bfe_m": base_flood_elevation_m,
        "wave_action_runup_m": wave_runup_height_m,
        "sea_level_rise_allowance_m": slr_scenario_m,
        "asce24_freeboard_m": freeboard_m,
        "minimum_finished_floor_elevation_dfe_m": round(dfe, 2),
        "subterranean_dry_floodproofing_required": True
    }

if __name__ == "__main__":
    # Coastal master plan in Red Sea / Arabian Gulf: BFE = 2.8m, Wave = 0.9m, SLR = 0.85m
    res = calculate_coastal_dfe(2.8, 0.9, slr_scenario_m=0.85, building_risk_category=3)
    print("Coastal Resilience Flood Elevations:")
    for k, v in res.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Inundation Hazard Contour Map**: Visualizing tidal water levels under Mean High Water Springs (MHWS), 50-year storm, and 100-year storm surge.
2. **Waterfront Edge Engineering Section (1:20 scale)**: Detailing rip-rap rock armor, geotextile filter fabric, and tidal flap check valves preventing seawater backflow.
