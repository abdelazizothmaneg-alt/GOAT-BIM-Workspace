---
name: urban-soundscape-noise-mapper
description: Maps environmental urban noise propagation, traffic decibel contours, acoustic diffraction over barriers, and quiet soundscape zoning adhering to ISO 9613-2, WHO Guidelines, and FHWA Traffic Noise Model (TNM).
---

# Urban Soundscape Noise Mapper

Urban environmental acoustics skill for modeling traffic noise emission, calculating outdoor sound attenuation over distance and barriers, generating sound level contours ($L_{den}$, $L_{night}$), and optimizing urban quiet facades.

---

## 1. Noise Standards & Outdoor Propagation (ISO 9613-2)

### Equivalent Sound Pressure Level ($L_{eq}$)
$$L_{eq} = 10 \log_{10} \left( \frac{1}{T} \int_0^T \frac{p^2(t)}{p_0^2} dt \right) \quad (\text{dBA})$$

### Sound Propagation Equation
$$L_{p} = L_{w} - A_{div} - A_{atm} - A_{gr} - A_{bar} - A_{misc}$$
- $L_w$: Acoustic sound power level of source ($\text{dBA}$).
- $A_{div}$: Geometrical divergence attenuation (point source $= 20\log_{10}(d) + 11$; line source highway $= 10\log_{10}(d)$).
- $A_{atm}$: Atmospheric absorption attenuation ($a \cdot d / 1000$).
- $A_{gr}$: Ground reflection attenuation (porous vegetated vs. hard asphalt).
- $A_{bar}$: Diffraction insertion loss provided by acoustic barrier/building wall.

### WHO Community Noise Limits
| Environment | Day ($L_{day}$) | Night ($L_{night}$) | Health Target |
| :--- | :--- | :--- | :--- |
| **Residential Outdoor** | $\le 55\text{ dBA}$ | $\le 45\text{ dBA}$ | Sleep disturbance prevention |
| **Hospital / School Zones** | $\le 45\text{ dBA}$ | $\le 40\text{ dBA}$ | Cognitive focus & recovery |
| **Urban Parks / Quiet Zones** | $\le 50\text{ dBA}$ | $\le 45\text{ dBA}$ | Psychological restoration |

---

## 2. Acoustic Barrier Diffraction (Maekawa / Kurze-Anderson)

The sound reduction $\Delta L$ provided by a barrier depends on the Fresnel number ($N$):
$$N = \frac{2 \delta}{\lambda} = \frac{2 (A + B - d)}{\lambda}$$
- $\delta = (A + B - d)$: Path length difference over barrier crest.
- $\lambda$: Wavelength of sound ($\lambda = c / f$, where $c \approx 343\text{ m/s}$).
- **Barrier Insertion Loss ($\Delta L_{bar}$)**:
  $$\Delta L_{bar} = 10 \log_{10}(3 + 20N) \quad (\text{for } N > 0, \text{ max limit } \approx 20\text{ - }25\text{ dBA})$$

---

## 3. Python Barrier Noise Attenuation Engine

```python
"""
Acoustic Barrier Insertion Loss Calculator
Based on Maekawa's Diffraction Formulation (ISO 9613-2)
"""

import math

def calculate_barrier_insertion_loss(source_dist_to_barrier_m: float, 
                                     receiver_dist_to_barrier_m: float, 
                                     barrier_height_above_line_of_sight_m: float, 
                                     frequency_hz: float = 500.0) -> float:
    """
    Calculates decibel attenuation (dBA) achieved by an acoustic wall/berm.
    frequency_hz: 500Hz is typical predominant road traffic frequency.
    """
    d1 = source_dist_to_barrier_m
    d2 = receiver_dist_to_barrier_m
    h = barrier_height_above_line_of_sight_m
    
    if h <= 0:
        return 0.0 # No line-of-sight blockage
        
    # Direct distance
    direct_d = d1 + d2
    # Diffracted path length over crest
    path_a = math.sqrt(d1**2 + h**2)
    path_b = math.sqrt(d2**2 + h**2)
    delta = (path_a + path_b) - direct_d
    
    # Speed of sound
    c = 343.0
    wavelength = c / frequency_hz
    
    # Fresnel Number
    n = (2.0 * delta) / wavelength
    
    # Maekawa formula
    attenuation_db = 10.0 * math.log10(3.0 + 20.0 * n)
    return round(min(attenuation_db, 22.0), 1) # Practical limit ~22 dBA

if __name__ == "__main__":
    highway_dist = 15.0 # m
    patio_dist = 25.0   # m
    barrier_h = 3.0     # 3m high acoustic wall
    
    loss = calculate_barrier_insertion_loss(highway_dist, patio_dist, barrier_h)
    print(f"Noise Barrier Insertion Loss at 500Hz: -{loss} dBA reduction")
```

---

## 4. Deliverables Checklist
1. **Masterplan Noise Zoning Map**: Color-coded noise contours ($< 50\text{ dBA}$ green, $50-65\text{ dBA}$ amber, $> 65\text{ dBA}$ red).
2. **Quiet Facade Strategy**: Orientation of residential bedroom windows towards protected inner courtyards.
3. **Sound Barrier Construction Details**: Absorptive acoustic wall specs (perforated metal with rockwool core or vegetated earth berms).
