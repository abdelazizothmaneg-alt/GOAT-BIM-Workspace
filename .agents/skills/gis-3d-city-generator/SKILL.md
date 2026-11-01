---
name: gis-3d-city-generator
description: Automates the generation of 3D digital urban city twins (LOD1/LOD2) from GIS data sources (OSM, Shapefiles, GeoJSON, LiDAR DEM), extruding parcel heights, projecting onto real topography, and exporting to Blender, 3ds Max, and Unreal Engine 5.
---

# GIS 3D City Generator (توليد التوائم الرقمية ثلاثية الأبعاد للمدن من الـ GIS)

A geospatial and computational urban modeling skill that automates the translation of 2D/2.5D municipal Geographic Information Systems (GIS) datasets—including OpenStreetMap (OSM), Shapefiles, GeoJSON, and Digital Elevation Models (DEM/LiDAR)—into parametric 3D urban context models (Level of Detail LOD1 and LOD2) projected onto real-world terrain coordinates.

---

## 1. City Generation Pipeline

```
[GIS Data Layers (Vector Footprints + GeoTIFF Elevation DEM)]
                           │
                           ▼
     [1. Geospatial Coordinate Reference System (CRS) Reprojection]
     - Reproject from WGS84 (EPSG:4326) to Local Projected CRS
     - (e.g., Ain el Abd / UTM zone 38N - EPSG:32638 for Riyadh)
                           │
                           ▼
     [2. Digital Elevation Model (DEM) Surface Meshing]
     - Convert raster elevation pixels into a triangulated TIN terrain mesh
     - Apply contour line smoothing & road right-of-way flattening
                           │
                           ▼
     [3. Footprint Parsing & Semantic Height Assignment]
     - If explicit height attribute exists: Extrude height = building:height
     - If level count exists: Extrude height = building:levels * 3.5m
     - Fallback default by zoning: Residential (12m), Commercial (24m)
                           │
                           ▼
     [4. Solid Extrusion & Terrain Snapping]
     - Raycast parcel bottom vertices to conform with terrain elevation
     - Generate clean watertight OBJ / FBX / glTF mesh models
                           │
                           ▼
     [5. Export for Rendering & Masterplanning]
     - 3ds Max / Blender (Urban context entourage)
     - Unreal Engine 5 / Cesium (Interactive Digital Twin)
```

---

## 2. Headless Python Extrusion Engine (Shapely & GeoPandas)

```python
import geopandas as gpd
from shapely.geometry import Polygon
import json

def generate_lod1_city_blocks(shapefile_path: str, default_floor_height_m: float = 3.5):
    """
    Parses cadastral parcels and generates 3D massing extrusions.
    """
    gdf = gpd.read_file(shapefile_path)
    
    city_elements = []

    for idx, row in gdf.iterrows():
        geom = row.geometry
        if geom is None or geom.is_empty:
            continue

        # Extract height or calculate from floor count
        if "height" in row and row["height"] is not None:
            total_height = float(row["height"])
        elif "levels" in row and row["levels"] is not None:
            total_height = float(row["levels"]) * default_floor_height_m
        else:
            total_height = 10.5 # Default 3-story baseline

        # Extract polygon boundary points
        if geom.geom_type == 'Polygon':
            coords = list(geom.exterior.coords)
        elif geom.geom_type == 'MultiPolygon':
            coords = list(max(geom.geoms, key=lambda p: p.area).exterior.coords)
        else:
            continue

        city_elements.append({
            "building_id": f"BLDG_{idx:05d}",
            "use_type": row.get("landuse", "mixed_use"),
            "height_meters": round(total_height, 2),
            "footprint_vertices": [(round(x, 2), round(y, 2)) for x, y in coords]
        })

    return {
        "total_buildings": len(city_elements),
        "buildings": city_elements
    }
```

---

## 3. Topographic Snapping Rules
1. **Curtain Wall Drop**: When a parcel sits across sloped terrain, extend the building base downward to the lowest intersected terrain vertex to prevent buildings from "floating" on hillsides.
2. **Road Corridor Flattening**: Flatten terrain elevation along primary vehicular corridors to avoid unnatural rollercoaster road gradients ($< 8\%$ maximum slope per road design manuals).

---

## 4. Downstream Pipeline Targets
- **Blender GIS**: Real-time displacement modifier driven by satellite GeoTIFF rasters.
- **3ds Max / V-Ray**: Instant low-poly background context blocks for aerial masterplan renderings.
- **Unreal Engine 5.5**: Ingested via Cesium 3D Tiles plugin for geospatial city exploration.
