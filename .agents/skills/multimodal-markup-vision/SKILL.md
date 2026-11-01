---
name: multimodal-markup-vision
description: Inspects and extracts handwritten engineer redlines, cloud markups, clash callouts, and official submittal review stamps (Approved, Approved as Noted, Revise & Resubmit, Rejected) from scanned architectural drawings, shop drawings, and PDF submittals, converting them into structured BCF topics or actionable Revit view annotations.
---

# Multimodal Markup Vision (فحص وتدقيق الملاحظات والمراجعات الهندسية)

A multimodal computer vision skill specialized in inspecting architectural submittals, shop drawings, and coordination sheets containing handwritten redlines, revision clouds, engineer callout bubbles, and official consultant review stamps. It extracts actionable issue items, maps their spatial bounding boxes, and formats them for BCF (BIM Collaboration Format) or direct Autodesk Revit RevisionCloud elements.

---

## 1. Markup Inspection Workflow

```
[Drawing / Submittal Scan (PDF / Image)]
                   │
                   ▼
  [1. Color Channel Separation (HSV)]
  - Isolate Red / Magenta / Blue / Green markup pen strokes
  - Filter out black/gray base drawing linework
                   │
                   ▼
  [2. Feature Classification & Extraction]
  ┌─────────────────────────┬────────────────────────┐
  │  Consultant Review      │  Redline Clouds &      │
  │  Stamp OCR & Status     │  Handwritten Callouts  │
  └─────────────────────────┴────────────────────────┘
  - Status Detection        - Contour detection (clouds)
  - Reviewer / Discipline   - Leader line tracing
  - Submittal Tracking No   - Proximity OCR matching
                   │
                   ▼
  [3. Structured Issue Schema & BIM Action]
  - Issue Title & Discipline (Arch / Struct / MEP)
  - Severity & Status (Approved / Revise & Resubmit)
  - Coordinate / Sheet View Mapping
  - BCF 2.1/3.0 XML or Revit RevisionCloud creation
```

---

## 2. Consultant Review Stamp Classification

Automated classification of official engineering and architectural submittal stamps:

| Stamp Status (English) | Arabic Term (المصطلح المعتمد) | Standard Action Required |
| :--- | :--- | :--- |
| **APPROVED (Code A)** | معتمد بالكامل | Proceed to fabrication / construction without comments. |
| **APPROVED AS NOTED (Code B)** | معتمد مع الأخذ بالملاحظات | Incorporate marked notes into final shop drawing set; work may proceed. |
| **REVISE & RESUBMIT (Code C)** | تعديل وإعادة تقديم | Critical engineering or code conflict; redesign required before site work. |
| **REJECTED (Code D)** | مرفوض تماماً | Non-compliant with specs or structural criteria; full re-submittal needed. |
| **FOR INFORMATION ONLY** | للعلم والاطلاع | Informational / records only; no official sign-off required. |

### Stamp Metadata Extraction Fields
- **Reviewer**: Architect / Structural Engineer / MEP Consultant.
- **Consultant Firm**: Company name & stamp registration ID.
- **Review Date**: ISO 8601 date format.
- **Specification Section**: e.g., `Spec 09 22 16 (Non-Structural Metal Framing)`.
- **Submittal Reference**: e.g., `SUB-ARC-042-Rev02`.

---

## 3. Redline & Cloud Isolation (Python Recipe)

```python
import cv2
import numpy as np

def extract_redline_markups(image_path: str):
    """
    Isolate redline annotations and revision clouds from base black linework.
    """
    img = cv2.imread(image_path)
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)

    # Red hue spans both ends of HSV spectrum: [0-10] and [170-180]
    lower_red1 = np.array([0, 70, 50])
    upper_red1 = np.array([10, 255, 255])
    lower_red2 = np.array([170, 70, 50])
    upper_red2 = np.array([180, 255, 255])

    mask1 = cv2.inRange(hsv, lower_red1, upper_red1)
    mask2 = cv2.inRange(hsv, lower_red2, upper_red2)
    red_mask = cv2.bitwise_or(mask1, mask2)

    # Morphological closing to bridge dotted or scalloped revision cloud arcs
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    closed = cv2.morphologyEx(red_mask, cv2.MORPH_CLOSE, kernel, iterations=2)

    # Find external contours (Revision clouds and callout boxes)
    contours, _ = cv2.findContours(closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    markups = []
    for cnt in contours:
        area = cv2.contourArea(cnt)
        if area > 400:  # Filter out tiny specks
            x, y, w, h = cv2.boundingRect(cnt)
            # Crop markup region for Multimodal OCR / LLM reading
            markup_crop = img[y:y+h, x:x+w]
            markups.append({
                "bounding_box": [int(x), int(y), int(w), int(h)],
                "area_px": float(area),
                "is_closed_cloud": True
            })

    return {
        "total_markups_found": len(markups),
        "markups": markups
    }
```

---

## 4. BCF (BIM Collaboration Format) Issue Generation

Detected markups and stamps are mapped directly to standard BCF topics:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Topic Guid="d8f24a1b-7c3e-4d82-8491-039c28bf1e04" TopicType="MarkupReview" TopicStatus="Open">
  <Title>Revise Drop Beam Clearance at Corridor Axis C-4</Title>
  <Priority>High</Priority>
  <CreationDate>2026-10-31T14:30:00Z</CreationDate>
  <CreationAuthor>Consultant Senior Structural Engineer</CreationAuthor>
  <Description>HVAC supply duct clashes with reinforced drop beam. Redline indicates rerouting through web opening or lowering false ceiling.</Description>
  <BimSnippet SnippetType="PDF_Markup" isExternal="false">
    <FileReference>A-102_Rev2_Marked.pdf</FileReference>
  </BimSnippet>
</Topic>
```

---

## 5. Revit API Integration: Creating Revision Clouds

```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BimVisionAutomation
{
    public static class RevitMarkupCloudCreator
    {
        public static void CreateCloudOnView(
            Document doc,
            View targetView,
            ElementId revisionId,
            IList<Curve> cloudCurves)
        {
            using (Transaction trans = new Transaction(doc, "Create Redline Revision Cloud"))
            {
                trans.Start();

                // RevisionCloud.Create creates a cloud around the redlined region
                RevisionCloud cloud = RevisionCloud.Create(
                    doc,
                    targetView,
                    revisionId,
                    cloudCurves
                );

                trans.Commit();
            }
        }
    }
}
```

---

## 6. Bilingual Arabic-English Review Note Handling
- Preserves technical bilingual comments common in GCC projects (e.g. `تأكيد عزل الرطوبة أسفل البلاط حسب مواصفات SBC 201`).
- Ensures inverted bracket/quote correction when parsing mixed Arabic text and English tags (`Wall Type W-04 (جدار خارجي مزدوج)`).
