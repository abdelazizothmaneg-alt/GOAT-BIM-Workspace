---
name: scan-to-bim-processing
description: >-
  Processes 3D laser scan point clouds (E57, LAS, RCP) for Scan-to-BIM modeling, automated RANSAC plane segmentation
  for walls and slabs, Revit PointCloudEngine API integration, and cloud-to-BIM deviation analysis.
---

# Scan-to-BIM & Reality Capture Processing

Engineering methodology and algorithms for translating dense 3D LiDAR point clouds into clean, parametric Autodesk Revit models.

## 1. Reality Capture Pipeline Overview

```text
[LiDAR Scan / Drone Photogrammetry] ──> [Filter & Subsample] ──> [RANSAC Plane Segmentation] ──> [Feature Extraction] ──> [Revit Native Elements]
```

---

## 2. RANSAC Plane Segmentation for Walls & Floors

Extract flat planar boundaries from unstructured point clusters $(X, Y, Z)$:

```python
import numpy as np

def ransac_plane_fitting(points, distance_threshold=0.015, max_iterations=1000):
    """
    Fits a mathematical plane Ax + By + Cz + D = 0 to 3D points using RANSAC.
    distance_threshold: maximum distance in meters for an inlier (e.g. 15mm for construction tolerance).
    """
    best_inliers = []
    best_plane = None
    num_points = len(points)
    
    for _ in range(max_iterations):
        # Sample 3 random points
        sample_indices = np.random.choice(num_points, 3, replace=False)
        p1, p2, p3 = points[sample_indices]
        
        # Calculate normal vector
        v1 = p2 - p1
        v2 = p3 - p1
        normal = np.cross(v1, v2)
        norm = np.linalg.norm(normal)
        if norm == 0:
            continue
        normal = normal / norm
        
        # D = - (A*x + B*y + C*z)
        d = -np.dot(normal, p1)
        
        # Calculate distance of all points to plane: |Ax + By + Cz + D|
        distances = np.abs(np.dot(points, normal) + d)
        inliers = np.where(distances < distance_threshold)[0]
        
        if len(inliers) > len(best_inliers):
            best_inliers = inliers
            best_plane = (normal[0], normal[1], normal[2], d)
            
    return best_plane, best_inliers
```

---

## 3. Revit Point Cloud API Integration (`PointCloudInstance`)

Query point clouds inside Revit projects:

```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;

namespace ORTools.RealityCapture
{
    public static class RevitPointCloudInspector
    {
        public static List<XYZ> ExtractBoundingBoxPoints(PointCloudInstance pcInstance, Outline outline)
        {
            List<XYZ> extractedPoints = new List<XYZ>();
            
            // Create filter for points within bounding box
            PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlanarFilter(new List<Plane>());
            
            // Read points in batches
            PointCollection pointCollection = pcInstance.GetPoints(filter, 0.05, 5000); // 50mm resolution
            foreach (CloudPoint pt in pointCollection)
            {
                extractedPoints.Add(new XYZ(pt.X, pt.Y, pt.Z));
            }

            return extractedPoints;
        }
    }
}
```

---

## 4. Cloud-to-BIM Deviation Analysis (Quality Control)
* Calculate perpendicular distance from point cloud points to the nearest Revit model surface.
* Generate a color-coded deviation heatmap:
  * Green: Within tolerance ($\le \pm 10\text{mm}$).
  * Yellow / Amber: Moderate deviation ($\pm 10\text{mm} - \pm 25\text{mm}$).
  * Red: Out of tolerance / structural deflection ($> \pm 25\text{mm}$).
