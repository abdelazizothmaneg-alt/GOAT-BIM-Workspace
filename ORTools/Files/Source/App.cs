using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using ORTools.UI;

namespace ORTools
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            string tabName = "GOAT BIM";
            try { app.CreateRibbonTab(tabName); } catch { }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // =========================================================================
            // 1. GOAT Setup Panel (LEVELS, GRIDS)
            // =========================================================================
            RibbonPanel setupPanel = app.CreateRibbonPanel(tabName, "GOAT Setup");

            AddRibbonButton(
                setupPanel,
                "GoatLevelBtn",
                "LEVELS",
                assemblyPath,
                "ORTools.Commands.OrLevelCommand",
                "Generate and manage multi-story levels and floor plans parametrically.",
                "or_level",
                "LEVELS");

            AddRibbonButton(
                setupPanel,
                "GoatGridBtn",
                "GRIDS",
                assemblyPath,
                "ORTools.Commands.OrGridCommand",
                "Generate horizontal, vertical, rotated and radial grids with automatic clash detection.",
                "or_grid",
                "GRIDS");

            // =========================================================================
            // 2. GOAT Data Panel (PARAMETER, SHEETLINK, RENUMBER, BOQ)
            // =========================================================================
            RibbonPanel dataPanel = app.CreateRibbonPanel(tabName, "GOAT Data");

            AddRibbonButton(
                dataPanel,
                "GoatParamBtn",
                "PARAMETER",
                assemblyPath,
                "ORTools.Commands.OrParameterCommand",
                "Bulk Instance & Type Parameter Editor, Color Splasher, Spatial Room Injector & Formula Combiner.",
                "or_parameter",
                "PARAMETER");

            AddRibbonButton(
                dataPanel,
                "GoatSheetLinkBtn",
                "SHEETLINK",
                assemblyPath,
                "ORTools.Commands.OrSheetLinkCommand",
                "Bidirectional Excel (.XLSX) & Live Spreadsheet Parameter Editor with Types Sheet & Element Creation.",
                "or_sheetlink",
                "SHEETLINK");

            AddRibbonButton(
                dataPanel,
                "GoatRenumberBtn",
                "RENUMBER",
                assemblyPath,
                "ORTools.Commands.GoatRenumberCommand",
                "Smart Renumbering for Rooms, Doors by Room (101A/101B), Windows, Grids & Viewports with Auto-Swap.",
                null,
                "RENUMBER");

            AddRibbonButton(
                dataPanel,
                "GoatBoqBtn",
                "BOQ",
                assemblyPath,
                "ORTools.Commands.GoatBoqCommand",
                "Automated Model Quantity Takeoff (QTO) & BOQ Excel Builder.",
                null,
                "BOQ");

            // =========================================================================
            // 3. GOAT Finishes Panel (FINISHES, ARCH)
            // =========================================================================
            RibbonPanel finishesPanel = app.CreateRibbonPanel(tabName, "GOAT Finishes");

            AddRibbonButton(
                finishesPanel,
                "GoatFinishesBtn",
                "FINISHES",
                assemblyPath,
                "ORTools.Commands.GoatFinishesCommand",
                "Automated Room Finishes & Data Engine: Parametric Walls, Skirting, Floors, Ceilings & Excel QTO.",
                null,
                "FINISHES");

            AddRibbonButton(
                finishesPanel,
                "GoatArchBtn",
                "ARCH",
                assemblyPath,
                "ORTools.Commands.GoatArchCommand",
                "Parametric Architectural Arch Generator (7 Profiles + Wall Cut) & Wardrobe/Joinery Builder.",
                null,
                "ARCH");

            // =========================================================================
            // 4. GOAT Ceiling Panel (CEILING)
            // =========================================================================
            RibbonPanel ceilingPanel = app.CreateRibbonPanel(tabName, "GOAT Ceiling");

            AddRibbonButton(
                ceilingPanel,
                "GoatCeilingBtn",
                "CEILING",
                assemblyPath,
                "ORTools.Commands.GoatCeilingCommand",
                "Automated Ceiling Generator & 600x600 Spatial Grid Array for Lighting/MEP Fixtures with Clash Avoidance.",
                null,
                "CEILING");

            // =========================================================================
            // 5. GOAT Documentation Panel (D VIEW, W VIEW, ROOM, DIM, ANNOTATE)
            // =========================================================================
            RibbonPanel docPanel = app.CreateRibbonPanel(tabName, "GOAT Documentation");

            AddRibbonButton(
                docPanel,
                "GoatDoorViewBtn",
                "D VIEW",
                assemblyPath,
                "ORTools.Commands.OrDoorViewCommand",
                "Automated 4-View Door Detail Sheet Generator (Plan, Section, Elevation, ISO & Schedule).",
                "d_view",
                "D_VIEW");

            AddRibbonButton(
                docPanel,
                "GoatWindowViewBtn",
                "W VIEW",
                assemblyPath,
                "ORTools.Commands.OrWindowViewCommand",
                "Automated 4-View Window Detail Sheet Generator (Plan, Section, Elevation, ISO & Schedule).",
                "w_view",
                "W_VIEW");

            AddRibbonButton(
                docPanel,
                "GoatRoomBtn",
                "ROOM",
                assemblyPath,
                "ORTools.Commands.GoatRoomCommand",
                "Glyph-Style Room Documentation Bundle (Enlarged Plan, RCP, 4 Elevations, 3D Section Box, Auto-Dim, Auto-Tag & Sheet Packing).",
                null,
                "ROOM");

            AddRibbonButton(
                docPanel,
                "GoatDimBtn",
                "DIM",
                assemblyPath,
                "ORTools.Commands.GoatDimCommand",
                "Automated Interior Room, Exterior Building & Elevation Dimensioning Engine.",
                null,
                "DIM");

            AddRibbonButton(
                docPanel,
                "GoatAnnotateBtn",
                "ANNOTATE",
                assemblyPath,
                "ORTools.Commands.GoatAnnotateCommand",
                "Smart Multi-Category Tagging Engine with Automatic Tag Collision Avoidance.",
                null,
                "ANNOTATE");

            // =========================================================================
            // 6. GOAT Delivery Panel (ALIGNER, SHEETS)
            // =========================================================================
            RibbonPanel deliveryPanel = app.CreateRibbonPanel(tabName, "GOAT Delivery");

            AddRibbonButton(
                deliveryPanel,
                "GoatAlignerBtn",
                "ALIGNER",
                assemblyPath,
                "ORTools.Commands.GoatAlignerCommand",
                "Auto-Pack Views on Active Sheet, Cross-Sheet Viewport Alignment & Duplicate Sheets with Views.",
                null,
                "ALIGNER");

            AddRibbonButton(
                deliveryPanel,
                "GoatSheetsBtn",
                "SHEETS",
                assemblyPath,
                "ORTools.Commands.OrSheetsCommand",
                "Unlimited Multi-Format Batch Exporter (PDF, DWG, DGN, DWF, NWC, IFC, IMG & ISO 19650 DocRegister).",
                "or_sheets",
                "SHEETS");

            return Result.Succeeded;
        }

        private void AddRibbonButton(
            RibbonPanel panel,
            string internalName,
            string buttonText,
            string assemblyPath,
            string className,
            string toolTip,
            string resourcePrefix,
            string fallbackGlyphKey)
        {
            PushButtonData btnData = new PushButtonData(internalName, buttonText, assemblyPath, className);
            PushButton btn = panel.AddItem(btnData) as PushButton;
            if (btn == null) return;

            btn.ToolTip = toolTip;

            bool loadedFromResource = false;
            if (!string.IsNullOrEmpty(resourcePrefix))
            {
                try
                {
                    btn.LargeImage = new BitmapImage(new Uri($"pack://application:,,,/ORTools;component/Resources/{resourcePrefix}_32.png", UriKind.Absolute));
                    btn.Image = new BitmapImage(new Uri($"pack://application:,,,/ORTools;component/Resources/{resourcePrefix}_16.png", UriKind.Absolute));
                    loadedFromResource = true;
                }
                catch
                {
                    loadedFromResource = false;
                }
            }

            if (!loadedFromResource)
            {
                try
                {
                    btn.LargeImage = GoatIconFactory.CreateRibbonIcon(fallbackGlyphKey, 32);
                    btn.Image = GoatIconFactory.CreateRibbonIcon(fallbackGlyphKey, 16);
                }
                catch { }
            }
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }
    }
}
