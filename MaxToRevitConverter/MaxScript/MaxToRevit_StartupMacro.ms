/*
================================================================================
  MaxToRevit Bridge Pro - Startup MacroScript Registration
  Automatically registers the "MaxToRevit" Toolbar/Menu MacroScript on 3ds Max startup.
================================================================================
*/

macroScript MaxToRevitConverter
    category:"MaxToRevit Bridge"
    buttonText:"MaxToRevit"
    toolTip:"MaxToRevit Bridge Pro — Convert 3ds Max Models to Revit (.rfa / .rvt)"
(
    on execute do
    (
        local scriptPath = @"C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\MaxScript\MaxToRevitConverter.ms"
        if doesFileExist scriptPath then
        (
            fileIn scriptPath
        )
        else
        (
            messageBox ("Could not find MaxToRevitConverter.ms at:\n" + scriptPath) title:"MaxToRevit Bridge"
        )
    )
)
