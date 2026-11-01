# AGENTS.md - قواعد مشتركة (Claude Code + Antigravity/Gemini)

المالك: م. عبد العزيز عثمان (أبو ياسين) - Architectural & BIM Automation Developer.
قواعد Revit API وقواعد التصميم التفصيلية موجودة في `CLAUDE.md` - اقراه قبل أي شغل.

## نظام العمل (الأهم)
- كل أداة تشتغل في worktree منفصل ومعاه branch خاص بيه:
  - Claude Code  -> `..\scratch-claude`  (branch: claude/work)
  - Antigravity  -> `..\scratch-gemini`  (branch: gemini/work)
- ممنوع تعديل `scratch` الرئيسي مباشرة. الدمج بيتم بإيد المهندس فقط بعد مراجعة `git diff`.
- ممنوع: `git push`، `git reset --hard`، `git clean`، حذف مجلدات، أو لمس ملفات خارج المجلد.
- Commit صغير ومتكرر برسالة واضحة. ممنوع الشغل على أكتر من مشروع في نفس الـ commit.

## تقسيم الملكية (يتعدل بإيد المهندس)
- GOAT BIM Suite (C# / Revit): Claude
- goat-bim-web (Next.js / Three.js): Antigravity
- MaxToRevit Bridge Pro, CAD-AI, SbcBaladyChecker: يتحدد قبل بداية كل مهمة
- لو المهمة بتلمس مشروع تاني: اسأل المهندس الأول.

## قواعد Revit (ملخص - التفاصيل في CLAUDE.md)
- Revit 2026 = .NET 8 (`net8.0-windows`)، Revit 2027 = .NET 10 (`net10.0-windows`). المشروع multi-target.
- مراجع الـ API من NuGet: `Nice3point.Revit.Api.RevitAPI` و `RevitAPIUI` بإصدار `$(RevitVersion).*`.
- تعديل الموديل جوه `Transaction` فقط، وأي شغل من UI/خيط تاني عبر `ExternalEvent`.
- للتطوير: نشر الـ addin في `%APPDATA%\Autodesk\Revit\Addins\<السنة>` (مش Program Files).
- اختبار على موديل تجريبي فقط، مش مشروع حقيقي.

## قاعدة Mockup أولاً
قبل كتابة أي واجهة أو أداة جديدة: اعمل Mockup تفاعلي (HTML) واعرضه على المهندس وانتظر الموافقة. بعد الموافقة بس ابدأ الكود.

## سجل التعديلات (إجباري)
بعد أي تعديل ملفات، أضف سطر في `PROJECTS_SHARED_CHANGELOG.md` بالصيغة:
`| YYYY-MM-DD HH:mm | Claude أو Gemini | المشروع | الملفات | الوصف |`
وماتقفلش المهمة قبل ما السطر يتكتب.