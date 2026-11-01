---
name: floorplan-vector-vision
description: Analyzes raster architectural floor plan images (JPG, PNG, scan PDF, screenshots), performs spatial segmentation, identifies exterior/interior wall centerlines, door swings, window openings, and room boundaries, and converts them into structured vector polylines or CAD/BIM elements.
---

# Floorplan Vector Vision (رؤية المخططات المعمارية وتحويلها إلى متجهات)

A specialized architectural computer vision skill designed to inspect raster floor plan drawings (scanned blueprints, PDF exports, concept screenshots, hand-drawn layout sketches), perform intelligent semantic segmentation, and extract clean, orthogonal vector geometry suitable for direct generation of CAD polylines or native Autodesk Revit walls, doors, and rooms.

---

## 1. Vision Analysis Pipeline

```
[Raster Floor Plan (PNG/JPG/PDF)]
               │
               ▼
   [1. Preprocessing & Scale Calibration]
   - Grayscale conversion, adaptive thresholding (Otsu)
   - Graphic scale bar / known dimension detection (pixel-to-mm ratio)
   - Text & annotation masking (removing text to prevent centerline artifacts)
               │
               ▼
   [2. Semantic Feature Segmentation]
   - Exterior Walls (Thick dark strokes / double parallel lines)
   - Interior Partitions (Thin / medium lines)
   - Door Swing Arcs (90° circular arcs + door leaf vectors)
   - Window Openings (Thin multi-line parallel bridges across wall pockets)
   - Room Spaces (Connected empty spatial regions)
               │
               ▼
   [3. Geometric Vectorization & Orthogonal Regularization]
   - Morphological thinning (Zhang-Suen / Medial Axis skeletonization)
   - Line segment approximation (Douglas-Peucker)
   - Orthogonal snapping (0°, 90°, 45° alignment)
   - Wall junction topology resolution (T-junctions, L-corners, X-crossings)
               │
               ▼
   [4. Downstream Export & BIM Generation]
   - AutoCAD DXF / Script: Closed LWPOLYLINE on specific layers
   - Autodesk Revit API: Wall.Create(), FamilyInstance (Doors/Windows), Spatial Rooms
```

---

## 2. Scale & Metric Calibration

Before any vector extraction, the image's real-world scale must be established:
1. **Dimension Line OCR**: Detect dimension tick marks and read the numeric value (e.g., `4500 mm` or `4.50 m`).
2. **Graphic Scale Bar**: Detect standard bar segments (0, 1m, 2m, 5m).
3. **Fallback Default Proportions**: When scale is absent, use standard architectural reference dimensions:
   - Standard single door leaf swing: `900 mm` (0.90 m).
   - Exterior wall typical thickness: `250 - 300 mm` (0.25 - 0.30 m).
   - Interior partition wall typical thickness: `100 - 150 mm` (0.10 - 0.15 m).
   - Standard stair tread depth: `280 - 300 mm`.

$$\text{Scale Ratio } S = \frac{\text{Distance}_{\text{real-world (mm)}}}{\text{Distance}_{\text{pixels}}}$$

---

## 3. Core Image Processing & Vectorization Recipe (Python)

```python
import cv2
import numpy as np

def process_floorplan(image_path: str, pixel_to_mm_ratio: float = 10.0):
    """
    Segment walls and extract clean centerlines from a floor plan image.
    """
    img = cv2.imread(image_path)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # 1. Binarization (Otsu thresholding to separate black lines from white background)
    _, binary = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)

    # 2. Text Removal (protect line skeletons from text character contamination)
    num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(binary, connectivity=8)
    wall_mask = np.zeros_like(binary)
    for i in range(1, num_labels):
        area = stats[i, cv2.CC_STAT_AREA]
        w = stats[i, cv2.CC_STAT_WIDTH]
        h = stats[i, cv2.CC_STAT_HEIGHT]
        aspect_ratio = max(w, h) / (min(w, h) + 1e-5)
        # Retain components that are large or elongated (walls)
        if area > 120 and (w > 30 or h > 30 or aspect_ratio > 3.0):
            wall_mask[labels == i] = 255

    # 3. Morphological Skeletonization (Medial Axis thinning)
    skeleton = cv2.ximgproc.thinning(wall_mask, thinningType=cv2.ximgproc.THINNING_ZHANGSUEN)

    # 4. Probabilistic Hough Line Transform for Straight Segments
    lines = cv2.HoughLinesP(
        skeleton,
        rho=1,
        theta=np.pi / 180,
        threshold=40,
        minLineLength=30,
        maxLineGap=10
    )

    vector_walls = []
    if lines is not None:
        for line in lines:
            x1, y1, x2, y2 = line[0]
            # Convert to millimeters & orthogonalize (snap to 0, 90, 45 degrees)
            dx = x2 - x1
            dy = y2 - y1
            length = np.hypot(dx, dy)
            angle = np.degrees(np.arctan2(dy, dx)) % 180

            # Snap angle
            if abs(angle - 0) < 6 or abs(angle - 180) < 6:
                y2 = y1  # Horizontal
            elif abs(angle - 90) < 6:
                x2 = x1  # Vertical
            elif abs(angle - 45) < 5:
                # 45-degree diagonal snap
                avg_delta = (abs(dx) + abs(dy)) / 2
                x2 = x1 + int(np.sign(dx) * avg_delta)
                y2 = y1 + int(np.sign(dy) * avg_delta)

            vector_walls.append({
                "start": (x1 * pixel_to_mm_ratio, y1 * pixel_to_mm_ratio),
                "end": (x2 * pixel_to_mm_ratio, y2 * pixel_to_mm_ratio),
                "length_mm": float(length * pixel_to_mm_ratio)
            })

    return {
        "status": "success",
        "wall_count": len(vector_walls),
        "walls": vector_walls
    }
```

