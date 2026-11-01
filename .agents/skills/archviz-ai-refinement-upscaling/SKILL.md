---
name: archviz-ai-refinement-upscaling
description: Applies generative AI inpainting, ControlNet geometric locking, and 8K super-resolution upscaling to enhance architectural micro-details without hallucinating design geometry.
---

# ArchViz AI Refinement & Detail Upscaling

Cutting-edge generative AI post-production skill for architectural visualization. Specializes in targeted inpainting, ControlNet geometric preservation, procedural micro-vegetation synthesis, and 8K/12K super-resolution detail enhancement.

---

## 1. The Core AI ArchViz Rule: "Zero Hallucination of Architecture"

Generic AI image generation breaks architectural geometry (bending straight columns, altering window rhythms, inventing impossible stairs). Professional ArchViz AI refinement enforces strict geometric locking:

```
[ 3D Render Output ] ──> [ ControlNet Depth + MLSD Wireframe Lock ] ──> [ Targeted Masking Inpaint ] ──> [ 8K Tectonic Upscale ]
                         (100% Structural Protection:                   (Applies ONLY to ground          (Restores micro-grooves,
                          zero changes to facade or massing)             foliage, puddles, textures)      concrete pores, fabric weaves)
```

---

## 2. Targeted Inpainting Workflows

### A. Foreground Organic Micro-Foliage
- **Problem**: 3D grass near the camera often looks repetitive, floating, or low-poly.
- **Workflow**:
  - Mask the bottom $15\%$ of the image (foreground sidewalk/curb interface).
  - Prompt: `"natural wild dandelion weeds growing in sidewalk concrete cracks, scattered gravel, damp soil, hyper-photorealistic, 8k"`.
  - Denoising Strength: $0.35 - 0.45$ (maintains base render lighting and perspective).

### B. Interior Life & Window Habitation
- **Problem**: Large commercial towers look abandoned with empty, dark interior floors.
- **Workflow**:
  - Inpaint interior window panes using a Cryptomatte glass mask.
  - Prompt: `"warm glowing modern Scandinavian office interior, wooden bookshelves, subtle blurred silhouettes of people working, 2700K ambient warmth"`.
  - Denoising Strength: $0.30$.

---

## 3. Super-Resolution 8K Tectonic Upscaling (ComfyUI / Ultimate SD Upscale)

To prepare competition boards and massive print banners ($A0 / A1$ at $300\text{ DPI}$):
1. **Model Selection**: Tile-based latent upscaler (e.g. RealESRGAN / 4x-UltraSharp).
2. **Tile Size**: $1024 \times 1024$ with $64\text{ px}$ overlap to eliminate boundary seam artifacts.
3. **Texture Injection**: Micro-scale noise injection restores pores in architectural limestone, brushed grain on anodized aluminum, and thread weaves on upholstered furniture.

---

## 4. Python ComfyUI / API Prompt Payload Generator

```python
"""
ArchViz AI Inpainting & Upscale Payload Generator
Configures ControlNet parameters to prevent architectural drift
"""

import json

def build_archviz_inpaint_payload(render_image_path: str, mask_path: str, prompt_concept: str, denoise_strength: float = 0.38) -> dict:
    return {
        "client_id": "archviz_ai_pipeline",
        "prompt": {
            "3": {
                "class_type": "KSampler",
                "inputs": {
                    "cfg": 6.5,
                    "denoise": denoise_strength, # Keep low to prevent architectural distortion!
                    "steps": 28,
                    "sampler_name": "dpmpp_2m_sde",
                    "scheduler": "karras"
                }
            },
            "10": {
                "class_type": "ControlNetApplyAdvanced",
                "inputs": {
                    "strength": 0.95, # 95% geometric lock
                    "start_percent": 0.0,
                    "end_percent": 0.85
                }
            },
            "20": {
                "class_type": "CLIPTextEncode",
                "inputs": {
                    "text": f"{prompt_concept}, architectural photography, raw photo, realistic textures, highly detailed, masterwork",
                    "negative_text": "cartoon, distorted geometry, bent lines, low resolution, blurred, oversaturated, deformed columns"
                }
            }
        }
    }

if __name__ == "__main__":
    payload = build_archviz_inpaint_payload(
        render_image_path="C:/Renders/Facade_4K.png",
        mask_path="C:/Renders/Sidewalk_Mask.png",
        prompt_concept="weathered stone pavement with wet water puddles and subtle autumn fallen leaves",
        denoise_strength=0.35
    )
    print("Configured ArchViz AI ControlNet Payload successfully.")
```

---

## 5. Deliverables Checklist
1. **Geometric Fidelity Difference Map**: Pixel-level comparison ensuring $0\%$ deviation on all structural loadbearing edges.
2. **8K Master Print File**: Ultra-high-resolution TIFF/PNG file rendered at $7680 \times 4320$ with verified micro-tectonic texture.
