---
name: corona-batch-camera-render-pipeline
description: Automates multi-camera batch rendering with per-camera overrides (sun angles, LightMix states), output naming conventions, and 32-bit multichannel CXR/EXR saving via 3dsmaxbatch.
---

# Corona Batch Camera Render Pipeline

Production batch rendering automation skill for Autodesk 3ds Max and Chaos Corona Renderer. Manages automated queue rendering across multiple cameras with per-camera scene overrides (sun angles, exposure, LightMix presets) and multi-channel 32-bit CXR/EXR output.

---

## 1. Multi-Camera Batch Architecture

```
Scene Camera Queue:
├── Camera 01 (Living Room - Morning) ──> Sun: 30° East ──> Exposure: +0.2 ──> Output: Cam01_Morning.cxr
├── Camera 02 (Kitchen - Midday)      ──> Sun: 65° South ──> Exposure: 0.0  ──> Output: Cam02_Noon.cxr
└── Camera 03 (Patio - Twilight)      ──> Sun: -2° Dusk  ──> LightMix: Blue ──> Output: Cam03_Dusk.cxr
```

---

## 2. Automated Multi-Camera Render Engine (MAXScript)

```maxscript
struct RenderCameraJob
(
    camName,
    outputFilename,
    sunAltitude,
    sunAzimuth,
    noiseLimit,
    exposureEV
)

fn executeCoronaBatchQueue jobList outputDir:"C:\\Renders\\" =
(
    makeDir outputDir
    local cm = CoronaRenderer.CoronaFp
    
    for job in jobList do
    (
        local camNode = getNodeByName job.camName
        if not isValidNode camNode do
        (
            format "WARNING: Camera % not found! Skipping...\n" job.camName
            continue
        )
        
        format "========================================\n"
        format "Starting Render Job: %\n" job.camName
        format "========================================\n"
        
        -- 1. Set Active Camera
        viewport.setCamera camNode
        
        -- 2. Apply Camera-Specific Sun & Exposure Overrides
        cm.setPropertyValue "toneMapping.exposure" job.exposureEV
        
        local sceneSun = for l in lights where classOf l == CoronaSun collect l
        if sceneSun.count > 0 do
        (
            -- Adjust sun position based on azimuth/altitude
            sceneSun[1].altitude = job.sunAltitude
            sceneSun[1].azimuth = job.sunAzimuth
        )
        
        -- 3. Configure Output Path & Noise Limit
        local fullOutputPath = outputDir + job.outputFilename + ".cxr"
        rendSaveFile = true
        rendOutputFilename = fullOutputPath
        renderers.current.target_image_noise_level__error = job.noiseLimit
        
        -- 4. Start Production Render
        max render production
        
        format "Completed: % -> Saved to %\n" job.camName fullOutputPath
    )
    
    format "ALL BATCH RENDER JOBS COMPLETED SUCCESSFULLY.\n"
)
```

---

## 3. Headless Execution via `3dsmaxbatch.exe`

Execute batch rendering headlessly on render farms or overnight without loading the full 3ds Max UI:

```powershell
# Run headless batch render via PowerShell
& "C:\Program Files\Autodesk\3ds Max 2025\3dsmaxbatch.exe" `
    "C:\Projects\LuxuryVilla\Villa_Scene.max" `
    -mxsFile "C:\Scripts\Corona_Batch_Run.ms" `
    -v 5
```

---

## 4. Multi-Channel 32-Bit CXR & EXR Specifications
- **Corona CXR Format**:
  - Contains complete LightMix layers and Bloom & Glare settings.
  - Can be adjusted losslessly in the standalone **Corona Image Editor (CIE)** without launching 3ds Max.
- **OpenEXR 32-bit Float**:
  - Compresses with `ZIP` (16 scanlines) or `DWAA` for optimal balance between file size and post-production grade fidelity in Photoshop / After Effects.

---

## 5. Deliverables Checklist
1. **Render Batch Log**: Log file detailing camera name, resolution ($4\text{K} = 3840 \times 2160$), render pass count, achieved noise level, and total elapsed duration.
2. **CXR Post-Production Bundle**: Verified export of `.cxr` files with active Cryptomatte masks for walls, glass, and furniture.
