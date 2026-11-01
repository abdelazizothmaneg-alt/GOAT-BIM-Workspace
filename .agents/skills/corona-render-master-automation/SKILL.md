---
name: corona-render-master-automation
description: Automates Chaos Corona Renderer in Autodesk 3ds Max via MAXScript and Python (pymxs), configuring multi-setup LightMix, baking VFB light states to scene, setting noise limits, Tone Mapping (ACEScg), and render elements.
---

# Corona Render Master Automation

Production-grade automation skill for Chaos Corona Renderer (Corona 10, 11, 12, 13) in Autodesk 3ds Max. Specializes in programmatic LightMix configuration, baking VFB adjustments to scene lights, noise limit thresholds, Tone Mapping pipelines, and multi-pass render elements.

---

## 1. Engine Initialization & Renderer Checks (MAXScript)

Before executing rendering tasks, ensure Chaos Corona is active and inspect the scriptable interfaces:

```maxscript
-- Set Chaos Corona as production renderer
renderers.current = CoronaRenderer.CoronaRenderer()

-- Inspect scriptable function publishing interfaces
showInterface CoronaRenderer.CoronaFp

-- Configure primary render stopping criteria
renderers.current.target_image_noise_level__error = 2.0 -- Stop at 2% noise
renderers.current.target_passes = 150                   -- Or maximum 150 passes
renderers.current.adaptivity_enabled = true             -- Enable adaptive sampling
```

---

## 2. Multi-Setup LightMix & Scene Baking (Corona 11/12)

### A. Automatic LightMix Setup
```maxscript
fn setupCoronaLightMix mode:#instances =
(
    -- mode can be #instances, #groups, or #layers
    local reMgr = maxOps.GetCurRenderElementMgr()
    
    -- Check if CShading_LightMix already exists
    local hasLightMix = false
    for i = 0 to (reMgr.NumRenderElements() - 1) do
    (
        local elem = reMgr.GetRenderElement i
        if (classOf elem == CShading_LightMix) then hasLightMix = true
    )
    
    if not hasLightMix do
    (
        local lmElem = CShading_LightMix()
        lmElem.elementName = "LightMix_Main"
        reMgr.AddRenderElement lmElem
        
        -- Generate individual light select elements for all scene lights
        for lt in lights where not lt.isTarget do
        (
            local lSel = CShading_LightSelect()
            lSel.elementName = "Light_" + lt.name
            lSel.lights = #(lt)
            reMgr.AddRenderElement lSel
        )
        format "LightMix and LightSelect elements created successfully.\n"
    )
)
```

### B. Baking LightMix Tweaks to Scene Lights
In Corona 12+, multi-setup LightMix is supported in VFB 2:
```maxscript
-- Bake 1st LightMix setup (Index 0) back into 3ds Max scene light intensities & colors
local result = CoronaRenderer.CoronaFp.bakeIthLightMix 0

if result == 0 then
    format "LightMix successfully baked into scene lights!\n"
else
    format "LightMix bake failed with return code: %\n" result
```

---

## 3. Corona Tone Mapping & ACEScg Configuration

```maxscript
fn configureCoronaToneMapping exposureVal:0.0 highlightComp:3.0 whiteBalanceK:5500 lutFile:"" =
(
    local cm = CoronaRenderer.CoronaFp
    
    -- Exposure (EV)
    cm.setPropertyValue "toneMapping.exposure" exposureVal
    
    -- Highlight compression to prevent burned out whites
    cm.setPropertyValue "toneMapping.highlightCompression" highlightComp
    
    -- White Balance (Kelvin)
    cm.setPropertyValue "toneMapping.whiteBalance" whiteBalanceK
    
    -- ACES Tone Mapping / Contrast
    cm.setPropertyValue "toneMapping.contrast" 1.15
    cm.setPropertyValue "toneMapping.saturation" 0.05
    
    -- Bloom & Glare
    cm.setPropertyValue "bloomAndGlare.bloomAmount" 0.5
    cm.setPropertyValue "bloomAndGlare.glareAmount" 0.2
    cm.setPropertyValue "bloomAndGlare.glareThreshold" 0.8
)
```

---

## 4. Python (`pymxs`) Production Render Script

```python
"""
Corona Headless Batch Execution via Python pymxs
"""

from pymxs import runtime as rt

def render_camera_view(camera_name, output_path, noise_limit=2.5):
    cam = rt.getNodeByName(camera_name)
    if not cam:
        raise ValueError(f"Camera '{camera_name}' not found in 3ds Max scene.")
        
    rt.viewport.setCamera(cam)
    
    # Configure render settings
    rt.renderers.current.target_image_noise_level__error = noise_limit
    rt.rendSaveFile = True
    rt.rendOutputFilename = output_path
    
    # Execute production render
    format_msg = f"Rendering camera '{camera_name}' to '{output_path}' with {noise_limit}% noise target..."
    print(format_msg)
    rt.max(rt.Name("render"), rt.Name("production"))
    print("Render complete.")

if __name__ == "__main__":
    # Example execution inside 3ds Max Python listener
    # render_camera_view("CoronaCamera_LivingRoom", @"C:\Renders\LivingRoom.cxr", noise_limit=2.0)
    pass
```

---

## 5. Deliverables Checklist
1. **Render Element Suite**: Verified presence of `CShading_LightMix`, `CShading_Beauty`, `CMasking_Cryptomatte`, and `CGeometry_Zdepth`.
2. **Raw Corona CXR File**: Saving multi-layer 32-bit float images enabling interactive post-render re-lighting in Corona Image Editor (CIE).
