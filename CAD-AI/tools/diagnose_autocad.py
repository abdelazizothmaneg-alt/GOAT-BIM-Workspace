"""
AutoCAD Connection Diagnostic Script
=====================================
This script checks:
1. If pyautocad module is installed
2. Where AutoCAD is installed on the system (via Windows Registry)
3. If AutoCAD is currently running
4. If we can connect to AutoCAD via COM automation

Run this script to diagnose AutoCAD connectivity issues.
"""

import sys
import subprocess
import json
from datetime import datetime


def check_pyautocad_installed():
    """Check if pyautocad module is available."""
    print("\n[1/4] Checking if pyautocad is installed...")
    try:
        import pyautocad
        print(f"  [OK] pyautocad is installed (version: {getattr(pyautocad, '__version__', 'unknown')})")
        return True
    except ImportError:
        print("  [X] pyautocad is NOT installed")
        print("      To install: pip install pyautocad")
        return False


def find_autocad_installations():
    """Find AutoCAD installations by checking Windows Registry."""
    print("\n[2/4] Searching for AutoCAD installations...")
    
    installations = []
    
    try:
        import winreg
        
        # Common registry paths where AutoCAD might be registered
        registry_paths = [
            (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Autodesk\AutoCAD"),
            (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Autodesk\AutoCAD"),
            (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Autodesk\AutoCAD"),
        ]
        
        for hkey, path in registry_paths:
            try:
                key = winreg.OpenKey(hkey, path)
                i = 0
                while True:
                    try:
                        subkey_name = winreg.EnumKey(key, i)
                        subkey_path = f"{path}\\{subkey_name}"
                        subkey = winreg.OpenKey(hkey, subkey_path)
                        
                        # Try to find version-specific subkeys
                        j = 0
                        while True:
                            try:
                                version_key_name = winreg.EnumKey(subkey, j)
                                version_path = f"{subkey_path}\\{version_key_name}"
                                version_key = winreg.OpenKey(hkey, version_path)
                                
                                try:
                                    install_path, _ = winreg.QueryValueEx(version_key, "AcadLocation")
                                    product_name = subkey_name
                                    installations.append({
                                        "product": product_name,
                                        "version": version_key_name,
                                        "path": install_path
                                    })
                                except (FileNotFoundError, OSError):
                                    pass
                                
                                winreg.CloseKey(version_key)
                                j += 1
                            except OSError:
                                break
                        
                        winreg.CloseKey(subkey)
                        i += 1
                    except OSError:
                        break
                winreg.CloseKey(key)
            except (FileNotFoundError, OSError):
                continue
        
        if installations:
            print(f"  [OK] Found {len(installations)} AutoCAD installation(s):")
            for inst in installations:
                print(f"       - {inst['product']} ({inst['version']})")
                print(f"         Path: {inst['path']}")
        else:
            print("  [X] No AutoCAD installations found in registry")
            print("      AutoCAD may not be installed, or installed in a non-standard way")
            
    except ImportError:
        print("  [X] winreg module not available (not on Windows?)")
        return []
    except Exception as e:
        print(f"  [X] Error checking registry: {e}")
        return []
    
    return installations


def check_autocad_running():
    """Check if AutoCAD process is currently running."""
    print("\n[3/4] Checking if AutoCAD is running...")
    
    try:
        # Use tasklist to find AutoCAD processes
        result = subprocess.run(
            ["tasklist", "/FI", "IMAGENAME eq acad.exe", "/FO", "CSV", "/NH"],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        output = result.stdout.strip()
        
        if "acad.exe" in output.lower():
            print("  [OK] AutoCAD (acad.exe) is currently running")
            return True
        else:
            print("  [X] AutoCAD is NOT currently running")
            print("      Please start AutoCAD and open a drawing before connecting")
            return False
            
    except subprocess.TimeoutExpired:
        print("  [?] Could not determine if AutoCAD is running (timeout)")
        return None
    except Exception as e:
        print(f"  [?] Could not determine if AutoCAD is running: {e}")
        return None


def test_com_connection():
    """Attempt to connect to AutoCAD via COM automation."""
    print("\n[4/4] Testing COM connection to AutoCAD...")
    
    try:
        from pyautocad import Autocad
        
        # Try to connect without creating a new instance
        # This will fail if AutoCAD is not running
        acad = Autocad(create_if_not_exists=False)
        
        try:
            # Try to access the application object
            app_name = acad.app.Name
            version = acad.app.Version
            print(f"  [OK] Successfully connected to: {app_name}")
            print(f"       Version: {version}")
            
            # Try to access the active document
            try:
                doc_name = acad.doc.Name
                doc_path = acad.doc.Path
                print(f"       Active document: {doc_name}")
                if doc_path:
                    print(f"       Document path: {doc_path}")
                return True, {
                    "app_name": app_name,
                    "version": version,
                    "doc_name": doc_name,
                    "doc_path": doc_path
                }
            except Exception as e:
                print(f"  [X] Connected to AutoCAD but no document is open")
                print(f"      Please open a drawing file (.dwg)")
                return False, {"error": "No document open", "details": str(e)}
                
        except Exception as e:
            print(f"  [X] Could not access AutoCAD application: {e}")
            return False, {"error": "Cannot access application", "details": str(e)}
            
    except ImportError:
        print("  [X] pyautocad not installed, cannot test connection")
        return False, {"error": "pyautocad not installed"}
    except Exception as e:
        error_msg = str(e)
        if "no running instance" in error_msg.lower() or "invalid class string" in error_msg.lower():
            print("  [X] AutoCAD is not running or COM server not available")
        else:
            print(f"  [X] COM connection failed: {e}")
        return False, {"error": "COM connection failed", "details": error_msg}


def generate_report():
    """Generate a comprehensive diagnostic report."""
    print("=" * 60)
    print("  AutoCAD Connection Diagnostic Report")
    print(f"  Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    results = {
        "timestamp": datetime.now().isoformat(),
        "pyautocad_installed": False,
        "autocad_installations": [],
        "autocad_running": False,
        "com_connected": False,
        "connection_details": None,
        "overall_status": "FAILED"
    }
    
    # Run all checks
    results["pyautocad_installed"] = check_pyautocad_installed()
    results["autocad_installations"] = find_autocad_installations()
    results["autocad_running"] = check_autocad_running()
    
    if results["pyautocad_installed"] and results["autocad_running"]:
        connected, details = test_com_connection()
        results["com_connected"] = connected
        results["connection_details"] = details
    else:
        if not results["pyautocad_installed"]:
            print("\n[4/4] Skipping COM test - pyautocad not installed")
        elif not results["autocad_running"]:
            print("\n[4/4] Skipping COM test - AutoCAD not running")
    
    # Determine overall status
    if results["com_connected"]:
        results["overall_status"] = "CONNECTED"
    elif results["autocad_running"] and results["pyautocad_installed"]:
        results["overall_status"] = "PARTIAL - AutoCAD running but COM failed"
    elif results["autocad_running"]:
        results["overall_status"] = "PARTIAL - AutoCAD running but pyautocad missing"
    elif results["autocad_installations"]:
        results["overall_status"] = "NOT CONNECTED - AutoCAD installed but not running"
    else:
        results["overall_status"] = "NOT CONNECTED - AutoCAD not found"
    
    # Print summary
    print("\n" + "=" * 60)
    print("  SUMMARY")
    print("=" * 60)
    print(f"\n  Overall Status: {results['overall_status']}")
    
    if results["overall_status"] == "CONNECTED":
        print("\n  [OK] Ready to use AutoCAD automation!")
        print("       You can now run scripts that interact with AutoCAD.")
    else:
        print("\n  Recommended actions:")
        if not results["pyautocad_installed"]:
            print("    1. Install pyautocad: pip install pyautocad")
        if not results["autocad_installations"]:
            print("    2. Install AutoCAD on this system")
        elif not results["autocad_running"]:
            print("    2. Start AutoCAD and open a drawing")
        if results["autocad_running"] and not results["com_connected"]:
            print("    3. Make sure a drawing (.dwg) file is open in AutoCAD")
    
    print("\n" + "=" * 60)
    
    return results


if __name__ == "__main__":
    results = generate_report()
    
    # Exit with appropriate code for scripting purposes
    if results["overall_status"] == "CONNECTED":
        sys.exit(0)
    else:
        sys.exit(1)
