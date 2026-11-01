# -*- coding: utf-8 -*-
"""
talk.py — The Interactive Dual-Agent Council Tool (Antigravity ⇄ Claude)
Chief Architect & Owner: المهندس عبد العزيز عثمان (أبو ياسين)

Usage:
  python talk.py                 -> Interactive Menu
  python talk.py prompt          -> Copy and print prompt for Claude
  python talk.py view            -> Open live Council Viewer in browser
  python talk.py claude          -> Ingest Claude's turn, build code, and sync
  python talk.py status          -> Display current dialogue status
"""

import os
import sys
import json
import re
import subprocess
import webbrowser
from datetime import datetime

WORKSPACE_DIR = os.path.dirname(os.path.abspath(__file__))
BRIDGE_DIR = os.path.join(WORKSPACE_DIR, "AgyClaudeBridge")
sys.path.insert(0, BRIDGE_DIR)
import council

DIALOGUE_RAW_URL = "https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/AI_COGNITIVE_DIALOGUE.md"
BRAIN_RAW_URL = "https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/AI_SHARED_BRAIN.md"
VIEWER_HTML = os.path.join(BRIDGE_DIR, "CouncilViewer.html")


def copy_to_clipboard(text):
    try:
        cmd = f"Set-Clipboard -Value @'\n{text}\n'@"
        subprocess.run(["powershell", "-NoProfile", "-Command", cmd], check=True, timeout=5)
        return True
    except Exception:
        return False


def get_claude_prompt():
    state = council.load_state()
    turns = state.get("turns", [])
    turn_num = len(turns) + 1
    
    prompt = f"""أهلاً يا Claude، أنا المهندس عبد العزيز عثمان (أبو ياسين).
المستودع العام متاح ومفهرس بالكامل على GitHub على الرابط:
https://github.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace

وقد بدأ زميلك Antigravity (Google Gemini) أولى جلسات "مجلس الحوار الفكري المشترك بينكما" (Turn #1) لمناقشة المشاريع وتطوير أداة تدقيق كود البناء السعودي ومنصة بلدي SbcBaladyChecker.

المطلوب منك الآن قراءة المداخلة الافتتاحية لزميلك Antigravity عبر الرابط المباشر التالي:
{DIALOGUE_RAW_URL}

وكذلك يمكنك مراجعة العقل المشترك وميثاق العمل الدائم عبر:
{BRAIN_RAW_URL}

وكود القواعد الحالي وموديل البيانات من الروابط التالية:
- https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/SbcBaladyChecker/Source/Core/Models.cs
- https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/SbcBaladyChecker/Source/Rules/HabitableRoomsRule.cs
- https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/SbcBaladyChecker/Source/Rules/SetbackAndZoningRule.cs

الآن، وجه ردك ومداخلتك مباشرة إلى زميلك Antigravity (في صورة Turn #2) متضمناً:
1. تحليلك المعماري والهندسي للأسئلة الثلاثة التي طرحها عليك حول حساب نسبة البناء بالأرضي (Coverage Ratio) ومعامل البناء (FAR) واستخراج مساحة الأرض ومحيط المبنى من الريفيت.
2. كود C# كامل ومتوافق لمعمارية كلاس BaladyFARAndCoverageRule.cs الذي يرث من ISbcRule، لكي يقوم زميلك Antigravity بترجمته وتجميعه واختباره فوراً داخل الريفيت (.NET 10.0 / Revit 2026-2027).
3. سؤالك أو توجيهك لزميلك Antigravity للخطوة التالية.

تحدث معه كمهندس ند لند في مجلس الحوار المشترك!"""
    return prompt


def show_prompt():
    prompt = get_claude_prompt()
    copied = copy_to_clipboard(prompt)
    print("=" * 70)
    print("📋 تم تجهيز رسالة التكليف الخاصة بـ Claude!")
    if copied:
        print("✅ تم نسخ الرسالة تلقائياً إلى الحافظة (Clipboard) — الصقها في شات Claude مباشرة!")
    else:
        print("⚠️ يرجى نسخ الرسالة التالية ولصقها في شات Claude:")
    print("=" * 70)
    print(prompt)
    print("=" * 70)


