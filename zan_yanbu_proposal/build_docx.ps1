$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Escape-Xml([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return "" }
    $text = $text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace('"', "&quot;").Replace("'", "&apos;")
    return $text
}

function Format-InlineRuns([string]$line, [string]$defaultColor = "1E293B", [int]$fontSizeHalfPt = 22, [bool]$forceBold = $false) {
    $line = $line -replace '<br\s*/?>', "`n"
    $line = $line -replace '`([^`]+)`', '$1'
    $line = $line -replace '(?<!\*)\*([^\*\n]+)\*(?!\*)', '$1'

    $sb = New-Object System.Text.StringBuilder
    $parts = [regex]::Split($line, '(\*\*[^\*\n]+\*\*)')
    foreach ($part in $parts) {
        if ([string]::IsNullOrEmpty($part)) { continue }
        $isBold = $forceBold
        $cleanPart = $part
        if ($part.StartsWith("**") -and $part.EndsWith("**") -and $part.Length -ge 4) {
            $isBold = $true
            $cleanPart = $part.Substring(2, $part.Length - 4)
        }
        $subLines = $cleanPart -split "`n"
        for ($i = 0; $i -lt $subLines.Count; $i++) {
            $escaped = Escape-Xml $subLines[$i]
            [void]$sb.Append('<w:r><w:rPr>')
            [void]$sb.Append('<w:rFonts w:ascii="Arial" w:hAnsi="Arial" w:cs="Arial"/>')
            if ($isBold) {
                [void]$sb.Append('<w:b/><w:bCs/>')
            }
            [void]$sb.Append("<w:color w:val=`"$defaultColor`"/>")
            [void]$sb.Append("<w:sz w:val=`"$fontSizeHalfPt`"/><w:szCs w:val=`"$fontSizeHalfPt`"/>")
            [void]$sb.Append('<w:rtl/>')
            [void]$sb.Append('</w:rPr>')
            if ($i -gt 0) {
                [void]$sb.Append('<w:br/>')
            }
            [void]$sb.Append("<w:t xml:space=`"preserve`">$escaped</w:t></w:r>")
        }
    }
    return $sb.ToString()
}

function New-ParagraphXml([string]$text, [string]$type = "normal") {
    $sb = New-Object System.Text.StringBuilder
    $bulletChar = [string][char]0x2022
    switch ($type) {
        "h1" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="left"/><w:spacing w:before="240" w:after="160"/>')
            [void]$sb.Append('<w:shd w:val="clear" w:color="auto" w:fill="0F3D3E"/>')
            [void]$sb.Append('<w:pBdr><w:right w:val="single" w:sz="36" w:space="12" w:color="C59B27"/></w:pBdr>')
            [void]$sb.Append('</w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "FFFFFF" 32 $true))
            [void]$sb.Append('</w:p>')
        }
        "h2" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="left"/><w:spacing w:before="280" w:after="120"/>')
            [void]$sb.Append('<w:shd w:val="clear" w:color="auto" w:fill="0F3D3E"/>')
            [void]$sb.Append('<w:pBdr><w:right w:val="single" w:sz="24" w:space="8" w:color="C59B27"/></w:pBdr>')
            [void]$sb.Append('</w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "FFFFFF" 26 $true))
            [void]$sb.Append('</w:p>')
        }
        "h3" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="left"/><w:spacing w:before="200" w:after="80"/>')
            [void]$sb.Append('<w:pBdr><w:bottom w:val="single" w:sz="12" w:space="4" w:color="0F3D3E"/></w:pBdr>')
            [void]$sb.Append('</w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "0F3D3E" 24 $true))
            [void]$sb.Append('</w:p>')
        }
        "h4" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="left"/><w:spacing w:before="160" w:after="60"/></w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "1B6B6D" 22 $true))
            [void]$sb.Append('</w:p>')
        }
        "callout" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="both"/><w:spacing w:before="80" w:after="80"/>')
            [void]$sb.Append('<w:ind w:left="180" w:right="180"/>')
            [void]$sb.Append('<w:shd w:val="clear" w:color="auto" w:fill="FFFBEB"/>')
            [void]$sb.Append('<w:pBdr><w:right w:val="single" w:sz="24" w:space="8" w:color="D97706"/><w:top w:val="single" w:sz="4" w:space="4" w:color="F59E0B"/><w:bottom w:val="single" w:sz="4" w:space="4" w:color="F59E0B"/><w:left w:val="single" w:sz="4" w:space="4" w:color="F59E0B"/></w:pBdr>')
            [void]$sb.Append('</w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "92400E" 21 $false))
            [void]$sb.Append('</w:p>')
        }
        "bullet" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="both"/><w:spacing w:before="40" w:after="60"/><w:ind w:left="360" w:hanging="180"/></w:pPr>')
            [void]$sb.Append((Format-InlineRuns ($bulletChar + " " + $text) "1E293B" 21 $false))
            [void]$sb.Append('</w:p>')
        }
        "hr" {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:spacing w:before="120" w:after="120"/><w:pBdr><w:bottom w:val="single" w:sz="8" w:space="1" w:color="CBD5E1"/></w:pBdr></w:pPr></w:p>')
        }
        default {
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="both"/><w:spacing w:before="60" w:after="80"/></w:pPr>')
            [void]$sb.Append((Format-InlineRuns $text "1E293B" 21 $false))
            [void]$sb.Append('</w:p>')
        }
    }
    return $sb.ToString()
}

function Convert-TableToXml($tableLines) {
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($tline in $tableLines) {
        $trimmed = $tline.Trim()
        if ($trimmed.StartsWith("|") -and $trimmed.EndsWith("|")) {
            $inner = $trimmed.Substring(1, $trimmed.Length - 2)
            $cells = $inner -split '\|'
            $isSep = $true
            foreach ($c in $cells) {
                if ($c.Trim() -notmatch '^[:\-]+$') {
                    $isSep = $false
                    break
                }
            }
            if (-not $isSep) {
                $rows.Add($cells)
            }
        }
    }
    if ($rows.Count -eq 0) { return "" }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('<w:tbl><w:tblPr><w:bidiVisual/><w:tblW w:w="5000" w:type="pct"/>')
    [void]$sb.Append('<w:tblBorders>')
    [void]$sb.Append('<w:top w:val="single" w:sz="6" w:space="0" w:color="94A3B8"/>')
    [void]$sb.Append('<w:bottom w:val="single" w:sz="8" w:space="0" w:color="0F3D3E"/>')
    [void]$sb.Append('<w:left w:val="single" w:sz="6" w:space="0" w:color="94A3B8"/>')
    [void]$sb.Append('<w:right w:val="single" w:sz="6" w:space="0" w:color="94A3B8"/>')
    [void]$sb.Append('<w:insideH w:val="single" w:sz="4" w:space="0" w:color="CBD5E1"/>')
    [void]$sb.Append('<w:insideV w:val="single" w:sz="4" w:space="0" w:color="CBD5E1"/>')
    [void]$sb.Append('</w:tblBorders>')
    [void]$sb.Append('<w:tblCellMar><w:top w:w="100" w:type="dxa"/><w:bottom w:w="100" w:type="dxa"/><w:left w:w="120" w:type="dxa"/><w:right w:w="120" w:type="dxa"/></w:tblCellMar>')
    [void]$sb.Append('</w:tblPr>')

    for ($r = 0; $r -lt $rows.Count; $r++) {
        $cells = $rows[$r]
        $isHeader = ($r -eq 0)
        $rowTextJoined = ($cells -join " ")
        $isTotalRow = ($rowTextJoined -match 'Base Scope Total|Subtotal Scope|1,080,000|1,242,000|50%')
        [void]$sb.Append('<w:tr>')
        if ($isHeader) {
            [void]$sb.Append('<w:trPr><w:tblHeader/></w:trPr>')
        }
        foreach ($cell in $cells) {
            $cellText = $cell.Trim()
            $fill = "FFFFFF"
            $textColor = "1E293B"
            $boldCell = $false
            if ($isHeader) {
                $fill = "0F3D3E"
                $textColor = "FFFFFF"
                $boldCell = $true
            } elseif ($isTotalRow) {
                $fill = "E2E8F0"
                $textColor = "0F172A"
                $boldCell = $true
            } elseif ($r % 2 -eq 0) {
                $fill = "F8FAFC"
            }
            [void]$sb.Append("<w:tc><w:tcPr><w:shd w:val=`"clear`" w:color=`"auto`" w:fill=`"$fill`"/><w:vAlign w:val=`"center`"/></w:tcPr>")
            [void]$sb.Append('<w:p><w:pPr><w:bidi/><w:jc w:val="left"/><w:spacing w:before="40" w:after="40"/></w:pPr>')
            [void]$sb.Append((Format-InlineRuns $cellText $textColor 19 $boldCell))
            [void]$sb.Append('</w:p></w:tc>')
        }
        [void]$sb.Append('</w:tr>')
    }
    [void]$sb.Append('</w:tbl><w:p><w:pPr><w:spacing w:before="40" w:after="40"/></w:pPr></w:p>')
    return $sb.ToString()
}

function Convert-MarkdownToDocx([string]$mdPath, [string]$docxPath) {
    $lines = [System.IO.File]::ReadAllLines($mdPath, [System.Text.Encoding]::UTF8)
    $bodySb = New-Object System.Text.StringBuilder

    $inTable = $false
    $tableBuffer = New-Object System.Collections.Generic.List[string]
    $inMermaid = $false
    $bulletChar = [string][char]0x2022

    foreach ($line in $lines) {
        $trimmed = $line.Trim()

        if ($trimmed.StartsWith('```mermaid')) {
            $inMermaid = $true
            continue
        }
        if ($inMermaid) {
            if ($trimmed.StartsWith('```')) {
                $inMermaid = $false
            }
            continue
        }

        if ($trimmed.StartsWith("|") -and $trimmed.EndsWith("|")) {
            $inTable = $true
            $tableBuffer.Add($trimmed)
            continue
        } elseif ($inTable) {
            [void]$bodySb.Append((Convert-TableToXml $tableBuffer))
            $tableBuffer.Clear()
            $inTable = $false
        }

        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        if ($trimmed -eq "---") {
            [void]$bodySb.Append((New-ParagraphXml "" "hr"))
            continue
        }

        if ($trimmed.StartsWith("# ")) {
            [void]$bodySb.Append((New-ParagraphXml $trimmed.Substring(2).Trim() "h1"))
        } elseif ($trimmed.StartsWith("## ")) {
            [void]$bodySb.Append((New-ParagraphXml $trimmed.Substring(3).Trim() "h2"))
        } elseif ($trimmed.StartsWith("### ")) {
            [void]$bodySb.Append((New-ParagraphXml $trimmed.Substring(4).Trim() "h3"))
        } elseif ($trimmed.StartsWith("#### ")) {
            [void]$bodySb.Append((New-ParagraphXml $trimmed.Substring(5).Trim() "h4"))
        } elseif ($trimmed.StartsWith("> ")) {
            $calloutText = $trimmed.Substring(2).Trim()
            if ($calloutText -match '^\[!(IMPORTANT|NOTE|WARNING|TIP|CAUTION)\]$') {
                continue
            }
            [void]$bodySb.Append((New-ParagraphXml $calloutText "callout"))
        } elseif ($trimmed.StartsWith("* ") -or $trimmed.StartsWith("- ") -or $trimmed.StartsWith($bulletChar + " ")) {
            [void]$bodySb.Append((New-ParagraphXml $trimmed.Substring(2).Trim() "bullet"))
        } elseif ($trimmed -match '^(\d+\.)\s+(.+)$') {
            [void]$bodySb.Append((New-ParagraphXml ($Matches[1] + " " + $Matches[2].Trim()) "bullet"))
        } else {
            [void]$bodySb.Append((New-ParagraphXml $trimmed "normal"))
        }
    }

    if ($inTable -and $tableBuffer.Count -gt 0) {
        [void]$bodySb.Append((Convert-TableToXml $tableBuffer))
    }

    $documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <w:body>
    $($bodySb.ToString())
    <w:sectPr>
      <w:bidi/>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134" w:header="720" w:footer="720" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

    $contentTypesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/settings.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"/>
</Types>
"@

    $relsXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
"@

    $docRelsXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings" Target="settings.xml"/>
</Relationships>
"@

    $stylesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:docDefaults>
    <w:rPrDefault>
      <w:rPr>
        <w:rFonts w:ascii="Arial" w:hAnsi="Arial" w:cs="Arial"/>
        <w:sz w:val="22"/>
        <w:szCs w:val="22"/>
        <w:lang w:val="en-US" w:bidi="ar-SA"/>
      </w:rPr>
    </w:rPrDefault>
    <w:pPrDefault>
      <w:pPr>
        <w:bidi/>
      </w:pPr>
    </w:pPrDefault>
  </w:docDefaults>
</w:styles>
"@

    $settingsXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:settings xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:view w:val="print"/>
  <w:compat>
    <w:compatSetting w:name="compatibilityMode" w:uri="http://schemas.microsoft.com/office/word" w:val="15"/>
  </w:compat>
</w:settings>
"@

    if (Test-Path $docxPath) { Remove-Item $docxPath -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($docxPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $enc = New-Object System.Text.UTF8Encoding($false)
        $files = @{
            "[Content_Types].xml" = $contentTypesXml
            "_rels/.rels" = $relsXml
            "word/_rels/document.xml.rels" = $docRelsXml
            "word/document.xml" = $documentXml
            "word/styles.xml" = $stylesXml
            "word/settings.xml" = $settingsXml
        }
        foreach ($kv in $files.GetEnumerator()) {
            $entry = $zip.CreateEntry($kv.Key)
            $stream = $entry.Open()
            $bytes = $enc.GetBytes($kv.Value)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Close()
        }
    } finally {
        $zip.Dispose()
    }
    Write-Output "Created DOCX: $docxPath"
}

$brainDir = "C:\Users\faqih\.gemini\antigravity\brain\64b8876c-51af-409c-9b30-1bc4f55e8b51"
$outDir = "C:\Users\faqih\.gemini\antigravity\scratch\zan_yanbu_proposal"

Convert-MarkdownToDocx "$brainDir\HMF_Technical_Proposal_ZAN_YAN_RFP_001.md" "$outDir\ZAN-YAN-RFP-001_HMF_Technical_Proposal.docx"
Convert-MarkdownToDocx "$brainDir\HMF_Financial_Proposal_ZAN_YAN_RFP_001.md" "$outDir\ZAN-YAN-RFP-001_HMF_Financial_Proposal.docx"
Convert-MarkdownToDocx "$brainDir\HMF_Appendix_05_Detailed_Programme_32_Weeks.md" "$outDir\ZAN-YAN-RFP-001_HMF_Appendix_05_Programme_32W.docx"

