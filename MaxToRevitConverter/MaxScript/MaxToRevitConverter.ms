/*
================================================================================
  MaxToRevit Bridge Pro - 3ds Max to Revit (.RFA / .RVT) Converter
  Compatible with: 3ds Max 2022 - 2027 & Autodesk Revit 2024 - 2026
================================================================================
*/

global M2R_Ctrl

struct MaxToRevit_CategoryItem
(
    Name = "",
    Group = "Arch"
)

struct MaxToRevit_MaterialInfo
(
    LayerName = "",
    MaterialName = "",
    R = 180,
    G = 180,
    B = 180,
    Transparency = 0,
    Shininess = 50
)

struct MaxToRevit_Controller
(
    -- Colors & Fonts
    clrBgDark,
    clrCard,
    clrInput,
    clrText,
    clrMuted,
    clrAccent,
    clrSuccess,
    fontTitle,
    fontSection,
    fontRegular,
    fontMono,

    -- UI Controls
    frm,
    cmbOutMode,
    cmbRevitVer,
    cmbTemplate,
    cmbScope,
    cmbPivot,
    cmbGeoMode,
    chkOptimize,
    trkOptimize,
    chkOpenInRevit,
    txtSavePath,
    btnBrowse,
    lblStatus,
    btnConvert,
    lblSelectedCatBadge,
    txtCatSearch,
    cmbCatFilter,
    lstCategories,
    chkWorkPlane,
    chkAlwaysVert,
    chkCutVoids,
    chkHostRebar,
    chkShared,
    chkRoomPoint,

    -- Data State
    allCategories = #(),
    selectedCategoryName = "Furniture",
    rftFullPaths = #(),
    lastExportWidthMm = 1000.0,
    lastExportDepthMm = 200.0,
    lastExportHeightMm = 1000.0,

    fn initCategories =
    (
        allCategories = #(
            MaxToRevit_CategoryItem Name:"Audio Visual Devices" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Casework" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Ceilings" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Columns" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Curtain Panels" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Data Devices" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Detail Items" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Doors" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Electrical Equipment" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Electrical Fixtures" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Entourage" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Fire Alarm Devices" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Fire Protection" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Floors" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Food Service Equipment" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Furniture" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Furniture Systems" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Generic Models" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Hardscape" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Lighting Fixtures" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Mass" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Mechanical Control Devices" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Mechanical Equipment" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Medical Equipment" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Parking" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Planting" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Plumbing Equipment" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Plumbing Fixtures" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Railings" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Railings - Supports" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Railings - Terminations" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Roads" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Roofs" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Signage" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Site" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Specialty Equipment" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Stairs" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Structural Columns" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Structural Connections" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Structural Foundations" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Structural Framing" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Structural Stiffeners" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Structural Trusses" Group:"Struct",
            MaxToRevit_CategoryItem Name:"Telephone Devices" Group:"MEP",
            MaxToRevit_CategoryItem Name:"Temporary Structures" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Topography" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Toposolid" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Vertical Circulation" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Walls" Group:"Arch",
            MaxToRevit_CategoryItem Name:"Windows" Group:"Arch"
        )
    ),

    fn createCard x y w h =
    (
        local pnl = dotNetObject "System.Windows.Forms.Panel"
        pnl.Location = dotNetObject "System.Drawing.Point" x y
        pnl.Size = dotNetObject "System.Drawing.Size" w h
        pnl.BackColor = M2R_Ctrl.clrCard
        pnl.BorderStyle = (dotNetClass "System.Windows.Forms.BorderStyle").FixedSingle
        pnl
    ),

    fn createLabel txt x y w h fnt clr =
    (
        local lbl = dotNetObject "System.Windows.Forms.Label"
        lbl.Text = txt
        lbl.Location = dotNetObject "System.Drawing.Point" x y
        lbl.Size = dotNetObject "System.Drawing.Size" w h
        lbl.Font = fnt
        lbl.ForeColor = clr
        lbl
    ),

    fn inferCategoryFromTemplate rftFileName =
    (
        local n = toLower rftFileName
        if (findString n "furniture system") != undefined then "Furniture Systems"
        else if (findString n "furniture") != undefined then "Furniture"
        else if (findString n "casework") != undefined then "Casework"
        else if (findString n "lighting") != undefined then "Lighting Fixtures"
        else if (findString n "door") != undefined then "Doors"
        else if (findString n "window") != undefined then "Windows"
        else if (findString n "plumbing") != undefined then "Plumbing Fixtures"
        else if (findString n "mechanical") != undefined then "Mechanical Equipment"
        else if (findString n "electrical equipment") != undefined then "Electrical Equipment"
        else if (findString n "electrical fixture") != undefined then "Electrical Fixtures"
        else if (findString n "specialty") != undefined then "Specialty Equipment"
        else if (findString n "entourage") != undefined then "Entourage"
        else if (findString n "planting") != undefined then "Planting"
        else if (findString n "parking") != undefined then "Parking"
        else if (findString n "site") != undefined then "Site"
        else if (findString n "structural column") != undefined then "Structural Columns"
        else if (findString n "column") != undefined then "Columns"
        else if (findString n "structural foundation") != undefined then "Structural Foundations"
        else if (findString n "structural framing") != undefined then "Structural Framing"
        else if (findString n "structural stiffener") != undefined then "Structural Stiffeners"
        else if (findString n "structural trusses") != undefined then "Structural Trusses"
        else if (findString n "curtain") != undefined then "Curtain Panels"
        else if (findString n "railing support") != undefined then "Railings - Supports"
        else if (findString n "railing termination") != undefined then "Railings - Terminations"
        else if (findString n "baluster") != undefined then "Railings"
        else if (findString n "data") != undefined then "Data Devices"
        else if (findString n "fire alarm") != undefined then "Fire Alarm Devices"
        else if (findString n "telephone") != undefined then "Telephone Devices"
        else if (findString n "detail") != undefined then "Detail Items"
        else "Generic Models"
    ),

    fn populateCategories =
    (
        M2R_Ctrl.lstCategories.BeginUpdate()
        M2R_Ctrl.lstCategories.Items.Clear()
        local q = toLower (trimRight (trimLeft M2R_Ctrl.txtCatSearch.Text))
        local flt = if M2R_Ctrl.cmbCatFilter.SelectedItem != undefined then (M2R_Ctrl.cmbCatFilter.SelectedItem as string) else "All"
        local grpCode = case flt of
        (
            "Architecture": "Arch"
            "Structure": "Struct"
            "MEP": "MEP"
            default: "All"
        )

        local selIdx = -1
        for c in M2R_Ctrl.allCategories do
        (
            local matchGroup = (grpCode == "All") or (c.Group == grpCode) or (c.Name == M2R_Ctrl.selectedCategoryName)
            local matchQuery = (q == "") or ((findString (toLower c.Name) q) != undefined)
            if matchGroup and matchQuery do
            (
                local idx = M2R_Ctrl.lstCategories.Items.Add c.Name
                if c.Name == M2R_Ctrl.selectedCategoryName do selIdx = idx
            )
        )
        if selIdx >= 0 do M2R_Ctrl.lstCategories.SelectedIndex = selIdx
        M2R_Ctrl.lstCategories.EndUpdate()
    ),

    fn populateTemplates =
    (
        M2R_Ctrl.cmbTemplate.Items.Clear()
        M2R_Ctrl.rftFullPaths = #()

        local templateDirs = #(
            @"C:\ProgramData\Autodesk\RVT 2026\Family Templates\English",
            @"C:\ProgramData\Autodesk\RVT 2025\Family Templates\English",
            @"C:\ProgramData\Autodesk\RVT 2024\Family Templates\English"
        )

        local foundFiles = #()
        for d in templateDirs where doesFileExist d do
        (
            foundFiles = getFiles (d + "\\*.rft")
            if foundFiles.count > 0 do exit
        )

        if foundFiles.count > 0 then
        (
            sort foundFiles
            local defaultIdx = 0
            for i = 1 to foundFiles.count do
            (
                local fName = filenameFromPath foundFiles[i]
                M2R_Ctrl.cmbTemplate.Items.Add fName
                append M2R_Ctrl.rftFullPaths foundFiles[i]
                if (toLower fName) == "metric furniture.rft" do defaultIdx = (i - 1)
            )
            M2R_Ctrl.cmbTemplate.SelectedIndex = defaultIdx
        )
        else
        (
            local fallbackNames = #(
                "Metric Generic Model.rft",
                "Metric Generic Model wall based.rft",
                "Metric Generic Model ceiling based.rft",
                "Metric Generic Model face based.rft",
                "Metric Generic Model floor based.rft",
                "Metric Furniture.rft",
                "Metric Furniture System.rft",
                "Metric Casework.rft",
                "Metric Casework wall based.rft",
                "Metric Lighting Fixture.rft",
                "Metric Lighting Fixture wall based.rft",
                "Metric Lighting Fixture ceiling based.rft",
                "Metric Door.rft",
                "Metric Window.rft",
                "Metric Plumbing Fixture.rft",
                "Metric Mechanical Equipment.rft",
                "Metric Electrical Equipment.rft",
                "Metric Electrical Fixture.rft",
                "Metric Specialty Equipment.rft",
                "Metric Entourage.rft",
                "Metric Planting.rft",
                "Metric Site.rft",
                "Metric Column.rft",
                "Metric Structural Column.rft",
                "Metric Structural Framing - Beams and Braces.rft"
            )
            for fName in fallbackNames do
            (
                M2R_Ctrl.cmbTemplate.Items.Add fName
                append M2R_Ctrl.rftFullPaths (@"C:\ProgramData\Autodesk\RVT 2026\Family Templates\English\" + fName)
            )
            M2R_Ctrl.cmbTemplate.SelectedIndex = 5
        )
    ),

    fn sanitizeName str =
    (
        local out = ""
        local badChars = "\\/:*?\"<>|{}[];'`~"
        for i = 1 to str.count do
        (
            local ch = str[i]
            if (findString badChars ch) == undefined then out += ch else out += "_"
        )
        if out == "" do out = "Max_Mat"
        out
    ),

    fn extractSingleMaterialColor mat layerName =
    (
        local r = 185
        local g = 185
        local b = 185
        local transp = 0
        local shiny = 50
        local mName = layerName

        if mat != undefined do
        (
            try ( mName = M2R_Ctrl.sanitizeName (mat.name as string) ) catch ()
            try
            (
                if isProperty mat #diffuse then
                (
                    r = mat.diffuse.r as integer
                    g = mat.diffuse.g as integer
                    b = mat.diffuse.b as integer
                )
                else if isProperty mat #Base_Color then
                (
                    r = mat.Base_Color.r as integer
                    g = mat.Base_Color.g as integer
                    b = mat.Base_Color.b as integer
                )
                else if isProperty mat #color then
                (
                    r = mat.color.r as integer
                    g = mat.color.g as integer
                    b = mat.color.b as integer
                )
            )
            catch ()

            try
            (
                if isProperty mat #opacity then
                    transp = (100 - (mat.opacity as integer))
                else if isProperty mat #transparency do
                    transp = (mat.transparency as integer)
            )
            catch ()
        )

        MaxToRevit_MaterialInfo LayerName:layerName MaterialName:mName R:r G:g B:b Transparency:transp Shininess:shiny
    ),

    fn getScaleFactorToMm =
    (
        -- Returns how many millimeters are in 1 current 3ds Max System Unit
        local oneMmInSystemUnits = units.decodeValue "1mm"
        if oneMmInSystemUnits > 0.0 then (1.0 / oneMmInSystemUnits) else 1.0
    ),

    fn computeNodesBounds nodes &minB &maxB =
    (
        minB = [1e9, 1e9, 1e9]
        maxB = [-1e9, -1e9, -1e9]
        for n in nodes do
        (
            if n.min.x < minB.x do minB.x = n.min.x
            if n.min.y < minB.y do minB.y = n.min.y
            if n.min.z < minB.z do minB.z = n.min.z
            if n.max.x > maxB.x do maxB.x = n.max.x
            if n.max.y > maxB.y do maxB.y = n.max.y
            if n.max.z > maxB.z do maxB.z = n.max.z
        )
    ),

    fn writeM2RFile nodes path =
    (
        -- Native mesh exchange file read by the Revit Bridge to build Extrusions / Free Form solids.
        -- Format:  O name|materialLayer   V x y z (mm)   F a b c (1-based)
        local sw = dotNetObject "System.IO.StreamWriter" path false
        sw.WriteLine "M2R 1"
        for n in nodes where isValidNode n do
        (
            local m = n.mesh
            local lyrName = if n.layer != undefined then n.layer.name else "0"
            sw.WriteLine ("O " + n.name + "|" + lyrName)
            local nv = getNumVerts m
            for v = 1 to nv do
            (
                local p = getVert m v
                sw.WriteLine ("V " + (formattedPrint p.x format:".4f") + " " + (formattedPrint p.y format:".4f") + " " + (formattedPrint p.z format:".4f"))
            )
            local nf = getNumFaces m
            for f = 1 to nf do
            (
                local fc = getFace m f
                sw.WriteLine ("F " + ((fc.x as integer) as string) + " " + ((fc.y as integer) as string) + " " + ((fc.z as integer) as string))
            )
        )
        sw.Close()
    ),

    fn prepareAndExportObjects srcNodes geoFilePath alignMode useOpt optPercent splitByMat &outMaterials =
    (
        local tempNodes = #()
        outMaterials = #()
        local seenLayers = #()

        undo off
        (
            with redraw off
            (
                for obj in srcNodes where (superClassOf obj == GeometryClass or canConvertTo obj Editable_Mesh) do
                (
                    -- IMPORTANT: snapshotAsMesh returns vertices ALREADY in World Space!
                    -- Do NOT set cloneObj.transform = obj.transform or it will double-rotate/scale!
                    local snapMesh = snapshotAsMesh obj
                    local cloneObj = Editable_mesh name:("M2R_" + (M2R_Ctrl.sanitizeName obj.name))
                    cloneObj.mesh = snapMesh
                    cloneObj.material = obj.material
                    cloneObj.wirecolor = color 55 55 55

                    if useOpt and optPercent < 100 do
                    (
                        try
                        (
                            local optMod = ProOptimizer()
                            addModifier cloneObj optMod
                            optMod.VertexPercent = optPercent as float
                            optMod.KeepMaterials = true
                            optMod.KeepUV = true
                            optMod.Calculate = true
                            collapseStack cloneObj
                        )
                        catch ()
                    )

                    local mat = cloneObj.material
                    if classOf mat == Multimaterial and splitByMat then
                    (
                        local numFaces = meshop.getNumFaces cloneObj.mesh
                        for subIdx = 1 to mat.numsubs do
                        (
                            local subMat = mat.materialList[subIdx]
                            local matId = mat.materialIDList[subIdx]
                            local faceBits = #{}
                            for f = 1 to numFaces do
                            (
                                if (getFaceMatID cloneObj.mesh f) == matId do faceBits[f] = true
                            )
                            if faceBits.numberSet > 0 do
                            (
                                local subName = if subMat != undefined then ("Mat_" + (M2R_Ctrl.sanitizeName subMat.name)) else ("Mat_ID_" + (matId as string))
                                local detachedMesh = meshop.detachFaces cloneObj.mesh faceBits delete:false asMesh:true
                                local partObj = Editable_mesh name:subName
                                partObj.mesh = detachedMesh
                                partObj.wirecolor = color 55 55 55
                                if subMat != undefined do
                                (
                                    partObj.material = subMat
                                )

                                local lyr = LayerManager.getLayerFromName subName
                                if lyr == undefined do lyr = LayerManager.newLayerFromName subName
                                lyr.addNode partObj

                                append tempNodes partObj
                                if (findItem seenLayers subName) == 0 do
                                (
                                    append seenLayers subName
                                    append outMaterials (M2R_Ctrl.extractSingleMaterialColor subMat subName)
                                )
                            )
                        )
                        delete cloneObj
                    )
                    else
                    (
                        local lyrName = if mat != undefined then ("Mat_" + (M2R_Ctrl.sanitizeName mat.name)) else ("Mat_" + (M2R_Ctrl.sanitizeName obj.name))
                        local lyr = LayerManager.getLayerFromName lyrName
                        if lyr == undefined do lyr = LayerManager.newLayerFromName lyrName
                        lyr.addNode cloneObj

                        append tempNodes cloneObj
                        if (findItem seenLayers lyrName) == 0 do
                        (
                            append seenLayers lyrName
                            append outMaterials (M2R_Ctrl.extractSingleMaterialColor mat lyrName)
                        )
                    )
                )

                if tempNodes.count > 0 do
                (
                    -- 1. Scale vertices to exact Millimeters if 3ds Max System Units are not already mm
                    local mmFactor = M2R_Ctrl.getScaleFactorToMm()
                    if (abs (mmFactor - 1.0)) > 0.0001 do
                    (
                        for n in tempNodes do
                        (
                            local nv = getNumVerts n.mesh
                            for v = 1 to nv do
                            (
                                setVert n.mesh v ((getVert n.mesh v) * mmFactor)
                            )
                            update n
                        )
                    )

                    local minB = [0,0,0]
                    local maxB = [0,0,0]
                    M2R_Ctrl.computeNodesBounds tempNodes &minB &maxB

                    local sizeX = maxB.x - minB.x
                    local sizeY = maxB.y - minB.y
                    local sizeZ = maxB.z - minB.z

                    -- 2. Smart Auto-Orientation for Windows / Doors / Wall-based Templates:
                    --    In Revit Wall/Window/Door templates, the Wall runs along X (Width),
                    --    Wall thickness is along Y (Depth), and Height is along Z!
                    if (alignMode == "Window" or alignMode == "WallOpening" or alignMode == "Wall") do
                    (
                        -- If the object is lying flat on the ground (Z is the thinnest dimension), rotate +90 deg around X
                        if (sizeZ < sizeY and sizeZ < sizeX) do
                        (
                            local rotXMat = rotateXMatrix 90
                            for n in tempNodes do
                            (
                                local nv = getNumVerts n.mesh
                                for v = 1 to nv do
                                (
                                    setVert n.mesh v ((getVert n.mesh v) * rotXMat)
                                )
                                update n
                            )
                            M2R_Ctrl.computeNodesBounds tempNodes &minB &maxB
                            sizeX = maxB.x - minB.x
                            sizeY = maxB.y - minB.y
                            sizeZ = maxB.z - minB.z
                        )

                        -- If the object is oriented along Y instead of X (Y is wider than X), rotate 90 deg around Z
                        if (sizeY > sizeX * 1.4) do
                        (
                            local rotZMat = rotateZMatrix 90
                            for n in tempNodes do
                            (
                                local nv = getNumVerts n.mesh
                                for v = 1 to nv do
                                (
                                    setVert n.mesh v ((getVert n.mesh v) * rotZMat)
                                )
                                update n
                            )
                            M2R_Ctrl.computeNodesBounds tempNodes &minB &maxB
                            sizeX = maxB.x - minB.x
                            sizeY = maxB.y - minB.y
                            sizeZ = maxB.z - minB.z
                        )
                    )

                    M2R_Ctrl.lastExportWidthMm  = sizeX
                    M2R_Ctrl.lastExportDepthMm  = sizeY
                    M2R_Ctrl.lastExportHeightMm = sizeZ

                    local centerX = (minB.x + maxB.x) * 0.5
                    local centerY = (minB.y + maxB.y) * 0.5
                    local offset = [0, 0, 0]

                    case alignMode of
                    (
                        "Bottom":      offset = [-centerX, -centerY, -minB.z]
                        "WallOpening": offset = [-centerX, -centerY, -minB.z] -- Door: Centered in wall (Y=0, X=0, Floor Z=0)
                        "Window":      offset = [-centerX, -centerY, (915.0 - minB.z)] -- Window: Centered in wall (Y=0, X=0) at Sill Height (915mm)
                        "Wall":        offset = [-centerX, -maxB.y, -minB.z]  -- Back face touches wall reference plane (Y=0)
                        "Ceiling":     offset = [-centerX, -centerY, -maxB.z] -- Top face touches ceiling reference plane (Z=0)
                        "World":       offset = [0, 0, 0]
                        default:       offset = [-centerX, -centerY, -minB.z]
                    )

                    if offset != [0,0,0] do
                    (
                        for n in tempNodes do
                        (
                            local nv = getNumVerts n.mesh
                            for v = 1 to nv do
                            (
                                setVert n.mesh v ((getVert n.mesh v) + offset)
                            )
                            update n
                        )
                    )

                    select tempNodes
                    makeDir (getFilenamePath geoFilePath) all:true
                    if (toLower (getFilenameType geoFilePath)) == ".m2r" then
                        M2R_Ctrl.writeM2RFile tempNodes geoFilePath
                    else
                        exportFile geoFilePath #noPrompt selectedOnly:true

                    for n in tempNodes where isValidNode n do delete n
                )
            )
        )
        tempNodes.count > 0
    )
)

