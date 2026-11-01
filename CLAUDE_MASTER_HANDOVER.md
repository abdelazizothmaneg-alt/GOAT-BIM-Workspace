# CLAUDE MASTER HANDOVER & PROJECT DOSSIER
**وثيقة التسليم والربط الشاملة بين Antigravity و Claude**
**المالك والمطور الرئيسي:** المهندس عبد العزيز عثمان (**أبو ياسين** / Eng. Abdelaziz Osman)
**المسار الفعلي لبيئة العمل المشتركة:** `C:\Users\faqih\.gemini\antigravity\scratch\`

---

## 🎯 1. ملخص الهدف (Executive Summary)
هذه الوثيقة تجمع كل ما تم إنجازه، وجميع المشاريع، والقرارات المعمارية والبرمجية، وسجل المحادثات، لتمكين **Claude** من الاتصال بنفس بيئة العمل والمشاريع التي تم بناؤها في **Antigravity (Gemini)**، والعمل معاً بشكل متزامن ومتكامل في نفس الملفات والأكواد.

---

## 👤 2. هوية المطور ومجال العمل (Developer Profile)
- **المهندس:** عبد العزيز عثمان (أبو ياسين).
- **المسمى المهني:** Senior Architectural Engineer & BIM Automation Developer.
- **التخصص الدقيق:** تطوير إضافات الأوتوديسك (Revit Add-ins بالـ C# و .NET 8/10 و WPF)، والربط بين 3ds Max و Revit و AutoCAD، وأتمتة الكود السعودي (SBC) ولوائح بلدي، والتصميم المعماري والحسابات الكمية.
- **أسلوب الصياغة والتواصل:** لغة عربية فصحى معمارية راقية، دعم كامل لتنسيقات اليمين لليسار (RTL)، والمصطلحات الهندسية الدقيقة.

---

## 📂 3. فهرس المشاريع المنجزة وحالتها الحالية (Active Projects Inventory)

### 1) أداة GOAT BIM Architectural Suite (Revit 2026 / 2027)
- **المجلد:** `C:\Users\faqih\.gemini\antigravity\scratch\GOAT_BIM_Suite_2026_2027\` (والمجلد التوأم `ORTools\`)
- **التقنية:** C# / .NET 10.0-windows / WPF Modeless / Revit API 2026-2027.
- **الخصائص والقرارات الأساسية:**
  - تم ترقية وتغيير الاسم من `OR Tools` إلى **`GOAT BIM`**.
  - **15 أداة معمارية متكاملة** موزعة على 6 Panels في شريط الريفيت (Ribbon):
    1. **GOAT Setup:** `LEVELS` (توليد المناسيب والبلانات), `GRIDS` (المحاور المتعامدة والقطرية والأوتودايمنشن).
    2. **GOAT Data:** `PARAMETER` (فلترة وتعديل البارامترات وتلوين الفيو Color Splash), `SHEETLINK` (محرر إكسل ثنائي الاتجاه بدون برامج خارجية), `RENUMBER` (إعادة الترقيم الذكي فراغياً للأبواب والغرف مع تعديل الأسماء مباشرة), `BOQ` (حصر الكميات المتعدد بالمعادلات).
    3. **GOAT Finishes:** `FINISHES` (تشطيبات الحوائط وتكسيات الأعمدة والوزرات والأرضيات والأسقف بـ 6 تبويبات), `ARCH` (7 أشكال أقواس معمارية بارامترية وقواطع دواليب).
    4. **GOAT Ceiling:** `CEILING` (توزيع أسقف وبلاطات 600×600 ووحدات إنارة ومخارج تكييف ورشاشات حريق).
    5. **GOAT Documentation:** `D VIEW` و `W VIEW` (لوحات ونماذج الأبواب والشبابيك), `ROOM` (حزمة توثيق الغرف: بلان مكبر، RCP، 4 واجهات داخلية، 3D آيزو، وتلوين الغرف بالاسم مع إمكانية تعديل الأسماء والأرقام), `DIM` (دايمنشن داخلي ذكي لا يقطع التاج، دايمنشن خارجي، ومناسيب), `ANNOTATE` (وضع التاجات الذكية مع منع التداخل الدوامي).
    6. **GOAT Delivery:** `ALIGNER` (محاذاة الفيو بورتات وتوزيع الشيتات), `SHEETS` (تصدير مجمع PDF/DWG/IFC/NWC وفق ISO 19650).
  - **الاعتمادية:** صفر مكتبات خارجية (Self-contained OpenXML Engine لحفظ وقراءة الإكسل `GoatExcelEngine.cs`).
  - **الأمان:** جميع العمليات مغلفة بـ `TransactionGroup` مدمجة (`Assimilate`) لتمكين **One Ctrl+Z**.
  - **ملاحظة قرار معماري:** تم حذف زر ونافذة `About` نهائياً بناءً على طلب المطور.

### 2) محول الماكس للريفيت (MaxToRevit Bridge Pro)
- **المجلد:** `C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\`
- **التقنية:** MAXScript + C# Revit 2026 Bridge Add-in.
- **الوظيفة:** تحويل موديلات 3ds Max (2025/2026/2027) بضغطة زر إلى فاميلي ريفيت أصلية (`.rfa`) أو موديل مشروع (`.rvt`) مع مطابقة الـ Pivot، وتصنيف الفاميلي والقوالب الإنجليزية، والحفاظ على طبقات وخامات الـ Materials دون تشويه الـ Topology.

### 3) نظام CAD-AI (أتمتة الأوتوكاد بالذكاء الاصطناعي)
- **المجلد:** `C:\Users\faqih\.gemini\antigravity\scratch\CAD-AI\`
- **التقنية:** Python 3, `pyautocad`, Mistral API.
- **المعمارية:** بنية **SET** (Standards, Engine, Tools) لضمان دقة تنفيذية قطعية 100% في بيئة الأوتوكاد مع توجيه ذكي للمخططات وتوليد الطبقات والرسومات.

### 4) مدقق الكود السعودي وبلدي (SbcBaladyChecker)
- **المجلد:** `C:\Users\faqih\.gemini\antigravity\scratch\SbcBaladyChecker\`
- **التقنية:** C# / Revit API / .NET 8.0.
- **الوظيفة:** التحقق الآلي من الارتدادات النظامية، ونسب البناء (FAR)، والتغطية، واشتراطات كود البناء السعودي (SBC 201 معماري و SBC 801 حريق) ولوائح منصة بلدي.

### 5) البوابة الرقمية التفاعلية (goat-bim-web)
- **المجلد:** `C:\Users\faqih\.gemini\antigravity\scratch\goat-bim-web\`
- **التقنية:** Next.js, React, Three.js / WebGL, Tailwind CSS.
- **الهوية البصرية:** طراز سويسري باوهاوس فاخر (Dark Luxury Architectural)، خطوط Cairo و IBM Plex Sans Arabic، عارض 3D تفاعلي للنماذج المعمارية ولوحة تحكم مركزية.

### 6) العروض والمقترحات والتوثيق
- **`zan_yanbu_proposal\`**: العرض الفني، والمالي، والجدول الزمني (32 أسبوع) لمشروع ينبع.
- **`school_quotation\`**: عرض السعر وحصر الكميات المعماري لمدرسة الشيخ عصام.
- **`abdelaziz_osman_cv\`**: السيرة الذاتية المهنية التنفيذية للمهندس عبد العزيز بصيغ Word, PDF, LaTeX, HTML.
- **`.agents\skills\`**: مكتبة ضخمة تضم أكثر من 160 مهارة متخصصة في الهندسة المعمارية، وتنسيق BIM، ونظم الإظهار، والبرمجة.

---

## 🔄 4. كيف تجعل Antigravity و Claude مربوطين ومتزامنين دائماً؟

نظام التشغيل وموقع الملفات هو **نقطة الحقيقة الواحدة المشتركة (Single Source of Truth)**. الملفات لا تُخزن في سحابة وهمية، بل على القرص الصلب مباشرة:
`C:\Users\faqih\.gemini\antigravity\scratch\`

لربط Claude مع هذه البيئة، أمامك **3 طرق عملية**:

### الطريقة الأولى (الأقوى والأفضل): استخدام Claude Code (CLI)
إذا كنت تستخدم أداة سطر الأوامر الرسمية من أنثروبيك **`claude`**:
1. افتح الـ Terminal (PowerShell) وانتقل للمجلد:
   ```powershell
   cd C:\Users\faqih\.gemini\antigravity\scratch
   claude
   ```
2. سيقرأ Claude فوراً ملف **`CLAUDE.md`** الموجود في جذر المجلد تلقائياً دون أي إعداد يدوي!
3. **النتيجة:** أي كود يكتبه أو يعدله كلود سيتعدل في نفس الملفات فوراً، وأي كود يعدله Antigravity سيقرأه كلود في اللحظة التالية مباشرة.

### الطريقة الثانية: استخدام Claude Desktop مع الـ MCP Filesystem
إذا كنت تستخدم تطبيق سطح المكتب **Claude Desktop**:
1. افتح إعدادات التطبيق أو عدل الملف: `%APPDATA%\Claude\claude_desktop_config.json`
2. أضف الـ Filesystem MCP Server لربطه بنفس المجلد:
   ```json
   {
     "mcpServers": {
       "antigravity-scratch": {
         "command": "npx",
         "args": [
           "-y",
           "@modelcontextprotocol/server-filesystem",
           "C:\\Users\\faqih\\.gemini\\antigravity\\scratch"
         ]
       }
     }
   }
   ```
3. أعد تشغيل Claude Desktop، وستجد أن كلود أصبح قادراً على قراءة وتعديل جميع مشاريعك في هذا المجلد مباشرة!

### الطريقة الثالثة: استخدام Claude.ai في المتصفح (مشاريع Claude Projects)
إذا كنت تعمل على موقع Claude.ai:
1. أنشئ مشروعاً جديداً (Project) باسم **`GOAT BIM & AEC Automation`**.
2. في قسم الـ **Project Knowledge**، قم برفع هذا الملف (`CLAUDE_MASTER_HANDOVER.md`) وملف (`CLAUDE.md`).
3. في خانة **Project Instructions**، انسخ والصق النص الموجود في القسم 5 أدناه.

---

## 📋 5. نص البداية الجاهز للنسخ إلى كلود (Kickoff Prompt for Claude)

> **انسخ هذا النص وضعه في أول رسالة لكلود:**
>
> "مرحباً كلود. أنت الآن تعمل كالمساعد المعماري والبرمجي الأول للمهندس عبد العزيز عثمان (أبو ياسين) - Senior Architectural & BIM Automation Developer.
> بيئة العمل والمشاريع التي نعمل عليها موجودة بالكامل على المسار المحلي:
> `C:\Users\faqih\.gemini\antigravity\scratch`
> 
> نحن نعمل بالتبادل بينك وبين Antigravity (Gemini) على نفس الملفات والأكواد مباشرة.
> أهم المشاريع النشطة حالياً:
> 1. إضافة **GOAT BIM Architectural Suite** للريفيت (Revit 2026/2027 بـ C# و .NET 10.0 WPF - 15 أداة معمارية كاملة).
> 2. أداة **MaxToRevit Bridge Pro** للتحويل بين 3ds Max والريفيت.
> 3. نظام **CAD-AI** لأتمتة الأوتوكاد ببنية SET.
> 4. أداة **SbcBaladyChecker** لفحص الكود السعودي وبلدي.
> 5. منصة **goat-bim-web** التفاعلية (Three.js / Next.js).
>
> الرجاء مراجعة ملف `CLAUDE.md` في جذر المجلد لفهم قواعد كتابة أكواد الـ Revit API (معاملات TransactionGroup، الفصل بين واجهات Modeless و ExternalEvent، وانعدام الـ Dependencies)، وملف `PROJECTS_SHARED_CHANGELOG.md` لمعرفة آخر التحديثات التي تمت."

---

## 📝 6. بروتوكول التنسيق المشترك (Shared Protocol)
- يوجد ملف اسمه **`PROJECTS_SHARED_CHANGELOG.md`** في جذر المجلد.
- كلما أنجزت أنت أو كلود أو Antigravity أي مهمة برمجية أو معمارية، يتم تدوين ملخص سريع بها هناك ليعرف الطرف الآخر ما تم فوراً.
