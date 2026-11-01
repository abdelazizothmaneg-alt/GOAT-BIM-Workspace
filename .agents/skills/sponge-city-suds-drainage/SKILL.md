---
name: sponge-city-suds-drainage
description: Plans and designs Sponge City and Sustainable Urban Drainage Systems (SuDS), including bioswales, rain gardens, permeable pavements, detention basins, and stormwater infiltration cells per CIRIA C753.
---

# Sponge City SuDS Drainage

Comprehensive urban landscape engineering skill for designing and sizing Sustainable Drainage Systems (SuDS) and Sponge City infrastructure, maximizing infiltration, filtering non-point pollutants, and preventing urban flash flooding.

---

## 1. SuDS Treatment Train & Principles (CIRIA C753)

The SuDS management train manages rainfall as close to its source as possible across four sequential stages:
1. **Source Control**: Green roofs, rainwater harvesting, permeable driveways.
2. **Site Control**: Bioswales, bioretention cells, rain gardens in roadway medians.
3. **Regional Control**: Retention ponds, wetland basins, underground infiltration crates.
4. **Conveyance**: Vegetated swales, infiltration trenches instead of buried concrete pipes.

---

## 2. Hydraulic Design & Runoff Retention

### Modified Rational Method
$$Q_{\text{peak}} = 0.278 \cdot C \cdot I \cdot A$$
- $Q_{\text{peak}}$: Peak runoff rate ($\text{m}^3/\text{s}$).
- $C$: Weighted runoff coefficient for the catchment basin:
  $$C_{\text{weighted}} = \frac{\sum (C_i \cdot A_i)}{A_{\text{total}}}$$
- $I$: Rainfall intensity ($\text{mm}/\text{h}$) from local IDF curves (e.g. 50-year return period).
- $A$: Area ($\text{km}^2$).

### Bioretention Cell / Rain Garden Sizing
Per CIRIA C753 & EPA guidelines:
$$A_f = \frac{V_w \cdot d_f}{k \cdot (h_f + d_f) \cdot t_f}$$
- $A_f$: Surface area of bioretention filter bed ($\text{m}^2$).
- $V_w$: Water quality capture volume ($\text{m}^3$), typically 25mm first flush storm.
- $d_f$: Filter media depth (typically $0.75 - 1.0\text{ m}$ engineered soil mix).
- $k$: Hydraulic conductivity of soil media ($\approx 0.10 - 0.30\text{ m}/\text{h}$).
- $h_f$: Average ponding depth ($0.15 - 0.30\text{ m}$).
- $t_f$: Target drain-down time ($24 - 48\text{ hours}$ to prevent mosquito breeding).

---

## 3. Structural Cross-Section Specification

### Standard Bioswale Typical Layers (Top to Bottom)
1. **Ponding Zone**: $150 - 250\text{ mm}$ freeboard with native drought/flood-tolerant vegetation.
2. **Mulch Layer**: $50 - 75\text{ mm}$ shredded hardwood mulch (adsorbs heavy metals and hydrocarbons).
3. **Engineered Bioretention Soil**: $600 - 900\text{ mm}$ ($85-88\%$ coarse sand, $8-12\%$ fines/clay, $3-5\%$ organic matter).
4. **Transition Choke Layer**: $100\text{ mm}$ coarse sand / pea gravel (prevents soil migration into stone bed).
5. **Storage Reservoir Aggregate**: $300 - 600\text{ mm}$ crushed stone ($20-40\text{ mm}$ clean uniformly graded, $40\%$ void space).
6. **Perforated Underdrain Pipe**: $150\text{ mm}$ HDPE slotted pipe connecting to municipal stormwater outlet.
7. **Geotextile Liner**: Non-woven geotextile ($150\text{ g}/\text{m}^2$) lining the perimeter (or impermeable geomembrane if adjacent to building foundations).

---

## 4. Python SuDS Sizing Calculator

```python
"""
SuDS Bioretention & Rain Garden Area Calculator
Compliant with CIRIA C753 The SuDS Manual
"""

def size_bioretention_basin(catchment_sqm: float, c_runoff: float, storm_depth_mm: float = 25.0, 
                            filter_depth_m: float = 0.8, ponding_depth_m: float = 0.2, 
                            soil_k_m_per_hr: float = 0.15, drain_time_hours: float = 24.0) -> dict:
    
    # 1. Calculate Water Quality Volume (m^3)
    v_w = (storm_depth_mm / 1000.0) * catchment_sqm * c_runoff
    
    # 2. Apply Darcy-based sizing equation
    # A_f = (V_w * d_f) / [k * (h_f + d_f) * t_f]
    numerator = v_w * filter_depth_m
    denominator = soil_k_m_per_hr * (ponding_depth_m + filter_depth_m) * drain_time_hours
    a_filter = numerator / denominator
    
    # Check percentage of catchment
    pct_catchment = (a_filter / catchment_sqm) * 100.0
    
    return {
        "catchment_area_sqm": catchment_sqm,
        "water_quality_volume_m3": round(v_w, 2),
        "required_filter_area_sqm": round(a_filter, 1),
        "percent_of_catchment": round(pct_catchment, 2)
    }

if __name__ == "__main__":
    res = size_bioretention_basin(catchment_sqm=5000, c_runoff=0.85, storm_depth_mm=30.0)
    print("SuDS Sizing Results:")
    for k, v in res.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables & Engineering Drawings
1. **Master Drainage Grading Plan**: Contoured flow paths directing 100% of street runoff into linear median bioswales.
2. **Construction Detailing**: 1:20 cross-sections of curb cut inlets with energy dissipation rip-rap rock aprons.
3. **Planting Palette**: Salt- and drought-tolerant halophytic species suitable for intermittent inundation.
