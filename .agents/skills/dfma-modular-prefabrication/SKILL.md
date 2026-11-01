---
name: dfma-modular-prefabrication
description: Applies Design for Manufacture and Assembly (DfMA) principles, volumetric modular pod segmentation, precast concrete hollowcore sizing, mass timber shop tickets, and transport/crane lifting logistics.
---

# Design for Manufacture & Assembly (DfMA) & Modular Prefabrication

Advanced prefabrication engineering skill for applying DfMA principles to architectural models, designing volumetric modular pods, sizing precast elements, generating fabrication shop tickets, and ensuring transportable envelope limits.

---

## 1. Core DfMA Principles in Modern Construction

```
Traditional On-Site Construction:           DfMA Factory Prefabrication:
High site labor, weather delays,            70-80% factory assembly, millimeter CNC accuracy,
messy tolerances (±15mm), slow progress     rapid dry on-site assembly, zero wet trade delays
```

### The 5 Golden Rules of DfMA:
1. **Standardization & Repeatability**: Maximize reuse of identical modular chassis and panel dimensions.
2. **Minimize Component Count**: Combine multiple separate layers into monolithic composite assemblies (e.g. insulated precast concrete sandwich panels with factory-installed windows).
3. **Self-Locating & Mistake-Proofing (Poka-Yoke)**: Design interlocking chamfered joints and alignment dowels so components physically cannot be assembled backwards.
4. **Transport Envelope Compliance**:
   - Standard road flatbed: Max width $\le 2.5\text{ m}$, length $\le 12.0\text{ m}$.
   - Oversized road transit (with police escort permit): Width up to $3.6 - 4.2\text{ m}$, height under overpasses $\le 4.5\text{ m}$.
5. **Integrated Lifting Provisions**: Pre-engineer cast-in lifting anchors positioned symmetrically around the 3D Center of Gravity (CoG).

---

## 2. Prefabrication Typologies

### A. Volumetric 3D Pods (Bathroom & Kitchen Pods)
- **Structure**: Lightweight cold-formed steel (CFS) or welded hollow structural section (HSS) tube chassis.
- **Finishes**: $100\%$ factory-fitted (tiling, sanitaryware, MEP piping, electrical lighting, and testing).
- **Hook-Up Connections**: Single point of MEP connection located in the corridor riser cupboard (wet-wall chase) via quick-connect flexible couplings.

### B. 2D Panelized Systems (Precast Hollowcore & Mass Timber CLT)
- **Precast Hollowcore Slabs**:
  - Thicknesses: $150\text{ mm}, 200\text{ mm}, 265\text{ mm}, 320\text{ mm}, 400\text{ mm}$.
  - Spans up to $16.0\text{ m}$ without intermediate beams.
  - Require structural structural topping screed ($50 - 75\text{ mm}$) with welded wire mesh for seismic diaphragm action.

---

## 3. Python Lifting Center of Gravity (CoG) & Crane Capacity Checker

```python
"""
Modular Pod Lifting Center of Gravity (CoG) & Sling Angle Sizer
"""

import math

def calculate_modular_pod_lift(pod_weight_tons: float, 
                               pod_length_m: float, 
                               pod_width_m: float, 
                               sling_angle_from_horizontal_deg: float = 60.0) -> dict:
    
    # Lifting sling tension increases as angle drops below 60 degrees!
    theta_rad = math.radians(sling_angle_from_horizontal_deg)
    if sling_angle_from_horizontal_deg < 45.0:
        return {"error": "Sling angle < 45 deg creates dangerous compressive crushing forces on pod roof frame."}
        
    num_slings = 4 # 4-point corner lift
    # Vertical load per sling = W / 4
    # Actual tension per sling = (W / 4) / sin(theta)
    tension_per_sling_tons = (pod_weight_tons / num_slings) / math.sin(theta_rad)
    
    # Required minimum hook height above pod top
    half_diagonal = math.sqrt((pod_length_m / 2.0)**2 + (pod_width_m / 2.0)**2)
    min_hook_headroom_m = half_diagonal * math.tan(theta_rad)
    
    return {
        "pod_weight_tons": pod_weight_tons,
        "sling_angle_deg": sling_angle_from_horizontal_deg,
        "tension_per_sling_tons": round(tension_per_sling_tons, 2),
        "required_shackle_capacity_tons": math.ceil(tension_per_sling_tons * 1.5), # 1.5 Safety Factor
        "required_crane_hook_headroom_m": round(min_hook_headroom_m, 2)
    }

if __name__ == "__main__":
    # 5.5 ton fully fitted hotel bathroom pod (3.2m x 2.4m)
    lift_data = calculate_modular_pod_lift(5.5, 3.2, 2.4, sling_angle_from_horizontal_deg=60.0)
    print("Modular Pod Lift Rigging Specs:")
    for k, v in lift_data.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **DfMA Modular Segmentation Drawing**: Color-coded plan showing boundary lines of volumetric pods, precast panels, and transport splices.
2. **Component Fabrication Ticket**: Individual sheet for each panel indicating CNC cutouts, rebar cage, embed plates, and tolerance limits ($\pm 2.0\text{ mm}$).
