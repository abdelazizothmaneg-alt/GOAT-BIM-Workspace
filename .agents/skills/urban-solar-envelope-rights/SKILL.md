---
name: urban-solar-envelope-rights
description: Generates Ralph Knowles solar envelopes, computes Sky Exposure Planes (SEP), and protects solar access rights and winter daylight for neighboring buildings in urban master plans.
---

# Urban Solar Envelope Rights

Computational urban geometry skill for calculating Ralph Knowles solar envelopes, sky exposure planes (SEP), daylight factor preservation, and solar access rights for adjacent urban plots.

---

## 1. Solar Envelope Theory & Mathematical Principles (Ralph Knowles)

A **Solar Envelope** is the largest spatial volume container that can be built on a parcel without casting shadows on neighboring plots during critical sun hours (e.g. winter solstice: December 21 between 09:00 and 15:00).

```
Sun Ray Vector S(t) at Altitude (alpha) & Azimuth (gamma)
     \
      \  (Solar Cut Plane)
       \──────────────┐
       │ Solar        │  Neighboring Property Line
       │ Envelope     ├──────────────────────────
       │ Permissible  │  Neighbor's Living Room Window
       │ Massing      │  (GUARANTEED ZERO SHADOW)
───────┴──────────────┴──────────────────────────
```

### Critical Parameters:
- **Solar Cut Angles**:
  - Solar altitude angle: $\alpha$
  - Solar azimuth angle: $\gamma$
- **Equation of Ray Plane Passing Through Neighbor Property Line**:
  $$z = z_0 + \tan(\alpha) \cdot d_{\perp}$$
  where $d_{\perp}$ is the perpendicular distance into the lot along the solar azimuth direction.

---

## 2. Sky Exposure Planes (SEP) & Setbacks

Municipal zoning systems (e.g. NYC Zoning Resolution, MOMRAH / Balady codes) enforce geometric angular planes sloping inward from the street center or property line:
- **Ratio (e.g. 1.5 to 1 / 2.5 to 1)**: For every $1\text{ m}$ of setback, the building can rise $1.5\text{ m}$ to $2.5\text{ m}$.
- Protects daylight reaching the street bed and ground-level pedestrian realm.

---

## 3. Python Solar Envelope Boundary Calculator

```python
"""
Solar Envelope Height Limit Calculator
Computes maximum building height at given distance from property boundary.
"""

import math

def calculate_max_envelope_height(distance_from_neighbor_line_m: float, 
                                  solar_altitude_deg: float, 
                                  base_datum_m: float = 0.0) -> float:
    """
    Computes max allowable elevation without shadowing neighbor at winter solstice altitude.
    """
    alpha_rad = math.radians(solar_altitude_deg)
    h_allowed = base_datum_m + distance_from_neighbor_line_m * math.tan(alpha_rad)
    return round(h_allowed, 2)

if __name__ == "__main__":
    # Winter solstice solar altitude at noon for Riyadh (~42 deg) or London (~15 deg)
    altitude_riyadh = 42.5
    altitude_london = 15.0
    
    dist = 12.0 # 12 meters into the site from northern property line
    h_riyadh = calculate_max_envelope_height(dist, altitude_riyadh)
    h_london = calculate_max_envelope_height(dist, altitude_london)
    
    print(f"Max envelope height at {dist}m setback in Riyadh: {h_riyadh} m")
    print(f"Max envelope height at {dist}m setback in London: {h_london} m")
```

---

## 4. Deliverables Checklist
1. **3D Solar Envelope Mesh (Rhino/Revit DirectShape)**: Isometric volume showing permissible building zoning envelope.
2. **Shadow Impact Study**: Equinox and Solstice shadow animations confirming zero unauthorized shadow spillage.
