using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace MaxToRevitBridge
{
    #region Data Models

    public class ConversionJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString("N");
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
        public string OutputMode { get; set; } = "RFA"; // "RFA" or "RVT"
        public string TargetRevitVersion { get; set; } = "2026";
        public string FamilyTemplatePath { get; set; } = "";
        public string ProjectTemplatePath { get; set; } = "";
        public string CategoryName { get; set; } = "Generic Models";
        public string GeometryFormat { get; set; } = "DXF"; // "DXF", "SAT", "OBJ"
        public string ImportUnits { get; set; } = "mm";     // Always scaled to mm by MaxScript
        public bool HideMeshEdges { get; set; } = true;
        public bool OpenInRevitAfterSave { get; set; } = false;
        public FamilyParamsSpec FamilyParameters { get; set; } = new FamilyParamsSpec();
        public List<MaterialSpec> Materials { get; set; } = new List<MaterialSpec>();
        public List<JobItem> Items { get; set; } = new List<JobItem>();
    }

    public class FamilyParamsSpec
    {
        public bool WorkPlaneBased { get; set; } = false;
        public bool AlwaysVertical { get; set; } = true;
        public bool CutWithVoidsWhenLoaded { get; set; } = false;
        public bool CanHostRebar { get; set; } = false;
        public bool Shared { get; set; } = false;
        public bool RoomCalculationPoint { get; set; } = false;
    }

    public class MaterialSpec
    {
        public string LayerName { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public byte R { get; set; } = 180;
        public byte G { get; set; } = 180;
        public byte B { get; set; } = 180;
        public int Transparency { get; set; } = 0; // 0..100
        public int Shininess { get; set; } = 50;   // 0..100
    }

    public class JobItem
    {
        public string ObjectName { get; set; } = "MaxObject";
        public string GeometryFilePath { get; set; } = "";
        public string TargetOutputPath { get; set; } = "";
        public double WidthMm { get; set; } = 0;
        public double DepthMm { get; set; } = 0;
        public double HeightMm { get; set; } = 0;
    }

    public class JobStatus
    {
        public string JobId { get; set; } = "";
        public string State { get; set; } = "Pending"; // "Processing", "Completed", "Failed"
        public string Message { get; set; } = "";
        public List<string> OutputFiles { get; set; } = new List<string>();
        public List<string> Logs { get; set; } = new List<string>();
        public string CompletedAt { get; set; } = "";
    }

    #endregion

    #region Revit Application & Ribbon

    public class App : IExternalApplication
    {
        public static bool AutoWatchEnabled = true;
        private static DateTime _lastQueueCheck = DateTime.MinValue;
        private static bool _isProcessing = false;

        public static string WorkspaceBridgeDir =>
            @"C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\BridgeData";

        public static string LocalAppBridgeDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MaxToRevitBridge");

        public static string[] GetQueueDirectories()
        {
            return new[]
            {
                Path.Combine(WorkspaceBridgeDir, "Queue"),
                Path.Combine(LocalAppBridgeDir, "Queue")
            };
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                foreach (string baseDir in new[] { WorkspaceBridgeDir, LocalAppBridgeDir })
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(baseDir, "Queue"));
                        Directory.CreateDirectory(Path.Combine(baseDir, "Processed"));
                        Directory.CreateDirectory(Path.Combine(baseDir, "Status"));
                        Directory.CreateDirectory(Path.Combine(baseDir, "Staging"));
                    }
                    catch { }
                }

                RibbonPanel panel = application.CreateRibbonPanel("MaxToRevit Bridge");
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                PushButtonData btnProcess = new PushButtonData(
                    "MaxToRevit_ProcessQueue",
                    "Convert Pending\n3ds Max Jobs",
                    assemblyPath,
                    typeof(ProcessQueueCommand).FullName);
                btnProcess.ToolTip = "Immediately processes any pending 3ds Max to Revit conversion jobs.";
                panel.AddItem(btnProcess);

                PushButtonData btnImport = new PushButtonData(
                    "MaxToRevit_ImportJob",
                    "Open Max Job\n(.max2rvt)",
                    assemblyPath,
                    typeof(ImportJobFileCommand).FullName);
                btnImport.ToolTip = "Browse for a .max2rvt.json job file exported from 3ds Max and convert it to RFA/RVT.";
                panel.AddItem(btnImport);

                PushButtonData btnToggle = new PushButtonData(
                    "MaxToRevit_ToggleWatch",
                    "Auto-Watch:\nON",
                    assemblyPath,
                    typeof(ToggleAutoWatchCommand).FullName);
                btnToggle.ToolTip = "Toggle automatic background watching of the 3ds Max export queue.";
                panel.AddItem(btnToggle);

                application.Idling += OnIdling;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("MaxToRevit Bridge Startup Error", ex.Message);
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.Idling -= OnIdling;
            return Result.Succeeded;
        }

        private void OnIdling(object sender, IdlingEventArgs e)
        {
            if (!AutoWatchEnabled || _isProcessing)
                return;

            if ((DateTime.UtcNow - _lastQueueCheck).TotalSeconds < 1.0)
                return;

            _lastQueueCheck = DateTime.UtcNow;

            try
            {
                List<string> pendingFiles = new List<string>();
                foreach (string qDir in GetQueueDirectories())
                {
                    if (Directory.Exists(qDir))
                    {
                        pendingFiles.AddRange(Directory.GetFiles(qDir, "*.max2rvt.json"));
                    }
                }

                if (pendingFiles.Count == 0)
                    return;

                UIApplication uiApp = sender as UIApplication;
                if (uiApp == null)
                    return;

                _isProcessing = true;
                try
                {
                    foreach (string jobFile in pendingFiles)
                    {
                        if (File.Exists(jobFile))
                        {
                            RevitConversionEngine.ProcessJobFile(uiApp, jobFile, showDialogOnSuccess: false);
                        }
                    }
                }
                finally
                {
                    _isProcessing = false;
                }
            }
            catch
            {
                _isProcessing = false;
            }
        }
    }

    #endregion

    #region External Commands

    [Transaction(TransactionMode.Manual)]
    public class ProcessQueueCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            List<string> pendingFiles = new List<string>();
            foreach (string qDir in App.GetQueueDirectories())
            {
                if (Directory.Exists(qDir))
                    pendingFiles.AddRange(Directory.GetFiles(qDir, "*.max2rvt.json"));
            }

            if (pendingFiles.Count == 0)
            {
                TaskDialog.Show("MaxToRevit Bridge", "No pending 3ds Max jobs found in the queue.\n\nUse the MaxToRevit script inside 3ds Max and click 'Convert & Save to Revit'.");
                return Result.Succeeded;
            }

            foreach (string jobFile in pendingFiles)
            {
                if (File.Exists(jobFile))
                    RevitConversionEngine.ProcessJobFile(commandData.Application, jobFile, showDialogOnSuccess: true);
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ImportJobFileCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            using (var ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Title = "Select 3ds Max to Revit Job File (.max2rvt.json)";
                ofd.Filter = "MaxToRevit Job (*.max2rvt.json;*.json)|*.max2rvt.json;*.json|All Files (*.*)|*.*";
                ofd.InitialDirectory = Path.Combine(App.WorkspaceBridgeDir, "Queue");

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RevitConversionEngine.ProcessJobFile(commandData.Application, ofd.FileName, showDialogOnSuccess: true);
                }
            }
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ToggleAutoWatchCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            App.AutoWatchEnabled = !App.AutoWatchEnabled;
            TaskDialog.Show("MaxToRevit Bridge",
                App.AutoWatchEnabled
                    ? "Auto-Watch Queue is now ENABLED.\nRevit will automatically convert jobs sent from 3ds Max."
                    : "Auto-Watch Queue is now DISABLED.\nClick 'Convert Pending 3ds Max Jobs' to run jobs manually.");
            return Result.Succeeded;
        }
    }

    #endregion

    #region Conversion Engine

    public static class RevitConversionEngine
    {
        private static readonly Dictionary<string, BuiltInCategory> CategoryMap =
            new Dictionary<string, BuiltInCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "Audio Visual Devices", BuiltInCategory.OST_AudioVisualDevices },
                { "Casework", BuiltInCategory.OST_Casework },
                { "Ceilings", BuiltInCategory.OST_Ceilings },
                { "Columns", BuiltInCategory.OST_Columns },
                { "Curtain Panels", BuiltInCategory.OST_CurtainWallPanels },
                { "Curtain Wall Panels", BuiltInCategory.OST_CurtainWallPanels },
                { "Data Devices", BuiltInCategory.OST_DataDevices },
                { "Detail Items", BuiltInCategory.OST_DetailComponents },
                { "Doors", BuiltInCategory.OST_Doors },
                { "Electrical Equipment", BuiltInCategory.OST_ElectricalEquipment },
                { "Electrical Fixtures", BuiltInCategory.OST_ElectricalFixtures },
                { "Entourage", BuiltInCategory.OST_Entourage },
                { "Fire Alarm Devices", BuiltInCategory.OST_FireAlarmDevices },
                { "Fire Protection", BuiltInCategory.OST_FireProtection },
                { "Floors", BuiltInCategory.OST_Floors },
                { "Food Service Equipment", BuiltInCategory.OST_FoodServiceEquipment },
                { "Furniture", BuiltInCategory.OST_Furniture },
                { "Furniture Systems", BuiltInCategory.OST_FurnitureSystems },
                { "Generic Models", BuiltInCategory.OST_GenericModel },
                { "Hardscape", BuiltInCategory.OST_Hardscape },
                { "Lighting Fixtures", BuiltInCategory.OST_LightingFixtures },
                { "Mass", BuiltInCategory.OST_Mass },
                { "Mechanical Control Devices", BuiltInCategory.OST_MechanicalControlDevices },
                { "Mechanical Equipment", BuiltInCategory.OST_MechanicalEquipment },
                { "Medical Equipment", BuiltInCategory.OST_MedicalEquipment },
                { "Parking", BuiltInCategory.OST_Parking },
                { "Planting", BuiltInCategory.OST_Planting },
                { "Plumbing Equipment", BuiltInCategory.OST_PlumbingEquipment },
                { "Plumbing Fixtures", BuiltInCategory.OST_PlumbingFixtures },
                { "Railings", BuiltInCategory.OST_StairsRailing },
                { "Railings - Supports", BuiltInCategory.OST_RailingSupport },
                { "Railings - Terminations", BuiltInCategory.OST_RailingTermination },
                { "Supports", BuiltInCategory.OST_RailingSupport },
                { "Terminations", BuiltInCategory.OST_RailingTermination },
                { "Roads", BuiltInCategory.OST_Roads },
                { "Roofs", BuiltInCategory.OST_Roofs },
                { "Signage", BuiltInCategory.OST_Signage },
                { "Site", BuiltInCategory.OST_Site },
                { "Specialty Equipment", BuiltInCategory.OST_SpecialityEquipment },
                { "Stairs", BuiltInCategory.OST_Stairs },
                { "Structural Columns", BuiltInCategory.OST_StructuralColumns },
                { "Structural Connections", BuiltInCategory.OST_StructConnections },
                { "Structural Foundations", BuiltInCategory.OST_StructuralFoundation },
                { "Structural Framing", BuiltInCategory.OST_StructuralFraming },
                { "Structural Stiffeners", BuiltInCategory.OST_StructuralStiffener },
                { "Structural Trusses", BuiltInCategory.OST_StructuralTruss },
                { "Telephone Devices", BuiltInCategory.OST_TelephoneDevices },
                { "Temporary Structures", BuiltInCategory.OST_TemporaryStructure },
                { "Topography", BuiltInCategory.OST_Topography },
                { "Toposolid", BuiltInCategory.OST_Toposolid },
                { "Vertical Circulation", BuiltInCategory.OST_VerticalCirculation },
                { "Walls", BuiltInCategory.OST_Walls },
                { "Windows", BuiltInCategory.OST_Windows }
            };

        public static bool ProcessJobFile(UIApplication uiApp, string jobFilePath, bool showDialogOnSuccess)
        {
            JobStatus status = new JobStatus();
            string jobDir = Path.GetDirectoryName(jobFilePath) ?? "";
            string baseBridgeDir = Directory.GetParent(jobDir)?.FullName ?? App.WorkspaceBridgeDir;
            string statusDir = Path.Combine(baseBridgeDir, "Status");
            string processedDir = Path.Combine(baseBridgeDir, "Processed");
            Directory.CreateDirectory(statusDir);
            Directory.CreateDirectory(processedDir);

            string statusFilePath = "";

            try
            {
                string json = File.ReadAllText(jobFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                ConversionJob job = JsonSerializer.Deserialize<ConversionJob>(json, options);

                if (job == null)
                    throw new InvalidOperationException("Failed to parse conversion job JSON.");

                status.JobId = job.JobId;
                status.State = "Processing";
                statusFilePath = Path.Combine(statusDir, $"{job.JobId}.status.json");
                WriteStatus(statusFilePath, status);

                // Move job file from Queue to Processed immediately so Idling does not pick it up twice
                string destJobPath = Path.Combine(processedDir, Path.GetFileName(jobFilePath));
                if (File.Exists(destJobPath))
                    File.Delete(destJobPath);
                File.Move(jobFilePath, destJobPath);

                Application app = uiApp.Application;

                foreach (JobItem item in job.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.GeometryFilePath) || !File.Exists(item.GeometryFilePath))
                    {
                        status.Logs.Add($"Skipped '{item.ObjectName}': Geometry file not found ({item.GeometryFilePath}).");
                        continue;
                    }

                    string outDir = Path.GetDirectoryName(item.TargetOutputPath);
                    if (!string.IsNullOrEmpty(outDir))
                        Directory.CreateDirectory(outDir);

                    if (string.Equals(job.OutputMode, "RVT", StringComparison.OrdinalIgnoreCase))
                    {
                        ConvertItemToRevitProject(uiApp, app, job, item, status);
                    }
                    else
                    {
                        ConvertItemToRevitFamily(uiApp, app, job, item, status);
                    }
                }

                status.State = "Completed";
                status.Message = $"Successfully converted {status.OutputFiles.Count} file(s).";
                status.CompletedAt = DateTime.Now.ToString("o");
                WriteStatus(statusFilePath, status);

                if (showDialogOnSuccess)
                {
                    TaskDialog.Show("MaxToRevit Bridge - Conversion Complete",
                        $"Successfully created {status.OutputFiles.Count} Revit file(s):\n\n" +
                        string.Join("\n", status.OutputFiles) +
                        (status.Logs.Count > 0 ? "\n\nDetails:\n" + string.Join("\n", status.Logs.Take(8)) : ""));
                }

                return true;
            }
            catch (Exception ex)
            {
                status.State = "Failed";
                status.Message = ex.Message;
                status.Logs.Add(ex.ToString());
                status.CompletedAt = DateTime.Now.ToString("o");
                if (!string.IsNullOrEmpty(statusFilePath))
                    WriteStatus(statusFilePath, status);

                if (showDialogOnSuccess)
                {
                    TaskDialog.Show("MaxToRevit Bridge - Error", $"Conversion failed:\n{ex.Message}");
                }
                return false;
            }
        }

        private static void ConvertItemToRevitFamily(
            UIApplication uiApp,
            Application app,
            ConversionJob job,
            JobItem item,
            JobStatus status)
        {
            string rftPath = ResolveFamilyTemplatePath(job.FamilyTemplatePath, job.CategoryName);
            status.Logs.Add($"Using Family Template: {Path.GetFileName(rftPath)}");

            Document famDoc = app.NewFamilyDocument(rftPath);
            string targetPath = item.TargetOutputPath;
            if (!targetPath.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase))
                targetPath = Path.ChangeExtension(targetPath, ".rfa");

            try
            {
                BuildFamilyContent(famDoc, job, item, rftPath, status);

                SaveAsOptions saveOpts = new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    Compact = true,
                    MaximumBackups = 1
                };

                famDoc.SaveAs(targetPath, saveOpts);
                status.OutputFiles.Add(targetPath);
                status.Logs.Add($"Saved Family: {targetPath}");
            }
            finally
            {
                famDoc.Close(false);
            }

            if (job.OpenInRevitAfterSave && File.Exists(targetPath))
            {
                try { uiApp.OpenAndActivateDocument(targetPath); } catch { }
            }
        }

        /// <summary>
        /// Configures the family (Category, Parameters, Opening size), creates Materials,
        /// builds the geometry (native Extrusion / Free Form, or Import Instance) and aligns windows to Sill Height.
        /// </summary>
        private static void BuildFamilyContent(Document famDoc, ConversionJob job, JobItem item, string rftPath, JobStatus status)
        {
            using (Transaction tx = new Transaction(famDoc, "MaxToRevit - Build Family"))
            {
                FailureHandlingOptions fho = tx.GetFailureHandlingOptions();
                fho.SetFailuresPreprocessor(new WarningSwallower());
                tx.SetFailureHandlingOptions(fho);
                tx.Start();

                // 1. Family Category
                ApplyFamilyCategory(famDoc, job.CategoryName, status);

                // 2. Family Parameters (Work Plane-Based, Always Vertical, Cut with Voids, etc.)
                ApplyFamilyParameters(famDoc, job.FamilyParameters, status);

                // 3. Window / Door opening size + Sill Height
                double sillHeightFeet = SyncOpeningDimensionsAndGetSillOffset(famDoc, item, rftPath, job.CategoryName, status);

                // 4. Revit Materials (created first so native geometry can use them)
                Dictionary<string, ElementId> matMap = ApplyMaterialsToDocument(famDoc, job.Materials, status);

                // 5. Geometry
                List<ElementId> geoIds = ImportGeometryIntoDocument(famDoc, item.GeometryFilePath, job, matMap, status);

                // Import Instance (DXF) layers only exist after import -> assign their materials now
                if (!item.GeometryFilePath.EndsWith(".m2r", StringComparison.OrdinalIgnoreCase))
                    ApplyMaterialsToDocument(famDoc, job.Materials, status);

                // 6. Align windows to Sill Height
                if (geoIds.Count > 0 && Math.Abs(sillHeightFeet) > 0.0001)
                {
                    try
                    {
                        famDoc.Regenerate();
                        double minZ = double.MaxValue;
                        foreach (ElementId id in geoIds)
                        {
                            BoundingBoxXYZ bb = famDoc.GetElement(id)?.get_BoundingBox(null);
                            if (bb != null) minZ = Math.Min(minZ, bb.Min.Z);
                        }
                        if (minZ < double.MaxValue)
                        {
                            double deltaZ = sillHeightFeet - minZ;
                            if (Math.Abs(deltaZ) > 0.001)
                            {
                                ElementTransformUtils.MoveElements(famDoc, geoIds, new XYZ(0, 0, deltaZ));
                                status.Logs.Add($"Aligned Window to Sill Height ({sillHeightFeet * 304.8:F0} mm).");
                            }
                        }
                    }
                    catch { }
                }

                // 7. Shaded 3D views
                SetViewsToShaded(famDoc);

                tx.Commit();
            }
        }

        private static double SyncOpeningDimensionsAndGetSillOffset(
            Document famDoc,
            JobItem item,
            string rftPath,
            string categoryName,
            JobStatus status)
        {
            double sillOffsetFeet = 0.0;
            if (!famDoc.IsFamilyDocument || famDoc.FamilyManager == null)
                return sillOffsetFeet;

            FamilyManager fm = famDoc.FamilyManager;
            const double mmToFeet = 1.0 / 304.8;

            try
            {
                // Ensure there is a current FamilyType if none exists
                if (fm.CurrentType == null)
                {
                    FamilyType existingType = null;
                    foreach (FamilyType ft in fm.Types)
                    {
                        if (ft != null && !string.IsNullOrWhiteSpace(ft.Name))
                        {
                            existingType = ft;
                            break;
                        }
                    }
                    if (existingType != null)
                        fm.CurrentType = existingType;
                    else
                        fm.NewType("Standard");
                }

                // Match Opening Width & Height to the 3ds Max Object's exact Width & Height!
                if (item.WidthMm > 50.0)
                {
                    FamilyParameter widthParam = fm.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)
                                              ?? fm.get_Parameter("Width");
                    if (widthParam != null && !widthParam.IsReadOnly && widthParam.StorageType == StorageType.Double)
                    {
                        fm.Set(widthParam, item.WidthMm * mmToFeet);
                        status.Logs.Add($"Resized Opening Width to {item.WidthMm:F0} mm.");
                    }
                }

                if (item.HeightMm > 50.0)
                {
                    FamilyParameter heightParam = fm.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)
                                               ?? fm.get_Parameter("Height");
                    if (heightParam != null && !heightParam.IsReadOnly && heightParam.StorageType == StorageType.Double)
                    {
                        fm.Set(heightParam, item.HeightMm * mmToFeet);
                        status.Logs.Add($"Resized Opening Height to {item.HeightMm:F0} mm.");
                    }
                }

                // Check if this is a Window template with a Sill Height parameter
                bool isWindow = categoryName.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                rftPath.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isWindow)
                {
                    FamilyParameter sillParam = fm.get_Parameter("Default Sill Height")
                                             ?? fm.get_Parameter("Sill Height");

                    if (sillParam != null && fm.CurrentType != null && fm.CurrentType.HasValue(sillParam))
                    {
                        double? val = fm.CurrentType.AsDouble(sillParam);
                        if (val.HasValue && val.Value > 0.0)
                        {
                            sillOffsetFeet = val.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status.Logs.Add($"Opening sync note: {ex.Message}");
            }

            return sillOffsetFeet;
        }

        private static void SetViewsToShaded(Document doc)
        {
            try
            {
                var views3D = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .Where(v => !v.IsTemplate);

                foreach (View3D v in views3D)
                {
                    try
                    {
                        v.DisplayStyle = DisplayStyle.ShadingWithEdges;
                        v.DetailLevel = ViewDetailLevel.Fine;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void ConvertItemToRevitProject(
            UIApplication uiApp,
            Application app,
            ConversionJob job,
            JobItem item,
            JobStatus status)
        {
            string rtePath = ResolveProjectTemplatePath(job.ProjectTemplatePath);
            Document projDoc = !string.IsNullOrEmpty(rtePath) && File.Exists(rtePath)
                ? app.NewProjectDocument(rtePath)
                : app.NewProjectDocument(UnitSystem.Metric);

            string targetPath = item.TargetOutputPath;
            if (!targetPath.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
                targetPath = Path.ChangeExtension(targetPath, ".rvt");

            try
            {
                bool placedViaFamily = false;
                string tempRfaPath = Path.Combine(Path.GetTempPath(), $"MaxToRevit_{Guid.NewGuid():N}_{SanitizeFileName(item.ObjectName)}.rfa");
                try
                {
                    string rftPath = ResolveFamilyTemplatePath(job.FamilyTemplatePath, job.CategoryName);
                    Document famDoc = app.NewFamilyDocument(rftPath);
                    try
                    {
                        BuildFamilyContent(famDoc, job, item, rftPath, status);

                        SaveAsOptions tempSaveOpts = new SaveAsOptions { OverwriteExistingFile = true, Compact = true, MaximumBackups = 1 };
                        famDoc.SaveAs(tempRfaPath, tempSaveOpts);
                    }
                    finally
                    {
                        famDoc.Close(false);
                    }

                    using (Transaction projTx = new Transaction(projDoc, "Load & Place Family in Project"))
                    {
                        projTx.Start();
                        if (projDoc.LoadFamily(tempRfaPath, out Family loadedFamily) && loadedFamily != null)
                        {
                            ISet<ElementId> symbolIds = loadedFamily.GetFamilySymbolIds();
                            if (symbolIds != null && symbolIds.Count > 0)
                            {
                                FamilySymbol symbol = projDoc.GetElement(symbolIds.First()) as FamilySymbol;
                                if (symbol != null)
                                {
                                    if (!symbol.IsActive)
                                        symbol.Activate();

                                    Level level = new FilteredElementCollector(projDoc)
                                        .OfClass(typeof(Level))
                                        .Cast<Level>()
                                        .OrderBy(l => l.Elevation)
                                        .FirstOrDefault();

                                    if (level != null)
                                    {
                                        projDoc.Create.NewFamilyInstance(XYZ.Zero, symbol, level, StructuralType.NonStructural);
                                    }
                                    else
                                    {
                                        projDoc.Create.NewFamilyInstance(XYZ.Zero, symbol, StructuralType.NonStructural);
                                    }
                                    placedViaFamily = true;
                                    status.Logs.Add($"Loaded & placed Family '{loadedFamily.Name}' ({job.CategoryName}) in Project.");
                                }
                            }
                        }
                        projTx.Commit();
                    }
                }
                catch (Exception famEx)
                {
                    status.Logs.Add($"Note: Fallback to Direct Project Import ({famEx.Message}).");
                }
                finally
                {
                    try { if (File.Exists(tempRfaPath)) File.Delete(tempRfaPath); } catch { }
                }

                if (!placedViaFamily)
                {
                    using (Transaction tx = new Transaction(projDoc, "Import Geometry into Project"))
                    {
                        tx.Start();
                        var projMats = ApplyMaterialsToDocument(projDoc, job.Materials, status);
                        ImportGeometryIntoDocument(projDoc, item.GeometryFilePath, job, projMats, status);
                        ApplyMaterialsToDocument(projDoc, job.Materials, status);
                        SetViewsToShaded(projDoc);
                        tx.Commit();
                    }
                }

                SaveAsOptions saveOpts = new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    Compact = true,
                    MaximumBackups = 1
                };

                projDoc.SaveAs(targetPath, saveOpts);
                status.OutputFiles.Add(targetPath);
                status.Logs.Add($"Saved Project: {targetPath}");
            }
            finally
            {
                projDoc.Close(false);
            }

            if (job.OpenInRevitAfterSave && File.Exists(targetPath))
            {
                try { uiApp.OpenAndActivateDocument(targetPath); } catch { }
            }
        }

        private static void ApplyFamilyCategory(Document famDoc, string categoryName, JobStatus status)
        {
            if (string.IsNullOrWhiteSpace(categoryName) || famDoc.OwnerFamily == null)
                return;

            BuiltInCategory bic = ResolveBuiltInCategory(categoryName);
            try
            {
                Category targetCat = famDoc.Settings.Categories.get_Item(bic);
                if (targetCat != null && famDoc.OwnerFamily.FamilyCategory?.Id != targetCat.Id)
                {
                    famDoc.OwnerFamily.FamilyCategory = targetCat;
                    status.Logs.Add($"Set Family Category to: {targetCat.Name}");
                }
            }
            catch (Exception ex)
            {
                status.Logs.Add($"Category note ({categoryName}): {ex.Message}");
            }
        }

        private static void ApplyFamilyParameters(Document famDoc, FamilyParamsSpec spec, JobStatus status)
        {
            if (spec == null || famDoc.OwnerFamily == null)
                return;

            Family owner = famDoc.OwnerFamily;
            TrySetBuiltInParam(owner, BuiltInParameter.FAMILY_WORK_PLANE_BASED, spec.WorkPlaneBased ? 1 : 0);
            TrySetBuiltInParam(owner, BuiltInParameter.FAMILY_ALWAYS_VERTICAL, spec.AlwaysVertical ? 1 : 0);
            TrySetBuiltInParam(owner, BuiltInParameter.FAMILY_ALLOW_CUT_WITH_VOIDS, spec.CutWithVoidsWhenLoaded ? 1 : 0);
            TrySetBuiltInParam(owner, BuiltInParameter.FAMILY_CAN_HOST_REBAR, spec.CanHostRebar ? 1 : 0);
            TrySetBuiltInParam(owner, BuiltInParameter.FAMILY_SHARED, spec.Shared ? 1 : 0);
            TrySetBuiltInParam(owner, BuiltInParameter.ROOM_CALCULATION_POINT, spec.RoomCalculationPoint ? 1 : 0);
        }

        private static void TrySetBuiltInParam(Element elem, BuiltInParameter bip, int value)
        {
            try
            {
                Parameter p = elem.get_Parameter(bip);
                if (p != null && !p.IsReadOnly && p.StorageType == StorageType.Integer)
                {
                    p.Set(value);
                }
            }
            catch
            {
            }
        }

        private static List<ElementId> ImportGeometryIntoDocument(
            Document doc,
            string geoFilePath,
            ConversionJob job,
            Dictionary<string, ElementId> matMap,
            JobStatus status)
        {
            string ext = Path.GetExtension(geoFilePath).ToLowerInvariant();

            // NATIVE Revit geometry (Extrusion / Free Form) - no Import Instance
            if (ext == ".m2r")
                return NativeGeometryBuilder.Build(doc, geoFilePath, matMap, status);

            View targetView = FindBestViewForImport(doc);
            if (targetView == null)
                throw new InvalidOperationException("Could not find a suitable View in the Revit document for geometry import.");

            ImportUnit unit = ResolveImportUnit(job.ImportUnits);
            ElementId importedId = ElementId.InvalidElementId;

            if (ext == ".sat")
            {
                SATImportOptions satOpts = new SATImportOptions
                {
                    Placement = ImportPlacement.Origin,
                    Unit = unit
                };
                importedId = doc.Import(geoFilePath, satOpts, targetView);
                status.Logs.Add($"Imported SAT geometry ({Path.GetFileName(geoFilePath)}).");
            }
            else if (ext == ".obj" || ext == ".stl")
            {
                ShapeImporter importer = new ShapeImporter();
                importer.InputFormat = ShapeImporterSourceFormat.Auto;
                IList<GeometryObject> geomObjs = importer.Convert(doc, geoFilePath);
                if (geomObjs != null && geomObjs.Count > 0)
                {
                    BuiltInCategory bic = doc.IsFamilyDocument
                        ? BuiltInCategory.OST_GenericModel
                        : ResolveBuiltInCategory(job.CategoryName);

                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(bic));
                    ds.SetShape(geomObjs);
                    ds.Name = Path.GetFileNameWithoutExtension(geoFilePath);
                    importedId = ds.Id;
                    status.Logs.Add($"Imported Mesh via DirectShape ({geomObjs.Count} geometry objects).");
                }
            }
            else
            {
                DWGImportOptions dwgOpts = new DWGImportOptions
                {
                    Placement = ImportPlacement.Origin,
                    Unit = unit,
                    OrientToView = false,
                    ThisViewOnly = false,
                    ColorMode = ImportColorMode.BlackAndWhite
                };
                doc.Import(geoFilePath, dwgOpts, targetView, out importedId);
                status.Logs.Add($"Imported DXF/DWG geometry with Material Layers ({Path.GetFileName(geoFilePath)}).");
            }

            if (importedId != null && importedId != ElementId.InvalidElementId)
            {
                Element impElem = doc.GetElement(importedId);
                if (impElem != null && impElem.Pinned)
                {
                    try { impElem.Pinned = false; } catch { }
                }
            }

            var result = new List<ElementId>();
            if (importedId != null && importedId != ElementId.InvalidElementId)
                result.Add(importedId);
            return result;
        }

        private static Dictionary<string, ElementId> ApplyMaterialsToDocument(Document doc, List<MaterialSpec> materials, JobStatus status)
        {
            Dictionary<string, ElementId> createdMatIds = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            if (materials == null || materials.Count == 0)
                return createdMatIds;

            foreach (MaterialSpec mSpec in materials)
            {
                string matName = string.IsNullOrWhiteSpace(mSpec.MaterialName) ? mSpec.LayerName : mSpec.MaterialName;
                if (string.IsNullOrWhiteSpace(matName))
                    continue;

                matName = SanitizeRevitName(matName);

                try
                {
                    Material revitMat = new FilteredElementCollector(doc)
                        .OfClass(typeof(Material))
                        .Cast<Material>()
                        .FirstOrDefault(m => m.Name.Equals(matName, StringComparison.OrdinalIgnoreCase));

                    ElementId matId;
                    if (revitMat == null)
                    {
                        matId = Material.Create(doc, matName);
                        revitMat = doc.GetElement(matId) as Material;
                    }
                    else
                    {
                        matId = revitMat.Id;
                    }

                    if (revitMat != null)
                    {
                        revitMat.Color = new Color(mSpec.R, mSpec.G, mSpec.B);
                        revitMat.Transparency = Math.Clamp(mSpec.Transparency, 0, 100);
                        revitMat.Smoothness = Math.Clamp(mSpec.Shininess, 0, 100);
                        revitMat.UseRenderAppearanceForShading = false;
                    }

                    if (!string.IsNullOrWhiteSpace(mSpec.LayerName))
                        createdMatIds[mSpec.LayerName] = matId;
                    createdMatIds[matName] = matId;
                }
                catch
                {
                }
            }

            try
            {
                foreach (Category cat in doc.Settings.Categories)
                {
                    AssignMaterialsToCategoryTree(doc, cat, createdMatIds);
                }
            }
            catch
            {
            }

            return createdMatIds;
        }

        private static void AssignMaterialsToCategoryTree(
            Document doc,
            Category parentCat,
            Dictionary<string, ElementId> matMap)
        {
            if (parentCat == null)
                return;

            if (matMap.TryGetValue(parentCat.Name, out ElementId directMatId))
            {
                try
                {
                    parentCat.Material = doc.GetElement(directMatId) as Material;
                    parentCat.LineColor = new Color(50, 50, 50);
                }
                catch { }
            }

            CategoryNameMap subCats = parentCat.SubCategories;
            if (subCats == null)
                return;

            foreach (Category subCat in subCats)
            {
                if (subCat == null)
                    continue;

                if (matMap.TryGetValue(subCat.Name, out ElementId subMatId))
                {
                    try
                    {
                        subCat.Material = doc.GetElement(subMatId) as Material;
                        subCat.LineColor = new Color(50, 50, 50);
                    }
                    catch { }
                }
            }
        }

        private static View FindBestViewForImport(Document doc)
        {
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .ToList();

            View planView = views.FirstOrDefault(v =>
                v.ViewType == ViewType.FloorPlan ||
                v.ViewType == ViewType.CeilingPlan ||
                v.Name.IndexOf("Ref. Level", StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.Name.IndexOf("Level 1", StringComparison.OrdinalIgnoreCase) >= 0);

            if (planView != null)
                return planView;

            View view3d = views.FirstOrDefault(v => v.ViewType == ViewType.ThreeD);
            if (view3d != null)
                return view3d;

            return views.FirstOrDefault();
        }

        private static ImportUnit ResolveImportUnit(string unitStr)
        {
            if (string.IsNullOrWhiteSpace(unitStr))
                return ImportUnit.Millimeter;

            switch (unitStr.Trim().ToLowerInvariant())
            {
                case "mm":
                case "millimeter":
                case "millimeters":
                    return ImportUnit.Millimeter;
                case "cm":
                case "centimeter":
                case "centimeters":
                    return ImportUnit.Centimeter;
                case "m":
                case "meter":
                case "meters":
                    return ImportUnit.Meter;
                case "in":
                case "inch":
                case "inches":
                    return ImportUnit.Inch;
                case "ft":
                case "foot":
                case "feet":
                    return ImportUnit.Foot;
                default:
                    return ImportUnit.Millimeter;
            }
        }

        private static BuiltInCategory ResolveBuiltInCategory(string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(categoryName) &&
                CategoryMap.TryGetValue(categoryName.Trim(), out BuiltInCategory bic))
            {
                return bic;
            }
            return BuiltInCategory.OST_GenericModel;
        }

        private static string ResolveFamilyTemplatePath(string requestedPath, string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(requestedPath) && File.Exists(requestedPath))
                return requestedPath;

            string[] versions = { "2026", "2025", "2024" };
            string[] langs = { "English", "English_I", "English-Imperial" };

            foreach (string ver in versions)
            {
                foreach (string lang in langs)
                {
                    string dir = $@"C:\ProgramData\Autodesk\RVT {ver}\Family Templates\{lang}";
                    if (!Directory.Exists(dir))
                        continue;

                    if (!string.IsNullOrWhiteSpace(requestedPath))
                    {
                        string fileName = Path.GetFileName(requestedPath);
                        string candidate = Path.Combine(dir, fileName);
                        if (File.Exists(candidate))
                            return candidate;
                    }

                    string genericCandidate = Path.Combine(dir, "Metric Generic Model.rft");
                    if (File.Exists(genericCandidate))
                        return genericCandidate;

                    string anyRft = Directory.GetFiles(dir, "*.rft", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(anyRft))
                        return anyRft;
                }
            }

            throw new FileNotFoundException($"Could not locate Revit Family Template (.rft). Requested: {requestedPath}");
        }

        private static string ResolveProjectTemplatePath(string requestedPath)
        {
            if (!string.IsNullOrWhiteSpace(requestedPath) && File.Exists(requestedPath))
                return requestedPath;

            string[] candidates =
            {
                @"C:\ProgramData\Autodesk\RVT 2026\Templates\English\DefaultMetric.rte",
                @"C:\ProgramData\Autodesk\RVT 2026\Templates\English\Default-Multi-Discipline_Metric.rte",
                @"C:\ProgramData\Autodesk\RVT 2026\Templates\English\Construction-DefaultMetric.rte",
                @"C:\ProgramData\Autodesk\RVT 2025\Templates\English\DefaultMetric.rte"
            };

            return candidates.FirstOrDefault(File.Exists) ?? "";
        }

        private static string SanitizeRevitName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Max_Material";

            char[] forbidden = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~' };
            foreach (char c in forbidden)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Object";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        private static void WriteStatus(string path, JobStatus status)
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(status, opts));
            }
            catch
            {
            }
        }
    }

    #endregion
}
