---
name: ai-image-generation
description: >-
  Generates and edits images by routing prompts across an extensive catalog of state-of-the-art AI image models
  (FLUX.1 Pro/Dev/Schnell, OpenAI DALL-E 3/GPT Image, ByteDance Dreamina, Alibaba Qwen Image) via runcomfy CLI and API endpoints.
  Handles Text-to-Image (T2I), Image-to-Image (I2I), style transfer, and inpainting.
metadata:
  requires:
    bins: ["runcomfy"]
---

# AI Image Generation & Model Router

High-performance visual generation skill integrating multi-model cloud inference for architectural renders, design concepts, and visual staging.

## 1. Dynamic Model Selection Matrix (Router)
Select the optimal engine based on the user's explicit creative intent:
* **Architectural & Photorealistic Fidelity:**
  * **Engine:** `FLUX.1 Pro` / `FLUX.1 Dev`
  * **Best For:** Accurate material textures (travertine, weathered zinc, wood grain), precise natural lighting (golden hour, overcast skylight), complex spatial geometries.
* **Typography & Precise In-Scene Signage:**
  * **Engine:** `OpenAI GPT Image` / `DALL-E 3`
  * **Best For:** Floor plan dimension text, storefront signage, branded banners, crisp lettering without character garbling.
* **Rapid Conceptual Iterations & Drafts:**
  * **Engine:** `FLUX.1 Schnell` / `ByteDance Dreamina` / `Z-Image Turbo`
  * **Best For:** Instant 4-step generation, concept exploration, client brainstorming sessions.
* **Stylistic & Painterly Rendering:**
  * **Engine:** `Alibaba Qwen Image` / `Wan 2.7`
  * **Best For:** Artistic concept art, watercolor architectural sketches, fantasy/editorial staging.

## 2. Execution Workflows

### A. Text-to-Image (T2I)
```bash
# General invocation via runcomfy CLI
runcomfy generate --model "flux-1-pro" \
  --prompt "Architectural hero photograph of a brutalist concrete villa, floor-to-ceiling glass, reflecting pool, dusk lighting, 8k resolution" \
  --aspect-ratio "16:9" \
  --output "./renders/villa_hero.png"
```

### B. Image-to-Image & Restyling (I2I)
```bash
# Restyling an existing architectural concept or room photograph
runcomfy edit --model "flux-1-dev" \
  --input "./input_facade.jpg" \
  --prompt "Transform facade cladding into vertical warm cedar wood battens and black anodized aluminum window mullions" \
  --strength 0.55 \
  --output "./renders/facade_restyled.png"
```

## 3. Best Practices
* Always specify explicit aspect ratios (`16:9` for landscapes/exteriors, `4:5` for architectural verticals/portraits, `1:1` for detail vignettes).
* Pair with [`architecture-rendering`](../architecture-rendering/SKILL.md) and [`interior-design-visualization`](../interior-design-visualization/SKILL.md) for domain-specific prompt enhancements.
