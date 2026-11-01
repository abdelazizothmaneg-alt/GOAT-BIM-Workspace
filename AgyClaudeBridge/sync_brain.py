"""
sync_brain.py — Dual-Agent Cloud & Local Synchronization Engine
Bridges Antigravity (Local) with Claude (Cloud) via Git and GitHub Raw Bus
"""

import os
import sys
import json
import subprocess
from datetime import datetime

WORKSPACE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BRAIN_MD_PATH = os.path.join(WORKSPACE_DIR, "AI_SHARED_BRAIN.md")
STATE_JSON_PATH = os.path.join(WORKSPACE_DIR, "ai_shared_state.json")


def update_brain(agent, message, code=None, auto_push=True):
    now_iso = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    # 1. Update JSON state
    if os.path.exists(STATE_JSON_PATH):
        try:
            with open(STATE_JSON_PATH, "r", encoding="utf-8") as f:
                state = json.load(f)
        except Exception:
            state = {}
    else:
        state = {}

    state["last_updated"] = now_iso
    state.setdefault("agent_dialogue", []).append({
        "from": agent,
        "timestamp": now_iso,
        "message": message,
        "code_attached": bool(code)
    })

    with open(STATE_JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(state, f, ensure_ascii=False, indent=2)

    # 2. Update AI_SHARED_BRAIN.md
    if os.path.exists(BRAIN_MD_PATH):
        with open(BRAIN_MD_PATH, "r", encoding="utf-8") as f:
            md_content = f.read()

        entry = f"\n\n### 💬 [{agent.upper()}] — *{now_iso}*\n> {message}\n"
        if code:
            entry += f"\n```csharp\n{code}\n```\n"

        if "## 💬 3. شريط الحوار الفكري الحي (Live Cognitive Dialogue)" in md_content:
            parts = md_content.split("## 💬 3. شريط الحوار الفكري الحي (Live Cognitive Dialogue)")
            new_md = parts[0] + "## 💬 3. شريط الحوار الفكري الحي (Live Cognitive Dialogue)" + entry + parts[1]
        else:
            new_md = md_content + entry

        with open(BRAIN_MD_PATH, "w", encoding="utf-8") as f:
            f.write(new_md)

    print(f"[OK] Brain updated by {agent} at {now_iso}")

    # 3. Git commit & push
    if auto_push:
        try:
            subprocess.run(["git", "add", "ai_shared_state.json", "AI_SHARED_BRAIN.md"], cwd=WORKSPACE_DIR, check=True)
            subprocess.run(["git", "commit", "-m", f"sync(brain): update by {agent} [{now_iso}]"], cwd=WORKSPACE_DIR, check=True)
            subprocess.run(["git", "push", "origin", "main"], cwd=WORKSPACE_DIR, check=True)
            print("[OK] Pushed to GitHub remote. Cloud version is live.")
        except Exception as e:
            print(f"[WARN] Git push skipped or failed: {e}")


def main():
    if len(sys.argv) < 3:
        print("Usage: python sync_brain.py <agent: antigravity|claude> <message>")
        return
    agent = sys.argv[1]
    msg = sys.argv[2]
    update_brain(agent, msg)


if __name__ == "__main__":
    main()