def view_council():
    if os.path.exists(VIEWER_HTML):
        print(f"🌐 فتح شاشة العرض المرئية للمجلس في المتصفح...")
        webbrowser.open(f"file:///{VIEWER_HTML}")
    else:
        print("❌ لم يتم العثور على CouncilViewer.html")


def ingest_claude_response():
    print("=" * 70)
    print("📥 استقبال مداخلة Claude (Turn الجديد)")
    print("=" * 70)
    print("الصق رد Claude هنا، وعند الانتهاء اكتب 'END' في سطر منفصل واضغط Enter:")
    
    lines = []
    while True:
        try:
            line = input()
            if line.strip() == "END":
                break
            lines.append(line)
        except EOFError:
            break
            
    content = "\n".join(lines).strip()
    if not content:
        print("⚠️ لم يتم إدخال أي نص.")
        return

    # Add turn
    council.add_turn("claude", content, auto_git=True)
    print("✅ تم تسجيل مداخلة Claude في المجلس ومزامنتها على GitHub!")

    # Check for C# code
    csharp_matches = re.findall(r"```(?:csharp|c#)?(.*?)```", content, re.DOTALL | re.IGNORECASE)
    if csharp_matches:
        for block in csharp_matches:
            block = block.strip()
            if "class BaladyFARAndCoverageRule" in block or "ISbcRule" in block:
                target_file = os.path.join(WORKSPACE_DIR, "SbcBaladyChecker", "Source", "Rules", "BaladyFARAndCoverageRule.cs")
                with open(target_file, "w", encoding="utf-8") as f:
                    f.write(block)
                print(f"💾 تم استخراج كود القاعدة وحفظه في: {target_file}")
                
                # Try dotnet build
                print("🔨 جاري بناء وترجمة المشروع عبر .NET SDK...")
                proj_path = os.path.join(WORKSPACE_DIR, "SbcBaladyChecker", "SbcBaladyChecker.csproj")
                res = subprocess.run(["dotnet", "build", proj_path, "-c", "Debug"], capture_output=True, text=True)
                if res.returncode == 0:
                    print("🎉 نجح البناء بنسبة 100%! تم التحقق من سلامة كود Claude!")
                else:
                    print("⚠️ ظهرت أخطاء تجميع:")
                    print(res.stdout[-800:])


def show_status():
    state = council.load_state()
    topic = state.get("topic", "")
    turns = state.get("turns", [])
    print("=" * 70)
    print(f"🏛️ المجلس المشترك: Antigravity ⇄ Claude")
    print(f"📌 الموضوع: {topic}")
    print(f"🔄 إجمالي المداخلات: {len(turns)}")
    for idx, t in enumerate(turns, start=1):
        ag = t.get("agent")
        name = "🤖 Antigravity" if ag in ["gemini", "antigravity"] else "🧠 Claude"
        ts = t.get("timestamp")
        print(f"   - Turn #{idx}: {name} [{ts}]")
    print("=" * 70)


def main():
    if len(sys.argv) > 1:
        cmd = sys.argv[1].lower()
        if cmd == "prompt":
            show_prompt()
            return
        elif cmd == "view":
            view_council()
            return
        elif cmd == "claude":
            ingest_claude_response()
            return
        elif cmd == "status":
            show_status()
            return

    # Interactive loop
    while True:
        print("\n" + "=" * 50)
        print("🏛️ أداة الحوار المشترك (The Dual-Agent Council)")
        print("=" * 50)
        print("[1] 📋 نسخ رسالة التكليف إلى Claude (مع روابط GitHub المباشرة)")
        print("[2] 📥 إدخال رد Claude وحفظ الكود وبنائه تلقائياً")
        print("[3] 🌐 فتح شاشة العرض المرئية للمجلس (Council Viewer)")
        print("[4] 📊 عرض ملخص المداخلات وحالة المجلس")
        print("[0] 🚪 خروج")
        choice = input("\nاختر رقماً (0-4): ").strip()
        
        if choice == "1":
            show_prompt()
        elif choice == "2":
            ingest_claude_response()
        elif choice == "3":
            view_council()
        elif choice == "4":
            show_status()
        elif choice == "0":
            break
        else:
            print("خيار غير صحيح.")


if __name__ == "__main__":
    main()
