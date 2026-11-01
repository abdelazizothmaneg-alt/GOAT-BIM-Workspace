---
name: biomimetic-formfinding-minimal-surfaces
description: Generates bio-inspired architectural geometry, Triply Periodic Minimal Surfaces (Gyroid, Schwarz P/D), catenary hanging chain form-finding (Gaudí), and soap film tensile membranes (Frei Otto).
---

# Biomimetic Form-Finding & Minimal Surfaces (Gyroids)

Master-level computational architectural geometry and bio-inspired structural form-finding skill. Implements Triply Periodic Minimal Surfaces (TPMS - Gyroids), Gaudí inverted catenary gravity equilibrium, and Frei Otto minimal-surface tensile membranes.

---

## 1. Triply Periodic Minimal Surfaces (TPMS) & Gyroid Equations

A minimal surface has zero mean curvature ($H = 0$) everywhere, minimizing surface area for a given boundary, eliminating internal stress concentrations, and maximizing structural strength-to-weight ratios.

### A. The Gyroid TPMS Approximation Formula:
$$\sin(x) \cdot \cos(y) + \sin(y) \cdot \cos(z) + \sin(z) \cdot \cos(x) = t$$
- When iso-value $t = 0$: Partitions space into two continuous, intertwined labyrinths without self-intersecting.
- **Applications**: 3D printed ultra-lightweight structural columns, heat-dissipating architectural facades, and acoustic absorption baffles.

### B. The Schwarz P (Primitive) Surface:
$$\cos(x) + \cos(y) + \cos(z) = t$$

### C. The Schwarz D (Diamond) Surface:
$$\cos(x)\cos(y)\cos(z) - \sin(x)\sin(y)\sin(z) = t$$

---

## 2. Gaudí Inverted Catenary & Frei Otto Soap Film Physics

### A. Gaudí Catenary Hanging Curve:
$$y = a \cdot \cosh\left(\frac{x}{a}\right) = \frac{a}{2} \cdot \left( e^{x/a} + e^{-x/a} \right)$$
- An inverted hanging chain represents a state of **pure axial compression** with zero bending moments ($M = 0$).

### B. Frei Otto Minimal Surface Tensile Net:
- Solves Laplace’s equation for surface coordinates $\mathbf{x}(u, v)$:
  $$\Delta \mathbf{x} = \frac{\partial^2 \mathbf{x}}{\partial u^2} + \frac{\partial^2 \mathbf{x}}{\partial v^2} = 0$$
- Simulates soap films dipped across rigid outer wire boundaries using particle-spring dynamic relaxation (Kangaroo physics).

---

## 3. Python Gyroid Mesh Generator (NumPy / Marching Cubes)

```python
"""
Parametric Gyroid TPMS Architectural Mesh Generator
Outputs volumetric isosurface points for Rhino / Blender / Revit DirectShape
"""

import math

def evaluate_gyroid(x: float, y: float, z: float, scale: float = 1.0) -> float:
    """Evaluates the Gyroid implicit field value at coordinate (x,y,z)."""
    sx = x * scale
    sy = y * scale
    sz = z * scale
    val = (math.sin(sx) * math.cos(sy) + 
           math.sin(sy) * math.cos(sz) + 
           math.sin(sz) * math.cos(sx))
    return val

def generate_gyroid_point_lattice(bounds_m: float = 3.0, resolution: int = 25, scale: float = 2.0, iso_threshold: float = 0.08) -> list:
    """
    Extracts 3D coordinates falling within the minimal surface boundary envelope.
    """
    points = []
    step = bounds_m / resolution
    for i in range(resolution):
        x = (i * step) - (bounds_m / 2.0)
        for j in range(resolution):
            y = (j * step) - (bounds_m / 2.0)
            for k in range(resolution):
                z = (k * step) - (bounds_m / 2.0)
                field_val = evaluate_gyroid(x, y, z, scale)
                if abs(field_val) <= iso_threshold:
                    points.append((round(x, 3), round(y, 3), round(z, 3)))
    return points

if __name__ == "__main__":
    lattice = generate_gyroid_point_lattice(bounds_m=2.0, resolution=20, scale=3.14159)
    print(f"Generated {len(lattice)} minimal surface coordinates for architectural canopy node.")
```

---

## 4. Deliverables Checklist
1. **Rhino / Grasshopper Definition (.gh)**: Kangaroo dynamic relaxation solver with catenary anchor points and mesh sub-division.
2. **Revit DirectShape Integration (C#)**: Conversion of non-manifold minimal surface meshes into native Revit massing elements.
3. **FEA Stress Uniformity Dossier**: Verification that bending stresses are $< 10\%$ of axial compressive forces.