---

## 4. Architectural Element Identification Rules

### A. Wall Classification
- **Exterior Boundary Walls**: Continuous outer loop, thickness $\ge 250\text{ mm}$, enclosed envelope.
- **Interior Partitions**: Internal branching networks, thickness $100 - 200\text{ mm}$, dividing zones.

### B. Doors & Openings
- **Single Swing Door**: Straight leaf line + $90^\circ$ circular arc pointing into the room.
- **Double Swing Door**: Two opposing quarter-circle arcs meeting at center.
- **Sliding Door (Pocket/Bypass)**: Two overlapping parallel lines within a wall gap.
- **Window**: Wall boundary lines that break into $2$ to $4$ parallel thin lines.

### C. Bilingual Space & Room Recognition (Arabic & English)
Detect room labels via OCR and associate the text centroid with the containing closed wall polygon:
| Arabic Label | English Standard | Typical Revit Room Tag |
| :--- | :--- | :--- |
| مجلس رجال | Majlis / Guest Reception | Living / Majlis |
| صالة معيشة | Family Living Room | Living Area |
| غرفة نوم رئيسية | Master Bedroom | Master Bedroom |
| مطبخ رئيسي / ديرتي | Dirty Kitchen / Prep Kitchen | Kitchen |
| دورة مياه / مغاسل | Powder Room / Bathroom | Toilet / WC |
| مستودع / مخزن | Storage / Pantry | Storage |
| غرفة خادمة | Maid's Room | Service Room |

---

## 5. Direct Autodesk Revit API Generation (.NET C#)

Snippet illustrating how to consume extracted vector coordinates and instantiate native Revit walls:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace BimVisionAutomation
{
    public static class FloorplanVectorImporter
    {
        public static void GenerateWallsFromVisionData(
            Document doc,
            Level targetLevel,
            WallType exteriorType,
            WallType interiorType,
            IEnumerable<(XYZ Start, XYZ End, bool IsExterior)> wallSegments)
        {
            using (Transaction t = new Transaction(doc, "Generate Walls from Vision Analysis"))
            {
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new SilentFailurePreprocessor());
                t.SetFailureHandlingOptions(failOpt);

                t.Start();

                double wallHeightFeet = 3000.0 / 304.8; // 3.0 meters default ceiling height

                foreach (var seg in wallSegments)
                {
                    Line wallCurve = Line.CreateBound(seg.Start, seg.End);
                    WallType selectedType = seg.IsExterior ? exteriorType : interiorType;

                    Wall wall = Wall.Create(
                        doc,
                        wallCurve,
                        selectedType.Id,
                        targetLevel.Id,
                        wallHeightFeet,
                        0.0,
                        false,
                        false
                    );
                }

                t.Commit();
            }
        }
    }

    public class SilentFailurePreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}
```

---

## 6. Best Practices & Quality Control

1. **Orthogonal Clamping**: Architectural drawings almost exclusively use $90^\circ$ and $45^\circ$ angles. Suppress floating-point micro-deviations (e.g., $89.84^\circ \rightarrow 90.00^\circ$).
2. **Endpoint Snapping & Welding**: Walls must share exact end vertices (tolerance $\le 50\text{ mm}$) to ensure Revit recognizes clean wall joins and encloses rooms without leakages.
3. **Coordinate Systems**: Map the image coordinates (Top-Left origin, Y down) to the CAD/Revit Cartesian coordinate system (Bottom-Left origin, Y up, Z up) with appropriate scaling ($1\text{ ft} = 304.8\text{ mm}$).
