"""
AgyClaudeBridge — Universal Live Synchronization Engine
Between Antigravity (Gemini) and Claude (Anthropic)
Owner: Eng. Abdelaziz Osman (أبو ياسين)
"""

import os
import sys
import json
import time
import glob
import argparse
import subprocess
from datetime import datetime

WORKSPACE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SYNC_MD_PATH = os.path.join(WORKSPACE_DIR, "LIVE_AGENT_SYNC.md")
BUS_JSON_PATH = os.path.join(os.path.dirname(__file__), "agent_bus.json")
LOCKS_JSON_PATH = os.path.join(os.path.dirname(__file__), "project_locks.json")

BRAIN_DIR = os.path.expanduser(r"~\.gemini\antigravity\brain")
CLAUDE_PROJECTS_DIR = os.path.expanduser(r"~\.claude\projects")


def init_bus():
    if not os.path.exists(BUS_JSON_PATH):
        data = {
            "version": "1.0",
            "last_updated": datetime.now().isoformat(),
            "messages": [],
            "recent_actions": {
                "antigravity": {"last_action": "Initialized sync bridge", "timestamp": datetime.now().isoformat()},
                "claude": {"last_action": "Awaiting initial prompt", "timestamp": datetime.now().isoformat()}
            }
        }
        with open(BUS_JSON_PATH, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)


