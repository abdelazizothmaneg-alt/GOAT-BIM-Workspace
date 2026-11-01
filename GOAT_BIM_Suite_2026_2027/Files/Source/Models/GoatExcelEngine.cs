using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ORTools.Models
{
    public enum GoatCellStyle
    {
        NormalEditable = 0,
        HeaderNavy = 1,
        HeaderTeal = 2,
        ReadOnlyGrey = 3,
        HighlightYellow = 4,
        SummaryBold = 5
    }

    public class GoatExcelCell
    {
        public string Value { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public bool IsNumeric { get; set; }
        public GoatCellStyle Style { get; set; } = GoatCellStyle.NormalEditable;

        public GoatExcelCell() { }

        public GoatExcelCell(string value, GoatCellStyle style = GoatCellStyle.NormalEditable, bool isNumeric = false, string formula = "")
        {
            Value = value ?? string.Empty;
            Style = style;
            IsNumeric = isNumeric;
            Formula = formula ?? string.Empty;
        }
    }

    public class GoatExcelSheet
    {
        public string Name { get; set; } = "Sheet1";
        public List<List<GoatExcelCell>> Rows { get; set; } = new List<List<GoatExcelCell>>();
        public List<double> ColumnWidths { get; set; } = new List<double>();
    }

    /// <summary>
    /// Self-contained OpenXML (.xlsx) multi-sheet reader and writer for GOAT BIM.
    /// Requires zero external NuGet packages and works even without Microsoft Excel installed.
    /// </summary>
    public static class GoatExcelEngine
    {
        public static void WriteXlsx(string filePath, IList<GoatExcelSheet> sheets)
        {
            if (sheets == null || sheets.Count == 0)
                throw new ArgumentException("At least one sheet is required.");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // 1. [Content_Types].xml
                WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml(sheets.Count));

                // 2. _rels/.rels
                WriteEntry(archive, "_rels/.rels", BuildRootRelsXml());

                // 3. xl/workbook.xml
                WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml(sheets));

                // 4. xl/_rels/workbook.xml.rels
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml(sheets.Count));

                // 5. xl/styles.xml
                WriteEntry(archive, "xl/styles.xml", BuildStylesXml());

                // 6. xl/worksheets/sheet{i}.xml
                for (int i = 0; i < sheets.Count; i++)
                {
                    WriteEntry(archive, $"xl/worksheets/sheet{i + 1}.xml", BuildWorksheetXml(sheets[i]));
                }
            }
        }

        public static Dictionary<string, List<List<string>>> ReadXlsx(string filePath)
        {
            var result = new Dictionary<string, List<List<string>>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath))
                return result;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // Load shared strings if present
                List<string> sharedStrings = new List<string>();
                ZipArchiveEntry sstEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (sstEntry != null)
                {
                    using (Stream s = sstEntry.Open())
                    {
                        XDocument sstDoc = XDocument.Load(s);
                        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                        foreach (XElement si in sstDoc.Descendants(ns + "si"))
                        {
                            string text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                            sharedStrings.Add(text);
                        }
                    }
                }

                // Map sheet names to sheet paths
                var sheetMap = new List<(string Name, string Target)>();
                ZipArchiveEntry wbEntry = archive.GetEntry("xl/workbook.xml");
                ZipArchiveEntry relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
                if (wbEntry != null && relsEntry != null)
                {
                    XDocument wbDoc;
                    XDocument relsDoc;
                    using (Stream s = wbEntry.Open()) wbDoc = XDocument.Load(s);
                    using (Stream s = relsEntry.Open()) relsDoc = XDocument.Load(s);

                    XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                    XNamespace pkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

                    var relDict = relsDoc.Descendants(pkgRelNs + "Relationship")
                        .ToDictionary(
                            x => x.Attribute("Id")?.Value ?? "",
                            x => x.Attribute("Target")?.Value ?? "");

                    foreach (XElement sh in wbDoc.Descendants(mainNs + "sheet"))
                    {
                        string name = sh.Attribute("name")?.Value ?? "Sheet";
                        string rId = sh.Attribute(rNs + "id")?.Value ?? "";
                        if (relDict.TryGetValue(rId, out string target))
                        {
                            if (!target.StartsWith("xl/"))
                                target = "xl/" + target.TrimStart('/');
                            sheetMap.Add((name, target));
                        }
                    }
                }

                foreach (var (name, target) in sheetMap)
                {
                    ZipArchiveEntry sheetEntry = archive.GetEntry(target);
                    if (sheetEntry == null) continue;

                    var rowsList = new List<List<string>>();
                    using (Stream s = sheetEntry.Open())
                    {
                        XDocument sheetDoc = XDocument.Load(s);
                        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                        foreach (XElement rowEl in sheetDoc.Descendants(ns + "row"))
                        {
                            var rowVals = new List<string>();
                            foreach (XElement cellEl in rowEl.Elements(ns + "c"))
                            {
                                string cellRef = cellEl.Attribute("r")?.Value ?? "";
                                int colIndex = GetColumnIndexFromCellReference(cellRef);
                                while (rowVals.Count < colIndex)
                                {
                                    rowVals.Add(string.Empty);
                                }

                                string tAttr = cellEl.Attribute("t")?.Value ?? "";
                                string val = string.Empty;
                                if (tAttr == "inlineStr")
                                {
                                    val = string.Concat(cellEl.Descendants(ns + "t").Select(x => x.Value));
                                }
                                else if (tAttr == "s")
                                {
                                    string idxStr = cellEl.Element(ns + "v")?.Value ?? "";
                                    if (int.TryParse(idxStr, out int sstIdx) && sstIdx >= 0 && sstIdx < sharedStrings.Count)
                                    {
                                        val = sharedStrings[sstIdx];
                                    }
                                }
                                else
                                {
                                    val = cellEl.Element(ns + "v")?.Value ?? string.Empty;
                                }

                                rowVals.Add(val);
                            }
                            rowsList.Add(rowVals);
                        }
                    }
                    result[name] = rowsList;
                }
            }

            return result;
        }

        private static void WriteEntry(ZipArchive archive, string entryPath, string xmlContent)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryPath, CompressionLevel.Fastest);
            using (Stream s = entry.Open())
            using (StreamWriter sw = new StreamWriter(s, new UTF8Encoding(false)))
            {
                sw.Write(xmlContent);
            }
        }

        private static string BuildContentTypesXml(int sheetCount)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">");
            sb.AppendLine(@"  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>");
            sb.AppendLine(@"  <Default Extension=""xml"" ContentType=""application/xml""/>");
            sb.AppendLine(@"  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>");
            sb.AppendLine(@"  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>");
            for (int i = 1; i <= sheetCount; i++)
            {
                sb.AppendLine($@"  <Override PartName=""/xl/worksheets/sheet{i}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>");
            }
            sb.AppendLine(@"</Types>");
            return sb.ToString();
        }

        private static string BuildRootRelsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";
        }

        private static string BuildWorkbookXml(IList<GoatExcelSheet> sheets)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">");
            sb.AppendLine(@"  <sheets>");
            for (int i = 0; i < sheets.Count; i++)
            {
                string safeName = EscapeXml(SanitizeSheetName(sheets[i].Name, i + 1));
                sb.AppendLine($@"    <sheet name=""{safeName}"" sheetId=""{i + 1}"" r:id=""rId{i + 1}""/>");
            }
            sb.AppendLine(@"  </sheets>");
            sb.AppendLine(@"</workbook>");
            return sb.ToString();
        }

        private static string BuildWorkbookRelsXml(int sheetCount)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">");
            for (int i = 1; i <= sheetCount; i++)
            {
                sb.AppendLine($@"  <Relationship Id=""rId{i}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{i}.xml""/>");
            }
            sb.AppendLine($@"  <Relationship Id=""rId{sheetCount + 1}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>");
            sb.AppendLine(@"</Relationships>");
            return sb.ToString();
        }

        private static string BuildStylesXml()
        {
            // Style indices:
            // 0 = Normal Editable (White bg, thin border)
            // 1 = Header Navy (#0F172A bg, White bold text)
            // 2 = Header Teal (#0284C7 bg, White bold text)
            // 3 = ReadOnly Grey (#F1F5F9 bg, #475569 text)
            // 4 = Highlight Yellow (#FEF3C7 bg, #92400E bold text)
            // 5 = Summary Bold (#ECFDF5 bg, #065F46 bold text)
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""5"">
    <font><sz val=""10""/><color rgb=""FF1E293B""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""10.5""/><color rgb=""FFFFFFFF""/><name val=""Segoe UI""/></font>
    <font><sz val=""10""/><color rgb=""FF64748B""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""10""/><color rgb=""FF92400E""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""10""/><color rgb=""FF065F46""/><name val=""Segoe UI""/></font>
  </fonts>
  <fills count=""7"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF0F172A""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF0284C7""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF1F5F9""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFFEF3C7""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFECFDF5""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style=""thin""><color rgb=""FFCBD5E1""/></left>
      <right style=""thin""><color rgb=""FFCBD5E1""/></right>
      <top style=""thin""><color rgb=""FFCBD5E1""/></top>
      <bottom style=""thin""><color rgb=""FFCBD5E1""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""6"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""4"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""3"" fillId=""5"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""4"" fillId=""6"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>";
        }

        private static string BuildWorksheetXml(GoatExcelSheet sheet)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

            if (sheet.ColumnWidths != null && sheet.ColumnWidths.Count > 0)
            {
                sb.AppendLine(@"  <cols>");
                for (int c = 0; c < sheet.ColumnWidths.Count; c++)
                {
                    double w = sheet.ColumnWidths[c] > 4 ? sheet.ColumnWidths[c] : 18;
                    sb.AppendLine($@"    <col min=""{c + 1}"" max=""{c + 1}"" width=""{w.ToString("0.##", CultureInfo.InvariantCulture)}"" customWidth=""1""/>");
                }
                sb.AppendLine(@"  </cols>");
            }

            sb.AppendLine(@"  <sheetData>");
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                int rowNum = r + 1;
                var row = sheet.Rows[r];
                sb.AppendLine($@"    <row r=""{rowNum}"">");
                for (int c = 0; c < row.Count; c++)
                {
                    var cell = row[c] ?? new GoatExcelCell();
                    string cellRef = GetColumnName(c) + rowNum.ToString(CultureInfo.InvariantCulture);
                    int styleIdx = (int)cell.Style;

                    if (!string.IsNullOrWhiteSpace(cell.Formula))
                    {
                        string cleanFormula = EscapeXml(cell.Formula.TrimStart('='));
                        string fallbackVal = EscapeXml(cell.Value ?? string.Empty);
                        sb.AppendLine($@"      <c r=""{cellRef}"" s=""{styleIdx}"" t=""str""><f>{cleanFormula}</f><v>{fallbackVal}</v></c>");
                    }
                    else if (cell.IsNumeric && double.TryParse(cell.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numVal))
                    {
                        sb.AppendLine($@"      <c r=""{cellRef}"" s=""{styleIdx}""><v>{numVal.ToString(CultureInfo.InvariantCulture)}</v></c>");
                    }
                    else
                    {
                        string escapedText = EscapeXml(cell.Value ?? string.Empty);
                        sb.AppendLine($@"      <c r=""{cellRef}"" s=""{styleIdx}"" t=""inlineStr""><is><t xml:space=""preserve"">{escapedText}</t></is></c>");
                    }
                }
                sb.AppendLine(@"    </row>");
            }
            sb.AppendLine(@"  </sheetData>");
            sb.AppendLine(@"</worksheet>");
            return sb.ToString();
        }

        public static string GetColumnName(int zeroBasedColumnIndex)
        {
            int dividend = zeroBasedColumnIndex + 1;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }

        private static int GetColumnIndexFromCellReference(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return 0;
            int colIndex = 0;
            foreach (char ch in cellRef)
            {
                if (char.IsLetter(ch))
                {
                    colIndex = colIndex * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
                }
                else
                {
                    break;
                }
            }
            return Math.Max(0, colIndex - 1);
        }

        private static string SanitizeSheetName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name)) return $"Sheet{index}";
            char[] invalid = { '\\', '/', '?', '*', ':', '[', ']' };
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Length > 31 ? name.Substring(0, 31) : name;
        }

        private static string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