-- Global Event Handlers referencing M2R_Ctrl
fn M2R_OnTemplateChanged s e =
(
    if M2R_Ctrl.cmbTemplate.SelectedIndex >= 0 do
    (
        local rftName = M2R_Ctrl.cmbTemplate.SelectedItem as string
        M2R_Ctrl.selectedCategoryName = M2R_Ctrl.inferCategoryFromTemplate rftName
        M2R_Ctrl.lblSelectedCatBadge.Text = M2R_Ctrl.selectedCategoryName
        M2R_Ctrl.populateCategories()
    )
)

fn M2R_OnCatSearchChanged s e =
(
    M2R_Ctrl.populateCategories()
)

fn M2R_OnCatFilterChanged s e =
(
    M2R_Ctrl.populateCategories()
)

fn M2R_OnCatListSelected s e =
(
    if M2R_Ctrl.lstCategories.SelectedItem != undefined do
    (
        M2R_Ctrl.selectedCategoryName = M2R_Ctrl.lstCategories.SelectedItem as string
        M2R_Ctrl.lblSelectedCatBadge.Text = M2R_Ctrl.selectedCategoryName
    )
)

fn M2R_OnOutModeChanged s e =
(
    local modeIdx = M2R_Ctrl.cmbOutMode.SelectedIndex
    local curPath = M2R_Ctrl.txtSavePath.Text
    local dir = getFilenamePath curPath
    local baseName = getFilenameFile curPath
    if baseName == "" do baseName = "Max_Export_01"

    if modeIdx == 0 then
    (
        M2R_Ctrl.txtSavePath.Text = dir + baseName + ".rfa"
        M2R_Ctrl.cmbTemplate.Enabled = true
    )
    else if modeIdx == 1 then
    (
        M2R_Ctrl.txtSavePath.Text = dir + baseName + ".rvt"
        M2R_Ctrl.cmbTemplate.Enabled = true
    )
    else
    (
        local ext = if M2R_Ctrl.cmbGeoMode.SelectedIndex == 2 then ".sat" else ".dxf"
        M2R_Ctrl.txtSavePath.Text = dir + baseName + ext
        M2R_Ctrl.cmbTemplate.Enabled = false
    )
)

