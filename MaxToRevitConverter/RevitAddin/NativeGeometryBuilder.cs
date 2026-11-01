using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace MaxToRevitBridge
{
    /// <summary>
    /// One 3ds Max object read from the .m2r mesh file (vertices in millimeters, 1-based triangle indices).
    /// </summary>
    public class M2RObject
    {
        public string Name = "MaxObject";
        public string Layer = "";
        public List<XYZ> VertsMm = new List<XYZ>();
        public List<int[]> Faces = new List<int[]>();
    }

    /// <summary>
    /// Swallows Revit warnings during conversion transactions so they do not block the batch.
    /// </summary>
    public class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (FailureMessageAccessor f in accessor.GetFailureMessages())
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                    accessor.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }

    /// <summary>
    /// Builds NATIVE Revit family geometry (like the Create tab) from 3ds Max meshes:
    ///   1. Prismatic objects (boxes, frames, profiles, panels) -> real Extrusion with a sketch profile.
    ///   2. Flat planar objects (glass panes)                   -> thin Extrusion (5 mm).
    ///   3. Any other closed mesh                               -> Free Form solid (FreeFormElement).
    ///   4. Open / invalid meshes                               -> DirectShape fallback.
    /// Each element gets its Revit Material, linked to a Family Type material parameter.
    /// </summary>
    public static class NativeGeometryBuilder
    {
        private const double MmToFt = 1.0 / 304.8;
        private const double LevelTolFt = 0.05 * MmToFt;      // 0.05 mm
        private const double PlaneThicknessMm = 5.0;

        public static List<M2RObject> Parse(string path)
        {
            var list = new List<M2RObject>();
            M2RObject cur = null;
            var ci = CultureInfo.InvariantCulture;

            foreach (string raw in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string line = raw.Trim();
                char tag = line[0];

                if (tag == 'O' && line.Length > 2)
                {
                    cur = new M2RObject();
                    string body = line.Substring(2);
                    int bar = body.LastIndexOf('|');
                    cur.Name = bar >= 0 ? body.Substring(0, bar) : body;
                    cur.Layer = bar >= 0 ? body.Substring(bar + 1) : "";
                    list.Add(cur);
                }
                else if (tag == 'V' && cur != null)
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        cur.VertsMm.Add(new XYZ(
                            double.Parse(p[1], NumberStyles.Float, ci),
                            double.Parse(p[2], NumberStyles.Float, ci),
                            double.Parse(p[3], NumberStyles.Float, ci)));
                    }
                }
                else if (tag == 'F' && cur != null)
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        cur.Faces.Add(new[]
                        {
                            int.Parse(p[1], ci) - 1,
                            int.Parse(p[2], ci) - 1,
                            int.Parse(p[3], ci) - 1
                        });
                    }
                }
            }
            return list;
        }

        public static List<ElementId> Build(
            Document doc,
            string path,
            Dictionary<string, ElementId> matMap,
            JobStatus status)
        {
            var ids = new List<ElementId>();
            List<M2RObject> objects = Parse(path);
            int nExt = 0, nFree = 0, nDs = 0, nFail = 0;

            foreach (M2RObject obj in objects)
            {
                Weld(obj, out List<XYZ> pts, out List<int[]> tris);
                if (tris.Count == 0)
                    continue;

                ElementId matId = ResolveMaterial(obj, matMap);
                ElementId id = ElementId.InvalidElementId;
                string kind = "";

                if (doc.IsFamilyDocument)
                {
                    id = TryExtrusion(doc, pts, tris);
                    if (IsValid(id)) { kind = "Extrusion"; nExt++; }

                    if (!IsValid(id))
                    {
                        id = TryFreeForm(doc, pts, tris, matId);
                        if (IsValid(id)) { kind = "FreeForm"; nFree++; }
                    }
                }

                if (!IsValid(id))
                {
                    id = TryDirectShape(doc, pts, tris, matId, obj.Name);
                    if (IsValid(id)) { kind = "DirectShape"; nDs++; }
                }

                if (!IsValid(id))
                {
                    nFail++;
                    status.Logs.Add($"Could not build native geometry for '{obj.Name}'.");
                    continue;
                }

                if (kind != "DirectShape")
                    AssignMaterial(doc, id, matId, obj.Layer);

                ids.Add(id);
            }

            status.Logs.Add($"Native geometry: {nExt} Extrusion(s), {nFree} Free Form solid(s), {nDs} DirectShape(s), {nFail} failed.");
            return ids;
        }

        #region Mesh preparation

        private static bool IsValid(ElementId id) => id != null && id != ElementId.InvalidElementId;

        private static void Weld(M2RObject obj, out List<XYZ> ptsFt, out List<int[]> tris)
        {
            ptsFt = new List<XYZ>();
            tris = new List<int[]>();
            var map = new Dictionary<(long, long, long), int>();
            var remap = new int[obj.VertsMm.Count];

            for (int i = 0; i < obj.VertsMm.Count; i++)
            {
                XYZ v = obj.VertsMm[i];
                var key = ((long)Math.Round(v.X * 100), (long)Math.Round(v.Y * 100), (long)Math.Round(v.Z * 100));
                if (!map.TryGetValue(key, out int idx))
                {
                    idx = ptsFt.Count;
                    ptsFt.Add(new XYZ(v.X * MmToFt, v.Y * MmToFt, v.Z * MmToFt));
                    map[key] = idx;
                }
                remap[i] = idx;
            }

            foreach (int[] f in obj.Faces)
            {
                if (f.Any(k => k < 0 || k >= remap.Length)) continue;
                int a = remap[f[0]], b = remap[f[1]], c = remap[f[2]];
                if (a == b || b == c || a == c) continue;
                XYZ n = (ptsFt[b] - ptsFt[a]).CrossProduct(ptsFt[c] - ptsFt[a]);
                if (n.GetLength() < 1e-10) continue;
                tris.Add(new[] { a, b, c });
            }
        }

        /// <summary>
        /// Returns closed boundary loops (vertex index lists) of a set of triangles, following triangle winding.
        /// </summary>
        private static List<List<int>> BoundaryLoops(IEnumerable<int[]> triangles)
        {
            var directed = new HashSet<(int, int)>();
            foreach (int[] t in triangles)
            {
                directed.Add((t[0], t[1]));
                directed.Add((t[1], t[2]));
                directed.Add((t[2], t[0]));
            }

            var outgoing = new Dictionary<int, List<int>>();
            foreach (var e in directed)
            {
                if (directed.Contains((e.Item2, e.Item1))) continue; // interior edge
                if (!outgoing.TryGetValue(e.Item1, out var lst))
                    outgoing[e.Item1] = lst = new List<int>();
                lst.Add(e.Item2);
            }

            var loops = new List<List<int>>();
            while (true)
            {
                int start = -1;
                foreach (var kv in outgoing)
                {
                    if (kv.Value.Count > 0) { start = kv.Key; break; }
                }
                if (start < 0) break;

                var loop = new List<int> { start };
                int curr = start;
                int guard = 0;
                while (guard++ < 1000000)
                {
                    if (!outgoing.TryGetValue(curr, out var nexts) || nexts.Count == 0)
                    {
                        loop = null; // open chain - invalid
                        break;
                    }
                    int next = nexts[nexts.Count - 1];
                    nexts.RemoveAt(nexts.Count - 1);
                    if (next == start) break;
                    loop.Add(next);
                    curr = next;
                }

                if (loop != null && loop.Count >= 3)
                    loops.Add(loop);
            }
            return loops;
        }

        private static List<XYZ> SimplifyLoop(List<XYZ> pts, double shortTol)
        {
            var list = new List<XYZ>(pts);
            bool changed = true;
            while (changed && list.Count >= 3)
            {
                changed = false;
                for (int i = 0; i < list.Count && list.Count >= 3; i++)
                {
                    XYZ prev = list[(i - 1 + list.Count) % list.Count];
                    XYZ cur = list[i];
                    XYZ next = list[(i + 1) % list.Count];

                    if (cur.DistanceTo(prev) < shortTol)
                    {
                        list.RemoveAt(i); changed = true; break;
                    }

                    XYZ d1 = cur - prev, d2 = next - cur;
                    if (d1.GetLength() > 1e-9 && d2.GetLength() > 1e-9 &&
                        d1.Normalize().CrossProduct(d2.Normalize()).GetLength() < 1e-5 &&
                        d1.DotProduct(d2) > 0)
                    {
                        list.RemoveAt(i); changed = true; break;
                    }
                }
            }
            return list;
        }

        private static double LoopArea(List<XYZ> loop, XYZ normal)
        {
            XYZ sum = XYZ.Zero;
            for (int i = 0; i < loop.Count; i++)
                sum += loop[i].CrossProduct(loop[(i + 1) % loop.Count]);
            return Math.Abs(sum.DotProduct(normal)) * 0.5;
        }

        #endregion

        #region 1-2. Extrusion (prismatic & planar objects)

        private static ElementId TryExtrusion(Document doc, List<XYZ> pts, List<int[]> tris)
        {
            XYZ[] axes = { XYZ.BasisZ, XYZ.BasisX, XYZ.BasisY };
            double shortTol = doc.Application.ShortCurveTolerance * 1.05;

            foreach (XYZ axis in axes)
            {
                double[] vals = pts.Select(p => p.DotProduct(axis)).ToArray();
                double min = vals.Min(), max = vals.Max();
                bool planar = (max - min) < LevelTolFt;

                if (!planar && vals.Any(v => Math.Abs(v - min) > LevelTolFt && Math.Abs(v - max) > LevelTolFt))
                    continue; // vertices on more than 2 levels -> not a prism along this axis

                var capTris = new List<int[]>();
                bool ok = true;
                foreach (int[] t in tris)
                {
                    bool allMin = t.All(k => Math.Abs(vals[k] - min) < LevelTolFt);
                    bool allMax = t.All(k => Math.Abs(vals[k] - max) < LevelTolFt);
                    if (allMin) { capTris.Add(t); continue; }
                    if (allMax) continue;

                    // Side face must be parallel to the extrusion axis
                    XYZ n = (pts[t[1]] - pts[t[0]]).CrossProduct(pts[t[2]] - pts[t[0]]).Normalize();
                    if (Math.Abs(n.DotProduct(axis)) > 1e-3) { ok = false; break; }
                }
                if (!ok || capTris.Count == 0) continue;

                List<List<int>> loops = BoundaryLoops(capTris);
                if (loops.Count == 0) continue;

                double baseLevel = planar ? min - (PlaneThicknessMm * 0.5 * MmToFt) : min;
                double depth = planar ? PlaneThicknessMm * MmToFt : (max - min);

                CurveArrArray profile = new CurveArrArray();
                foreach (List<int> loopIdx in loops)
                {
                    List<XYZ> loopPts = loopIdx
                        .Select(k => pts[k] - axis * (pts[k].DotProduct(axis) - baseLevel))
                        .ToList();
                    loopPts = SimplifyLoop(loopPts, shortTol);
                    if (loopPts.Count < 3) continue;

                    CurveArray ca = new CurveArray();
                    for (int i = 0; i < loopPts.Count; i++)
                        ca.Append(Line.CreateBound(loopPts[i], loopPts[(i + 1) % loopPts.Count]));
                    profile.Append(ca);
                }
                if (profile.Size == 0) continue;

                using (SubTransaction st = new SubTransaction(doc))
                {
                    st.Start();
                    try
                    {
                        Plane plane = Plane.CreateByNormalAndOrigin(axis, axis * baseLevel);
                        SketchPlane sp = SketchPlane.Create(doc, plane);
                        Extrusion ext = doc.FamilyCreate.NewExtrusion(true, profile, sp, depth);
                        doc.Regenerate();

                        if (ext != null && HasSolidVolume(ext))
                        {
                            st.Commit();
                            return ext.Id;
                        }
                        st.RollBack();
                    }
                    catch
                    {
                        if (st.HasStarted() && !st.HasEnded()) st.RollBack();
                    }
                }
            }
            return ElementId.InvalidElementId;
        }

        private static bool HasSolidVolume(Element e)
        {
            GeometryElement ge = e.get_Geometry(new Options());
            if (ge == null) return false;
            foreach (GeometryObject go in ge)
            {
                if (go is Solid s && s.Volume > 1e-9) return true;
            }
            return false;
        }

        #endregion

        #region 3. Free Form solid

        private static ElementId TryFreeForm(Document doc, List<XYZ> pts, List<int[]> tris, ElementId matId)
        {
            Solid solid = BuildSolid(pts, tris, matId);
            if (solid == null || solid.Volume <= 1e-9)
                return ElementId.InvalidElementId;

            using (SubTransaction st = new SubTransaction(doc))
            {
                st.Start();
                try
                {
                    FreeFormElement ffe = FreeFormElement.Create(doc, solid);
                    doc.Regenerate();
                    st.Commit();
                    return ffe.Id;
                }
                catch
                {
                    if (st.HasStarted() && !st.HasEnded()) st.RollBack();
                }
            }
            return ElementId.InvalidElementId;
        }

        /// <summary>
        /// Merges coplanar adjacent triangles into clean polygon faces (removes triangulation lines)
        /// and builds a closed Revit Solid.
        /// </summary>
        private static Solid BuildSolid(List<XYZ> pts, List<int[]> tris, ElementId matId)
        {
            try
            {
                var faces = BuildMergedFaces(pts, tris, matId);
                Solid s = RunBuilder(faces, TessellatedShapeBuilderTarget.Solid, TessellatedShapeBuilderFallback.Abort,
                    out _);
                return s;
            }
            catch
            {
                return null;
            }
        }

        private static List<TessellatedFace> BuildMergedFaces(List<XYZ> pts, List<int[]> tris, ElementId matId)
        {
            int n = tris.Count;
            var normals = new XYZ[n];
            var dists = new double[n];
            for (int i = 0; i < n; i++)
            {
                int[] t = tris[i];
                normals[i] = (pts[t[1]] - pts[t[0]]).CrossProduct(pts[t[2]] - pts[t[0]]).Normalize();
                dists[i] = normals[i].DotProduct(pts[t[0]]);
            }

            // Edge adjacency
            var edgeToTris = new Dictionary<(int, int), List<int>>();
            for (int i = 0; i < n; i++)
            {
                int[] t = tris[i];
                for (int k = 0; k < 3; k++)
                {
                    int a = t[k], b = t[(k + 1) % 3];
                    var key = a < b ? (a, b) : (b, a);
                    if (!edgeToTris.TryGetValue(key, out var l)) edgeToTris[key] = l = new List<int>();
                    l.Add(i);
                }
            }

            var group = Enumerable.Repeat(-1, n).ToArray();
            var faces = new List<TessellatedFace>();

            for (int seed = 0; seed < n; seed++)
            {
                if (group[seed] >= 0) continue;
                var members = new List<int>();
                var stack = new Stack<int>();
                stack.Push(seed);
                group[seed] = seed;

                while (stack.Count > 0)
                {
                    int cur = stack.Pop();
                    members.Add(cur);
                    int[] t = tris[cur];
                    for (int k = 0; k < 3; k++)
                    {
                        int a = t[k], b = t[(k + 1) % 3];
                        var key = a < b ? (a, b) : (b, a);
                        foreach (int nb in edgeToTris[key])
                        {
                            if (group[nb] >= 0) continue;
                            if (normals[nb].DotProduct(normals[seed]) > 0.99999 &&
                                Math.Abs(dists[nb] - dists[seed]) < LevelTolFt)
                            {
                                group[nb] = seed;
                                stack.Push(nb);
                            }
                        }
                    }
                }

                var memberTris = members.Select(m => tris[m]).ToList();
                List<List<int>> loops = members.Count > 1 ? BoundaryLoops(memberTris) : null;

                if (loops != null && loops.Count > 0)
                {
                    var loopPts = loops.Select(l => l.Select(k => pts[k]).ToList())
                                       .OrderByDescending(l => LoopArea(l, normals[seed]))
                                       .Select(l => (IList<XYZ>)l)
                                       .ToList();
                    faces.Add(new TessellatedFace(loopPts, matId));
                }
                else
                {
                    foreach (int[] t in memberTris)
                        faces.Add(new TessellatedFace(new List<XYZ> { pts[t[0]], pts[t[1]], pts[t[2]] }, matId));
                }
            }
            return faces;
        }

        private static Solid RunBuilder(
            List<TessellatedFace> faces,
            TessellatedShapeBuilderTarget target,
            TessellatedShapeBuilderFallback fallback,
            out IList<GeometryObject> allObjects)
        {
            allObjects = null;
            var b = new TessellatedShapeBuilder();
            b.OpenConnectedFaceSet(target == TessellatedShapeBuilderTarget.Solid);
            foreach (TessellatedFace f in faces)
            {
                if (b.DoesFaceHaveEnoughLoopsAndVertices(f))
                    b.AddFace(f);
            }
            b.CloseConnectedFaceSet();
            b.Target = target;
            b.Fallback = fallback;
            b.Build();

            TessellatedShapeBuilderResult r = b.GetBuildResult();
            allObjects = r.GetGeometricalObjects();
            return allObjects?.OfType<Solid>().FirstOrDefault(s => s.Volume > 1e-9);
        }

        #endregion

        #region 4. DirectShape fallback

        private static ElementId TryDirectShape(Document doc, List<XYZ> pts, List<int[]> tris, ElementId matId, string name)
        {
            try
            {
                if (!DirectShape.IsSupportedDocument(doc))
                    return ElementId.InvalidElementId;

                var triFaces = tris.Select(t => new TessellatedFace(
                    new List<XYZ> { pts[t[0]], pts[t[1]], pts[t[2]] }, matId)).ToList();

                RunBuilder(triFaces, TessellatedShapeBuilderTarget.AnyGeometry,
                    TessellatedShapeBuilderFallback.Mesh, out IList<GeometryObject> geo);

                if (geo == null || geo.Count == 0)
                    return ElementId.InvalidElementId;

                ElementId catId = new ElementId(BuiltInCategory.OST_GenericModel);
                if (doc.IsFamilyDocument && doc.OwnerFamily?.FamilyCategory != null &&
                    DirectShape.IsValidCategoryId(doc.OwnerFamily.FamilyCategory.Id, doc))
                {
                    catId = doc.OwnerFamily.FamilyCategory.Id;
                }

                using (SubTransaction st = new SubTransaction(doc))
                {
                    st.Start();
                    DirectShape ds = DirectShape.CreateElement(doc, catId);
                    ds.SetShape(geo);
                    ds.Name = name;
                    st.Commit();
                    return ds.Id;
                }
            }
            catch
            {
                return ElementId.InvalidElementId;
            }
        }

        #endregion

        #region Materials

        private static ElementId ResolveMaterial(M2RObject obj, Dictionary<string, ElementId> matMap)
        {
            if (matMap != null && !string.IsNullOrWhiteSpace(obj.Layer) &&
                matMap.TryGetValue(obj.Layer, out ElementId id))
                return id;
            return ElementId.InvalidElementId;
        }

        /// <summary>
        /// Sets the element Material and links it to a Family Type parameter (e.g. "Material Glass")
        /// so the material can be changed later from Type Properties in the project.
        /// </summary>
        private static void AssignMaterial(Document doc, ElementId elemId, ElementId matId, string layerName)
        {
            if (!IsValid(matId)) return;
            Element elem = doc.GetElement(elemId);
            Parameter p = elem?.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            if (p == null || p.IsReadOnly) return;

            try { p.Set(matId); } catch { return; }

            if (!doc.IsFamilyDocument) return;
            try
            {
                FamilyManager fm = doc.FamilyManager;
                if (fm.CurrentType == null)
                    fm.NewType("Standard");

                Material mat = doc.GetElement(matId) as Material;
                string baseName = mat != null ? mat.Name : layerName;
                if (baseName.StartsWith("Mat_", StringComparison.OrdinalIgnoreCase))
                    baseName = baseName.Substring(4);
                string paramName = "Material " + CleanParamName(baseName);

                FamilyParameter fp = fm.get_Parameter(paramName) ??
                    fm.AddParameter(paramName, GroupTypeId.Materials, SpecTypeId.Reference.Material, false);

                fm.Set(fp, matId);
                fm.AssociateElementParameterToFamilyParameter(p, fp);
            }
            catch
            {
                // Element keeps its directly assigned material
            }
        }

        private static string CleanParamName(string s)
        {
            char[] bad = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~', '-' };
            foreach (char c in bad) s = s.Replace(c, '_');
            s = s.Trim();
            return string.IsNullOrEmpty(s) ? "Default" : s;
        }

        #endregion
    }
}
