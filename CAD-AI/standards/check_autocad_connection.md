# Check AutoCAD Connection

**Goal**: Verify that Antigravity is connected to AutoCAD.

---

## Quick Check (When user asks "Are you connected to AutoCAD?")

### Step 1: Run Connection Test

```powershell
python check_connection.py
```

This script adds text **"Hello from Google Antigravity"** at point (0,0,0) in AutoCAD and performs Zoom Extents.

### Step 2: Ask User for Confirmation

> "Can you see **'Hello from Google Antigravity'** text in your AutoCAD drawing?"

- **If YES** → Connection confirmed. Ready to proceed.
- **If NO** → Go to Step 3.

### Step 3: Run Diagnostic (Only if Step 2 fails)

```powershell
python diagnose_autocad.py
```

Review the diagnostic output to identify the issue.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `pyautocad is NOT installed` | Run: `pip install pyautocad` |
| `No AutoCAD installations found` | Install AutoCAD on this system |
| `AutoCAD is NOT running` | Start AutoCAD and open a `.dwg` file |
| `No document is open` | Open a drawing file in AutoCAD |
| `COM connection failed` | Restart AutoCAD, ensure no dialogs are blocking |
| `Command line busy / Prompt open` | Press ESC or Enter in AutoCAD command line |
| `Virtual Desktop isolation` | Use `cad_bridge.py` to switch thread desktop to 'Default' |

---

## Prerequisites

- **Python**: Installed
- **Package Manager**: pip (`pip install pyautocad pywin32 comtypes`)
- **Bridge**: `tools/cad_bridge.py` connects across virtual desktops
