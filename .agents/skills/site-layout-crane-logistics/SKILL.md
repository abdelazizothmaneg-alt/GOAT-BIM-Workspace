---
name: site-layout-crane-logistics
description: Optimizes construction site layouts, tower crane positioning, load-radius capacity envelopes, slewing overlap anti-collision zones, and outrigger bearing pressure calculations.
---

# Site Layout & Crane Logistics

Construction logistics engineering skill for planning dynamic jobsite layouts, tower crane positioning, crane capacity load-radius envelopes, multi-crane slewing anti-collision zones, and heavy transport circulation.

---

## 1. Tower Crane Typologies & Selection Criteria

```
Tight Urban Site / High Rise Density  ──> Luffing-Jib Tower Crane (Jib tilts up, stays within boundary)
Open Greenfield / Long Reach Required ──> Hammerhead / Flat-Top Crane (Horizontal trolley, fixed jib)
Fast Mobilization / Low Rise (< 30m)   ──> Self-Erecting Tower Crane (Hydraulic fold-out)
```

### Critical Sizing Formula:
$$\text{Safety Factor} = \frac{\text{Crane Rated Capacity at Radius } R}{\text{Gross Pick Weight (Load + Rigging + Hook Block)}} \ge 1.25$$

---

## 2. Multi-Crane Slewing Overlap & Anti-Collision

When two or more tower cranes operate with overlapping slewing radii:
1. **Vertical Clearance Rule**:
   - Minimum vertical clearance between the highest point of the lower crane (jib or pendant) and the lowest point of the higher crane must be **$\ge 3.0\text{ m}$**.
2. **Electronic Anti-Collision System (ACS)**:
   - Real-time radio telemetry links between cranes automatically decelerate and cut power to slewing and trolley drives before entering shared collision zones.
3. **Out-of-Service Weathervaning**:
   - When shut down, cranes must be free to 360-degree weathervane (rotate freely with the wind) without striking adjacent structures or static obstacles.

---

## 3. Crane Outrigger Ground Bearing Pressure

For mobile hydraulic cranes during heavy structural picks:
$$P_{\text{max}} = \frac{2 \cdot (W_{\text{crane}} + W_{\text{load}})}{3 \cdot L_{\text{mat}} \cdot W_{\text{mat}}}$$
- Outrigger reaction forces must not exceed the allowable soil bearing capacity ($q_{\text{all}}$).
- Heavy hardwood timber or steel distribution spreader mats must be sized to reduce pressure below $150\text{ kPa}$ (or verified against structural slab capacity if placed on basement podiums).

---

## 4. Python Crane Load-Radius Envelope Checker

```python
"""
Tower Crane Capacity vs. Pick Weight Checker
Verifies load chart feasibility across crane working radii.
"""

def check_crane_pick(crane_load_chart: list, pick_weight_tons: float, pick_radius_m: float, rigging_weight_tons: float = 0.5) -> dict:
    gross_weight = pick_weight_tons + rigging_weight_tons
    
    # Linear interpolation of rated capacity from load chart
    # chart format: [(radius_m, capacity_tons), ...]
    sorted_chart = sorted(crane_load_chart, key=lambda x: x[0])
    
    if pick_radius_m < sorted_chart[0][0] or pick_radius_m > sorted_chart[-1][0]:
        return {"status": "FAILED", "reason": f"Pick radius {pick_radius_m}m outside working envelope."}
        
    capacity_at_radius = None
    for i in range(len(sorted_chart) - 1):
        r1, c1 = sorted_chart[i]
        r2, c2 = sorted_chart[i+1]
        if r1 <= pick_radius_m <= r2:
            # Interpolate
            capacity_at_radius = c1 + (c2 - c1) * ((pick_radius_m - r1) / (r2 - r1))
            break
            
    utilization_pct = (gross_weight / capacity_at_radius) * 100.0
    safe = utilization_pct <= 80.0 # 80% safe working threshold
    
    return {
        "pick_radius_m": pick_radius_m,
        "gross_weight_tons": gross_weight,
        "crane_rated_capacity_tons": round(capacity_at_radius, 2),
        "utilization_percent": round(utilization_pct, 1),
        "status": "APPROVED" if safe else "CRITICAL_OVERLOAD"
    }

if __name__ == "__main__":
    # Example Liebherr 280 EC-H 12 Load Chart
    chart_data = [(20.0, 12.0), (30.0, 8.4), (40.0, 5.9), (50.0, 4.4), (60.0, 3.4), (70.0, 2.8)]
    
    # Check pick of 4.5 ton precast panel at 45m radius
    result = check_crane_pick(chart_data, pick_weight_tons=4.5, pick_radius_m=45.0)
    print("Crane Pick Verification:")
    for k, v in result.items():
        print(f"  {k}: {v}")
```

---

## 5. Site Logistics Plan Components
1. **Dynamic Site Logistics Drawing**: 1:200 layout showing crane radii, material staging bays, concrete pump parking, waste skips, and segregated pedestrian safety corridors.
2. **Heavy Lift Protocol Dossier**: Rigging diagrams, shackle capacities, wind speed shutdown thresholds ($> 14\text{ m/s}$ or $28\text{ knots}$).
