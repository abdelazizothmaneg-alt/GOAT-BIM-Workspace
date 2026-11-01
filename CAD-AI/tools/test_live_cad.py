import sys
import win32com.client

print("=" * 50)
print("Testing direct connection to running AutoCAD...")
print("=" * 50)

# Try connecting to the active AutoCAD session
try:
    acad = win32com.client.GetActiveObject("AutoCAD.Application")
    print(f"[SUCCESS] Connected to AutoCAD successfully!")
    print(f"AutoCAD Version: {acad.Version}")
    print(f"AutoCAD Caption: {acad.Caption}")
    
    if acad.Documents.Count == 0:
        print("[WARNING] AutoCAD is open, but NO drawing file is open!")
        print("Please create a new drawing (Ctrl+N) or open a DWG file.")
    else:
        doc = acad.ActiveDocument
        print(f"Active Drawing: {doc.Name}")
        
        # Draw test text
        import pythoncom
        pt = win32com.client.VARIANT(pythoncom.VT_ARRAY | pythoncom.VT_R8, [0.0, 0.0, 0.0])
        txt = doc.ModelSpace.AddText("Hello from Google Antigravity!", pt, 25.0)
        doc.Regen(1)
        print("[SUCCESS] Added text 'Hello from Google Antigravity!' at (0, 0, 0)")
        print("Please check your AutoCAD drawing screen now!")
except Exception as e:
    err_str = str(e)
    print(f"[FAILED] Could not connect to running AutoCAD: {e}")
    if "-2147221021" in err_str:
        print("\nReason: Operation unavailable (AutoCAD is not registered in Windows ROT).")
        print("Common causes:")
        print("1. No DWG file is open (AutoCAD is sitting on the 'Start' screen).")
        print("2. A popup dialog box or command is active in AutoCAD (press ESC in AutoCAD).")
        print("3. AutoCAD was launched with 'Run as Administrator'.")
