---
name: cloud-to-bim-deviation-inspector
description: Inspects as-built 3D laser scan point clouds (E57, LAS) against as-designed Revit/IFC models, generating surface-to-mesh deviation heatmaps to verify column verticality, slab flatness, and structural tolerances per USIBD LOA standards.
---

# Cloud-to-BIM Deviation Inspector (فحص انحرافات الموقع بمقارنة سحابة النقاط مع الـ BIM)

A high-definition reality capture QA/QC skill that performs automated geometric deviation analysis between as-built terrestrial laser scan (TLS) point clouds (E57, LAS, RCP) and as-designed Autodesk Revit or IFC models. It measures structural column plumbness, floor slab flatness ($F_F/F_L$), and generates 3D colorized deviation heatmaps compliant with **USIBD Level of Accuracy (LOA 10–50)**.

---

## 1. Deviation Inspection Workflow

```
[3D Laser Scan Point Cloud (E57 / LAS)] ────┐
                                            ├─► [1. Cloud-to-BIM Registration]
[As-Designed Revit BIM Model (OBJ / IFC)] ──┘   - Shared project survey coordinates
                                                - Iterative Closest Point (ICP) alignment
                                                            │
                                                            ▼
                                                [2. Cloud-to-Mesh (C2M) Distance Engine]
                                                - For each point p in scan:
                                                  Compute Euclidean distance d to nearest triangle
                                                            │
                                                            ▼
                                                [3. Tolerance Classification & Heatmap]
                                                - Green: Within tolerance (|d| <= 5 mm)
                                                - Yellow: Minor deviation (5 mm < |d| <= 15 mm)
                                                - Red: Critical deviation (|d| > 25 mm)
                                                            │
                                                            ▼
                                                [4. Sectional Profile & Report Generation]
                                                - Slab surface contour flatness (ASTM E1155)
                                                - Verticality tilt angle for cast columns
```

---

## 2. USIBD Level of Accuracy (LOA) Thresholds

| LOA Level | Upper Tolerance Limit ($\pm$) | Target Construction Use |
| :--- | :--- | :--- |
| **LOA 10** | $\pm 50\text{ mm}$ ($2\text{ in}$) | Earthworks, bulk excavation, site massing. |
| **LOA 20** | $\pm 30\text{ mm}$ ($1.2\text{ in}$) | Deep foundations, secant piling, substructure. |
| **LOA 30** | $\pm 15\text{ mm}$ ($0.6\text{ in}$) | Reinforced concrete framing, slabs, blockwork. |
| **LOA 40** | $\pm 5\text{ mm}$ ($0.2\text{ in}$) | Curtain wall mullions, structural steel, MEP pre-fab. |
| **LOA 50** | $\pm 1\text{ mm}$ ($0.04\text{ in}$) | High-precision architectural joinery, heritage restoration. |

---

## 3. Automated C2M Distance Computation (Python & Open3D)

```python
import open3d as o3d
import numpy as np

def compute_scan_to_bim_deviation(point_cloud_path: str, bim_mesh_path: str, tolerance_mm: float = 15.0):
    """
    Computes Euclidean distance between laser scan points and the BIM surface mesh.
    """
    # 1. Load scan and mesh
    pcd = o3d.io.read_point_cloud(point_cloud_path)
    mesh = o3d.io.read_triangle_mesh(bim_mesh_path)
    mesh.compute_vertex_normals()

    # 2. Build raycasting scene for distance query
    scene = o3d.t.geometry.RaycastingScene()
    scene.add_triangles(o3d.t.geometry.TriangleMesh.from_legacy(mesh))

    query_points = o3d.core.Tensor(np.asarray(pcd.points), dtype=o3d.core.Dtype.Float32)
    distances = scene.compute_distance(query_points).numpy() * 1000.0 # Convert meters to mm

    # 3. Classify deviations
    compliant_points = np.sum(distances <= tolerance_mm)
    critical_deviations = np.sum(distances > 25.0)
    total_points = len(distances)

    compliance_percentage = (compliant_points / total_points) * 100.0

    return {
        "total_scan_points": total_points,
        "mean_deviation_mm": round(float(np.mean(distances)), 2),
        "max_deviation_mm": round(float(np.max(distances)), 2),
        "compliance_percent": round(float(compliance_percentage), 2),
        "critical_points_count": int(critical_deviations),
        "usibd_rating": "LOA 40" if np.max(distances) <= 5 else "LOA 30" if np.max(distances) <= 15 else "LOA 20"
    }
```

---

## 4. Engineering Verification Checklist
1. **Floor Flatness & Levelness ($F_F / F_L$)**: Extract surface elevation grid on poured slabs to ensure compliance with ASTM E1155 before laying delicate marble or raised access floors.
2. **Column Vertical Plumbness**: Measure vertical deflection over height; maximum permissible out-of-plumb deviation is $H / 500$ per ACI 117.
