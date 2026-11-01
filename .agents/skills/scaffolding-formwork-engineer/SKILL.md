---
name: scaffolding-formwork-engineer
description: Calculates lateral concrete pressure on formwork per ACI 347R, verifies modular scaffolding tower buckling stability per EN 12811 / OSHA, and schedules slab reshoring and striking cycles.
---

# Scaffolding & Formwork Engineer

Structural site engineering skill for designing and verifying concrete formwork systems, computing maximum lateral hydrostatic pressures, verifying modular falsework/scaffolding buckling stability, and establishing slab stripping and reshoring cycles.

---

## 1. Lateral Concrete Pressure on Formwork (ACI 347R)

When fresh concrete is placed, it exerts lateral pressure against vertical formwork (walls and columns).

### ACI 347 Pressure Equations:
1. **For Columns**:
   $$P_{\text{max}} = C_w \cdot C_c \cdot \left[ 7.2 + \frac{785 \cdot R}{T + 17.8} \right] \quad (\text{kPa})$$
   - Maximum limit: $P_{\text{max}} \le \gamma \cdot h$ (Full hydrostatic liquid head) or $150\text{ kPa}$.
2. **For Walls ($R \le 2.1\text{ m/h}$)**:
   $$P_{\text{max}} = C_w \cdot C_c \cdot \left[ 7.2 + \frac{785 \cdot R}{T + 17.8} \right] \le 100\text{ kPa}$$
3. **For Walls ($R > 2.1\text{ m/h}$)**:
   $$P_{\text{max}} = C_w \cdot C_c \cdot \left[ 7.2 + \frac{1156}{T + 17.8} + \frac{244 \cdot R}{T + 17.8} \right] \le 100\text{ kPa}$$

- $R$: Rate of concrete placement ($\text{m}/\text{h}$).
- $T$: Temperature of concrete in form ($\text{}^\circ\text{C}$).
- $C_w$: Unit weight coefficient ($1.0$ for normal concrete $23.5 - 24.5\text{ kN/m}^3$).
- $C_c$: Chemistry coefficient ($1.0$ for Type I cement without retarders; $1.2$ for slag/fly ash blends).

---

## 2. Falsework & Scaffolding Buckling Stability (EN 12811)

Modular shoring towers (Cuplock, Ringlock, Kwikstage) fail primarily by elastic buckling of vertical standards under vertical slab dead + live loads:

### Euler Critical Buckling Load
$$P_{\text{cr}} = \frac{\pi^2 \cdot E \cdot I}{(K \cdot L)^2}$$
- $E$: Modulus of elasticity of steel ($200\text{ GPa} = 200,000\text{ N/mm}^2$).
- $I$: Moment of inertia of scaffolding tube ($\text{mm}^4$).
- $L$: Node-to-node unsupported height between horizontal ledgers (typically $1.0\text{ m}, 1.5\text{ m}$, or $2.0\text{ m}$).
- $K$: Effective length factor ($K \approx 1.0$ for pin-ended pinned nodes; $0.7$ with rigid diagonal bracing).

---

## 3. Formwork Striking & Reshoring Schedule

| Structural Element | Minimum Compressive Strength ($% f'_c$) | Typical Striking Time (Standard Temp $20^\circ\text{C}-25^\circ\text{C}$) |
| :--- | :--- | :--- |
| **Vertical Formwork** (Columns, Wall faces) | $15\% - 20\%$ ($> 5\text{ MPa}$) | 24 - 36 hours |
| **Slab Soffits** (Spans $\le 4.5\text{ m}$) | $70\%$ ($> 20\text{ MPa}$) | 7 days (immediate reshoring required) |
| **Beam Soffits & Cantilevers** | $85\% - 100\%$ ($> 25\text{ MPa}$) | 14 - 21 days |

---

## 4. Python Formwork Lateral Pressure Calculator

```python
"""
ACI 347R Formwork Lateral Concrete Pressure Calculator
"""

def calculate_wall_formwork_pressure(rate_of_pour_m_hr: float, 
                                     concrete_temp_c: float, 
                                     wall_height_m: float, 
                                     concrete_density_kn_m3: float = 24.0, 
                                     c_w: float = 1.0, 
                                     c_c: float = 1.0) -> dict:
    
    r = rate_of_pour_m_hr
    t = concrete_temp_c
    hydrostatic_max = concrete_density_kn_m3 * wall_height_m
    
    if r <= 2.1:
        p_calc = c_w * c_c * (7.2 + (785.0 * r) / (t + 17.8))
        p_design = min(p_calc, 100.0, hydrostatic_max)
    else:
        p_calc = c_w * c_c * (7.2 + (1156.0 / (t + 17.8)) + (244.0 * r) / (t + 17.8))
        p_design = min(p_calc, 100.0, hydrostatic_max)
        
    p_design = max(p_design, 30.0 * c_w) # Minimum design threshold 30 kPa
    
    return {
        "pour_rate_m_hr": r,
        "concrete_temp_c": t,
        "calculated_pressure_kpa": round(p_calc, 1),
        "hydrostatic_limit_kpa": round(hydrostatic_max, 1),
        "governing_design_pressure_kpa": round(p_design, 1)
    }

if __name__ == "__main__":
    # 4m high retaining wall poured at 1.5 m/hr at 30 deg C
    wall_calc = calculate_wall_formwork_pressure(rate_of_pour_m_hr=1.5, concrete_temp_c=30.0, wall_height_m=4.0)
    print("Formwork Pressure Results:")
    for k, v in wall_calc.items():
        print(f"  {k}: {v}")
```

---

## 5. Deliverables Checklist
1. **Formwork Design Calculation Package**: Verifying plywood bending, secondary timber/aluminum joists, and steel waler ties.
2. **Scaffolding Inspection Tag System**: Scafftag system (Green = Safe to use, Yellow = Specific restrictions, Red = Do Not Use).