fn M2R_OnOptCheckChanged s e =
(
    M2R_Ctrl.trkOptimize.Enabled = M2R_Ctrl.chkOptimize.Checked
    if M2R_Ctrl.chkOptimize.Checked then
        M2R_Ctrl.chkOptimize.Text = ("ProOptimizer (Safe Clone): " + (M2R_Ctrl.trkOptimize.Value as string) + "%")
    else
        M2R_Ctrl.chkOptimize.Text = "ProOptimizer (Safe Clone): OFF"
)

fn M2R_OnOptScroll s e =
(
    M2R_Ctrl.chkOptimize.Text = ("ProOptimizer (Safe Clone): " + (M2R_Ctrl.trkOptimize.Value as string) + "%")
)

fn M2R_OnBrowseClicked s e =
(
    local sfd = dotNetObject "System.Windows.Forms.SaveFileDialog"
    local modeIdx = M2R_Ctrl.cmbOutMode.SelectedIndex
    if modeIdx == 0 then
    (
        sfd.Title = "Save Revit Family (.rfa)"
        sfd.Filter = "Revit Family (*.rfa)|*.rfa"
        sfd.DefaultExt = "rfa"
    )
    else if modeIdx == 1 then
    (
        sfd.Title = "Save Revit Project Model (.rvt)"
        sfd.Filter = "Revit Project (*.rvt)|*.rvt"
        sfd.DefaultExt = "rvt"
    )
    else
    (
        sfd.Title = "Save CAD / BIM Geometry (.dxf / .sat / .obj)"
        sfd.Filter = "DXF Geometry (*.dxf)|*.dxf|ACIS Solid (*.sat)|*.sat|OBJ Mesh (*.obj)|*.obj"
        sfd.DefaultExt = "dxf"
    )
    sfd.FileName = filenameFromPath M2R_Ctrl.txtSavePath.Text
    local initDir = getFilenamePath M2R_Ctrl.txtSavePath.Text
    if doesFileExist initDir do sfd.InitialDirectory = initDir

    if sfd.ShowDialog() == (dotNetClass "System.Windows.Forms.DialogResult").OK do
    (
        M2R_Ctrl.txtSavePath.Text = sfd.FileName
    )
)

