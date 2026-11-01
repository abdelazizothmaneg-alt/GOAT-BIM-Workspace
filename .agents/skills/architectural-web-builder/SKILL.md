---
name: architectural-web-builder
description: Designs and builds high-performance architectural agency websites, interactive 3D WebGL/Three.js building viewers, before/after render sliders, responsive dark/light themes, and bilingual Arabic (RTL) / English layouts.
---

# Architectural Web Builder (تصميم وبناء المواقع المعمارية التفاعلية)

A high-end web design and front-end engineering skill tailored for architectural firms, visualization studios, and BIM consultancies. It crafts **Awwwards-tier digital experiences** featuring interactive 3D WebGL model viewports, smooth render-to-reality comparison sliders, elegant responsive grid layouts, and seamless bilingual (Arabic RTL and English LTR) typography.

---

## 1. Architectural Web Experience Architecture

```
[Hero Section]
  - Full-viewport project hero canvas (Cinematic video loop / 3D WebGL)
  - Minimalist fixed header with language toggle (عربي / EN) & Theme switch
                    │
                    ▼
[Featured Projects Grid / Case Studies]
  - Asymmetric masonry grid with hover elevation & metadata badges
  - Project filters: Residential, Commercial, Cultural, Urban Design
                    │
                    ▼
[Interactive 3D Model & Floor Plan Viewer]
  - Three.js / React Three Fiber interactive building massing
  - Orbit, pan, and click on floors to isolate levels
                    │
                    ▼
[Render-to-Reality Interactive Slider]
  - Draggable split-screen curtain comparing 3D render vs built photo
                    │
                    ▼
[Firm Philosophy, Awards & Leadership]
  - Monospaced factsheet numbers, publications, and team credentials
                    │
                    ▼
[Footer / Contact / Downloadable Dossier]
  - Interactive project enquiry form & PDF Capability Statement download
```

---

## 2. Interactive 3D Model Viewer Component (Three.js / React Three Fiber)

```jsx
import React, { useRef } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, useGLTF, Environment, ContactShadows } from '@react-three/drei';

function ArchitecturalBuildingModel({ modelUrl }) {
  const { scene } = useGLTF(modelUrl);
  const modelRef = useRef();

  // Subtle architectural turntable rotation
  useFrame((state, delta) => {
    if (modelRef.current) {
      modelRef.current.rotation.y += delta * 0.05;
    }
  });

  return (
    <primitive 
      ref={modelRef} 
      object={scene} 
      scale={0.5} 
      position={[0, -1, 0]} 
    />
  );
}

export default function BuildingViewerCanvas() {
  return (
    <div className="w-full h-[600px] bg-neutral-950 rounded-2xl overflow-hidden relative">
      <Canvas camera={{ position: [5, 4, 7], fov: 40 }}>
        <ambientLight intensity={0.7} />
        <directionalLight position={[10, 15, 10]} intensity={1.5} castShadow />
        <ArchitecturalBuildingModel modelUrl="/models/villa_massing.glb" />
        <Environment preset="city" />
        <ContactShadows position={[0, -1, 0]} opacity={0.6} scale={10} blur={2.5} far={4} />
        <OrbitControls enableZoom={true} maxPolarAngle={Math.PI / 2.1} minDistance={3} maxDistance={15} />
      </Canvas>
      <div className="absolute bottom-4 right-4 text-xs font-mono text-neutral-400 bg-neutral-900/80 px-3 py-1.5 rounded-lg border border-neutral-800">
        اسحب للتدوير ثلاثي الأبعاد • 3D Interactive Model
      </div>
    </div>
  );
}
```

---

## 3. Render-to-Reality Interactive Comparison Slider (Tailwind / JS)

```html
<div class="relative w-full aspect-[16/9] rounded-2xl overflow-hidden select-none" id="comparison-slider">
  <!-- Built Reality Photo (Bottom Layer) -->
  <img src="/assets/built_photo.webp" alt="Realized Project" class="absolute inset-0 w-full h-full object-cover">
  
  <!-- 3D Render (Top Clipped Layer) -->
  <div class="absolute inset-0 w-1/2 overflow-hidden border-r-2 border-white shadow-2xl" id="clipped-layer">
    <img src="/assets/3d_render.webp" alt="3D Architectural Render" class="absolute inset-0 w-[200%] max-w-none h-full object-cover">
  </div>

  <!-- Draggable Handle -->
  <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-10 h-10 bg-white text-neutral-950 rounded-full flex items-center justify-center font-bold shadow-xl cursor-ew-resize">
    ↔
  </div>
</div>
```

---

## 4. Typography & Styling Guidelines
- **Arabic Heading**: `Cairo` (font-weight: 700 / 800) or `Almarai` (font-weight: 800).
- **Body & Technical Specs**: `Inter` / `JetBrains Mono` for metadata numbers (e.g., $12,500\text{ m}^2$, $\text{LOD } 400$).
- **Color Identity**: Minimalist dark background (`#0a0d14` or `#0f172a`) paired with warm architectural stone accents (`#d4af37` gold, `#e27b58` terracotta, `#14b8a6` teal).
