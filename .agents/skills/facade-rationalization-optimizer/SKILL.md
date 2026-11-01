---
name: facade-rationalization-optimizer
description: Rationalizes complex double-curved architectural facades into cost-effective flat/quad panels, clusters panel types with machine learning (K-Means), and automates native Revit Adaptive Component generation via Rhino.Inside.Revit.
---

# Facade Rationalization Optimizer (ترشيد وتصنيع الواجهات المعمارية المعقدة)

A computational facade engineering skill designed to solve the economic and fabrication bottlenecks of complex architectural building envelopes. It converts free-form, double-curved surfaces into planar panels, minimizes distinct panel mold types through clustering algorithms, and automates the streaming of fabrication-ready **Adaptive Components** into Autodesk Revit.

---

## 1. Computational Pipeline

```
[Free-Form Double-Curved Facade Surface (NURBS / SubD)]
                           │
                           ▼
     [1. Surface Discretization & Panelization]
     - Isocurve UV parameterization / Diamond Quad Grid
     - Planarity verification: Diagonal distance & Out-of-plane twist angle
                           │
                           ▼
     [2. Planarization Optimization Solver (Kangaroo)]
     - Project non-planar quads onto tangent planes
     - Co-planarity deviation threshold: Max <= 2.0 mm (fabrication tolerance)
                           │
                           ▼
     [3. Machine Learning Typology Clustering (Scikit-Learn)]
     - Feature vector: [Edge1, Edge2, Edge3, Edge4, Diag1, Diag2, Area]
     - K-Means grouping to reduce 2,000 unique panels down to <= 30 mold groups
                           │
                           ▼
     [4. Direct Revit Generation (Rhino.Inside.Revit)]
     - Instantiate native 4-Point Adaptive Component Families
     - Populate parameters: Panel Mark, Cluster_Type_ID, Fabrication_Status
```

---

## 2. Planarity Analysis & Twist Angle Formula

For any 4-corner quad panel with vertices $P_0, P_1, P_2, P_3$:
1. The diagonal vectors are:
   $$\vec{d}_1 = P_2 - P_0, \quad \vec{d}_2 = P_3 - P_1$$
2. The plane normal is computed via the cross product:
   $$\vec{n} = \frac{\vec{d}_1 \times \vec{d}_2}{\|\vec{d}_1 \times \vec{d}_2\|}$$
3. The out-of-plane deviation $h$ of vertex $P_3$ relative to the plane formed by $P_0, P_1, P_2$ is:
   $$h = \left| (P_3 - P_0) \cdot \frac{(P_1 - P_0) \times (P_2 - P_0)}{\|(P_1 - P_0) \times (P_2 - P_0)\|} \right|$$
   - If $h \le 2.0\text{ mm}$, the panel is **Planar** (Standard cold-formed or flat glass).
   - If $h > 2.0\text{ mm}$, the panel requires **Cold-Bending** or expensive **Hot-Bent Molds**.

---

## 3. Panel Clustering & Typology Reduction (Python Recipe)

```python
import numpy as np
from sklearn.cluster import KMeans

def cluster_facade_panels(panel_vertices_list, target_types_count: int = 25):
    """
    Groups panels by geometric similarity to minimize distinct factory molds.
    Each panel is represented by its 4 edge lengths and 2 diagonal lengths.
    """
    feature_matrix = []
    
    for quad in panel_vertices_list:
        # quad is 4 points: [[x0,y0,z0], [x1,y1,z1], [x2,y2,z2], [x3,y3,z3]]
        pts = np.array(quad)
        e0 = np.linalg.norm(pts[1] - pts[0])
        e1 = np.linalg.norm(pts[2] - pts[1])
        e2 = np.linalg.norm(pts[3] - pts[2])
        e3 = np.linalg.norm(pts[0] - pts[3])
        d0 = np.linalg.norm(pts[2] - pts[0])
        d1 = np.linalg.norm(pts[3] - pts[1])
        
        feature_matrix.append([e0, e1, e2, e3, d0, d1])

    X = np.array(feature_matrix)

    # Apply K-Means
    kmeans = KMeans(n_clusters=target_types_count, random_state=42, n_init=10)
    labels = kmeans.fit_predict(X)

    clustered_results = []
    for idx, cluster_id in enumerate(labels):
        clustered_results.append({
            "panel_index": idx,
            "type_id": f"TYPE_{cluster_id:02d}",
            "cluster_center": kmeans.cluster_centers_[cluster_id].tolist()
        })

    return {
        "total_panels": len(panel_vertices_list),
        "unique_types_count": target_types_count,
        "panels": clustered_results
    }
```

---

## 4. Instantiating Revit Adaptive Components (Rhino.Inside.Revit)

```python
import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitServices")
from Autodesk.Revit.DB import Document, Transaction, XYZ, FamilySymbol
from Autodesk.Revit.DB.Structure import AdaptiveComponentInstanceUtils

def place_adaptive_panels(doc, family_symbol, panels_data):
    """
    Instantiates 4-point adaptive component panels in Revit and sets their mark parameters.
    """
    with Transaction(doc, "Generate Rationalized Facade Panels") as t:
        t.Start()
        if not family_symbol.IsActive:
            family_symbol.Activate()

        for panel in panels_data:
            # 1. Create instance
            instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, family_symbol)
            point_ids = AdaptiveComponentInstanceUtils.GetPlacementPoints(instance)

            # 2. Set placement point coordinates (XYZ in feet)
            coords = panel["corners_feet"]
            for i in range(4):
                point_elem = doc.GetElement(point_ids[i])
                point_elem.Position = coords[i]

            # 3. Inject metadata
            instance.LookupParameter("Panel_Type_ID").Set(panel["type_id"])
            instance.LookupParameter("Mark").Set(f"P-{panel['panel_index']:04d}")

        t.Commit()
```

---

## 5. Economic & Fabrication Value
- **Standard Flat Glazing**: \$250 - \$450 per $\text{m}^2$.
- **Cold-Bent Glazing** ($h \le 30\text{ mm}$): \$600 - \$900 per $\text{m}^2$.
- **Hot-Bent Double-Curved Glass**: \$1,800 - \$3,500 per $\text{m}^2$.
- **Outcome**: Rationalization typically cuts exterior envelope procurement costs by **40% to 65%** while preserving the architect's design intent.
