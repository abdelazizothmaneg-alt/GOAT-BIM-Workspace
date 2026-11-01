---
name: tactical-urbanism-pop-up
description: Plans and designs rapid-deployment tactical urbanism interventions, parklets, temporary pedestrian plazas, pop-up protected bike lanes, and low-cost placemaking kits.
---

# Tactical Urbanism & Pop-Up Interventions

Agile urban planning skill for rapid, low-cost, short-term urban interventions, temporary street pedestrianization, sidewalk parklets, and asphalt art placemaking that test long-term spatial policy.

---

## 1. Tactical Urbanism Typologies & Guidelines

```
Intervention Scale: Demonstration (1-7 Days) ──> Pilot (1-12 Months) ──> Interim (1-3 Years) ──> Permanent Capital Project
Materials:          Chalk, Traffic Cones, Hay   Paint, Planters, Plastic Armadillos   Precast Curbs, Epoxy Gravel   Poured Concrete, Pavers
```

### Key Tactical Interventions:
1. **Parklets (حدائق الأرصفة المصغرة)**:
   - Converting 1 or 2 curbside parking spaces ($2.2\text{ m} \times 6.0\text{ m}$) into public seating platforms flush with sidewalk curb.
   - Built on modular galvanized steel or treated wood joists, with $0.9\text{ m}$ safety buffer from traffic.
2. **Curb Extensions (Bulb-outs) via Asphalt Art**:
   - Shortens pedestrian crossing distances by $40\%-50\%$.
   - Paint markings protected by flex-posts (delineators) and heavy concrete planters ($> 250\text{ kg}$).
3. **Pop-Up Pedestrian Plazas**:
   - Weekend pedestrianization of commercial alleys using moveable bistro chairs, string lights, and shade sails.

---

## 2. Low-Cost Material Specification & Safety

| Component | Tactical Material | Durability | Safety Function |
| :--- | :--- | :--- | :--- |
| **Pavement Paint** | High-durability acrylic traffic paint / Methyl Methacrylate (MMA) with glass beads | 6 - 24 months | Skid resistance ($BPN \ge 45$) |
| **Separation Buffer** | Heavy recycled plastic wheel stops or rubber armadillo humps | 12 - 36 months | Physical wheel deflection |
| **Buffer Planters** | Self-watering GFRC / Polyethylene planters with native shrubs | 3 - 5 years | Kinetic vehicle impact barrier |
| **Street Furniture** | High-density polyethylene (HDPE) or modular slatted timber | 2 - 5 years | Weatherproof, reconfigurable |

---

## 3. Python Parklet Sizing & Evaluation Tool

```python
"""
Parklet Geometry & Parking Displacement ROI Calculator
"""

def evaluate_parklet_intervention(num_parking_spaces: int, avg_pedestrians_per_day: int, retail_boost_pct: float = 15.0) -> dict:
    space_width_m = 2.4
    space_length_m = 6.0
    total_area_sqm = num_parking_spaces * space_width_m * space_length_m
    
    # 1 parking space serves ~1-2 parked cars over hours
    # 1 parklet serves ~15-30 people per hour
    seated_capacity = num_parking_spaces * 8
    
    return {
        "displaced_parking_spots": num_parking_spaces,
        "reclaimed_public_space_sqm": total_area_sqm,
        "instant_seating_capacity": seated_capacity,
        "estimated_daily_visitors": seated_capacity * 12,
        "projected_local_retail_boost": f"+{retail_boost_pct}%"
    }

if __name__ == "__main__":
    res = evaluate_parklet_intervention(2, 450)
    print("Tactical Parklet Evaluation:")
    for k, v in res.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Phasing & Implementation Plan**: Phased schedule from 48-hour pilot to 1-year evaluation.
2. **Maintenance & Public Engagement Protocol**: Community stewardship agreement for planter watering and furniture security.
