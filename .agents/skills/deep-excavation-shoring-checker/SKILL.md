---
name: deep-excavation-shoring-checker
description: Audits deep excavation shoring systems, secant/diaphragm walls, ground anchor prestress loads, dewatering drawdown, and adjacent building settlement monitoring conforming to CIRIA C760 and Eurocode 7.
---

# Deep Excavation Shoring Checker

Geotechnical and site engineering skill for auditing deep basement excavations ($> 8\text{ m}$ depth), retaining wall systems (secant/diaphragm walls), multi-tier ground anchor pre-stressing, dewatering drawdown, and ground settlement monitoring.

---

## 1. Shoring System Typologies & Applications

```
Shallow (< 5m, dry)    ──> Soldier Piles (H-beams) + Shotcrete Lagging
Medium (5m - 10m, dry) ──> Contiguous Pile Wall (Tangent Piles) + Soil Nails
Deep (> 8m, high water)──> Secant Bored Piles (Hard/Soft interlocking)
Mega Deep (> 15m)      ──> Diaphragm Wall (Slurry Wall) + Multi-level Struts / Prestressed Anchors
```

### Secant Bored Pile Wall Interlocking:
- **Primary (Soft) Piles**: Cast first with low-strength plain concrete ($f'_c \approx 10 - 15\text{ MPa}$).
- **Secondary (Hard) Piles**: Bored between primary piles, cutting into them by $75 - 150\text{ mm}$ with reinforced high-strength concrete ($f'_c \ge 35\text{ MPa}$).
- *Result*: Water-tight retaining barrier suitable for deep urban basements with high water tables.

---

## 2. Lateral Earth Pressure & Ground Anchor Loads

### Rankine Apparent Earth Pressure (Peck's Envelope for Braced Cuts)
For dense sand and gravel:
$$p = 0.65 \cdot \gamma \cdot H \cdot K_a$$
- $\gamma$: Soil unit weight ($\text{kN}/\text{m}^3$).
- $H$: Total excavation depth ($\text{m}$).
- $K_a$: Active earth pressure coefficient:
  $$K_a = \tan^2\left(45^\circ - \frac{\phi}{2}\right)$$

### Ground Tieback Anchor Prestressing
- **Free Length ($L_{\text{free}}$)**: Must extend beyond the active failure shear plane (minimum $5.0\text{ m}$).
- **Fixed Bond Length ($L_{\text{bond}}$)**: Pressure-grouted into competent bedrock/strata:
  $$T_{\text{ult}} = \pi \cdot D_{\text{hole}} \cdot L_{\text{bond}} \cdot \tau_{\text{ult}}$$
  - Factor of Safety: $\text{FOS} \ge 2.0$ against bond failure; proof-tested to $1.25 - 1.50 \times$ design lock-off load.

---

## 3. Dewatering & Groundwater Drawdown (Dupuit-Thiem)

For unconfined aquifer radial flow to deep well points:
$$Q = \frac{\pi \cdot k \cdot (H^2 - h_w^2)}{\ln(R / r_w)}$$
- $k$: Soil permeability ($\text{m}/\text{s}$).
- $H$: Initial groundwater table height ($\text{m}$).
- $h_w$: Drawdown water level at well radius $r_w$ ($\text{m}$).
- $R$: Radius of influence ($R = 3000 \cdot (H - h_w) \cdot \sqrt{k}$).

---

## 4. Python Lateral Pressure & Settlement Trigger Engine

```python
"""
Deep Excavation Earth Pressure & Settlement Trigger Monitor
Compliant with CIRIA C760
"""

import math

def calculate_active_earth_pressure(soil_gamma: float, excavation_depth_m: float, friction_angle_deg: float) -> dict:
    phi_rad = math.radians(friction_angle_deg)
    k_a = (math.tan(math.radians(45.0) - (phi_rad / 2.0))) ** 2
    
    # Peck's trapezoidal/rectangular envelope for sand
    peck_pressure_kpa = 0.65 * soil_gamma * excavation_depth_m * k_a
    total_lateral_thrust_kn = peck_pressure_kpa * excavation_depth_m
    
    return {
        "depth_m": excavation_depth_m,
        "k_a": round(k_a, 3),
        "apparent_pressure_kpa": round(peck_pressure_kpa, 2),
        "total_thrust_kn_per_m": round(total_lateral_thrust_kn, 1)
    }

if __name__ == "__main__":
    # 12m deep basement excavation in Riyadh gravel/limestone
    calc = calculate_active_earth_pressure(soil_gamma=19.5, excavation_depth_m=12.0, friction_angle_deg=34.0)
    print("Lateral Pressure Results:")
    for k, v in calc.items():
        print(f"  {k}: {v}")
```

---

## 5. Settlement Trigger Levels (Red / Amber / Green)
- **Green (Normal)**: Horizontal wall deflection $< 15\text{ mm}$, neighboring ground settlement $< 10\text{ mm}$.
- **Amber (Alert Level - 70% threshold)**: Horizontal deflection $15 - 25\text{ mm}$. Increase monitoring frequency to 2x daily; inspect adjacent buildings for hairline cracks.
- **Red (Action Level - Stop Work)**: Deflection $> 25\text{ mm}$ or rate of movement $> 3\text{ mm}/\text{day}$. Immediately stop excavation; install emergency walers/berms.
