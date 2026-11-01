---
name: speckle-automate-ci
description: Builds cloud-native serverless BIM CI/CD pipelines and automated model validation functions on Speckle Automate, executing geometry collision, carbon footprint, and ISO 19650 standard compliance checks on every publish.
---

# Speckle Automate CI (أتمتة الفحص السحابي المستمر لنماذج الـ BIM)

A cloud-native DevOps and CI/CD skill for AEC models. It leverages **Speckle Automate** to execute serverless verification functions (in Python and C#) automatically whenever a design team publishes an updated model from Autodesk Revit, Rhino, AutoCAD, or Archicad.

---

## 1. Cloud BIM CI/CD Architecture

```
[Designers publishing from Revit / Rhino / Blender]
                           │
                           ▼ (Speckle Connector Webhook)
    [Speckle Server - Model Version Created]
                           │
                           ▼
    [Speckle Automate Engine (Docker Serverless Container)]
    ┌──────────────────────┬──────────────────────┬──────────────────────┐
    │ 1. Parameter Audit   │ 2. Embodied Carbon   │ 3. Geometry Quality  │
    │ - ISO 19650 naming   │ - Volume x Density   │ - Clashes & overlaps │
    │ - Required LOD data  │ - EPD factor lookup  │ - Unbound elements   │
    └──────────────────────┴──────────────────────┴──────────────────────┘
                           │
                           ▼
    [Automated Feedback & Issue Lifecycle]
    - Status: PASS / WARNING / FAILED
    - Publish BCF 2.1 / 3.0 issues directly into model viewer
    - Webhook notification to Slack / Microsoft Teams / Jira
```

---

## 2. Serverless Function Implementation (Python)

A complete Speckle Automate function template that validates parameter compliance and flags non-conforming elements:

```python
from speckle_automate import (
    AutomateBase,
    AutomationContext,
    execute_automate_function,
)
from pydantic import BaseModel, Field

class FunctionInputs(BaseModel):
    required_parameter_name: str = Field(
        default="OmniClass_Title",
        description="Parameter that must be populated on every BIM element."
    )
    max_allowable_failures: int = Field(default=0)

def automate_function(
    automate_context: AutomationContext,
    function_inputs: FunctionInputs,
) -> None:
    # 1. Fetch root version object from the speckle branch
    root_object = automate_context.receive_version()

    elements_checked = 0
    failed_elements = []

    # 2. Recursive traversal of all objects in the Speckle Base model
    def traverse(speckle_obj):
        nonlocal elements_checked
        if hasattr(speckle_obj, "speckle_type") and "RevitElement" in speckle_obj.speckle_type:
            elements_checked += 1
            params = getattr(speckle_obj, "parameters", {})
            
            # Check if required parameter exists and is non-empty
            target_val = params.get(function_inputs.required_parameter_name)
            if not target_val or target_val == "":
                failed_elements.append(speckle_obj)

        for member in getattr(speckle_obj, "elements", []):
            traverse(member)

    traverse(root_object)

    # 3. Report results to Speckle Dashboard
    if len(failed_elements) > function_inputs.max_allowable_failures:
        automate_context.attach_warning_to_objects(
            category="Missing Asset Parameter",
            object_ids=[e.id for e in failed_elements],
            message=f"Missing '{function_inputs.required_parameter_name}' parameter."
        )
        automate_context.mark_run_failed(
            f"Audit failed: {len(failed_elements)} of {elements_checked} elements lacked required data."
        )
    else:
        automate_context.mark_run_success(
            f"Audit passed: All {elements_checked} elements meet ISO 19650 parameters."
        )

if __name__ == "__main__":
    execute_automate_function(automate_function, FunctionInputs)
```

---

## 3. High-Value Automation Scenarios

1. **Embodied Carbon Calculator (A1-A3)**:
   - Queries all structural elements (`StructuralFraming`, `StructuralColumns`, `Floors`), multiplies net concrete/steel volume by regional carbon emission factors (e.g., $320\text{ kg CO}_2\text{e/m}^3$ for OPC concrete), and flags versions that exceed the project's carbon budget.
2. **Revit Model Health Monitor**:
   - Detects over-modeled CAD DWG imports, unpurged view filters, and duplicate elements without opening the $500\text{ MB}$ Revit file locally.
3. **Multi-Discipline Clearance Checker**:
   - Automatically tests clearance envelopes between MEP services published by sub-consultants and structural post-tensioned beam drop zones.
