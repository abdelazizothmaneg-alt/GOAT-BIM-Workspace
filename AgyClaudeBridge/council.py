"""
council.py — The Dual-Agent Cognitive Council Engine
Facilitates structured, autonomous debate & collaboration between Antigravity (Gemini) and Claude (Anthropic).
Owner: Eng. Abdelaziz Osman (أبو ياسين)
"""

import os
import sys
import json
import argparse
import subprocess
from datetime import datetime

WORKSPACE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
COUNCIL_MD = os.path.join(WORKSPACE_DIR, "AI_COGNITIVE_DIALOGUE.md")
COUNCIL_JSON = os.path.join(os.path.dirname(__file__), "council_state.json")
VIEWER_HTML = os.path.join(os.path.dirname(__file__), "CouncilViewer.html")

RAW_URL = "https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/AI_COGNITIVE_DIALOGUE.md"


def load_state():
    if os.path.exists(COUNCIL_JSON):
        try:
            with open(COUNCIL_JSON, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            pass
    return {
        "session_id": datetime.now().strftime("COUNCIL-%Y%m%d-%H%M"),
        "topic": "General Architectural & Computational BIM Strategy",
        "last_updated": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "turns": []
    }


def save_state(state):
    state["last_updated"] = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(COUNCIL_JSON, "w", encoding="utf-8") as f:
        json.dump(state, f, ensure_ascii=False, indent=2)


def render_markdown(state):
    turns = state.get("turns", [])
    topic = state.get("topic", "Architectural Alignment")
    sid = state.get("session_id", "ACTIVE")
    last_up = state.get("last_updated", "")

    md = f"""# 🏛️ THE DUAL-AGENT COUNCIL — مجلس الحوار المعماري والبرمجي
> **Continuous Peer-to-Peer Dialogue: Antigravity (Gemini) ⇄ Claude (Anthropic)**
> **Owner & Chief Architect:** المهندس عبد العزيز عثمان (**أبو ياسين**)
> **Active Topic:** {topic}
> **Session ID:** `{sid}` | **Last Updated:** `{last_up}`
> **Live Raw Link:** `{RAW_URL}`

---

## 🎯 جدول أعمال المجلس الحالي (Agenda & Goals)
1. مراجعة وتقييم ما تم إنجازه حتى الآن عبر مشاريعنا الخمسة (Revit, 3ds Max, AutoCAD, Web 3D, GCC Proposals).
2. مناقشة المعمارية الفنية لأداة **`SbcBaladyChecker`** وكيفية صياغة كود `BaladyFARAndCoverageRule.cs`.
3. التوافق على الخطوات القادمة وحل أي معضلات هندسية قبل التنفيذ الفعلي.

---

## 📜 سجل النقاش والحوار المباشر (Verbatim Dialogue Stream)

"""
    if not turns:
        md += "*المجلس مفتوح وبانتظار المداخلة الافتتاحية...*\n"
    else:
        for idx, t in enumerate(turns, start=1):
            agent = t.get("agent", "Unknown")
            name = "🤖 Antigravity (Google Gemini)" if agent.lower() in ["gemini", "antigravity"] else "🧠 Claude (Anthropic)"
            ts = t.get("timestamp", "")
            content = t.get("content", "")

            md += f"### {name} — *Turn #{idx} ({ts})*\n\n"
            md += f"{content}\n\n"
            md += "---\n\n"

    md += """## ⚡ تعليمات التفاعل للوكيلين (Council Rules)
1. كل وكيل يخاطب زميله مباشرة كمهندس ند لند (Peer-to-Peer Architectural Discussion).
2. يجب أن تتضمن كل مداخلة: (أ) تقييم للوضع الراهن، (ب) طرح الحلول الهندسية المقترحة، (ج) سؤالاً أو طلباً محدداً للطرف الآخر للرد عليه.
3. التوافق النهائي في المجلس يتحول تلقائياً إلى كود يتم بناؤه وتجميعه على جهاز المهندس عبد العزيز.
"""

    with open(COUNCIL_MD, "w", encoding="utf-8") as f:
        f.write(md)


def render_html_viewer(state):
    turns = state.get("turns", [])
    topic = state.get("topic", "AEC Dual-Agent Strategy")
    last_up = state.get("last_updated", "")

    html_turns = ""
    for idx, t in enumerate(turns, start=1):
        agent = t.get("agent", "Unknown").lower()
        is_gemini = agent in ["gemini", "antigravity"]
        avatar = "🤖" if is_gemini else "🧠"
        title = "Antigravity (Gemini)" if is_gemini else "Claude (Anthropic)"
        badge_class = "badge-gemini" if is_gemini else "badge-claude"
        align_class = "msg-gemini" if is_gemini else "msg-claude"
        ts = t.get("timestamp", "")
        body_raw = json.dumps(t.get("content", ""))

        html_turns += f"""
        <div class="message-card {align_class}">
            <div class="msg-header">
                <span class="avatar">{avatar}</span>
                <span class="name">{title}</span>
                <span class="badge {badge_class}">Turn #{idx}</span>
                <span class="timestamp">{ts}</span>
            </div>
            <div class="msg-body" id="msg-{idx}"></div>
            <script>
                document.getElementById('msg-{idx}').innerHTML = marked.parse({body_raw});
            </script>
        </div>
        """

    html = f"""<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="refresh" content="8">
    <title>The Dual-Agent Council — غرفة الحوار المباشر</title>
    <link href="https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700&family=JetBrains+Mono&display=swap" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
    <style>
        body {{
            background-color: #0B0D10;
            color: #F8FAFC;
            font-family: 'Cairo', sans-serif;
            margin: 0;
            padding: 24px;
        }}
        .header {{
            background: #14171F;
            border: 1px solid #202634;
            border-radius: 12px;
            padding: 20px 24px;
            margin-bottom: 24px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .header h1 {{ margin: 0; font-size: 1.5rem; color: #C5A880; }}
        .header p {{ margin: 4px 0 0; color: #94A3B8; font-size: 0.9rem; }}
        .status-pill {{
            background: #10B98122;
            color: #10B981;
            border: 1px solid #10B98144;
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 600;
        }}
        .chat-container {{
            display: flex;
            flex-direction: column;
            gap: 20px;
            max-width: 1050px;
            margin: 0 auto;
        }}
        .message-card {{
            background: #14171F;
            border: 1px solid #202634;
            border-radius: 12px;
            padding: 24px;
            box-shadow: 0 4px 16px rgba(0,0,0,0.4);
            line-height: 1.8;
        }}
        .msg-gemini {{ border-right: 5px solid #38BDF8; }}
        .msg-claude {{ border-right: 5px solid #F59E0B; }}
        .msg-header {{
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 14px;
            border-bottom: 1px solid #1B202A;
            padding-bottom: 10px;
        }}
        .avatar {{ font-size: 1.5rem; }}
        .name {{ font-weight: 700; font-size: 1.1rem; }}
        .badge {{
            padding: 3px 12px;
            border-radius: 12px;
            font-size: 0.78rem;
            font-weight: 700;
        }}
        .badge-gemini {{ background: #38BDF822; color: #38BDF8; }}
        .badge-claude {{ background: #F59E0B22; color: #F59E0B; }}
        .timestamp {{ margin-right: auto; color: #64748B; font-size: 0.82rem; font-family: 'JetBrains Mono', monospace; }}
        .msg-body {{ color: #CBD5E1; font-size: 0.96rem; }}
        pre, code {{ font-family: 'JetBrains Mono', monospace; background: #0E1116; border-radius: 6px; }}
        pre {{ padding: 14px; overflow-x: auto; border: 1px solid #202634; }}
        code {{ padding: 2px 6px; color: #38BDF8; }}
        pre code {{ color: #E2E8F0; padding: 0; }}
        blockquote {{ border-right: 3px solid #C5A880; margin: 12px 0; padding: 4px 16px; color: #94A3B8; background: #0B0D1088; }}
        h1, h2, h3, h4 {{ color: #F1F5F9; margin-top: 18px; margin-bottom: 8px; }}
        ul, ol {{ padding-right: 24px; }}
        li {{ margin-bottom: 6px; }}
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1>🏛️ مجلس الحوار الفكري المشترك (Antigravity ⇄ Claude)</h1>
            <p>الموضوع: {topic} | آخر تحديث: {last_up} (تحديث تلقائي كل 8 ثوانٍ)</p>
        </div>
        <div class="status-pill">● متصل ومتزامن سحابياً</div>
    </div>
    <div class="chat-container">
        {html_turns}
    </div>
</body>
</html>
"""
    with open(VIEWER_HTML, "w", encoding="utf-8") as f:
        f.write(html)


def add_turn(agent, content, auto_git=True):
    state = load_state()
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    turn = {
        "agent": agent,
        "content": content,
        "timestamp": ts
    }
    state.setdefault("turns", []).append(turn)
    save_state(state)
    render_markdown(state)
    render_html_viewer(state)
    print(f"[OK] Added turn #{len(state['turns'])} from {agent}")

    if auto_git:
        try:
            subprocess.run(["git", "add", "AI_COGNITIVE_DIALOGUE.md", "AgyClaudeBridge/council_state.json", "AgyClaudeBridge/CouncilViewer.html"], cwd=WORKSPACE_DIR, check=True)
            subprocess.run(["git", "commit", "-m", f"council: turn #{len(state['turns'])} by {agent} [{ts}]"], cwd=WORKSPACE_DIR, check=True)
            subprocess.run(["git", "push", "origin", "main"], cwd=WORKSPACE_DIR, check=True)
            print("[OK] Pushed turn to GitHub. Raw dialogue link updated.")
        except Exception as e:
            print(f"[WARN] Git push: {e}")


def main():
    parser = argparse.ArgumentParser(description="Council Engine")
    subparsers = parser.add_subparsers(dest="command")

    # turn command
    t_parser = subparsers.add_parser("turn", help="Record a turn in the dialogue")
    t_parser.add_argument("agent", choices=["gemini", "antigravity", "claude"], help="Speaking agent")
    t_parser.add_argument("content", help="Speech content / message")

    # start command
    s_parser = subparsers.add_parser("start", help="Start new topic")
    s_parser.add_argument("topic", help="Topic title")

    # render command
    subparsers.add_parser("render", help="Re-render markdown and html")

    args = parser.parse_args()

    if args.command == "turn":
        add_turn(args.agent, args.content)
    elif args.command == "start":
        state = load_state()
        state["topic"] = args.topic
        state["turns"] = []
        save_state(state)
        render_markdown(state)
        render_html_viewer(state)
        print(f"[OK] Started new topic: {args.topic}")
    else:
        state = load_state()
        render_markdown(state)
        render_html_viewer(state)
        print(f"[OK] Rendered council files.")


if __name__ == "__main__":
    main()
