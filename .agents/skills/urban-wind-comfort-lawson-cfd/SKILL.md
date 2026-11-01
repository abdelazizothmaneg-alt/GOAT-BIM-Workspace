---
name: urban-wind-comfort-lawson-cfd
description: Evaluates urban pedestrian wind microclimates using Computational Fluid Dynamics (CFD), testing high-rise downwash, corner accelerations, and public plaza comfort against Lawson and Davenport Criteria.
---

# Urban Wind Comfort & Lawson CFD Criteria

Master urban microclimate engineering skill for simulating pedestrian-level wind comfort around skyscraper clusters and public plazas. Implements the **Lawson Criteria (LDDC)** and **Davenport Framework** using Computational Fluid Dynamics (CFD) and long-term meteorological wind roses.

---

## 1. Aerodynamic Wind Phenomena Around Tall Buildings

```
Upstream Wind Profile ──> High-Rise Stagnation Point (~0.7 H)
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
       Downwash Jet to Ground           Corner Streamline Acceleration
    (Hits plaza pavement at high speed) (Venturi effect between narrow alleys)
```

### Mitigation Strategies:
1. **Podium Deflection**: A $2 - 4$ story podium deflects the downwash vortex before it reaches pedestrian sidewalks.
2. **Horizontal Canopies / Eyebrows**: Projecting cantilevers ($2.5 - 4.0\text{ m}$) placed at the second floor line.
3. **Aerodynamic Corner Chamfering**: Softening building sharp corners ($R \ge 3.0\text{ m}$) reduces vortex shedding and edge acceleration by up to $30\%$.

---

## 2. The Lawson Comfort Criteria (T.V. Lawson / LDDC)

The Lawson criteria assess the wind speed exceeded for no more than **$5\%$ of the time** across the year:

| Pedestrian Activity | Exceedance Velocity Threshold ($5\%$ limit) | Urban Program Application |
| :--- | :--- | :--- |
| **Sitting (Long-term)** | $v \le 4.0\text{ m/s}$ | Outdoor café dining, park benches, amphitheaters |
| **Standing (Short-term)** | $v \le 6.0\text{ m/s}$ | Building entrance canopies, bus stops, taxi queues |
| **Strolling / Leisure Walk**| $v \le 8.0\text{ m/s}$ | Pedestrian shopping malls, promenade walkways |
| **Business Walking** | $v \le 10.0\text{ m/s}$ | Main vehicular street sidewalks |
| **Unacceptable (Nuisance)**| $v > 10.0\text{ m/s}$ | Uncomfortable for general pedestrian transit |
| **SAFETY CRITERIA (Distress)**| **$v > 15.0\text{ m/s}$ (exceeded $> 0.022\%$ of time)** | **DANGEROUS**: Risk of pedestrians losing balance. Mandatory mitigation! |

---

## 3. Python Lawson Comfort Classification Tool

```python
"""
Pedestrian Wind Comfort Evaluator
Classifies CFD sensor points into Lawson Comfort Zones
"""

def evaluate_lawson_comfort(sensor_id: str, wind_speed_at_5pct_exceedance_m_s: float, proposed_space_use: str) -> dict:
    v = wind_speed_at_5pct_exceedance_m_s
    
    # Determine achieved Lawson class
    if v <= 4.0:
        achieved_class = "Sitting"
    elif v <= 6.0:
        achieved_class = "Standing"
    elif v <= 8.0:
        achieved_class = "Strolling"
    elif v <= 10.0:
        achieved_class = "Business_Walking"
    else:
        achieved_class = "Uncomfortable"
        
    is_safe = v < 15.0 # Safety threshold
    
    # Check compliance with intended space use
    requirements = {
        "outdoor_cafe": 4.0,
        "building_entrance": 6.0,
        "sidewalk": 8.0,
        "transit_stop": 6.0
    }
    
    target_max_v = requirements.get(proposed_space_use, 8.0)
    compliant = v <= target_max_v
    
    return {
        "sensor": sensor_id,
        "wind_speed_m_s": round(v, 2),
        "achieved_lawson_category": achieved_class,
        "intended_use": proposed_space_use,
        "is_compliant": compliant,
        "safety_limit_passed": is_safe,
        "recommendation": "Compliant" if compliant else "Requires architectural canopy or evergreen windbreak planting."
    }

if __name__ == "__main__":
    # Test outdoor dining plaza between two 40-story towers
    result = evaluate_lawson_comfort("Sensor_Plaza_04", wind_speed_at_5pct_exceedance_m_s=6.8, proposed_space_use="outdoor_cafe")
    print("Lawson Wind Comfort Audit:")
    for k, v in result.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Pedestrian Comfort Contour Map**: Color-coded urban map at $1.5\text{ m}$ elevation showing Lawson zones across the master plan.
2. **Wind Safety Hazard Dossier**: Identification of localized exceedances of the $15\text{ m/s}$ distress threshold with canopy mitigation details.
