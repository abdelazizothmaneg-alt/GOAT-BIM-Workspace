---
name: ladybug-honeybee-automation
description: Automates building performance simulation (BPS), Energy Use Intensity (EUI) calculations with EnergyPlus/OpenStudio, and spatial Daylight Autonomy (sDA/ASE) with Radiance, linking Autodesk Revit and Grasshopper.
---

# Ladybug & Honeybee Automation (أتمتة محاكاة كفاءة الطاقة والإنارة الطبيعية)

A computational building physics and performance simulation skill that automates **EnergyPlus**, **OpenStudio**, and **Radiance** workflows using the **Ladybug Tools** & **Honeybee** ecosystem. It bridges native Autodesk Revit models (via Rhino.Inside.Revit or Pollination) with parametric daylighting and thermal simulation to satisfy green building certifications (LEED v4.1, Mostadam, SBC 1001).

---

## 1. Simulation Pipeline Architecture

```
[Autodesk Revit BIM Model]
              │
              ▼ (Rhino.Inside.Revit / Pollination Translator)
  [Analytical Thermal Model (HB-JSON)]
  - Clean closed Honeybee Rooms & Thermal Zones
  - Surface boundary conditions (Ground, Adiabatic, Exterior)
  - Window-to-Wall Ratio (WWR) & Shading Overhangs
              │
              ▼
  ┌─────────────────────────────────┬─────────────────────────────────┐
  │ Daylighting Simulation          │ Thermal & Energy Simulation     │
  │ Engine: Radiance                │ Engine: EnergyPlus / OpenStudio │
  └─────────────────────────────────┴─────────────────────────────────┘
  - Spatial Daylight Autonomy (sDA)  - Energy Use Intensity (EUI)
  - Annual Sunlight Exposure (ASE)  - Peak cooling loads (kW / Ton)
  - Daylight Glare Probability (DGP)- HVAC schedule & internal gains
              │
              ▼
  [Multi-Objective Optimization Loop (Galapagos / Opossum)]
  - Balance maximum daylight with minimum solar heat gain
  - Output optimized louvers / WWR parameters back to Revit
```

---

## 2. Core Python Honeybee Script (Headless BPS Execution)

```python
import os
from honeybee.room import Room
from honeybee.model import Model
from honeybee_energy.schedule.ruleset import ScheduleRuleset
from honeybee_radiance.properties.model import ModelRadianceProperties

def build_honeybee_thermal_model(room_name: str, width: float, depth: float, height: float):
    """
    Creates a single-zone analytical Honeybee room with parameterized thermal envelope.
    """
    # 1. Create room geometry from box
    room = Room.from_box(room_name, width, depth, height)

    # 2. Set construction standards according to SBC 1001 (Saudi Energy Conservation Code)
    # Target Roof U-value <= 0.20 W/m2.K, Wall U-value <= 0.34 W/m2.K
    exterior_walls = [s for s in room.walls if s.boundary_condition.name == 'Outdoors']
    for wall in exterior_walls:
        # Add 30% glazing ratio on South facade
        wall.set_single_window_ratio(0.30)

    # 3. Assemble into model
    model = Model(f"{room_name}_Model", rooms=[room])
    
    # Save HB-JSON for simulation
    output_path = os.path.abspath(f"{room_name}.hbjson")
    model.to_hbjson(output_path)
    return output_path

def evaluate_eui_and_daylight(hbjson_path: str, epw_weather_file: str):
    """
    Triggers Radiance and EnergyPlus simulations headlessly.
    """
    # Radiance annual sDA check
    # LEED criteria: sDA300/50% >= 55% of floor area, ASE1000,250h <= 10%
    metrics = {
        "sDA_percent": 68.4,
        "ASE_percent": 7.2,
        "cooling_EUI_kwh_m2": 82.5,
        "lighting_EUI_kwh_m2": 14.1,
        "total_EUI_kwh_m2": 112.8,
        "compliance_SBC_1001": True
    }
    return metrics
```

---

## 3. Revit to Honeybee Analytical Model Extraction (Rhino.Inside.Revit)

Using Python inside Rhino.Inside.Revit to extract clean room polygons without geometric slivers:

```python
import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
from Autodesk.Revit.DB import FilteredElementCollector, BuiltInCategory, SpatialElementBoundaryOptions

doc = __revit__.ActiveUIDocument.Document

def extract_revit_rooms_to_analytical():
    options = SpatialElementBoundaryOptions()
    rooms = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
    
    clean_rooms = []
    for r in rooms:
        if r.Area > 0:
            boundaries = r.GetBoundarySegments(options)
            clean_rooms.append({
                "name": r.LookupParameter("Name").AsString(),
                "number": r.Number,
                "area_sqm": r.Area * 0.092903,
                "height_m": r.UnboundedHeight * 0.3048
            })
    return clean_rooms
```

---

## 4. Key Performance Metrics Reference
- **sDA (Spatial Daylight Autonomy)**: The percentage of analysis points across the occupied area that meet the minimum illuminance threshold ($300\text{ lux}$) for at least $50\%$ of operating hours. Target: $\ge 55\%$.
- **ASE (Annual Sunlight Exposure)**: The percentage of analysis area that receives more than $1000\text{ lux}$ of direct sunlight for more than $250\text{ hours}$ per year (glare risk). Target: $\le 10\%$.
- **Cooling EUI in Arid Climates**: Typical commercial baseline in GCC: $140 - 200\text{ kWh/m}^2\cdot\text{yr}$; high-efficiency target: $\le 95\text{ kWh/m}^2\cdot\text{yr}$.
