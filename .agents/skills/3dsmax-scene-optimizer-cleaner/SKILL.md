---
name: 3dsmax-scene-optimizer-cleaner
description: Diagnoses and cleans heavy 3ds Max scenes, converts duplicate meshes to instances, automates Corona Proxy (.cgeo) export, relinks missing textures, and purges CAD debris to prevent crashes.
---

# 3ds Max Scene Optimizer & Proxy Cleaner

Production diagnostic and scene maintenance skill for Autodesk 3ds Max. Specializes in scene memory optimization, automatic instance conversion, Corona Proxy (`.cgeo`) batch baking, missing asset resolution, and CAD geometry purging.

---

## 1. Scene Diagnostics & Crash Prevention (MAXScript)

Heavy architectural models imported from Revit, SketchUp, or AutoCAD frequently cause 3ds Max to run out of RAM (OOM) or crash during the raycasting preprocessing phase.

```
Common Crash Causes:
├── Unique Duplicate Meshes: 1,000 identical chairs imported as independent meshes (10GB RAM)
├── Missing Asset Paths: Network drives timing out during texture loading
├── Excessive Mesh Subdivisions: Faucets or door handles with 500,000 polygons
└── Corrupt Motion Clips / Note Tracks: Legacy animation garbage bloating file sizes by 500MB
```

---

## 2. Automated Instance Converter & Duplicate Deduplication

```maxscript
fn convertDuplicatesToInstances tolerance:0.01 =
(
    local processedObjects = #()
    local instancesCreated = 0
    
    local allMeshes = for obj in geometry where (classOf obj == Editable_Poly or classOf obj == Editable_Mesh) collect obj
    
    for i = 1 to allMeshes.count do
    (
        local baseObj = allMeshes[i]
        if (findItem processedObjects baseObj == 0) do
        (
            append processedObjects baseObj
            local baseVertCount = baseObj.numVerts
            local baseFaceCount = baseObj.numFaces
            local baseBBox = baseObj.max - baseObj.min
            
            -- Scan remaining meshes for identical polycount and bounding box
            for j = (i + 1) to allMeshes.count do
            (
                local compObj = allMeshes[j]
                if (findItem processedObjects compObj == 0) do
                (
                    if (compObj.numVerts == baseVertCount and compObj.numFaces == baseFaceCount) do
                    (
                        local compBBox = compObj.max - compObj.min
                        if (distance baseBBox compBBox < tolerance) do
                        (
                            -- Match found! Replace geometry with baseObj instance
                            compObj.baseObject = baseObj.baseObject
                            append processedObjects compObj
                            instancesCreated += 1
                        )
                    )
                )
            )
        )
    )
    format "Instance Optimization Complete: Created % instances. RAM saved dramatically!\n" instancesCreated
)
```

---

## 3. Batch Corona Proxy (`.cgeo`) Export

High-polygon assets (trees, vehicles, complex sofas) must be converted to Corona Proxies:

```maxscript
fn exportBatchCoronaProxies targetObjects exportFolder:"C:\\Proxies\\" =
(
    makeDir exportFolder
    
    for obj in targetObjects where isValidNode obj do
    (
        local proxyFilePath = exportFolder + obj.name + ".cgeo"
        
        -- Export geometry to Corona Proxy file
        -- CoronaProxyExporter interface handles creation
        local cProxy = CoronaProxy()
        cProxy.name = "Proxy_" + obj.name
        cProxy.file = proxyFilePath
        cProxy.displayMode = 2 -- Point Cloud (saves GPU viewport memory)
        cProxy.pointCloudCount = 2500
        cProxy.transform = obj.transform
        
        -- Copy material
        cProxy.material = obj.material
        
        -- Hide original mesh
        hide obj
    )
    format "Batch Proxy Export completed to %.\n" exportFolder
)
```

---

## 4. Asset Relinking & CAD Layer Purge

```maxscript
fn purgeUnusedSceneGarbage =
(
    -- 1. Purge Empty Layers
    local layMgr = LayerManager
    for i = (layMgr.count - 1) downTo 1 do
    (
        local lyr = layMgr.getLayer i
        local nodes = #()
        lyr.nodes &nodes
        if (nodes.count == 0) do
        (
            layMgr.deleteLayerByName lyr.name
        )
    )
    
    -- 2. Clean Corrupt Note Tracks & Anim Layers
    numNoteTracks trackViewNodes
    clearUndoBuffer()
    gc light:true
    
    format "Purged empty layers and cleared undo memory buffer.\n"
)
```

---

## 5. Deliverables Checklist
1. **Scene Pre-Flight Audit Report**: Total polygon count, number of unique vs instanced objects, and missing bitmap tally.
2. **Normalized System Units**: Verified match between 3ds Max System Units and display units (Millimeters or Meters).
