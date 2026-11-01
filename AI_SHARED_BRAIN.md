# 🧠 AI SHARED BRAIN — Antigravity (Gemini) ⇄ Claude (Anthropic)
> **Autonomous Dual-Agent Cognitive Workspace & Handoff Protocol**
> **Repository:** [abdelazizothmaneg-alt/GOAT-BIM-Workspace](https://github.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace)
> **Owner & Lead Architect:** المهندس عبد العزيز عثمان (**أبو ياسين**)
> **Live Raw Link:** `https://raw.githubusercontent.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/main/AI_SHARED_BRAIN.md`

---

## 🎯 1. Current Strategic Focus (الهدف الحالي المشترك)
- **Active Sprint:** تطوير وتوسيع مشروع **`SbcBaladyChecker`** وإضافة القواعد البلدية ومعايير كود البناء السعودي (SBC 201 معماري و SBC 801 حريق).
- **Active Task:** برمجة كلاس القاعدة البلدية الجديد: `BaladyFARAndCoverageRule.cs`
- **GitHub Issue Link:** [Issue #1 — Implement BaladyFARAndCoverageRule](https://github.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/issues/1)
- **Active Turn:** 🧠 **Claude** (صياغة ومراجعة الكود المعماري واللوائح).

---

## 👥 2. تقاسم الأدوار بين الوكيلين (Division of Intelligence)

| الوكيل الذكي (Agent) | البيئة (Environment) | التخصص ونقاط القوة (Domain Focus) |
| :--- | :--- | :--- |
| **🧠 Claude** | سحابي (Cloud / Web / Desktop) | **التخطيط المعماري، تدقيق لوائح الكود السعودي، مراجعة الكود واكتشاف الحالات الحدية (Edge Cases)، وصياغة كلاسات C# نظيفة.** |
| **🤖 Antigravity (Gemini)** | محلي على جهاز المهندس (Local OS) | **التنفيذ الفعلي، تشغيل وبناء الأكواد (`dotnet build`), الربط مع Revit API 2026/2027، إدارة Git، والتحكم في الملفات والطرفية.** |

---

## 💬 3. شريط الحوار الفكري الحي (Live Cognitive Dialogue)

### 🤖 [Antigravity ➔ Claude] — *2026-11-01 19:30*
> "أهلاً كلود. مستودعنا المشترك أصبح الآن **عاماً (Public)** ومتاحاً لك بالكامل على السحابة:
> `https://github.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace`
> 
> كل ملفات مشاريع المهندس عبد العزيز (GOAT BIM Suite, SbcBaladyChecker, CAD-AI, MaxToRevit, WebGL viewer) وأرشيف الـ 43 محادثة سابقة أصبحت بين يديك.
> لقد فتحت لك مهمة العمل رقم 1 على GitHub ([Issue #1](https://github.com/abdelazizothmaneg-alt/GOAT-BIM-Workspace/issues/1)).
> 
> المطلوب منك:
> 1. طالع واجهة `ISbcRule` في `SbcBaladyChecker/Source/Core/Models.cs`.
> 2. اكتب كود قاعدة `BaladyFARAndCoverageRule.cs` بالكامل لحساب نسبة البناء الإجمالية (FAR) وتغطية الأرض (Coverage %) مع مطابقتها لمعايير بلدي.
> 3. أرسل الكود داخل وسام كود C# نظيف، وسأتولى أنا تنزيله، وبنائه في المشروع فوراً، وتحديث نتيجة الـ Build هنا في شاشة الذاكرة المشتركة."

### 🧠 [Claude ➔ Antigravity] — *بانتظار رد كلود*
> *(هنا سيتم تسجيل تحليل كلود للكود والتوصيات المعمارية فور إرسالها)*

---

## 📐 4. المعايير الهندسية للقاعدة المطلوبة (`BaladyFARAndCoverageRule`)
- **اسم الكلاس:** `BaladyFARAndCoverageRule`
- **الواجهة:** ترث من `ISbcRule`
- **الحسابات المطلوبة:**
  1. **نسبة تغطية الأرض (Ground Coverage Ratio - GCR / Footprint %):**
     $$\text{GCR} = \frac{\text{مساحة الدور الأرضي}}{\text{مساحة الأرض الكلية}} \times 100$$
     - فيلا سكنية: الحد الأقصى الافتراضي 60% (مع إمكانية الزيادة حتى 70% بنظام الارتدادات المحدث).
     - عمائر سكنية: 65%.
     - تجاري ومختلط: 70%.
  2. **معامل البناء الإجمالي (Floor Area Ratio - FAR):**
     $$\text{FAR} = \frac{\text{إجمالي المسطحات المبنية في كافة الأدوار}}{\text{مساحة الأرض}}$$
     - فيلا سكنية: أقصى حد 2.4.
     - عمائر سكنية: أقصى حد 3.0.
     - تجاري: 4.0.
  3. **المخرجات:** توليد عناصر `AuditResultItem` مع تصنيف `RuleSeverity.Compliant` أو `Warning` أو `Violation`.

---

## ⚡ 5. بروتوكول التبادل التلقائي (Zero-Friction Protocol)
1. **كلود:** يكتب الكود المطلوب أو المراجعة المعمارية.
2. **Antigravity:** يأخذ الكود ويضعه في مكانه المحدد في المشروع (`SbcBaladyChecker/Source/Rules/BaladyFARAndCoverageRule.cs`).
3. **الفحص الآلي:** يقوم Antigravity بتشغيل `dotnet build` والتأكد من انعدام أخطاء التجميع.
4. **التحديث السحابي:** يعمل Antigravity فوراً `git commit` و `git push`، فتتحدث هذه الشاشة على السحابة، ويراها كلود محدثة على الرابط المباشر.
