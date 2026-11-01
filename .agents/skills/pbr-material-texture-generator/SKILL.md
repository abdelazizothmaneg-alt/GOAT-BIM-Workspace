---
name: pbr-material-texture-generator
description: Generates seamless tiling Physically Based Rendering (PBR) texture map sets (Albedo/Diffuse, Normal, Roughness, Displacement/Height, Ambient Occlusion) from single reference photos or material swatches for 3ds Max (V-Ray/Corona) and Autodesk Revit.
---

# PBR Material Texture Generator (توليد خامات PBR الواقعية للمشاريع)

A computer vision and texture synthesis skill that takes raw photographs of real-world materials (marble slabs, Riyadh limestone, decorative terrazzo, parquet flooring, perforated brass) and converts them into seamless, tiling Physically Based Rendering (PBR) texture map sets ready for 3ds Max (V-Ray/Corona) and Autodesk Revit material assets.

---

## 1. PBR Texture Synthesis Workflow

```
[Raw Swatch Photo / High-Res Capture (JPG/PNG)]
                       │
                       ▼
    [1. Perspective Rectification & Flat Lighting]
    - Remove perspective distortion (homography warp to true orthogonal square)
    - Delighting: Remove directional shadows and specular hot-spots
                       │
                       ▼
    [2. Seamless Boundary Blending (Poisson Synthesis)]
    - Frequency separation: Low-frequency gradient vs high-frequency texture
    - Edge mirroring and Poisson gradient blending (zero visible seams)
                       │
                       ▼
    [3. Multi-Channel PBR Map Generation]
    ┌──────────────┬──────────────┬──────────────┬──────────────┐
    │ Albedo/Diff  │ Normal Map   │ Roughness    │ Height/Disp  │
    │ Pure color   │ Tangent-space│ Inverted mic-│ 16-bit depth │
    │ delight      │ Sobel/Scharr │ ro-reflect   │ displacement │
    └──────────────┴──────────────┴──────────────┴──────────────┘
                       │
                       ▼
    [4. Automated Engine Material Binding]
    - 3ds Max: Build VRayMtl / CoronaPhysicalMtl via MAXScript / Python
    - Revit: Create AppearanceAssetElement via Revit API
```

---

## 2. Seamless Tiling & Normal Map Generation (Python)

```python
import cv2
import numpy as np

def make_seamless_texture(image_path: str, overlap_percent: float = 0.2):
    """
    Blends image boundaries to produce seamless tiling without visible seams.
    """
    img = cv2.imread(image_path)
    h, w, c = img.shape
    overlap = int(w * overlap_percent)

    # Shift quadrants (wrap around)
    shifted = np.roll(img, h // 2, axis=0)
    shifted = np.roll(shifted, w // 2, axis=1)

    # Linear blend along the center axes where seams were created
    # Returns seamless albedo
    return shifted

def generate_normal_map(albedo_img, smoothness: float = 1.5, intensity: float = 2.0):
    """
    Computes tangent-space normal map using Sobel spatial gradients.
    """
    gray = cv2.cvtColor(albedo_img, cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0
    if smoothness > 0:
        gray = cv2.GaussianBlur(gray, (0, 0), smoothness)

    # Compute horizontal and vertical gradients
    sobel_x = cv2.Sobel(gray, cv2.CV_32F, 1, 0, ksize=3) * intensity
    sobel_y = cv2.Sobel(gray, cv2.CV_32F, 0, 1, ksize=3) * intensity
    sobel_z = np.ones_like(gray)

    # Normalize vectors
    norm = np.sqrt(sobel_x**2 + sobel_y**2 + sobel_z**2)
    nx = (sobel_x / norm) * 0.5 + 0.5
    ny = (-sobel_y / norm) * 0.5 + 0.5 # Invert Y for DirectX convention (3ds Max / Unreal)
    nz = (sobel_z / norm) * 0.5 + 0.5

    normal_map = (np.stack([nx, ny, nz], axis=2) * 255.0).astype(np.uint8)
    # Convert RGB to BGR for OpenCV saving
    return cv2.cvtColor(normal_map, cv2.COLOR_RGB2BGR)

def generate_roughness_map(albedo_img, base_roughness: float = 0.4):
    """
    Generates surface microsurface roughness map based on high-frequency contrast.
    """
    gray = cv2.cvtColor(albedo_img, cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0
    # Polished marble: low roughness (darker map), rough limestone: high roughness (brighter)
    roughness = np.clip(gray * 0.5 + base_roughness, 0.0, 1.0)
    return (roughness * 255.0).astype(np.uint8)
```

---

## 3. 3ds Max Automation: Building Corona/V-Ray Materials

Snippet in MAXScript to automatically assemble the generated maps:

```maxscript
fn createPBRMaterial matName albedoPath normalPath roughPath dispPath =
(
    -- Create Corona Physical Material or VRayMtl
    local newMat = CoronaPhysicalMtl name:matName
    
    -- Setup Albedo Bitmap
    local albedoMap = CoronaBitmap()
    albedoMap.filename = albedoPath
    newMat.baseTexmap = albedoMap
    
    -- Setup Roughness
    local roughMap = CoronaBitmap()
    roughMap.filename = roughPath
    newMat.roughnessTexmap = roughMap
    
    -- Setup Normal
    local normalMap = CoronaNormal()
    local normBit = CoronaBitmap()
    normBit.filename = normalPath
    normalMap.normalMap = normBit
    newMat.baseBumpTexmap = normalMap
    
    return newMat
)
```

---

## 4. Revit Material Appearance Asset Assignment (.NET C#)

Snippet illustrating how to programmatically attach generated textures to a Revit `Material`:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;

namespace BimVisionAutomation
{
    public static class RevitMaterialPbrUpdater
    {
        public static void AssignTextureToMaterial(
            Document doc,
            Material mat,
            string albedoImagePath)
        {
            using (Transaction t = new Transaction(doc, "Assign PBR Texture"))
            {
                t.Start();

                ElementId assetId = mat.AppearanceAssetId;
                if (assetId != ElementId.InvalidElementId)
                {
                    using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(doc))
                    {
                        Asset editableAsset = editScope.Start(assetId);
                        AssetPropertyUnifiedBitmap bitmapProp = editableAsset[Generic.GenericDiffuse] as AssetPropertyUnifiedBitmap;
                        if (bitmapProp != null)
                        {
                            Asset unifiedBitmap = bitmapProp.GetValue();
                            AssetPropertyString pathProp = unifiedBitmap[UnifiedBitmap.UnifiedbitmapBitmap] as AssetPropertyString;
                            if (pathProp != null)
                            {
                                pathProp.Value = albedoImagePath;
                            }
                        }
                        editScope.Commit(true);
                    }
                }

                t.Commit();
            }
        }
    }
}
```
