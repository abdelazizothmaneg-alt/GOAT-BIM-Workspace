---
name: structural-fea-load-takedown
description: Audits structural analytical models, performs vertical gravity load takedown (tributary area method per ASCE 7 / SBC 301), and bridges Revit with Finite Element Analysis (FEA) software (ETABS, Robot, SAP2000).
---

# Structural FEA Load Takedown & Interoperability

Structural engineering automation skill for auditing analytical wireframe connectivity, performing automated gravity load takedowns (tributary area accumulation), and synchronizing structural geometry between Revit and FEA analysis engines (ETABS, SAP2000, Autodesk Robot).

---

## 1. Analytical Model Integrity & Node Snapping

A common cause of catastrophic analysis failures in structural software is broken analytical nodes:
- **Node Coincidence Tolerance**:
  $$\text{Distance between Analytical Nodes} \le 5.0\text{ mm}$$
- **Analytical Rigid Links**:
  - Automatically bridge geometric offsets between beam centerlines and non-coaxial column faces or shear wall centerlines.
- **Slab-to-Wall Diaphragm Continuity**:
  - Ensure slab analytical surface edge nodes project directly onto the supporting beam/wall analytical axes to transfer out-of-plane shear.

---

## 2. Gravity Load Takedown (Tributary Area Method)

For any column $C_j$ at floor level $k$:
$$P_{\text{axial}, k} = \sum_{m=k}^{\text{Roof}} \left[ A_{T, m} \cdot \left( w_{\text{dead}, m} + \psi \cdot w_{\text{live}, m} \right) + W_{\text{column}, m} \right]$$
- $A_{T, m}$: Tributary area of the column at level $m$ ($\text{m}^2$) computed via Voronoi or midpoint bisectors.
- $w_{\text{dead}}$: Area dead load (slab self-weight + screed + finishes + MEP ceiling allowance, typically $7.5 - 10.0\text{ kPa}$).
- $w_{\text{live}}$: Design live load (residential $= 2.0\text{ kPa}$, office $= 2.5 - 3.0\text{ kPa}$, corridors $= 4.0\text{ kPa}$).

### Live Load Reduction Factor ($L / L_0$ per SBC 301 / ASCE 7):
$$L = L_0 \left( 0.25 + \frac{4.57}{\sqrt{K_{LL} \cdot A_T}} \right)$$
- $K_{LL}$: Live load element factor ($4$ for interior columns, $3$ for edge columns).
- Reduction applies when $K_{LL} \cdot A_T \ge 37.16\text{ m}^2$ ($400\text{ sq ft}$), with a maximum reduction cap of $50\%$ for elements supporting one floor and $60\%$ for columns supporting two or more floors.

---

## 3. Python Tributary Area & Load Takedown Engine

```python
"""
Structural Column Gravity Load Takedown & Live Load Reduction
Based on ASCE 7-16 & SBC 301
"""

import math

def calculate_column_load_takedown(num_stories: int, 
                                   tributary_area_sqm: float, 
                                   dead_load_kpa: float = 8.5, 
                                   live_load_kpa: float = 3.0, 
                                   column_self_weight_kn_per_floor: float = 35.0) -> dict:
    
    # 1. Check Live Load Reduction (ASCE 7)
    k_ll = 4.0 # Interior column
    influence_area = k_ll * tributary_area_sqm
    
    if influence_area >= 37.16:
        reduction_factor = 0.25 + (4.57 / math.sqrt(influence_area))
        # Cap reduction at 0.40 (max 60% reduction for multi-story)
        reduction_factor = max(reduction_factor, 0.40)
    else:
        reduction_factor = 1.0
        
    reduced_live_load = live_load_kpa * reduction_factor
    
    # Floor load per level
    floor_dead_kn = tributary_area_sqm * dead_load_kpa + column_self_weight_kn_per_floor
    floor_live_kn = tributary_area_sqm * reduced_live_load
    
    total_unfactored_dl = floor_dead_kn * num_stories
    total_unfactored_ll = floor_live_kn * num_stories
    
    # Ultimate Factored Axial Load (1.2 DL + 1.6 LL)
    p_ultimate_kn = (1.2 * total_unfactored_dl) + (1.6 * total_unfactored_ll)
    
    return {
        "num_stories": num_stories,
        "tributary_area_sqm": tributary_area_sqm,
        "live_load_reduction_ratio": round(reduction_factor, 2),
        "total_dead_load_kn": round(total_unfactored_dl, 1),
        "total_reduced_live_load_kn": round(total_unfactored_ll, 1),
        "factored_p_ultimate_kn": round(p_ultimate_kn, 1),
        "approx_foundation_load_tons": round(p_ultimate_kn / 9.81, 1)
    }

if __name__ == "__main__":
    # 15-story residential tower column with 36 m2 tributary area
    column_result = calculate_column_load_takedown(15, tributary_area_sqm=36.0)
    print("Column Load Takedown Results:")
    for k, v in column_result.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Analytical Wireframe Audit Report**: Identification of orphaned nodes, overlapping members, and unassigned support boundary conditions.
2. **Column Load Schedule**: Level-by-level axial force table ($P_D, P_L, P_U$) ready for foundation geotechnical sizing.
