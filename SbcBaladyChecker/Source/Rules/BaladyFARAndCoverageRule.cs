using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public enum FarSeverity { Info, Warning, Error }

    public sealed record FarFinding(FarSeverity Severity, string Code, string MessageAr);

    public sealed class FarCoverageSettings
    {
        public double? PlotAreaSqMeters { get; set; }
        public double? MaxCoverageRatio { get; set; } = 0.60;
        public double? MaxFar { get; set; }
        public string GroundLevelName { get; set; }
        public bool IncludeUpperProjections { get; set; } = false;
        public double PlotMismatchTolerance { get; set; } = 0.02;
        public double BasementToleranceFeet { get; set; } = 0.5;
        public List<string> ExcludeKeywords { get; set; } = new()
        {
            "basement", "قبو", "بدروم", "roof", "سطح", "parapet", "مظلة", "مظلات", "canopy", "carport"
        };
    }

    public sealed class FarCoverageResult
    {
        public double PlotAreaM2 { get; set; }
        public double FootprintM2 { get; set; }
        public double TotalFloorAreaM2 { get; set; }
        public double CoverageRatio => PlotAreaM2 > 0 ? FootprintM2 / PlotAreaM2 : 0;
        public double Far => PlotAreaM2 > 0 ? TotalFloorAreaM2 / PlotAreaM2 : 0;
        public string PlotSource { get; set; } = "N/A";
        public string FootprintSource { get; set; } = "N/A";
        public string FarSource { get; set; } = "N/A";
    }

    /// <summary>
    /// BaladyFARAndCoverageRule — Developed jointly by Claude (Logic Architecture) and Antigravity (System Integration).
    /// Audits Ground Coverage Ratio (GCR) and Floor Area Ratio (FAR) according to Saudi Balady municipal codes.
    /// </summary>
    public class BaladyFARAndCoverageRule : ISbcRule
    {
        public string RuleId => "BALADY-FAR-COV";
        public string Name => "نسبة البناء بالأرضي ومعامل البناء الإجمالي (Coverage & FAR)";
        public string SbcReference => "لائحة البناء البلدية - منصة بلدي & SBC 201 Section 503";
        public SbcCategory Category => SbcCategory.Balady_Zoning;

        private const double LoopTol = 1e-3; // feet

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var auditItems = new List<AuditResultItem>();

            var farSettings = new FarCoverageSettings
            {
                PlotAreaSqMeters = settings.PlotAreaSqMeters,
                MaxCoverageRatio = settings.Typology switch
                {
                    BuildingTypology.ResidentialVilla => 0.70, // فلل سكنية حتى 70% حسب التحديثات
                    BuildingTypology.ResidentialApartments => 0.65,
                    BuildingTypology.CommercialRetail => 0.60,
                    BuildingTypology.CommercialOffice => 0.60,
                    BuildingTypology.MixedUse => 0.65,
                    _ => 0.60
                },
                MaxFar = settings.Typology switch
                {
                    BuildingTypology.ResidentialVilla => 2.4,
                    BuildingTypology.ResidentialApartments => 3.0,
                    BuildingTypology.CommercialRetail => 2.0,
                    BuildingTypology.CommercialOffice => 3.5,
                    BuildingTypology.MixedUse => 3.0,
                    _ => 2.5
                }
            };

            var (result, findings) = Execute(doc, farSettings);

            foreach (var f in findings)
            {
                var severity = f.Severity switch
                {
                    FarSeverity.Error => RuleSeverity.Violation,
                    FarSeverity.Warning => RuleSeverity.Warning,
                    _ => RuleSeverity.Compliant
                };

                string title = f.Code switch
                {
                    "COV_EXCEEDED" => "تجاوز الحد الأقصى لنسبة التغطية بالأرضي",
                    "COV_OK" => "نسبة البناء بالأرضي مطابقة للاشتراطات البلدية",
                    "FAR_EXCEEDED" => "تجاوز معامل البناء الإجمالي (FAR)",
                    "FAR_OK" => "معامل البناء الإجمالي (FAR) مطابق للاشتراطات",
                    "FAR_PLOT_MISMATCH" => "فارق بين مساحة الصك وخط الملكية في الموديل",
                    "FAR_PLOT_AUTO" => "تم اعتماد مساحة الأرض تلقائياً من خط الملكية",
                    "FAR_PLOT_MISSING" => "مساحة الأرض غير محددة",
                    "FAR_FOOTPRINT_EMPTY" => "تعذر استخراج الأثر الأرضي للمبنى",
                    "FAR_FOOTPRINT_SKIPPED" => "تخطي بعض العناصر غير الهندسية في الأثر الأرضي",
                    "FAR_FLOORS_FALLBACK" => "حساب المساحة الإجمالية من مسطحات البلاطات",
                    _ => "فحص اشتراطات البناء البلدية"
                };

                auditItems.Add(new AuditResultItem
                {
                    RuleCode = f.Code,
                    Title = title,
                    Description = f.MessageAr,
                    Category = SbcCategory.Balady_Zoning,
                    Severity = severity,
                    ElementId = ElementId.InvalidElementId,
                    ElementName = "موقع المشروع وعناصر المبنى",
                    ElementCategory = "اشتراطات بلدي",
                    CurrentValue = $"نسبة التغطية: {(result.CoverageRatio * 100):F1}% | FAR: {result.Far:F2}",
                    AllowedRange = $"أقصى تغطية: {(farSettings.MaxCoverageRatio.GetValueOrDefault() * 100):F0}% | أقصى FAR: {farSettings.MaxFar:F2}",
                    Recommendation = severity == RuleSeverity.Violation 
                        ? "يجب تعديل مسطح الدور الأرضي أو تقليص مساحات الأدوار المتكررة للالتزام بلائحة بلدي."
                        : "المشروع مطابق للاشتراطات البلدية المعتمدة."
                });
            }

            return auditItems;
        }

        public (FarCoverageResult Result, List<FarFinding> Findings) Execute(
            Document doc, FarCoverageSettings s)
        {
            var r = new FarCoverageResult();
            var f = new List<FarFinding>();

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
            if (levels.Count == 0)
            {
                f.Add(new(FarSeverity.Error, "FAR_NO_LEVELS", "لا توجد مناسيب (Levels) في النموذج؛ تعذّر الحساب."));
                return (r, f);
            }

            Level ground = ResolveGroundLevel(levels, s);
            double groundElev = ground.Elevation;
            var aboveGrade = levels.Where(l => !IsExcludedLevel(l, groundElev, s)).ToList();

            // ---------- 1) Plot area ----------
            double? autoFt2 = TryDetectPlotAreaFt2(doc);
            if (s.PlotAreaSqMeters is > 0)
            {
                r.PlotAreaM2 = s.PlotAreaSqMeters.Value;
                r.PlotSource = "AuditSettings (مساحة الصك)";
                if (autoFt2 is > 0)
                {
                    double autoM2 = ToM2(autoFt2.Value);
                    double diff = Math.Abs(autoM2 - r.PlotAreaM2) / r.PlotAreaM2;
                    if (diff > s.PlotMismatchTolerance)
                        f.Add(new(FarSeverity.Warning, "FAR_PLOT_MISMATCH",
                            $"مساحة الأرض المُدخلة {Fmt(r.PlotAreaM2)} م² تختلف عن خط الملكية في النموذج {Fmt(autoM2)} م² بنسبة {Fmt(diff * 100)}%."));
                }
            }
            else if (autoFt2 is > 0)
            {
                r.PlotAreaM2 = ToM2(autoFt2.Value);
                r.PlotSource = "خط الملكية في النموذج (غير مطابق للصك)";
                f.Add(new(FarSeverity.Warning, "FAR_PLOT_AUTO",
                    "اعتُمدت مساحة الأرض من خط الملكية في النموذج؛ أدخل مساحة الصك في الإعدادات لنتيجة نظامية."));
            }
            else
            {
                f.Add(new(FarSeverity.Error, "FAR_PLOT_MISSING",
                    "مساحة الأرض غير متوفرة: أدخلها في الإعدادات أو أنشئ خط ملكية مغلقاً."));
                return (r, f);
            }

            // ---------- 2) Ground footprint ----------
            var fpLevels = s.IncludeUpperProjections
                ? aboveGrade
                : aboveGrade.Where(l => l.Id == ground.Id).ToList();
            var fpIds = new HashSet<ElementId>(fpLevels.Select(l => l.Id));

            var fpElements = new List<Element>();
            foreach (var fl in new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>())
            {
                if (!fpIds.Contains(fl.LevelId)) continue;
                if (NameMatches(doc.GetElement(fl.GetTypeId())?.Name, s.ExcludeKeywords)) continue;
                fpElements.Add(fl);
            }
            foreach (var w in new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>())
            {
                if (w.WallType?.Function != WallFunction.Exterior) continue;
                var baseId = w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId();
                if (baseId == null || !fpIds.Contains(baseId)) continue;
                fpElements.Add(w);
            }

            double fpFt2 = ProjectedUnionAreaFt2(fpElements, out int failed);
            r.FootprintM2 = ToM2(fpFt2);
            r.FootprintSource = $"اتحاد هندسي لبلاطات وجدران خارجية ({fpElements.Count} عنصر) على منسوب {ground.Name}";
            if (fpFt2 <= 0)
                f.Add(new(FarSeverity.Warning, "FAR_FOOTPRINT_EMPTY",
                    "تعذّر استخراج الأثر الأرضي: لا بلاطات أو جدران خارجية صالحة على المنسوب الأرضي."));
            if (failed > 0)
                f.Add(new(FarSeverity.Warning, "FAR_FOOTPRINT_SKIPPED",
                    $"تم تجاوز {failed} عنصراً/وجهاً عند حساب الأثر الأرضي (هندسة غير مدعومة أو فشل الاتحاد)؛ راجع النتيجة يدوياً."));

            // ---------- 3) Total floor area (FAR numerator) ----------
            double farFt2 = TryGrossBuildingAreaFt2(doc, aboveGrade, s, out int areaCount);
            if (areaCount > 0)
            {
                r.FarSource = $"مناطق Gross Building ({areaCount} منطقة)";
            }
            else
            {
                farFt2 = FloorsAreaFt2(doc, aboveGrade, s);
                r.FarSource = "مساحات البلاطات (بديل)";
                f.Add(new(FarSeverity.Warning, "FAR_FLOORS_FALLBACK",
                    "لا توجد مناطق Gross Building؛ حُسب معامل البناء من البلاطات، وفتحات السلالم والمصاعد مخصومة فقد تكون النتيجة أقل من الفعلية."));
            }
            r.TotalFloorAreaM2 = ToM2(farFt2);

            // ---------- 4) Evaluation ----------
            if (s.MaxCoverageRatio is > 0)
            {
                bool bad = r.CoverageRatio > s.MaxCoverageRatio.Value + 1e-9;
                f.Add(new(bad ? FarSeverity.Error : FarSeverity.Info, bad ? "COV_EXCEEDED" : "COV_OK",
                    $"نسبة البناء بالأرضي {Fmt(r.CoverageRatio * 100)}% " +
                    $"({Fmt(r.FootprintM2)} م² من {Fmt(r.PlotAreaM2)} م²) " +
                    (bad ? $"تتجاوز الحد {Fmt(s.MaxCoverageRatio.Value * 100)}%." : $"ضمن الحد {Fmt(s.MaxCoverageRatio.Value * 100)}%.")));
            }

            if (s.MaxFar is > 0)
            {
                bool bad = r.Far > s.MaxFar.Value + 1e-9;
                f.Add(new(bad ? FarSeverity.Error : FarSeverity.Info, bad ? "FAR_EXCEEDED" : "FAR_OK",
                    $"معامل البناء {Fmt(r.Far)} ({Fmt(r.TotalFloorAreaM2)} م² / {Fmt(r.PlotAreaM2)} م²) " +
                    (bad ? $"يتجاوز الحد {Fmt(s.MaxFar.Value)}." : $"ضمن الحد {Fmt(s.MaxFar.Value)}.")));
            }
            else
            {
                f.Add(new(FarSeverity.Info, "FAR_REPORT_ONLY",
                    $"معامل البناء المحسوب {Fmt(r.Far)}؛ لم يُحدَّد حد أقصى في الإعدادات (يتبع تقسيم المنطقة)."));
            }

            return (r, f);
        }

        // ================= Plot area (best effort; verify locally) =================
        private static double? TryDetectPlotAreaFt2(Document doc)
        {
            var lines = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_SiteProperty)
                .WhereElementIsNotElementType().ToList();
            if (lines.Count != 1) return null; // none or ambiguous (multiple plots)

            var curves = new List<Curve>();
            var ge = lines[0].get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine });
            if (ge == null) return null;
            CollectCurves(ge, curves);
            return LoopAreaFt2(curves);
        }

        private static void CollectCurves(GeometryElement ge, List<Curve> acc)
        {
            foreach (var go in ge)
            {
                if (go is Curve c) acc.Add(c);
                else if (go is GeometryInstance gi) CollectCurves(gi.GetInstanceGeometry(), acc);
            }
        }

        private static double? LoopAreaFt2(List<Curve> curves)
        {
            if (curves.Count < 3) return null;
            var rem = new List<Curve>(curves);
            var first = rem[0]; rem.RemoveAt(0);
            var pts = new List<XYZ>(first.Tessellate());
            XYZ start = first.GetEndPoint(0);
            XYZ end = first.GetEndPoint(1);

            while (rem.Count > 0)
            {
                int idx = rem.FindIndex(c => Near(c.GetEndPoint(0), end) || Near(c.GetEndPoint(1), end));
                if (idx < 0) return null; // open or multi-loop
                var c = rem[idx]; rem.RemoveAt(idx);
                var t = c.Tessellate().ToList();
                if (!Near(c.GetEndPoint(0), end)) t.Reverse();
                pts.AddRange(t.Skip(1));
                end = t[^1];
            }
            if (!Near(end, start)) return null;

            double a = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i]; var q = pts[(i + 1) % pts.Count];
                a += p.X * q.Y - q.X * p.Y;
            }
            return Math.Abs(a) / 2.0;
        }

        private static bool Near(XYZ a, XYZ b) => a.DistanceTo(b) < LoopTol;

        // ================= Footprint via projected Boolean union =================
        private static double ProjectedUnionAreaFt2(IEnumerable<Element> elements, out int failed)
        {
            failed = 0;
            Solid acc = null;
            var opt = new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false };

            foreach (var el in elements)
            {
                var ge = el.get_Geometry(opt);
                if (ge == null) { failed++; continue; }

                foreach (var solid in GetSolids(ge))
                foreach (Face face in solid.Faces)
                {
                    if (face is not PlanarFace pf) continue;
                    if (pf.FaceNormal.DistanceTo(-XYZ.BasisZ) > 1e-6) continue; // bottom faces only

                    try
                    {
                        var t = Transform.CreateTranslation(new XYZ(0, 0, -pf.Origin.Z));
                        var loops = pf.GetEdgesAsCurveLoops()
                            .Select(l => CurveLoop.CreateViaTransform(l, t)).ToList();
                        var ext = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, 1.0);

                        acc = acc == null
                            ? ext
                            : BooleanOperationsUtils.ExecuteBooleanOperation(acc, ext, BooleanOperationsType.Union);
                    }
                    catch
                    {
                        failed++;
                    }
                }
            }

            if (acc == null) return 0;
            double area = 0;
            foreach (Face face in acc.Faces)
                if (face is PlanarFace pf && pf.FaceNormal.DistanceTo(-XYZ.BasisZ) < 1e-6)
                    area += pf.Area;
            return area;
        }

        private static IEnumerable<Solid> GetSolids(GeometryElement ge)
        {
            foreach (var go in ge)
            {
                if (go is Solid s && s.Volume > 1e-9) yield return s;
                else if (go is GeometryInstance gi)
                    foreach (var inner in GetSolids(gi.GetInstanceGeometry())) yield return inner;
            }
        }

        // ================= FAR numerator =================
        private static double TryGrossBuildingAreaFt2(
            Document doc, List<Level> aboveGrade, FarCoverageSettings s, out int count)
        {
            var ok = new HashSet<ElementId>(aboveGrade.Select(l => l.Id));
            double total = 0; count = 0;
            foreach (var a in new FilteredElementCollector(doc)
                         .OfCategory(BuiltInCategory.OST_Areas).WhereElementIsNotElementType().Cast<Area>())
            {
                if (a.Area <= 0 || !ok.Contains(a.LevelId)) continue;
                total += a.Area; count++;
            }
            return total;
        }

        private static double FloorsAreaFt2(Document doc, List<Level> aboveGrade, FarCoverageSettings s)
        {
            var ok = new HashSet<ElementId>(aboveGrade.Select(l => l.Id));
            double total = 0;
            foreach (var fl in new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>())
            {
                if (!ok.Contains(fl.LevelId)) continue;
                if (NameMatches(doc.GetElement(fl.GetTypeId())?.Name, s.ExcludeKeywords)) continue;
                total += fl.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;
            }
            return total;
        }

        // ================= Helpers =================
        private static Level ResolveGroundLevel(List<Level> levels, FarCoverageSettings s)
        {
            if (!string.IsNullOrWhiteSpace(s.GroundLevelName))
            {
                var named = levels.FirstOrDefault(l =>
                    string.Equals(l.Name, s.GroundLevelName, StringComparison.OrdinalIgnoreCase));
                if (named != null) return named;
            }
            return levels.OrderBy(l => Math.Abs(l.Elevation)).First();
        }

        private static bool IsExcludedLevel(Level l, double groundElev, FarCoverageSettings s) =>
            l.Elevation < groundElev - s.BasementToleranceFeet || NameMatches(l.Name, s.ExcludeKeywords);

        private static bool NameMatches(string name, IEnumerable<string> keywords) =>
            !string.IsNullOrEmpty(name) &&
            keywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        private static double ToM2(double ft2) =>
            UnitUtils.ConvertFromInternalUnits(ft2, UnitTypeId.SquareMeters);

        private static string Fmt(double v) => v.ToString("F2", CultureInfo.InvariantCulture);
    }
}
