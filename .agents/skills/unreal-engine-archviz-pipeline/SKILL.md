---
name: unreal-engine-archviz-pipeline
description: Automates the real-time architectural visualization pipeline from Revit/3ds Max into Unreal Engine 5 using Datasmith, Dataprep visual recipe rules, Lumen dynamic global illumination, Nanite geometry virtualization, and Pixel Streaming deployment.
---

# Unreal Engine ArchViz Pipeline (بايبلاين الإظهار التفاعلي في Unreal Engine 5)

An advanced real-time architectural visualization skill connecting Autodesk Revit and 3ds Max models to **Unreal Engine 5 (UE5.4/5.5)**. It automates data ingestion via **Datasmith**, executes automated material mapping and mesh cleanup with **Dataprep**, configures real-time **Lumen** dynamic lighting and **Nanite** virtualized geometry, and packages projects for interactive web **Pixel Streaming**.

---

## 1. Unreal Engine 5 ArchViz Workflow

```
[Autodesk Revit / 3ds Max BIM Model]
                 │
                 ▼ (Datasmith Exporter Plugin)
     [.udatasmith Scene File]
                 │
                 ▼
     [Dataprep Automated Optimization Recipe]
     ┌────────────────────────────────────────────────────────┐
     │ 1. Filter: Meshes with > 5,000 Polygons -> Enable Nanite│
     │ 2. Filter: Material Name matches 'Glass' -> Apply Master│
     │ 3. Filter: Material Name matches 'Water' -> Apply Fluid │
     │ 4. Weld duplicate vertices & generate collision boxes   │
     └────────────────────────────────────────────────────────┘
                 │
                 ▼
     [Real-Time Rendering Engines in UE5]
     - Lumen Dynamic Global Illumination (Zero light baking needed)
     - Hardware Ray Traced Reflections & Virtual Shadow Maps (VSM)
     - Camera Sequencer & Cinematic Depth-of-Field
                 │
                 ▼
     [Interactive Client Delivery]
     - Standalone Windows Executable (.exe)
     - Pixel Streaming via WebRTC (Run high-end UE5 in any browser)
```

---

## 2. Headless Dataprep Automation Script (Python in UE5)

Unreal Engine provides an editor-level Python API to batch-process architectural models:

```python
import unreal

def execute_archviz_dataprep_import(udatasmith_file_path: str, destination_folder: str = "/Game/BIM_Import"):
    """
    Automates the ingestion and Dataprep pipeline for incoming Revit exports.
    """
    # 1. Create Dataprep context
    dataprep_system = unreal.DataprepEditingContext()
    
    # 2. Setup Datasmith producer
    producer = unreal.DatasmithFileProducer()
    producer.set_file_path(udatasmith_file_path)
    
    # 3. Create consumer to save assets into project
    consumer = unreal.DataprepAssetConsumer()
    consumer.set_destination_path(destination_folder)

    # 4. Filter and enable Nanite on all static meshes
    # In UE5 Python API, static meshes can be batch updated:
    editor_asset_subsystem = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    imported_assets = editor_asset_subsystem.list_assets(destination_folder)

    for asset_path in imported_assets:
        asset = editor_asset_subsystem.load_asset(asset_path)
        if isinstance(asset, unreal.StaticMesh):
            nanite_settings = asset.get_editor_property("nanite_settings")
            nanite_settings.set_editor_property("enabled", True)
            asset.set_editor_property("nanite_settings", nanite_settings)

    print(f"Successfully processed {len(imported_assets)} assets with Nanite enabled.")
```

---

## 3. Lumen & Lighting Best Practices
1. **Lumen Scene Detail**: Set `Lumen Scene Detail = 2.0` in the PostProcessVolume for crisp architectural contact shadows.
2. **Two-Sided Materials**: Ensure architectural walls, ceiling slabs, and roof components use two-sided materials to prevent light leaking through single-surface meshes.
3. **Emissive Light Cards**: For hidden LED coves and linear neon strips, use Lumen emissive materials with high intensity ($> 1000\text{ nits}$) rather than hundreds of individual point lights.
