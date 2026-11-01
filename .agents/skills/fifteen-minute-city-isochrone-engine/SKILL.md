---
name: fifteen-minute-city-isochrone-engine
description: Computes walkable network isochrone polygons, Walk Score ratings, and Shannon Entropy Land-Use Diversity Indices for 15-Minute City urban master plans.
---

# 15-Minute City Isochrone & Diversity Engine

Urban analytics and smart city master planning skill for evaluating the **15-Minute City model** (Carlos Moreno). Generates street-network walking isochrone polygons and computes the **Shannon Entropy Land-Use Diversity Index**.

---

## 1. Network Isochrones vs. Euclidean Buffers

```
Euclidean Circular Buffer (Flawed):            Actual Network Isochrone (Truth):
         ┌───────┐                                     ▲ (Follows sidewalks)
       ┌─┘       └─┐                                   │
      ┌┘     ●     └┐                          ◄───────●───────► (Dead ends cut off,
       └─┐       ┌─┘                                   │          barriers respected)
         └───────┘                                     ▼
Assumes birds flying over walls               Follows actual navigable pedestrian paths
```

### Pedestrian Walking Speeds & Radii:
- Average human walking speed: $v_{\text{walk}} \approx 4.5\text{ to } 5.0\text{ km/h}$ ($1.25 - 1.40\text{ m/s}$).
- **5-Minute Isochrone**: $\approx 400\text{ m}$ (Immediate neighborhood essentials: convenience store, pocket park).
- **10-Minute Isochrone**: $\approx 800\text{ m}$ (Primary schools, health clinics, metro stations).
- **15-Minute Isochrone**: $\approx 1,200\text{ m}$ (High schools, supermarkets, sports complexes, civic libraries).

---

## 2. Shannon Entropy Land-Use Diversity Index ($H$)

A sustainable, self-sufficient neighborhood must balance residential, commercial, and civic uses to eliminate car dependence:

$$H = -\frac{\sum_{i=1}^{N} \left( P_i \cdot \ln(P_i) \right)}{\ln(N)}$$
- $P_i$: Proportion of land-use category $i$ relative to total built floor area ($GFA_i / GFA_{total}$).
- $N$: Total number of distinct land-use categories ($N \ge 5$: Residential, Commercial Retail, Office, Healthcare/Education, Public Green/Leisure).
- **Entropy Score ($0.0 \le H \le 1.0$)**:
  - $H < 0.40$: Mono-functional dormitory suburb (High vehicular traffic, low vibrancy).
  - $0.40 \le H \le 0.70$: Moderate mixed-use.
  - **$H \ge 0.75$**: **Exemplary 15-Minute City** (Vibrant, balanced 24/7 self-sufficient urban ecosystem).

---

## 3. Python Shannon Entropy & 15-Minute Score Engine

```python
"""
15-Minute City Land-Use Shannon Entropy & Walk Score Calculator
"""

import math
from typing import Dict

def calculate_shannon_entropy(land_use_gfa_sqm: Dict[str, float]) -> dict:
    total_gfa = sum(land_use_gfa_sqm.values())
    if total_gfa == 0:
        return {"error": "Total GFA cannot be zero."}
        
    num_categories = len(land_use_gfa_sqm)
    if num_categories <= 1:
        return {"entropy_score": 0.0, "status": "Monocultural Zoning"}
        
    entropy_sum = 0.0
    proportions = {}
    
    for category, gfa in land_use_gfa_sqm.items():
        p_i = gfa / total_gfa
        proportions[category] = f"{round(p_i * 100, 1)}%"
        if p_i > 0:
            entropy_sum += p_i * math.log(p_i)
            
    # Normalized Entropy H in range [0, 1]
    h_normalized = -entropy_sum / math.log(num_categories)
    
    # Classification
    if h_normalized >= 0.75:
        rating = "EXEMPLARY_15_MINUTE_CITY"
    elif h_normalized >= 0.55:
        rating = "MODERATE_MIXED_USE"
    else:
        rating = "SUBURBAN_MONOCULTURE"
        
    return {
        "total_gfa_sqm": total_gfa,
        "category_proportions": proportions,
        "shannon_entropy_h": round(h_normalized, 3),
        "urban_vibrancy_rating": rating
    }

if __name__ == "__main__":
    # Test Mixed-Use Downtown Master Plan
    master_plan = {
        "Residential": 120000.0,
        "Office_Commercial": 65000.0,
        "Retail_Dining": 35000.0,
        "Civic_Education": 25000.0,
        "Parks_Recreation": 20000.0
    }
    result = calculate_shannon_entropy(master_plan)
    print("15-Minute City Entropy Assessment:")
    for k, v in result.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Network Isochrone Map (GeoJSON / SHP)**: 5, 10, and 15-minute walking catchment polygons anchored to transit hubs.
2. **Amenity Deficit Heatmap**: Highlighting residential parcels falling outside the 15-minute walking threshold to primary healthcare or fresh grocers.
