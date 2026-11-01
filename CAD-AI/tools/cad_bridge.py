import ctypes
import threading
import win32com.client
import pythoncom

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

def switch_to_default_desktop():
    """Switches the current thread to the interactive 'Default' desktop."""
    try:
        hdesk = user32.OpenDesktopW("Default", 0, False, 0x1FF)
        if hdesk:
            user32.SetThreadDesktop(hdesk)
    except Exception:
        pass

def get_acad():
    """
    Connects to the active interactive AutoCAD instance on the Default desktop.
    Returns (acad_app, active_doc).
    """
    result = {}
    error_container = {}

    def _connect():
        switch_to_default_desktop()
        pythoncom.CoInitialize()
        try:
            acad = win32com.client.GetActiveObject("AutoCAD.Application")
            result["acad"] = acad
            result["doc"] = acad.ActiveDocument
        except Exception as e:
            error_container["error"] = e
        finally:
            pythoncom.CoUninitialize()

    t = threading.Thread(target=_connect)
    t.start()
    t.join()

    if "error" in error_container:
        raise error_container["error"]
    return result["acad"], result["doc"]

def run_cad_task(callback):
    """
    Executes a callback function inside a thread switched to the Default desktop
    with COM initialized. The callback receives (acad, doc).
    """
    res = {}
    err = {}

    def _worker():
        switch_to_default_desktop()
        pythoncom.CoInitialize()
        try:
            acad = win32com.client.GetActiveObject("AutoCAD.Application")
            doc = acad.ActiveDocument
            res["val"] = callback(acad, doc)
        except Exception as e:
            err["val"] = e
        finally:
            pythoncom.CoUninitialize()

    t = threading.Thread(target=_worker)
    t.start()
    t.join()

    if "val" in err:
        raise err["val"]
    return res.get("val")