fn M2R_OnConvertClicked s e =
(
    local scopeIdx = M2R_Ctrl.cmbScope.SelectedIndex
    local targetNodes = #()

    if scopeIdx == 2 then
    (
        targetNodes = for o in geometry where not o.isHidden collect o
    )
    else
    (
        targetNodes = for o in selection where (superClassOf o == GeometryClass or canConvertTo o Editable_Mesh) collect o
    )

    if targetNodes.count == 0 do
    (
        messageBox "Please select at least one 3D geometry object in 3ds Max (or switch Export Scope to 'Entire Max Scene')." title:"MaxToRevit Bridge"
        return false
    )

    local prevSelection = selection as array
    local modeIdx = M2R_Ctrl.cmbOutMode.SelectedIndex
    local outputModeStr = case modeIdx of ( 0: "RFA"; 1: "RVT"; default: "BIM" )
    local geoModeIdx = M2R_Ctrl.cmbGeoMode.SelectedIndex
    local geoExt = case geoModeIdx of ( 1: ".dxf"; 2: ".sat"; 3: ".obj"; default: ".m2r" )
    local geoFormatStr = case geoModeIdx of ( 1: "DXF"; 2: "SAT"; 3: "OBJ"; default: "NATIVE" )

    local rftNameLower = if M2R_Ctrl.cmbTemplate.SelectedItem != undefined then (toLower (M2R_Ctrl.cmbTemplate.SelectedItem as string)) else ""
    local catNameLower = toLower M2R_Ctrl.selectedCategoryName

    local alignMode = case M2R_Ctrl.cmbPivot.SelectedIndex of
    (
        0: (
            if (findString rftNameLower "window") != undefined or catNameLower == "windows" then
                "Window"
            else if (findString rftNameLower "door") != undefined or catNameLower == "doors" then
                "WallOpening"
            else if (findString rftNameLower "wall based") != undefined then
                "Wall"
            else if (findString rftNameLower "ceiling based") != undefined then
                "Ceiling"
            else
                "Bottom"
        )
        1: "Bottom"
        2: "Wall"
        3: "Ceiling"
        4: "World"
        default: "Bottom"
    )

    local useOpt = M2R_Ctrl.chkOptimize.Checked
    local optPct = M2R_Ctrl.trkOptimize.Value
    local splitByMat = (geoModeIdx == 0 or geoModeIdx == 1)

    local bridgeDir  = @"C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\BridgeData"
    local queueDir   = bridgeDir + "\\Queue"
    local stagingDir = bridgeDir + "\\Staging"
    local statusDir  = bridgeDir + "\\Status"
    makeDir queueDir all:true
    makeDir stagingDir all:true
    makeDir statusDir all:true

    -- Also mirror to LocalAppData for backward compatibility
    local localAppData = systemTools.getEnvVariable "LOCALAPPDATA"
    local legacyQueueDir = localAppData + "\\MaxToRevitBridge\\Queue"
    local legacyStatusDir = localAppData + "\\MaxToRevitBridge\\Status"
    makeDir legacyQueueDir all:true
    makeDir legacyStatusDir all:true

    local jobId = ((dotNetClass "System.Guid").NewGuid()).ToString("N")
    local jobMaterials = #()
    local jobItemsJson = #()

    M2R_Ctrl.lblStatus.Text = "Exporting geometry from 3ds Max..."
    M2R_Ctrl.frm.Refresh()

    if outputModeStr == "BIM" then
    (
        local directOutPath = M2R_Ctrl.txtSavePath.Text
        local mats = #()
        M2R_Ctrl.prepareAndExportObjects targetNodes directOutPath alignMode useOpt optPct splitByMat &mats
        if prevSelection.count > 0 then select prevSelection else clearSelection()
        M2R_Ctrl.lblStatus.Text = ("Saved direct BIM geometry: " + (filenameFromPath directOutPath))
        messageBox ("Successfully exported direct BIM geometry to:\n" + directOutPath) title:"MaxToRevit Bridge"
        return true
    )

    if scopeIdx == 1 then
    (
        local outFolder = getFilenamePath M2R_Ctrl.txtSavePath.Text
        makeDir outFolder all:true
        for idx = 1 to targetNodes.count do
        (
            local obj = targetNodes[idx]
            local cleanObjName = M2R_Ctrl.sanitizeName obj.name
            local tempGeoPath = stagingDir + "\\" + jobId + "_" + (idx as string) + "_" + cleanObjName + geoExt
            local targetRfaPath = outFolder + cleanObjName + (if outputModeStr == "RVT" then ".rvt" else ".rfa")
            local mats = #()
            if (M2R_Ctrl.prepareAndExportObjects #(obj) tempGeoPath alignMode useOpt optPct splitByMat &mats) do
            (
                for m in mats do append jobMaterials m
                local itemJson = "{\"ObjectName\":\"" + cleanObjName +
                    "\",\"GeometryFilePath\":\"" + (substituteString tempGeoPath "\\" "\\\\") +
                    "\",\"TargetOutputPath\":\"" + (substituteString targetRfaPath "\\" "\\\\") +
                    "\",\"WidthMm\":" + (M2R_Ctrl.lastExportWidthMm as string) +
                    ",\"DepthMm\":" + (M2R_Ctrl.lastExportDepthMm as string) +
                    ",\"HeightMm\":" + (M2R_Ctrl.lastExportHeightMm as string) + "}"
                append jobItemsJson itemJson
            )
        )
    )
    else
    (
        local cleanName = getFilenameFile M2R_Ctrl.txtSavePath.Text
        if cleanName == "" do cleanName = "Max_Family"
        local tempGeoPath = stagingDir + "\\" + jobId + "_" + (M2R_Ctrl.sanitizeName cleanName) + geoExt
        local targetOutPath = M2R_Ctrl.txtSavePath.Text
        local mats = #()
        if (M2R_Ctrl.prepareAndExportObjects targetNodes tempGeoPath alignMode useOpt optPct splitByMat &mats) do
        (
            jobMaterials = mats
            local itemJson = "{\"ObjectName\":\"" + (M2R_Ctrl.sanitizeName cleanName) +
                "\",\"GeometryFilePath\":\"" + (substituteString tempGeoPath "\\" "\\\\") +
                "\",\"TargetOutputPath\":\"" + (substituteString targetOutPath "\\" "\\\\") +
                "\",\"WidthMm\":" + (M2R_Ctrl.lastExportWidthMm as string) +
                ",\"DepthMm\":" + (M2R_Ctrl.lastExportDepthMm as string) +
                ",\"HeightMm\":" + (M2R_Ctrl.lastExportHeightMm as string) + "}"
            append jobItemsJson itemJson
        )
    )

    if prevSelection.count > 0 then select prevSelection else clearSelection()

    if jobItemsJson.count == 0 do
    (
        M2R_Ctrl.lblStatus.Text = "Export failed: No valid 3D mesh geometry found."
        messageBox "Could not export geometry from the selected objects." title:"MaxToRevit Bridge"
        return false
    )

    local matsJsonArr = #()
    for m in jobMaterials do
    (
        local mj = "{\"LayerName\":\"" + m.LayerName + "\",\"MaterialName\":\"" + m.MaterialName + "\",\"R\":" + (m.R as string) + ",\"G\":" + (m.G as string) + ",\"B\":" + (m.B as string) + ",\"Transparency\":" + (m.Transparency as string) + ",\"Shininess\":" + (m.Shininess as string) + "}"
        append matsJsonArr mj
    )

    local matsJoined = ""
    for i = 1 to matsJsonArr.count do
    (
        matsJoined += matsJsonArr[i]
        if i < matsJsonArr.count do matsJoined += ","
    )

    local itemsJoined = ""
    for i = 1 to jobItemsJson.count do
    (
        itemsJoined += jobItemsJson[i]
        if i < jobItemsJson.count do itemsJoined += ","
    )

    local selRftPath = if M2R_Ctrl.cmbTemplate.SelectedIndex >= 0 and M2R_Ctrl.cmbTemplate.SelectedIndex < M2R_Ctrl.rftFullPaths.count then
        M2R_Ctrl.rftFullPaths[M2R_Ctrl.cmbTemplate.SelectedIndex + 1]
    else
        @"C:\ProgramData\Autodesk\RVT 2026\Family Templates\English\Metric Generic Model.rft"

    local bWorkPlane  = if M2R_Ctrl.chkWorkPlane.Checked then "true" else "false"
    local bAlwaysVert = if M2R_Ctrl.chkAlwaysVert.Checked then "true" else "false"
    local bCutVoids   = if M2R_Ctrl.chkCutVoids.Checked then "true" else "false"
    local bHostRebar  = if M2R_Ctrl.chkHostRebar.Checked then "true" else "false"
    local bShared     = if M2R_Ctrl.chkShared.Checked then "true" else "false"
    local bRoomPt     = if M2R_Ctrl.chkRoomPoint.Checked then "true" else "false"
    local bOpenRevit  = if M2R_Ctrl.chkOpenInRevit.Checked then "true" else "false"

    local jsonContent = "{\n" +
        "  \"JobId\": \"" + jobId + "\",\n" +
        "  \"OutputMode\": \"" + outputModeStr + "\",\n" +
        "  \"TargetRevitVersion\": \"2026\",\n" +
        "  \"FamilyTemplatePath\": \"" + (substituteString selRftPath "\\" "\\\\") + "\",\n" +
        "  \"ProjectTemplatePath\": \"C:\\\\ProgramData\\\\Autodesk\\\\RVT 2026\\\\Templates\\\\English\\\\DefaultMetric.rte\",\n" +
        "  \"CategoryName\": \"" + M2R_Ctrl.selectedCategoryName + "\",\n" +
        "  \"GeometryFormat\": \"" + geoFormatStr + "\",\n" +
        "  \"ImportUnits\": \"mm\",\n" +
        "  \"HideMeshEdges\": true,\n" +
        "  \"OpenInRevitAfterSave\": " + bOpenRevit + ",\n" +
        "  \"FamilyParameters\": {\n" +
        "    \"WorkPlaneBased\": " + bWorkPlane + ",\n" +
        "    \"AlwaysVertical\": " + bAlwaysVert + ",\n" +
        "    \"CutWithVoidsWhenLoaded\": " + bCutVoids + ",\n" +
        "    \"CanHostRebar\": " + bHostRebar + ",\n" +
        "    \"Shared\": " + bShared + ",\n" +
        "    \"RoomCalculationPoint\": " + bRoomPt + "\n" +
        "  },\n" +
        "  \"Materials\": [" + matsJoined + "],\n" +
        "  \"Items\": [" + itemsJoined + "]\n" +
        "}"

    local jobFilePath = queueDir + "\\" + jobId + ".max2rvt.json"
    local legacyJobFilePath = legacyQueueDir + "\\" + jobId + ".max2rvt.json"
    (dotNetClass "System.IO.File").WriteAllText jobFilePath jsonContent
    try ( (dotNetClass "System.IO.File").WriteAllText legacyJobFilePath jsonContent ) catch ()

    local revitProcs = (dotNetClass "System.Diagnostics.Process").GetProcessesByName "Revit"
    if revitProcs.count == 0 then
    (
        local revitExe = @"C:\Program Files\Autodesk\Revit 2026\Revit.exe"
        if doesFileExist revitExe then
        (
            M2R_Ctrl.lblStatus.Text = "Job queued! Launching Revit 2026 to build RFA/RVT..."
            if queryBox ("Geometry & Family settings (" + M2R_Ctrl.selectedCategoryName + ") have been exported!\n\nRevit 2026 is not currently open.\nWould you like to launch Revit 2026 now so it automatically generates your " + outputModeStr + " file(s)?") title:"MaxToRevit Bridge" do
            (
                shellLaunch revitExe ""
            )
        )
        else
        (
            messageBox ("Job queued at:\n" + jobFilePath + "\n\nOpen Revit to complete conversion.") title:"MaxToRevit Bridge"
        )
    )
    else
    (
        M2R_Ctrl.lblStatus.Text = "Waiting for Revit 2026 Bridge to save file..."
        M2R_Ctrl.frm.Refresh()

        local statusFile1 = statusDir + "\\" + jobId + ".status.json"
        local statusFile2 = legacyStatusDir + "\\" + jobId + ".status.json"
        local finished = false
        for waitStep = 1 to 24 do
        (
            sleep 0.5
            local activeStatusFile = if doesFileExist statusFile1 then statusFile1 else (if doesFileExist statusFile2 then statusFile2 else "")
            if activeStatusFile != "" do
            (
                local stText = (dotNetClass "System.IO.File").ReadAllText activeStatusFile
                if (findString stText "\"Completed\"") != undefined do
                (
                    finished = true
                    exit
                )
                if (findString stText "\"Failed\"") != undefined do
                (
                    M2R_Ctrl.lblStatus.Text = "Revit reported an error. Check status log."
                    messageBox ("Revit encountered an error while building the file:\n\n" + stText) title:"MaxToRevit Bridge"
                    return false
                )
            )
        )

        if finished then
        (
            M2R_Ctrl.lblStatus.Text = ("Saved: " + (filenameFromPath M2R_Ctrl.txtSavePath.Text))
            messageBox ("Successfully converted and saved Revit " + outputModeStr + "!\n\nCategory: " + M2R_Ctrl.selectedCategoryName + "\nTemplate: " + (M2R_Ctrl.cmbTemplate.SelectedItem as string) + "\nDimensions: " + ((M2R_Ctrl.lastExportWidthMm as integer) as string) + " x " + ((M2R_Ctrl.lastExportHeightMm as integer) as string) + " mm\nOutput: " + M2R_Ctrl.txtSavePath.Text) title:"MaxToRevit Bridge — Success"
        )
        else
        (
            M2R_Ctrl.lblStatus.Text = "Job sent to Revit Queue! (Switch to Revit window if busy)"
            messageBox ("Job sent to Revit 2026!\nIf Revit was busy in a command, switch to Revit or click 'Convert Pending 3ds Max Jobs' on the Add-Ins tab.\n\nTarget Output:\n" + M2R_Ctrl.txtSavePath.Text) title:"MaxToRevit Bridge"
        )
    )
)

fn MaxToRevit_LaunchUI =
(
    try
    (
        if M2R_Ctrl != undefined and M2R_Ctrl.frm != undefined and not M2R_Ctrl.frm.IsDisposed do
        (
            M2R_Ctrl.frm.Close()
        )
    )
    catch ()

    dotNet.loadAssembly "System.Windows.Forms"
    dotNet.loadAssembly "System.Drawing"

    M2R_Ctrl = MaxToRevit_Controller()
    M2R_Ctrl.initCategories()

    M2R_Ctrl.clrBgDark  = (dotNetClass "System.Drawing.Color").FromArgb 30 33 40
    M2R_Ctrl.clrCard    = (dotNetClass "System.Drawing.Color").FromArgb 40 44 54
    M2R_Ctrl.clrInput   = (dotNetClass "System.Drawing.Color").FromArgb 24 27 34
    M2R_Ctrl.clrText    = (dotNetClass "System.Drawing.Color").FromArgb 235 238 245
    M2R_Ctrl.clrMuted   = (dotNetClass "System.Drawing.Color").FromArgb 160 168 184
    M2R_Ctrl.clrAccent  = (dotNetClass "System.Drawing.Color").FromArgb 56 132 255
    M2R_Ctrl.clrSuccess = (dotNetClass "System.Drawing.Color").FromArgb 40 180 99

    M2R_Ctrl.fontTitle   = dotNetObject "System.Drawing.Font" "Segoe UI" 10.5 ((dotNetClass "System.Drawing.FontStyle").Bold)
    M2R_Ctrl.fontSection = dotNetObject "System.Drawing.Font" "Segoe UI" 9.0 ((dotNetClass "System.Drawing.FontStyle").Bold)
    M2R_Ctrl.fontRegular = dotNetObject "System.Drawing.Font" "Segoe UI" 8.5 ((dotNetClass "System.Drawing.FontStyle").Regular)
    M2R_Ctrl.fontMono    = dotNetObject "System.Drawing.Font" "Consolas" 8.5 ((dotNetClass "System.Drawing.FontStyle").Regular)

    local frm = dotNetObject "System.Windows.Forms.Form"
    frm.Text = "MaxToRevit Bridge Pro — 3ds Max to Revit (.RFA / .RVT) Converter"
    frm.Size = dotNetObject "System.Drawing.Size" 860 570
    frm.StartPosition = (dotNetClass "System.Windows.Forms.FormStartPosition").CenterScreen
    frm.FormBorderStyle = (dotNetClass "System.Windows.Forms.FormBorderStyle").FixedDialog
    frm.MaximizeBox = false
    frm.BackColor = M2R_Ctrl.clrBgDark
    frm.ForeColor = M2R_Ctrl.clrText
    M2R_Ctrl.frm = frm

    -- Header Bar
    local pnlHeader = M2R_Ctrl.createCard 12 10 822 46
    local lblHeaderTitle = M2R_Ctrl.createLabel "MaxToRevit Bridge Pro — Convert 3ds Max Models to Revit (.RFA / .RVT)" 14 6 580 20 M2R_Ctrl.fontTitle M2R_Ctrl.clrText
    local lblHeaderSub   = M2R_Ctrl.createLabel "Select Family Template (.rft), Category, Parameters, and Geometry Optimization" 14 25 580 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local lblRevitBadge  = M2R_Ctrl.createLabel "Revit 2026 Ready" 690 12 120 22 M2R_Ctrl.fontSection M2R_Ctrl.clrSuccess
    lblRevitBadge.TextAlign = (dotNetClass "System.Drawing.ContentAlignment").MiddleRight
    pnlHeader.Controls.AddRange #(lblHeaderTitle, lblHeaderSub, lblRevitBadge)
    frm.Controls.Add pnlHeader

    -- Card 1: Output Format, Revit Version & RFT Template
    local pnlOut = M2R_Ctrl.createCard 12 64 480 148
    local lblSec1 = M2R_Ctrl.createLabel "1. Output Format, Revit Version & Family Template (.rft)" 12 10 450 18 M2R_Ctrl.fontSection M2R_Ctrl.clrText

    local lblOutMode = M2R_Ctrl.createLabel "Convert To:" 12 34 140 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbOutMode = dotNetObject "System.Windows.Forms.ComboBox"
    cmbOutMode.Location = dotNetObject "System.Drawing.Point" 12 52
    cmbOutMode.Size = dotNetObject "System.Drawing.Size" 220 24
    cmbOutMode.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbOutMode.BackColor = M2R_Ctrl.clrInput
    cmbOutMode.ForeColor = M2R_Ctrl.clrText
    cmbOutMode.Font = M2R_Ctrl.fontRegular
    cmbOutMode.Items.AddRange #("Revit Family (.rfa)", "Revit Project Model (.rvt)", "Direct CAD/BIM (.dxf / .sat)")
    cmbOutMode.SelectedIndex = 0
    M2R_Ctrl.cmbOutMode = cmbOutMode

    local lblRevitVer = M2R_Ctrl.createLabel "Target Revit Version:" 246 34 200 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbRevitVer = dotNetObject "System.Windows.Forms.ComboBox"
    cmbRevitVer.Location = dotNetObject "System.Drawing.Point" 246 52
    cmbRevitVer.Size = dotNetObject "System.Drawing.Size" 220 24
    cmbRevitVer.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbRevitVer.BackColor = M2R_Ctrl.clrInput
    cmbRevitVer.ForeColor = M2R_Ctrl.clrText
    cmbRevitVer.Font = M2R_Ctrl.fontRegular
    cmbRevitVer.Items.AddRange #("Revit 2026 (Installed)", "Revit 2025", "Revit 2024")
    cmbRevitVer.SelectedIndex = 0
    M2R_Ctrl.cmbRevitVer = cmbRevitVer

    local lblTemplate = M2R_Ctrl.createLabel "Family Template (.rft from Revit Library):" 12 84 440 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbTemplate = dotNetObject "System.Windows.Forms.ComboBox"
    cmbTemplate.Location = dotNetObject "System.Drawing.Point" 12 104
    cmbTemplate.Size = dotNetObject "System.Drawing.Size" 454 24
    cmbTemplate.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbTemplate.BackColor = M2R_Ctrl.clrInput
    cmbTemplate.ForeColor = M2R_Ctrl.clrText
    cmbTemplate.Font = M2R_Ctrl.fontRegular
    M2R_Ctrl.cmbTemplate = cmbTemplate

    pnlOut.Controls.AddRange #(lblSec1, lblOutMode, cmbOutMode, lblRevitVer, cmbRevitVer, lblTemplate, cmbTemplate)
    frm.Controls.Add pnlOut

    -- Card 2: 3ds Max Geometry, Materials, ProOptimizer & Pivot
    local pnlGeo = M2R_Ctrl.createCard 12 220 480 180
    local lblSec2 = M2R_Ctrl.createLabel "2. 3ds Max Geometry, Materials, Pivot & ProOptimizer" 12 10 450 18 M2R_Ctrl.fontSection M2R_Ctrl.clrText

    local lblScope = M2R_Ctrl.createLabel "Export Scope:" 12 34 210 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbScope = dotNetObject "System.Windows.Forms.ComboBox"
    cmbScope.Location = dotNetObject "System.Drawing.Point" 12 52
    cmbScope.Size = dotNetObject "System.Drawing.Size" 220 24
    cmbScope.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbScope.BackColor = M2R_Ctrl.clrInput
    cmbScope.ForeColor = M2R_Ctrl.clrText
    cmbScope.Font = M2R_Ctrl.fontRegular
    cmbScope.Items.AddRange #(
        "Selected Objects -> Single File",
        "Batch: Each Selected -> Separate .RFA",
        "Entire Max Scene -> Single File"
    )
    cmbScope.SelectedIndex = 0
    M2R_Ctrl.cmbScope = cmbScope

    local lblPivot = M2R_Ctrl.createLabel "Pivot / Host Alignment:" 246 34 210 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbPivot = dotNetObject "System.Windows.Forms.ComboBox"
    cmbPivot.Location = dotNetObject "System.Drawing.Point" 246 52
    cmbPivot.Size = dotNetObject "System.Drawing.Size" 220 24
    cmbPivot.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbPivot.BackColor = M2R_Ctrl.clrInput
    cmbPivot.ForeColor = M2R_Ctrl.clrText
    cmbPivot.Font = M2R_Ctrl.fontRegular
    cmbPivot.Items.AddRange #(
        "Auto by Template (Bottom / Wall / Ceiling)",
        "Bottom-Center at (0,0,0) [Floor/Base]",
        "Back-Center at (0,0,0) [Wall Host]",
        "Top-Center at (0,0,0) [Ceiling Host]",
        "Keep Original World Coordinates"
    )
    cmbPivot.SelectedIndex = 0
    M2R_Ctrl.cmbPivot = cmbPivot

    local lblGeoMode = M2R_Ctrl.createLabel "Material & Geometry Mode:" 12 86 220 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local cmbGeoMode = dotNetObject "System.Windows.Forms.ComboBox"
    cmbGeoMode.Location = dotNetObject "System.Drawing.Point" 12 104
    cmbGeoMode.Size = dotNetObject "System.Drawing.Size" 220 24
    cmbGeoMode.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbGeoMode.BackColor = M2R_Ctrl.clrInput
    cmbGeoMode.ForeColor = M2R_Ctrl.clrText
    cmbGeoMode.Font = M2R_Ctrl.fontRegular
    cmbGeoMode.Items.AddRange #(
        "Native Revit Geometry (Extrusion / Free Form)",
        "Import Instance by Material (DXF Layers)",
        "Clean ACIS Solid (.SAT)",
        "DirectShape Mesh (.OBJ)"
    )
    cmbGeoMode.SelectedIndex = 0
    M2R_Ctrl.cmbGeoMode = cmbGeoMode

    local chkOptimize = dotNetObject "System.Windows.Forms.CheckBox"
    chkOptimize.Text = "ProOptimizer (Safe Clone): OFF"
    chkOptimize.Location = dotNetObject "System.Drawing.Point" 246 84
    chkOptimize.Size = dotNetObject "System.Drawing.Size" 220 20
    chkOptimize.Font = M2R_Ctrl.fontRegular
    chkOptimize.ForeColor = M2R_Ctrl.clrText
    chkOptimize.Checked = false
    M2R_Ctrl.chkOptimize = chkOptimize

    local trkOptimize = dotNetObject "System.Windows.Forms.TrackBar"
    trkOptimize.Location = dotNetObject "System.Drawing.Point" 242 104
    trkOptimize.Size = dotNetObject "System.Drawing.Size" 226 32
    trkOptimize.Minimum = 10
    trkOptimize.Maximum = 100
    trkOptimize.TickFrequency = 10
    trkOptimize.Value = 50
    trkOptimize.Enabled = false
    M2R_Ctrl.trkOptimize = trkOptimize

    local chkOpenInRevit = dotNetObject "System.Windows.Forms.CheckBox"
    chkOpenInRevit.Text = "Keep converted file open in Revit after saving"
    chkOpenInRevit.Location = dotNetObject "System.Drawing.Point" 12 144
    chkOpenInRevit.Size = dotNetObject "System.Drawing.Size" 340 22
    chkOpenInRevit.Font = M2R_Ctrl.fontRegular
    chkOpenInRevit.ForeColor = M2R_Ctrl.clrMuted
    chkOpenInRevit.Checked = true
    M2R_Ctrl.chkOpenInRevit = chkOpenInRevit

    pnlGeo.Controls.AddRange #(lblSec2, lblScope, cmbScope, lblPivot, cmbPivot, lblGeoMode, cmbGeoMode, chkOptimize, trkOptimize, chkOpenInRevit)
    frm.Controls.Add pnlGeo

    -- Card 3: Output Path & Convert Button
    local pnlSave = M2R_Ctrl.createCard 12 408 480 112
    local lblSavePath = M2R_Ctrl.createLabel "Output File / Folder Path:" 12 10 300 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted

    local defaultOutDir = (sysInfo.tempdir + "MaxToRevit_Exports")
    makeDir defaultOutDir all:true
    local defaultFileName = if selection.count > 0 then selection[1].name else "Max_Family_01"

    local txtSavePath = dotNetObject "System.Windows.Forms.TextBox"
    txtSavePath.Location = dotNetObject "System.Drawing.Point" 12 30
    txtSavePath.Size = dotNetObject "System.Drawing.Size" 364 24
    txtSavePath.BackColor = M2R_Ctrl.clrInput
    txtSavePath.ForeColor = M2R_Ctrl.clrText
    txtSavePath.Font = M2R_Ctrl.fontMono
    txtSavePath.Text = (defaultOutDir + "\\" + defaultFileName + ".rfa")
    M2R_Ctrl.txtSavePath = txtSavePath

    local btnBrowse = dotNetObject "System.Windows.Forms.Button"
    btnBrowse.Text = "Browse..."
    btnBrowse.Location = dotNetObject "System.Drawing.Point" 384 28
    btnBrowse.Size = dotNetObject "System.Drawing.Size" 82 26
    btnBrowse.FlatStyle = (dotNetClass "System.Windows.Forms.FlatStyle").Flat
    btnBrowse.BackColor = M2R_Ctrl.clrInput
    btnBrowse.ForeColor = M2R_Ctrl.clrText
    btnBrowse.Font = M2R_Ctrl.fontRegular
    M2R_Ctrl.btnBrowse = btnBrowse

    local lblStatus = M2R_Ctrl.createLabel ("Ready — " + (selection.count as string) + " object(s) currently selected in 3ds Max.") 12 68 270 32 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    M2R_Ctrl.lblStatus = lblStatus

    local btnConvert = dotNetObject "System.Windows.Forms.Button"
    btnConvert.Text = "Convert && Save to Revit"
    btnConvert.Location = dotNetObject "System.Drawing.Point" 290 64
    btnConvert.Size = dotNetObject "System.Drawing.Size" 176 36
    btnConvert.FlatStyle = (dotNetClass "System.Windows.Forms.FlatStyle").Flat
    btnConvert.BackColor = M2R_Ctrl.clrAccent
    btnConvert.ForeColor = M2R_Ctrl.clrText
    btnConvert.Font = M2R_Ctrl.fontSection
    M2R_Ctrl.btnConvert = btnConvert

    pnlSave.Controls.AddRange #(lblSavePath, txtSavePath, btnBrowse, lblStatus, btnConvert)
    frm.Controls.Add pnlSave

    -- Card 4 (Right Column): Family Category & Parameters
    local pnlCat = M2R_Ctrl.createCard 502 64 332 456
    local lblSec3 = M2R_Ctrl.createLabel "3. Family Category & Parameters" 12 10 200 18 M2R_Ctrl.fontSection M2R_Ctrl.clrText
    local lblSelectedCatBadge = M2R_Ctrl.createLabel "Furniture" 210 10 110 18 M2R_Ctrl.fontSection M2R_Ctrl.clrAccent
    lblSelectedCatBadge.TextAlign = (dotNetClass "System.Drawing.ContentAlignment").MiddleRight
    M2R_Ctrl.lblSelectedCatBadge = lblSelectedCatBadge

    local lblSearch = M2R_Ctrl.createLabel "Search Category:" 12 34 160 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted
    local lblFilter = M2R_Ctrl.createLabel "Filter List:" 198 34 120 16 M2R_Ctrl.fontRegular M2R_Ctrl.clrMuted

    local txtCatSearch = dotNetObject "System.Windows.Forms.TextBox"
    txtCatSearch.Location = dotNetObject "System.Drawing.Point" 12 52
    txtCatSearch.Size = dotNetObject "System.Drawing.Size" 178 24
    txtCatSearch.BackColor = M2R_Ctrl.clrInput
    txtCatSearch.ForeColor = M2R_Ctrl.clrText
    txtCatSearch.Font = M2R_Ctrl.fontRegular
    M2R_Ctrl.txtCatSearch = txtCatSearch

    local cmbCatFilter = dotNetObject "System.Windows.Forms.ComboBox"
    cmbCatFilter.Location = dotNetObject "System.Drawing.Point" 198 52
    cmbCatFilter.Size = dotNetObject "System.Drawing.Size" 120 24
    cmbCatFilter.DropDownStyle = (dotNetClass "System.Windows.Forms.ComboBoxStyle").DropDownList
    cmbCatFilter.BackColor = M2R_Ctrl.clrInput
    cmbCatFilter.ForeColor = M2R_Ctrl.clrText
    cmbCatFilter.Font = M2R_Ctrl.fontRegular
    cmbCatFilter.Items.AddRange #("All", "Architecture", "Structure", "MEP")
    cmbCatFilter.SelectedIndex = 0
    M2R_Ctrl.cmbCatFilter = cmbCatFilter

    local lstCategories = dotNetObject "System.Windows.Forms.ListBox"
    lstCategories.Location = dotNetObject "System.Drawing.Point" 12 84
    lstCategories.Size = dotNetObject "System.Drawing.Size" 306 196
    lstCategories.BackColor = M2R_Ctrl.clrInput
    lstCategories.ForeColor = M2R_Ctrl.clrText
    lstCategories.Font = M2R_Ctrl.fontRegular
    lstCategories.BorderStyle = (dotNetClass "System.Windows.Forms.BorderStyle").FixedSingle
    M2R_Ctrl.lstCategories = lstCategories

    local lblFamParams = M2R_Ctrl.createLabel "Family Parameters:" 12 288 260 18 M2R_Ctrl.fontSection M2R_Ctrl.clrText

    local chkWorkPlane = dotNetObject "System.Windows.Forms.CheckBox"
    chkWorkPlane.Text = "Work Plane-Based"
    chkWorkPlane.Location = dotNetObject "System.Drawing.Point" 16 310
    chkWorkPlane.Size = dotNetObject "System.Drawing.Size" 290 20
    chkWorkPlane.Font = M2R_Ctrl.fontRegular
    chkWorkPlane.ForeColor = M2R_Ctrl.clrText
    chkWorkPlane.Checked = false
    M2R_Ctrl.chkWorkPlane = chkWorkPlane

    local chkAlwaysVert = dotNetObject "System.Windows.Forms.CheckBox"
    chkAlwaysVert.Text = "Always vertical"
    chkAlwaysVert.Location = dotNetObject "System.Drawing.Point" 16 333
    chkAlwaysVert.Size = dotNetObject "System.Drawing.Size" 290 20
    chkAlwaysVert.Font = M2R_Ctrl.fontRegular
    chkAlwaysVert.ForeColor = M2R_Ctrl.clrText
    chkAlwaysVert.Checked = true
    M2R_Ctrl.chkAlwaysVert = chkAlwaysVert

    local chkCutVoids = dotNetObject "System.Windows.Forms.CheckBox"
    chkCutVoids.Text = "Cut with Voids When Loaded"
    chkCutVoids.Location = dotNetObject "System.Drawing.Point" 16 356
    chkCutVoids.Size = dotNetObject "System.Drawing.Size" 290 20
    chkCutVoids.Font = M2R_Ctrl.fontRegular
    chkCutVoids.ForeColor = M2R_Ctrl.clrText
    chkCutVoids.Checked = false
    M2R_Ctrl.chkCutVoids = chkCutVoids

    local chkHostRebar = dotNetObject "System.Windows.Forms.CheckBox"
    chkHostRebar.Text = "Can host rebar"
    chkHostRebar.Location = dotNetObject "System.Drawing.Point" 16 379
    chkHostRebar.Size = dotNetObject "System.Drawing.Size" 290 20
    chkHostRebar.Font = M2R_Ctrl.fontRegular
    chkHostRebar.ForeColor = M2R_Ctrl.clrText
    chkHostRebar.Checked = false
    M2R_Ctrl.chkHostRebar = chkHostRebar

    local chkShared = dotNetObject "System.Windows.Forms.CheckBox"
    chkShared.Text = "Shared Family"
    chkShared.Location = dotNetObject "System.Drawing.Point" 16 402
    chkShared.Size = dotNetObject "System.Drawing.Size" 290 20
    chkShared.Font = M2R_Ctrl.fontRegular
    chkShared.ForeColor = M2R_Ctrl.clrText
    chkShared.Checked = false
    M2R_Ctrl.chkShared = chkShared

    local chkRoomPoint = dotNetObject "System.Windows.Forms.CheckBox"
    chkRoomPoint.Text = "Room Calculation Point"
    chkRoomPoint.Location = dotNetObject "System.Drawing.Point" 16 425
    chkRoomPoint.Size = dotNetObject "System.Drawing.Size" 290 20
    chkRoomPoint.Font = M2R_Ctrl.fontRegular
    chkRoomPoint.ForeColor = M2R_Ctrl.clrText
    chkRoomPoint.Checked = false
    M2R_Ctrl.chkRoomPoint = chkRoomPoint

    pnlCat.Controls.AddRange #(
        lblSec3, lblSelectedCatBadge, lblSearch, lblFilter,
        txtCatSearch, cmbCatFilter, lstCategories,
        lblFamParams, chkWorkPlane, chkAlwaysVert, chkCutVoids, chkHostRebar, chkShared, chkRoomPoint
    )
    frm.Controls.Add pnlCat

    -- Populate Initial Data
    M2R_Ctrl.populateTemplates()
    M2R_Ctrl.populateCategories()

    -- Wire Events
    dotNet.addEventHandler cmbTemplate "SelectedIndexChanged" M2R_OnTemplateChanged
    dotNet.addEventHandler txtCatSearch "TextChanged" M2R_OnCatSearchChanged
    dotNet.addEventHandler cmbCatFilter "SelectedIndexChanged" M2R_OnCatFilterChanged
    dotNet.addEventHandler lstCategories "SelectedIndexChanged" M2R_OnCatListSelected
    dotNet.addEventHandler cmbOutMode "SelectedIndexChanged" M2R_OnOutModeChanged
    dotNet.addEventHandler chkOptimize "CheckedChanged" M2R_OnOptCheckChanged
    dotNet.addEventHandler trkOptimize "Scroll" M2R_OnOptScroll
    dotNet.addEventHandler btnBrowse "Click" M2R_OnBrowseClicked
    dotNet.addEventHandler btnConvert "Click" M2R_OnConvertClicked

    frm.Show()
)

macroScript MaxToRevitConverter
    category:"MaxToRevit Bridge"
    buttonText:"MaxToRevit"
    toolTip:"MaxToRevit Bridge Pro — Convert 3ds Max Models to Revit (.rfa / .rvt)"
(
    on execute do
    (
        MaxToRevit_LaunchUI()
    )
)

MaxToRevit_LaunchUI()
