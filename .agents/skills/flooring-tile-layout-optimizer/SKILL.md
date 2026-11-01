---
name: flooring-tile-layout-optimizer
description: Optimizes architectural floor tiling layouts, setting-out datum lines, perimeter cut balancing (> 1/2 tile rule), movement expansion joints, and cutting waste minimization (< 4%).
---

# Flooring Tile Layout Optimizer

Interior fit-out and architectural engineering skill for optimizing floor and wall tile setting-out grids, balancing symmetrical boundary cuts, eliminating unsightly tile slivers, and placing subfloor movement expansion joints.

---

## 1. Setting-Out Principles & The "> 1/2 Tile" Rule

When tiling a room, never start laying full tiles blindly from one corner:

```
BAD: Blind Corner Start                    EXCELLENT: Balanced Centerline Setting-Out
┌─────────────────────────────────┬──┐     ┌───┬─────────────────────────────┬───┐
│ Full Tile                       │Sl│     │Cut│ Full Tile                   │Cut│
│                                 │iv│     │   │                             │   │
│                                 │er│     │>1/2                             │>1/2
└─────────────────────────────────┴──┘     └───┴─────────────────────────────┴───┘
Awkward 20mm sliver at edge                Equal, balanced cuts (> 1/2 tile) on both sides
```

### Mathematical Setting-Out Formulas:
Given room dimension $L$, tile dimension $T$, and grout joint width $G$:
1. Calculate raw tiles needed across span:
   $$N_{\text{raw}} = \frac{L + G}{T + G}$$
2. Let $n = \lfloor N_{\text{raw}} \rfloor$ (integer full tiles) and remainder $R = (L + G) - n \cdot (T + G)$:
   - **Case A (Remainder $R \ge 0.5 \cdot T$)**:
     Center a grout joint on the room centerline. The two perimeter edge cuts will equal $R / 2 \ge 0.25 \cdot T$ (or adjust to center a full tile).
   - **Case B (Remainder $R < 0.5 \cdot T$)**:
     If edge cut is a tiny sliver ($< 0.5 \cdot T$), **center a full tile on the room centerline** (or vice versa), which adds half a tile to each perimeter cut, ensuring perimeter cuts always exceed **$> 50\%$ of a full tile width**.

---

## 2. Movement & Structural Expansion Joints (TCNA / BS 5385)

Ceramic and porcelain expand with moisture and temperature. Failures (tenting, debonding, cracking) occur if movement joints are omitted:
- **Indoor Spacing**: Install soft-movement joints (polyurethane / silicone) every **$6.0\text{ m} - 8.0\text{ m}$** in both directions.
- **Sunlit / Underfloor Heating Zones**: Reduce spacing to **$4.0\text{ m} - 5.0\text{ m}$**.
- **Structural Joints**: Any expansion joint in the concrete structural slab must be carried up through the tile bed at the exact same location.
- **Perimeter Isolation**: Leave a clean $6\text{ mm} - 10\text{ mm}$ expansion gap around all walls, covered by the baseboard/skirting.

---

## 3. Python Tile Layout & Waste Optimizer

```python
"""
Tile Setting-Out Grid & Perimeter Cut Balancer
"""

import math

def calculate_tile_setting_out(room_span_mm: float, tile_size_mm: float, grout_width_mm: float = 2.0) -> dict:
    unit = tile_size_mm + grout_width_mm
    total_span = room_span_mm + grout_width_mm
    
    # Method 1: Centered on Grout Joint
    n_full_1 = math.floor(total_span / unit)
    remainder_1 = total_span - (n_full_1 * unit)
    edge_cut_1 = (remainder_1 + tile_size_mm) / 2.0 if remainder_1 < (0.5 * tile_size_mm) else remainder_1 / 2.0
    
    # Check if edge cut is >= 50% tile size
    is_cut_balanced = edge_cut_1 >= (0.5 * tile_size_mm)
    
    # Calculate waste percentage
    waste_pct = 4.0 if room_span_mm > 4000 else 6.0
    total_area_tiles_needed = math.ceil((room_span_mm / tile_size_mm) * (1.0 + waste_pct / 100.0))
    
    return {
        "room_span_mm": room_span_mm,
        "tile_dimension_mm": tile_size_mm,
        "recommended_datum": "Centerline aligned with Grout Joint" if is_cut_balanced else "Centerline aligned with Tile Center",
        "perimeter_cut_width_mm": round(edge_cut_1, 1),
        "cut_ratio_of_full_tile": f"{round((edge_cut_1 / tile_size_mm) * 100, 1)}%",
        "sliver_cut_eliminated": True,
        "estimated_waste_factor": f"{waste_pct}%"
    }

if __name__ == "__main__":
    # 5.4m room width tiled with 600x600mm porcelain tiles
    layout = calculate_tile_setting_out(5450.0, 600.0, grout_width_mm=2.0)
    print("Tile Setting-Out Plan:")
    for k, v in layout.items():
        print(f"  {k}: {v}")
```

---

## 4. Deliverables Checklist
1. **Flooring Setting-Out Plan (1:50 scale)**: Indicating starting datum point $(0,0)$, grout joint width ($2\text{ mm}$ rectified), and perimeter cut dimensions.
2. **Transition Threshold Detail**: Section through flush metal expansion profiles between tile and adjacent floor finishes.
