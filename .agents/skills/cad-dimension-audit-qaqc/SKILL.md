---
name: cad-dimension-audit-qaqc
description: Audits 2D AutoCAD DWG drawings for dimension discrepancies, text overrides (hacked/forced numbers), cumulative string dimension drift, scale inconsistencies, and unassociated dimensions before BIM conversion.
---

# CAD Dimension Audit QA/QC (تدقيق أبعاد وتناقضات المخططات الهندسية)

A high-precision Quality Assurance / Quality Control (QA/QC) skill specialized in auditing AutoCAD DWG drawings and blueprints. It scans drawing entities, detects manual text overrides (`TextOverride`) that mask drafting errors or discrepancies, checks cumulative string dimension tolerances, and highlights non-associative dimensions to ensure pristine geometric data before BIM modeling or quantity takeoff.

---

## 1. Audit Framework

```
[AutoCAD DWG / DXF Drawing]
             │
             ▼
   [1. Selection & Entity Filtering]
   - Collect All Rotated, Aligned, Linear, and Ordinate Dimensions
   - Inspect Drawing Units ($LUNITS) & Dimension Styles ($DIMSTYLE)
             │
             ▼
   [2. Geometric Discrepancy & Override Detection]
   ┌────────────────────────────────────────────────────────┐
   │ Check 1: Text Override Detection                       │
   │ - TextOverride != "" AND does not contain "<>" token   │
   │ - Compare Text Number vs True Measurement              │
   ├────────────────────────────────────────────────────────┤
   │ Check 2: Cumulative Dimension String Drift             │
   │ - Sum of sub-dimensions == Total overall dimension     │
   │ - Tolerance threshold: Delta <= 2.0 mm                 │
   ├────────────────────────────────────────────────────────┤
   │ Check 3: Dimension Association & Floating Ticks        │
   │ - Unassociated dimensions whose definition points lack │
   │   underlying geometry vertices                         │
   └────────────────────────────────────────────────────────┘
             │
             ▼
   [3. Automated Tagging & QA/QC Output]
   - Move non-compliant dimensions to layer "QAQC_DIM_ERRORS"
   - Color-code offending dimensions Red (Color 1)
   - Export CSV / Excel audit log with entity Handles
```

---

## 2. Automated AutoLISP Audit Routine

This in-process AutoLISP command can be run directly inside AutoCAD to instantly audit and highlight overridden dimensions:

```lisp
(defun c:AuditDimOverrides ( / ss i ent obj overrideVal actualMeas diff handle count)
  (vl-load-com)
  (setq count 0)
  
  (princ "\n==============================================")
  (princ "\n--- Running CAD Dimension QA/QC Audit ---")
  (princ "\n==============================================")
  
  ;; Select all dimension objects in the drawing
  (if (setq ss (ssget "_X" '((0 . "*DIMENSION"))))
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        
        (setq overrideVal (vla-get-TextOverride obj))
        (setq actualMeas  (vla-get-Measurement obj))
        (setq handle      (vla-get-Handle obj))
        
        ;; Check if text override exists and does NOT contain <> (the actual measurement token)
        (if (and (/= overrideVal "")
                 (not (vl-string-search "<>" overrideVal)))
          (progn
            (princ (strcat "\n[DISCREPANCY] Handle: " handle
                           " | Overridden Text: \"" overrideVal "\""
                           " | True Measurement: " (rtos actualMeas 2 2)))
            
            ;; Turn offending dimension Red for visual inspection
            (vla-put-Color obj 1)
            (setq count (1+ count))
          )
        )
        (setq i (1+ i))
      )
      (princ (strcat "\n\n>>> Audit Complete: Found " (itoa count) " overridden dimensions."))
    )
    (princ "\nNo dimensions found in the active drawing.")
  )
  (princ)
)
```

---

## 3. External Batch Python Audit (via COM / pyautocad)

A headless script to audit dozens of incoming consultant drawings automatically:

```python
import csv
from pyautocad import Autocad

def run_cad_dimension_audit(output_csv: str = "cad_dim_discrepancies.csv"):
    acad = Autocad(create_if_not_exists=False)
    doc_name = acad.doc.Name
    print(f"Auditing Drawing: {doc_name}")

    discrepancies = []

    for obj in acad.model:
        if obj.EntityName and "Dimension" in obj.EntityName:
            override = obj.TextOverride.strip() if obj.TextOverride else ""
            actual_measurement = obj.Measurement

            # Flag if text is overridden without measurement placeholder '<>'
            if override != "" and "<>" not in override:
                # Attempt to extract numeric value from override text
                try:
                    cleaned_num = float(''.join(c for c in override if c.isdigit() or c == '.'))
                    delta = abs(cleaned_num - actual_measurement)
                except ValueError:
                    delta = 999.0 # Non-numeric override

                if delta > 1.0: # Discrepancy > 1mm
                    discrepancies.append({
                        "handle": obj.Handle,
                        "layer": obj.Layer,
                        "override_text": override,
                        "actual_measurement_mm": round(actual_measurement, 2),
                        "delta_mm": round(delta, 2),
                        "severity": "CRITICAL" if delta > 20.0 else "WARNING"
                    })
                    # Highlight in CAD
                    obj.Color = 1 # Red

    # Write findings to CSV
    if discrepancies:
        with open(output_csv, "w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=discrepancies[0].keys())
            writer.writeheader()
            writer.writerows(discrepancies)
        print(f"Audit finished. Logged {len(discrepancies)} discrepancies to {output_csv}")
    else:
        print("Audit clean: Zero overridden dimensions detected.")

if __name__ == "__main__":
    run_cad_dimension_audit()
```

---

## 4. Key Rules for CAD-to-BIM Cleanliness
1. **Never Model Overrides**: If a dimension text says $4500\text{ mm}$ but measures $4380\text{ mm}$, always verify against structural grid lines and architectural clearances before placing Revit walls.
2. **Cumulative String Validation**: For multi-segment dimension lines, check that:
   $$\left| \text{Overall Dimension} - \sum \text{Sub-Dimensions} \right| \le 1.0\text{ mm}$$
3. **Layer Isolation**: Ensure all verified dimensions reside on standard AIA layers (e.g., `A-ANNO-DIMS` or `S-ANNO-DIMS`), purging untracked miscellaneous layers.
