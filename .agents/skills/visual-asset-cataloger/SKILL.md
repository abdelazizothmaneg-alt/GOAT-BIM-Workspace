---
name: visual-asset-cataloger
description: Indexes, categorizes, and searches architectural project renders and design reference libraries using multimodal vector embeddings (CLIP/SigLIP), natural language semantic search, automated architectural taxonomy tagging, and color palette extraction.
---

# Visual Asset Cataloger (فهرسة والبحث الدلالي في أرشيف الصور المعمارية)

An AI-driven visual asset management and semantic search skill for architectural firms, interior designers, and visualization studios. It transforms unorganized image repositories into high-speed searchable vector databases capable of natural language queries, visual similarity lookups, automated architectural taxonomy classification, and harmonic color palette extraction.

---

## 1. Cataloging & Retrieval Architecture

```
[Raw Image Repository (Renders, Site Photos, Moodboards)]
                           │
                           ▼
     [1. Visual Feature & Embedding Extraction]
     - Multimodal Vision Backbone (OpenAI CLIP ViT-L/14 or SigLIP)
     - 768 / 1152-dimensional dense visual vectors
     - K-Means dominant color extraction (HEX / RGB / RAL)
                           │
                           ▼
     [2. Automated Architectural Taxonomy Classification]
     - Typology: Residential Villa, High-Rise Commercial, Mosque, Civic
     - Architectural Style: Salmani, Neoclassic, Modern, Brutalist, Islamic
     - Space Type: Exterior Facade, Master Bedroom, Majlis, Landscaping
     - Lighting / Atmosphere: Direct Noon Sun, Golden Hour, Moody Dusk
                           │
                           ▼
     [3. Vector Database Storage & Indexing]
     - Fast vector indexing (FAISS / Qdrant / ChromaDB / SQLite-vss)
     - Metadata filtering by project name, year, author, and dominant color
                           │
                           ▼
     [4. Dual Search Interfaces]
     - Natural Language Search: "Travertine villa with cantilevered shading"
     - Reverse Image Similarity: Query by image tensor cosine distance
```

---

## 2. Semantic Search & Zero-Shot Classification (Python)

```python
import torch
from PIL import Image
import numpy as np

class ArchitecturalImageCataloger:
    def __init__(self, model_name: str = "openai/clip-vit-base-patch32"):
        # Uses standard CLIP / OpenCLIP / SigLIP embeddings
        from transformers import CLIPProcessor, CLIPModel
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.model = CLIPModel.from_pretrained(model_name).to(self.device)
        self.processor = CLIPProcessor.from_pretrained(model_name)

        # Standard Architectural Classification Taxonomies
        self.style_candidates = [
            "Modern minimalist architecture",
            "Contemporary Salmani architecture with Najdi elements",
            "Luxury classic and neoclassical interior",
            "Japandi and Scandinavian minimalist interior",
            "Industrial raw concrete and exposed metal"
        ]

    def encode_image(self, image_path: str) -> np.ndarray:
        image = Image.open(image_path).convert("RGB")
        inputs = self.processor(images=image, return_tensors="pt").to(self.device)
        with torch.no_grad():
            image_features = self.model.get_image_features(**inputs)
            # Normalize vector for cosine similarity
            image_features /= image_features.norm(dim=-1, keepdim=True)
        return image_features.cpu().numpy()[0]

    def classify_style(self, image_path: str):
        image = Image.open(image_path).convert("RGB")
        inputs = self.processor(
            text=self.style_candidates,
            images=image,
            return_tensors="pt",
            padding=True
        ).to(self.device)

        with torch.no_grad():
            outputs = self.model(**inputs)
            logits_per_image = outputs.logits_per_image
            probs = logits_per_image.softmax(dim=1).cpu().numpy()[0]

        best_idx = int(np.argmax(probs))
        return {
            "predicted_style": self.style_candidates[best_idx],
            "confidence": float(probs[best_idx]),
            "all_scores": {style: float(prob) for style, prob in zip(self.style_candidates, probs)}
        }
```

---

## 3. Automated Dominant Color & Palette Extraction

Using K-Means clustering to identify the primary and secondary material colors:

```python
import cv2
from sklearn.cluster import KMeans

def extract_dominant_palette(image_path: str, n_colors: int = 5):
    img = cv2.imread(image_path)
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    resized = cv2.resize(img_rgb, (150, 150))
    pixels = resized.reshape(-1, 3)

    kmeans = KMeans(n_clusters=n_colors, n_init=5, random_state=42)
    kmeans.fit(pixels)

    palette = []
    for center in kmeans.cluster_centers_:
        r, g, b = [int(c) for c in center]
        hex_code = f"#{r:02x}{g:02x}{b:02x}"
        palette.append({
            "rgb": [r, g, b],
            "hex": hex_code
        })
    return palette
```

---

## 4. Querying Examples (Bilingual Natural Language)
- Arabic query: `"فيلا مودرن حجر طبيعي مع مسطحات مائية وإضاءة مسائية"` -> Maps to embedding and matches pool with $>0.82$ cosine similarity.
- English query: `"Double height living room with acoustic fluted timber slats"` -> Instantly isolates matching portfolio renders across projects.
