---
name: develop-pyrevit-extensions
description: >-
  Develops production-grade pyRevit extensions, custom tabs, panels, pushbuttons, pulldowns, smart UI forms (WPF/XAML),
  and Python scripts running on IronPython or CPython engines inside Autodesk Revit.
---

# Develop pyRevit Extensions

Comprehensive engineering guide for authoring, packaging, and maintaining modular pyRevit extensions for Autodesk Revit.

## Extension Bundle Hierarchy

pyRevit discovers tools via a strict filesystem convention:
```text
MyExtension.extension/
├── extension.json (or bundle.yaml)
└── MyTab.tab/
    ├── MyPanel.panel/
    │   ├── MyTool.pushbutton/
    │   │   ├── script.py          # Python execution logic
    │   │   ├── bundle.yaml        # Tool metadata, title, tooltip, min/max Revit version
    │   │   └── icon.png           # 32x32 / 96dpi PNG icon
    │   └── MyStack.pulldown/
    │       ├── SubToolA.pushbutton/
    │       └── SubToolB.pushbutton/
```

## Script Development & pyRevit Modules

### 1. Fundamental Imports & Execution Context
```python
# -*- coding: utf-8 -*-
from pyrevit import revit, DB, UI, forms, script

doc = revit.doc          # Active Revit Document
uidoc = revit.uidoc      # Active UIDocument
app = revit.app          # Revit Application instance
output = script.get_output()
```

### 2. Transaction Management
```python
# Always encapsulate document changes within a Transaction
with revit.Transaction("Modify Elements"):
    for elem in selected_elements:
        param = elem.LookupParameter("Comments")
        if param and not param.IsReadOnly:
            param.Set("Updated by pyRevit")
```

### 3. Interactive UI Forms
* Use `forms.SelectFromList.show(items, multiselect=True, title="Select Items")` for quick element selection.
* Use `forms.ask_for_string(prompt="Enter text:", title="Input")` for text prompts.
* Use `forms.WPFWindow` with XAML for complex dialogs and dockable panes.

### 4. Engine & Performance Configuration
* In `bundle.yaml`, specify the engine:
  ```yaml
  engine: 2712  # IronPython 2.7.12
  # Or for modern Python 3 / CPython:
  # engine: cpython38
  ```