def get_bus_data():
    init_bus()
    try:
        with open(BUS_JSON_PATH, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return {"messages": [], "recent_actions": {}}


def save_bus_data(data):
    data["last_updated"] = datetime.now().isoformat()
    with open(BUS_JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


def get_git_status():
    try:
        res = subprocess.run(
            ["git", "status", "--short"],
            cwd=WORKSPACE_DIR,
            capture_output=True,
            text=True,
            timeout=5
        )
        return res.stdout.strip()
    except Exception:
        return "Git not available or clean"


def get_git_last_commit():
    try:
        res = subprocess.run(
            ["git", "log", "-1", "--oneline"],
            cwd=WORKSPACE_DIR,
            capture_output=True,
            text=True,
            timeout=5
        )
        return res.stdout.strip()
    except Exception:
        return "No commits yet"


def get_latest_antigravity_snippet():
    """Extracts the latest user prompt or action from Antigravity's transcript."""
    try:
        transcript_files = glob.glob(os.path.join(BRAIN_DIR, "*", ".system_generated", "logs", "transcript.jsonl"))
        if not transcript_files:
            return "No Antigravity transcripts found."
        latest_file = max(transcript_files, key=os.path.getmtime)
        with open(latest_file, "r", encoding="utf-8", errors="ignore") as f:
            lines = [line.strip() for line in f if line.strip()]
            for line in reversed(lines[-20:]):
                try:
                    entry = json.loads(line)
                    if entry.get("source") == "USER_EXPLICIT" and entry.get("type") == "USER_INPUT":
                        content = entry.get("content", "")
                        if content:
                            return content[:300].strip()
                    elif entry.get("type") == "PLANNER_RESPONSE" and entry.get("content"):
                        content = entry.get("content", "")
                        if content:
                            return content[:300].strip()
                except Exception:
                    continue
        return "Transcript active."
    except Exception as e:
        return f"Error reading Antigravity transcript: {e}"


def get_latest_claude_snippet():
    """Extracts the latest user prompt or action from Claude's transcript."""
    try:
        claude_files = glob.glob(os.path.join(CLAUDE_PROJECTS_DIR, "*", "*.jsonl"))
        if not claude_files:
            return "No Claude Code transcripts found yet."
        latest_file = max(claude_files, key=os.path.getmtime)
        with open(latest_file, "r", encoding="utf-8", errors="ignore") as f:
            lines = [line.strip() for line in f if line.strip()]
            for line in reversed(lines[-20:]):
                try:
                    entry = json.loads(line)
                    msg = entry.get("message", {})
                    if isinstance(msg, dict):
                        content = msg.get("content", "")
                        if isinstance(content, str) and content:
                            return content[:300].strip()
                except Exception:
                    continue
        return "Claude transcript active."
    except Exception as e:
        return f"Error reading Claude transcript: {e}"


def render_live_markdown():
    bus = get_bus_data()
    git_status = get_git_status()
    git_commit = get_git_last_commit()
    agy_snippet = get_latest_antigravity_snippet()
    claude_snippet = get_latest_claude_snippet()

    recent_msgs = bus.get("messages", [])[-5:]

    md = f"""# 📡 LIVE AGENT SYNC BUS — Antigravity ⇄ Claude
> **Live Synchronization & Handoff Channel**
> **Workspace:** `{WORKSPACE_DIR}`
> **Lead Developer:** المهندس عبد العزيز عثمان (**أبو ياسين**)
> **Last Synchronized:** `{datetime.now().strftime("%Y-%m-%d %H:%M:%S")}`

---

## 🚦 System Status & Concurrency
- **Git HEAD:** `{git_commit}`
- **Working Tree Status:** `{"Clean" if not git_status else "Modified files present"}`
{f"```text\n{git_status}\n```" if git_status else ""}

---

## 💬 Shared Direct Message Stream (آخر رسائل متبادلة بين الوكيلين)
"""
    if recent_msgs:
        for m in reversed(recent_msgs):
            sender = m.get("sender", "Unknown").upper()
            ts = m.get("timestamp", "")
            text = m.get("message", "")
            md += f"- **[{ts}] {sender}:** {text}\n"
    else:
        md += "- *لا توجد رسائل مباشرة مسجلة بعد. استخدم `python AgyClaudeBridge/bridge.py post <agent> <message>` لإرسال رسالة.* \n"

    md += f"""
---

## 🤖 Latest Context from Antigravity (Gemini)
- **Last Active Focus:**
> {agy_snippet}

---

## 🧠 Latest Context from Claude (Anthropic)
- **Last Active Focus:**
> {claude_snippet}

---

## 📌 Instructions for Both Agents
1. **Always read this file (`LIVE_AGENT_SYNC.md`)** before beginning any new generation or coding turn.
2. **If you modify code**, immediately log your update:
   `python AgyClaudeBridge/bridge.py post <gemini|claude> "ملخص التعديل"`
3. **Check the Working Tree Status** above before editing to ensure you do not overwrite concurrent work.
"""

    with open(SYNC_MD_PATH, "w", encoding="utf-8") as f:
        f.write(md)


def post_message(sender, message):
    bus = get_bus_data()
    entry = {
        "sender": sender,
        "message": message,
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }
    bus.setdefault("messages", []).append(entry)
    if sender.lower() in ["gemini", "antigravity"]:
        bus.setdefault("recent_actions", {})["antigravity"] = {"last_action": message, "timestamp": datetime.now().isoformat()}
    elif sender.lower() == "claude":
        bus.setdefault("recent_actions", {})["claude"] = {"last_action": message, "timestamp": datetime.now().isoformat()}
    save_bus_data(bus)
    render_live_markdown()
    print(f"[OK] Message posted from {sender}: {message}")


def watch_daemon(interval=5):
    print(f"[*] AgyClaudeBridge Daemon running... Monitoring every {interval}s")
    print(f"[*] Workspace: {WORKSPACE_DIR}")
    print(f"[*] Live sync file: {SYNC_MD_PATH}")
    last_mtime = 0
    try:
        while True:
            render_live_markdown()
            time.sleep(interval)
    except KeyboardInterrupt:
        print("\n[*] Daemon stopped.")


def main():
    parser = argparse.ArgumentParser(description="AgyClaudeBridge — Universal Live Sync Engine")
    subparsers = parser.add_subparsers(dest="command")

    # sync command
    subparsers.add_parser("sync", help="Force render LIVE_AGENT_SYNC.md")

    # post command
    post_parser = subparsers.add_parser("post", help="Post a message or update to the shared stream")
    post_parser.add_argument("sender", choices=["gemini", "antigravity", "claude", "user"], help="Sender identifier")
    post_parser.add_argument("message", help="Message or task summary")

    # watch command
    watch_parser = subparsers.add_parser("watch", help="Run background watcher daemon")
    watch_parser.add_argument("--interval", type=int, default=5, help="Check interval in seconds")

    args = parser.parse_args()

    if args.command == "post":
        post_message(args.sender, args.message)
    elif args.command == "watch":
        watch_daemon(args.interval)
    else:
        render_live_markdown()
        print(f"[OK] Rendered {SYNC_MD_PATH}")


if __name__ == "__main__":
    main()
