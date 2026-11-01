---
name: cad
description: >-
  Programmatic CAD modeling and parametric solid geometry creation using Python-based CAD kernels (CadQuery, build123d, OpenSCAD, and pythonocc),
  generating STEP, STL, and DXF files from natural language and mathematical specifications.
---

# Programmatic CAD Modeling (Text-to-CAD)

Constructive Solid Geometry (CSG) and Boundary Representation (B-Rep) parametric CAD authoring.

## 1. CadQuery & build123d Modeling Patterns
```python
import cadquery as cq

# Parametric architectural column with capital and base
height = 3000.0
shaft_radius = 200.0
base_size = 600.0
base_height = 150.0

# Build base
base = cq.Workplane("XY").box(base_size, base_size, base_height)

# Build shaft on top of base
shaft = (base.faces(">Z")
         .workplane()
         .circle(shaft_radius)
         .extrude(height - 2 * base_height))

# Build capital on top of shaft
capital = (shaft.faces(">Z")
           .workplane()
           .rect(base_size, base_size)
           .extrude(base_height))

# Combine and export to standard STEP format
result = base.union(shaft).union(capital)
cq.exporters.export(result, "parametric_column.step")
```

## 2. Boolean Operations & Filleting
* **Union, Difference, Intersection:** Always ensure solid manifolds with closed water-tight boundaries.
* **Edge Filleting & Chamfering:** Apply fillets (`.edges("|Z").fillet(5.0)`) post-Boolean to prevent topological non-manifold singularities.
* **Parametric Constraints:** Express geometric dimensions as driven variables to enable rapid generative variations.
