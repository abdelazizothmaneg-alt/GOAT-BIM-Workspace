import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

from cad_bridge import run_cad_task
import pythoncom
import win32com.client

def verify_connection(acad, doc):
    print(f"Successfully connected to active document: {doc.Name}")
    print(f"AutoCAD Application: {acad.Caption}")
    
    # Prompt in command line
    doc.Utility.Prompt("\nPython Connection Established via Google Antigravity.\n")
    
    # Add a test text object
    pt = win32com.client.VARIANT(pythoncom.VT_ARRAY | pythoncom.VT_R8, [0.0, 0.0, 0.0])
    text_content = "Hello from Google Antigravity"
    text_height = 25.0
    
    text_obj = doc.ModelSpace.AddText(text_content, pt, text_height)
    doc.Regen(1) # acActiveViewport
    print(f"Added text '{text_content}' at (0, 0, 0)")
    return True

def main():
    print("Attempting to connect to AutoCAD...")
    try:
        run_cad_task(verify_connection)
        print("\nSUCCESS! Check your AutoCAD screen now.")
    except Exception as e:
        print(f"An error occurred: {e}")
        print("Please ensure AutoCAD is running and a drawing is open.")

if __name__ == "__main__":
    main()
