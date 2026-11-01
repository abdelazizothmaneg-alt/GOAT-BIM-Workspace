---
name: concrete-pour-logistics-planner
description: Plans and optimizes large-volume concrete pour logistics, raft foundation pour lifts, transit mixer truck dispatch cycles, pump truck positioning, and cold joint prevention compliant with ACI 304R and ACI 305R (Hot Weather Concreting).
---

# Concrete Pour Logistics Planner

Construction site engineering skill for managing massive concrete pours (raft foundations, deep transfer slabs, core walls), transit mixer dispatch cycles, concrete pump truck setup, and cold-joint mitigation in hot climates.

---

## 1. Concrete Pour Logistics & Sizing Rules (ACI 304R / 305R)

### A. Cold Joint Prevention Equation
To avoid catastrophic structural cold joints, each fresh concrete layer must be covered and vibrated with the subsequent lift before the initial setting time ($T_{\text{initial}}$):
$$Q_{\text{pour, min}} \ge \frac{A_{\text{lift}} \cdot h_{\text{layer}}}{T_{\text{initial}} - \Delta t_{\text{buffer}}} \quad (\text{m}^3/\text{h})$$
- $A_{\text{lift}}$: Surface area of the pour zone ($\text{m}^2$).
- $h_{\text{layer}}$: Thickness of individual layer (typically $300 - 500\text{ mm}$).
- $T_{\text{initial}}$: Concrete initial set time (reduced to $60 - 90\text{ min}$ in high-temperature arid climates unless retarders are added).
- $\Delta t_{\text{buffer}}$: Safety buffer (typically $15 - 30\text{ min}$).

### B. Transit Mixer Fleet Sizing
$$N_{\text{trucks}} = \left\lceil \frac{Q_{\text{pump}} \cdot T_{\text{roundtrip}}}{V_{\text{truck}}} \right\rceil + 2 \text{ (Standby trucks)}$$
- $Q_{\text{pump}}$: Actual continuous pumping output ($\approx 45 - 60\text{ m}^3/\text{h}$ per boom pump).
- $V_{\text{truck}}$: Truck drum capacity (typically $8 - 10\text{ m}^3$).
- $T_{\text{roundtrip}}$: Batching plant loading ($15\text{ min}$) + Transit to site ($30\text{ min}$) + Discharge & Slump test ($15\text{ min}$) + Washout & Return transit ($30\text{ min}$) $= 90\text{ min}$ ($1.5\text{ h}$).

---

## 2. Hot Weather Concreting Protocols (ACI 305R / SBC Concrete)

When ambient temperature exceeds $35^\circ\text{C}$ or concrete temperature approaches $32^\circ\text{C}$:
1. **Chilled Water & Flake Ice Replacement**:
   - Replace $50\% - 75\%$ of mixing water with crushed flake ice to maintain discharge temperature $\le 30^\circ\text{C}$.
2. **Night Pour Scheduling**:
   - Shift massive pours to start between 18:00 and 20:00 to capitalize on cooler night ambient temperatures and reduced road traffic.
3. **Curing & Evaporation Control**:
   - Apply aliphatic alcohol evaporation retardant film immediately after screeding if surface evaporation rate exceeds $1.0\text{ kg}/(\text{m}^2\cdot\text{h})$.

---

## 3. Python Concrete Pour & Fleet Dispatch Simulator

```python
"""
Concrete Pour Fleet Dispatch Simulator
Computes required trucks, pumps, and pour duration
"""

import math

def plan_concrete_pour(total_volume_m3: float, 
                       pour_area_sqm: float, 
                       layer_thickness_m: float = 0.4, 
                       truck_capacity_m3: float = 9.0, 
                       roundtrip_time_min: float = 80.0, 
                       initial_set_hours: float = 2.0) -> dict:
    
    # Min hourly rate to prevent cold joint
    layer_volume = pour_area_sqm * layer_thickness_m
    min_rate_m3_h = layer_volume / (initial_set_hours - 0.5) # with 30 min buffer
    
    # Typical stationary/boom pump capacity = 50 m3/h
    pumps_needed = math.ceil(min_rate_m3_h / 50.0)
    actual_pour_rate = pumps_needed * 45.0 # Conservative 45 m3/h per pump
    
    # Total pour duration
    duration_hours = total_volume_m3 / actual_pour_rate
    
    # Truck fleet calculation
    roundtrip_hours = roundtrip_time_min / 60.0
    trucks_active = math.ceil((actual_pour_rate * roundtrip_hours) / truck_capacity_m3)
    total_fleet = trucks_active + 2 # Add standbys
    
    return {
        "total_volume_m3": total_volume_m3,
        "min_rate_for_cold_joint_prevention": round(min_rate_m3_h, 1),
        "required_concrete_pumps": pumps_needed,
        "total_active_trucks": trucks_active,
        "total_recommended_fleet": total_fleet,
        "estimated_duration_hours": round(duration_hours, 1)
    }

if __name__ == "__main__":
    # 2500 m3 Raft Foundation Pour (50m x 25m x 2m raft)
    raft_plan = plan_concrete_pour(total_volume_m3=2500, pour_area_sqm=1250, layer_thickness_m=0.4)
    print("Concrete Pour Plan:")
    for k, v in raft_plan.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Concrete Pour Sequence Plan**: 2D layout indicating pump positions, washout pits, truck queuing lanes, and pour progression vectors.
2. **Quality Control Register**: Slump test log, cube/cylinder compressive test schedule (7, 28, and 56 days), and thermocouple core temperature logging.
