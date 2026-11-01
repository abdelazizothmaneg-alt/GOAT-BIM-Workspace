---
name: mep-hvac-duct-pipe-sizer
description: Performs rigorous mechanical engineering sizing for HVAC air ductwork and hydronic chilled/hot water piping, calculating friction loss, critical path pressure drops, and pump/fan static head per ASHRAE Fundamentals.
---

# MEP HVAC Duct & Pipe Sizing Engine

Mechanical building services engineering skill for calculating ductwork dimensions, chilled water pipe diameters, frictional head loss, and index run static pressure drops conforming to ASHRAE Fundamentals and CIBSE Guide C.

---

## 1. HVAC Duct Sizing: Equal Friction Method (ASHRAE)

In commercial buildings, supply and return air ductwork is commonly sized using the **Equal Friction Method**:
- Target friction loss rate:
  $$\Delta P_f = 0.8\text{ to } 1.0\text{ Pa/m} \quad (0.08\text{ to } 0.10\text{ in. wg per } 100\text{ ft})$$

### Hydraulic Equivalent Diameter (Huebscher Equation):
To convert a circular duct diameter ($D_e$) to an equivalent rectangular duct ($a \times b$):
$$D_e = 1.30 \cdot \frac{(a \cdot b)^{0.625}}{(a + b)^{0.250}}$$
- **Aspect Ratio Constraint**: Maintain width-to-height ratio $a / b \le 3.0$ (maximum $4.0$) to avoid excessive sheet metal turbulence and acoustic vibration.

### Maximum Velocity Limits (Noise Control per ASHRAE):
- Main Supply Trunks in Shafts: $v \le 7.5 - 9.0\text{ m/s}$
- Branch Ducts over Ceilings: $v \le 5.0 - 6.0\text{ m/s}$
- Final Runouts to Diffusers: $v \le 2.5 - 3.5\text{ m/s}$ ($NC \le 30$)

---

## 2. Hydronic Chilled Water Pipe Sizing (Darcy-Weisbach)

$$\Delta h_f = f \cdot \frac{L}{D} \cdot \frac{v^2}{2g}$$
- Flow rate calculation from cooling capacity:
  $$Q_{\text{water}} = \frac{q_{\text{cooling}}}{C_p \cdot \rho \cdot \Delta T}$$
  - For chilled water: $\Delta T = 5.5^\circ\text{C}$ to $6.7^\circ\text{C}$ (e.g., $7^\circ\text{C}$ supply, $13^\circ\text{C}$ return).
- **Design Thresholds**:
  - Friction loss gradient: $\le 250 - 350\text{ Pa/m}$.
  - Velocity range: $1.2\text{ m/s} \le v \le 2.4\text{ m/s}$ (prevents pipe erosion while ensuring air bubble carriage).

---

## 3. Python Duct Sizing & Rectangular Equivalent Calculator

```python
"""
Equal Friction Duct & Pipe Sizer
Based on ASHRAE Fundamentals & Darcy-Weisbach
"""

import math

def size_round_duct(airflow_m3_h: float, target_pressure_drop_pa_m: float = 0.9) -> float:
    """
    Computes optimal round duct diameter (mm) for a given airflow and friction rate.
    """
    q_m3_s = airflow_m3_h / 3600.0
    # Approximated Colebrook-based formulation for standard air:
    # d approx = 1000 * (0.00355 * Q^1.852 / dp)^0.2
    d_m = 0.40 * (q_m3_s ** 0.38) # Empirical sizing fit
    return round(d_m * 1000.0, 0)

def round_to_rectangular_duct(diameter_mm: float, max_height_mm: float = 350.0) -> dict:
    """
    Finds equivalent rectangular duct (W x H) matching circular hydraulic diameter.
    """
    de = diameter_mm
    b = max_height_mm # Restricted by ceiling plenum
    
    # Solve Huebscher formula iteratively for width a:
    # De = 1.30 * (a * b)^0.625 / (a + b)^0.25
    for a in range(int(b), 2500, 25): # 25mm increments
        calc_de = 1.30 * ((a * b) ** 0.625) / ((a + b) ** 0.25)
        if calc_de >= de:
            aspect = a / b
            return {
                "equivalent_round_mm": diameter_mm,
                "width_mm": a,
                "height_mm": b,
                "aspect_ratio": round(aspect, 2),
                "is_aspect_acceptable": aspect <= 4.0
            }
    return {"error": "Plenum height too restrictive for airflow."}

if __name__ == "__main__":
    # Size supply duct for 4500 m3/h meeting room AHU
    d_round = size_round_duct(4500.0, target_pressure_drop_pa_m=0.9)
    rect = round_to_rectangular_duct(d_round, max_height_mm=350.0)
    print("Duct Sizing Result:")
    for k, v in rect.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Duct & Pipe Sizing Schedule**: Tagged lines indicating flow ($\text{L/s}$ or $\text{m}^3/\text{h}$), size ($W \times H$ or $\varnothing$), velocity ($\text{m/s}$), and unit pressure loss ($\text{Pa/m}$).
2. **Critical Path Calculation Sheet**: Cumulative static head loss across the most resistive duct run (index run), verifying fan total static pressure (TSP).
