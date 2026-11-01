---
name: 3dsmax-automation
description: >-
  Automates Autodesk 3ds Max workflows using MAXScript and Python (pymxs), scene hierarchy traversal, material/texture inspection,
  mesh topology optimization, headless batch execution (3dsmaxbatch), and clean export pipelines (FBX/glTF/USD) for Revit and BIM interop.
---

# 3ds Max Automation & Interop Engine

Engineering reference for scripting, batch-processing, and extracting geometric/material data from Autodesk 3ds Max scenes, specifically optimized for Revit and BIM interoperability (`MaxToRevitConverter`).

## 1. Python (`pymxs`) & MAXScript Architecture

Always combine Python's data-processing power with native MAXScript scene runtime:

```python
from pymxs import runtime as rt

def inspect_and_clean_scene():
    """Inspects scene geometry, resets transforms, and prepares meshes for BIM export."""
    # Deselect all
    rt.clearSelection()
    
    # Iterate through all geometry nodes
    geom_nodes = [node for node in rt.geometry if not rt.isGroupHead(node)]
    
    cleaned_nodes = []
    for node in geom_nodes:
        # Skip hidden or frozen nodes
        if node.isHidden or node.isFrozen:
            continue
            
        # Ensure node is an Editable Mesh or Poly
        if rt.classOf(node) != rt.Editable_Poly and rt.classOf(node) != rt.Editable_mesh:
            rt.convertToPoly(node)
            
        # Reset XForm to ensure correct scaling and rotation axes
        rt.resetXForm(node)
        rt.collapseStack(node)
        
        # Verify normals are facing outward
        rt.unifyNormals(node)
        
        cleaned_nodes.append(node)
        
    return cleaned_nodes
```

---

## 2. Geometry & Mesh Extraction for Revit DirectShape

When exporting geometry for Revit ingestion (e.g., via `MaxToRevitConverter`):
* **Coordinate System Alignment:**
  * 3ds Max: $Z$-Up, Right-Handed (Standard architectural units: Millimeters).
  * Revit: $Z$-Up, Right-Handed (Internal units: Decimal Feet).
  * Conversion factor: $\text{Feet} = \text{Millimeters} / 304.8$.
* **Mesh Tessellation Invariants:**
  * Eliminate non-manifold vertices, zero-area faces, and coincident coplanar polygons.
  * Decompose n-gons into triangles/quads before exporting to prevent Revit geometry kernel (`Revit.DB.TessellatedShapeBuilder`) errors.
* **Material & Multi/Sub-Object Translation:**
  ```python
  def get_node_materials(node):
      """Extracts diffuse colors and texture paths from Standard or Physical materials."""
      mat = node.material
      if mat is None:
          return {"type": "default", "color": [0.8, 0.8, 0.8]}
          
      if rt.classOf(mat) == rt.Physical_Material:
          col = mat.base_color
          return {
              "type": "Physical",
              "color": [col.r / 255.0, col.g / 255.0, col.b / 255.0],
              "roughness": mat.roughness
          }
      return {"type": str(rt.classOf(mat)), "name": mat.name}
  ```

---

## 3. Headless Batch Execution (`3dsmaxbatch.exe`)

Run automation jobs without launching the full 3ds Max UI:
```bash
# Execute headless script on target Max file
"C:\Program Files\Autodesk\3ds Max 2026\3dsmaxbatch.exe" \
  "C:\Projects\Assets\source_model.max" \
  -sceneFile "C:\Projects\Assets\source_model.max" \
  -script "C:\Scripts\export_clean_fbx.py" \
  -v 5
```

---

## 4. Standard Interop Formats
* **glTF / GLB:** Preferred for lightweight web viewers, preserving PBR textures with zero scale distortion.
* **FBX:** Preserves hierarchy and UV coordinates for direct import into Revit Family Editor (`.rfa`) or DirectShape components.
* **OpenUSD (`.usdc` / `.usda`):** High-fidelity architectural asset interchange preserving complex materials and instances.
