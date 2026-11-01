---
name: complete-streets-cross-section
description: Designs multimodal Complete Street cross-sections adhering to NACTO and AASHTO standards, integrating protected cycle tracks, BRT transit lanes, pedestrian clear zones, utility corridors, and curb management.
---

# Complete Streets Cross-Section

Multimodal urban street design skill for structuring balanced, pedestrian-priority right-of-ways (ROW) using National Association of City Transportation Officials (NACTO) and AASHTO guidelines.

---

## 1. Street Hierarchy & Dimension Standards (NACTO)

### Standard Right-of-Way (ROW) Zonal Allocation
A complete urban street right-of-way is divided into four distinct functional zones:

```
|--- Frontage ---|--- Clear Path ---|--- Furnishing / Bioswale ---|--- Cycle Track ---|--- Buffer ---|--- Parking/Flex ---|--- Travel Lane ---|--- Median ---|
|   (1.0-1.5m)   |    (2.0-3.5m)    |         (1.5-2.5m)          |    (2.0-2.5m)     |  (0.5-0.9m)  |     (2.2-2.5m)     |    (3.0-3.2m)     |  (2.0-3.0m)  |
```

### Lane Width Dimensional Rules
- **Pedestrian Clear Walking Zone**: Minimum $1.8\text{ m}$ (two wheelchairs passing); Recommended $2.4 - 3.6\text{ m}$ in commercial corridors.
- **Protected Bike Lane (Cycle Track)**:
  - One-way: $2.0\text{ m}$ clear ($1.5\text{ m}$ absolute minimum) $+ 0.6\text{ m}$ buffer.
  - Two-way: $3.0\text{ m}$ clear ($2.5\text{ m}$ minimum) $+ 0.9\text{ m}$ physical concrete buffer.
- **Vehicular Travel Lanes**:
  - Target design speed $\le 40\text{ km/h}$: Lane width $= 3.0\text{ m}$ ($10\text{ ft}$). Narrower lanes reduce speeding and pedestrian fatalities.
  - Bus / BRT / Freight curb lanes: $3.3 - 3.5\text{ m}$ ($11 - 11.5\text{ ft}$).
- **Curb Radius at Intersections**:
  - Keep effective turning radius $R \le 3.0 - 4.5\text{ m}$ ($10 - 15\text{ ft}$) with curb extensions (bulb-outs) to shorten pedestrian crossing distance.

---

## 2. Curb Zone Management & Flex Spaces

Modern streets treat the curb as a flexible programmatic asset rather than pure parking:
1. **Mobility Hubs**: E-scooter docking, bike-share stations.
2. **Parklets & Outdoor Dining**: Modular deck platforms flush with sidewalk grade.
3. **Loading / Logistics**: Timed delivery bays (commercial mornings, rideshare evenings).
4. **Stormwater Infrastructure**: Curb extensions retrofitted as bioretention rain gardens.

---

## 3. Parametric Street Cross-Section Generator (Python / SVG)

```python
"""
Parametric Complete Street Cross-Section Generator
Outputs SVG technical diagram representing lane dimensions
"""

def generate_street_svg(row_width_m: float, zones: list) -> str:
    total_allocated = sum(z["width"] for z in zones)
    if abs(total_allocated - row_width_m) > 0.05:
        raise ValueError(f"Total zone width ({total_allocated}m) does not match ROW ({row_width_m}m)")
        
    scale = 30 # 30 pixels per meter
    svg_width = int(row_width_m * scale)
    svg_height = 200
    
    svg = [f'<svg width="{svg_width}" height="{svg_height}" xmlns="http://www.w3.org/2000/svg">']
    svg.append('<style>text{font-family:sans-serif;font-size:11px;text-anchor:middle;}</style>')
    svg.append(f'<rect width="{svg_width}" height="{svg_height}" fill="#f8fafc"/>')
    
    current_x = 0
    for z in zones:
        w_px = z["width"] * scale
        color = z["color"]
        name = z["name"]
        dim_text = f'{z["width"]}m'
        
        svg.append(f'<rect x="{current_x}" y="40" width="{w_px}" height="100" fill="{color}" stroke="#334155" stroke-width="1.5"/>')
        svg.append(f'<text x="{current_x + w_px/2}" y="95" fill="#0f172a" font-weight="bold">{name}</text>')
        svg.append(f'<text x="{current_x + w_px/2}" y="115" fill="#475569">{dim_text}</text>')
        current_x += w_px
        
    svg.append('</svg>')
    return "\n".join(svg)

if __name__ == "__main__":
    # 24-Meter Commercial Boulevard
    boulevard_zones = [
        {"name": "Sidewalk", "width": 3.5, "color": "#e2e8f0"},
        {"name": "Bioswale/Tree", "width": 2.0, "color": "#bbf7d0"},
        {"name": "Cycle Track", "width": 2.0, "color": "#bae6fd"},
        {"name": "Buffer", "width": 0.5, "color": "#cbd5e1"},
        {"name": "Bus Lane", "width": 3.5, "color": "#fef08a"},
        {"name": "Median", "width": 1.0, "color": "#86efac"},
        {"name": "Bus Lane", "width": 3.5, "color": "#fef08a"},
        {"name": "Buffer", "width": 0.5, "color": "#cbd5e1"},
        {"name": "Cycle Track", "width": 2.0, "color": "#bae6fd"},
        {"name": "Bioswale/Tree", "width": 2.0, "color": "#bbf7d0"},
        {"name": "Sidewalk", "width": 3.5, "color": "#e2e8f0"},
    ]
    svg_output = generate_street_svg(24.0, boulevard_zones)
    print("Generated SVG markup successfully for 24m Complete Street.")
```

---

## 4. Deliverables Checklist
1. **Multimodal Street Dimension Matrix**: Exact breakdown of sidewalk, buffer, cycle track, and vehicular lane widths.
2. **CAD / Revit Civil 3D Assemblies**: Standard assemblies with subassemblies for curbs, gutters, bioswales, and asphalt crowned cross-slopes ($2.0\%$).
3. **Intersection Sight Triangle & Crossing Plans**: Curb bulb-out layouts with high-visibility continental crosswalk markings ($3.0 - 4.0\text{ m}$ wide).
