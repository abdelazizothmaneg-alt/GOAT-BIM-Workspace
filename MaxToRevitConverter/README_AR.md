# MaxToRevit Bridge Pro — أداة تحويل ملفات 3ds Max إلى Revit (.RFA / .RVT)

تم بناء وتثبيت الأداة بالكامل على جهازك لـ **3ds Max (2025 / 2026 / 2027)** و **Autodesk Revit 2026**.

---

## 📂 مسارات ملفات الأداة

1. **سكريبت 3ds Max الرئيسي (جاهز للسحب والإفلات المباشر في الماكس):**
   - `C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\MaxScript\MaxToRevitConverter.ms`
2. **كود وإضافة Revit 2026 Bridge Add-in:**
   - الكود المصدري: `C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\RevitAddin\MaxToRevitBridge.cs`
   - الإضافة المثبتة في الريفيت: `%APPDATA%\Autodesk\Revit\Addins\2026\MaxToRevitBridge.addin`
3. **مثبت الأداة التلقائي:**
   - `C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter\Install_MaxToRevit.bat`

---

## 🚀 طريقة الاستخدام خطوة بخطوة

### الطريقة الأولى: السحب والإفلات (الأسرع)
1. افتح **3ds Max** وحدد المجسمات اللي عايز تحولها.
2. اسحب ملف [`MaxToRevitConverter.ms`](file:///C:/Users/faqih/.gemini/antigravity/scratch/MaxToRevitConverter/MaxScript/MaxToRevitConverter.ms) وأفلته داخل شاشة الـ Viewport في الماكس (أو من قائمة `Scripting -> Run Script...`).
3. ستفتح لك نافذة **MaxToRevit Bridge Pro**.

### الطريقة الثانية: كزرار دائم (Toolbar Button) في 3ds Max
تم تثبيت الماكرو تلقائياً في نسخ الماكس عندك (2025 و 2026 و 2027). لإظهار الزرار في شريط الأدوات:
1. من داخل 3ds Max افتح: `Customize -> Customize User Interface...`
2. اختر تبويب **Toolbars**، ومن قائمة **Category** اختر: `MaxToRevit Bridge`.
3. اسحب `MaxToRevit` وحطه في أي شريط أدوات (Toolbar) عندك.

---

## ⚙️ خطوات التحويل داخل الواجهة

1. **Output Format & Target Revit Version:**
   - اختر `Revit Family (.rfa)` أو `Revit Project Model (.rvt)` أو `Direct CAD/BIM (.dxf / .sat)`.
   - اختر قالب الفاميلي **Family Template (`.rft`)** من مكتبة الريفيت الإنجليزية المثبتة على جهازك (مثل `Metric Furniture.rft`, `Metric Generic Model wall based.rft`, `Metric Lighting Fixture ceiling based.rft`... إلخ).
2. **Family Category & Parameters (العمود الأيمن):**
   - يتغير الـ **Category** تلقائياً بمجرد اختيار القالب، ويمكنك تغييره يدوياً لأي تصنيف من التصنيفات (مع مربع بحث سريع وفلتر `Architecture / Structure / MEP`).
   - حدد خصائص الـ **Family Parameters** (`Work Plane-Based`, `Always vertical`, `Cut with Voids When Loaded`, `Can host rebar`, `Shared Family`, `Room Calculation Point`).
3. **إعدادات المجسم والماتريال والـ Pivot:**
   - **Export Scope:** تحويل العناصر المحددة لملف واحد، أو **Batch Mode** (كل عنصر محدد يتحول لملف `.rfa` مستقل باسمه)، أو المشهد بالكامل.
   - **Pivot / Host Alignment:** يضبط نقطة الارتكاز `(0,0,0)` تلقائياً حسب نوع القالب (في المنتصف السفلي للأرضيات/الأثاث، أو الخلف للحوائط `Wall-based`، أو الأعلى للأسقف `Ceiling-based`).
   - **Material & Geometry Mode:** يفصل أجزاء الموديل حسب الـ Material إلى Layers/Subcategories داخل الريفيت وينشئ Materials في الريفيت بنفس ألوان الـ RGB في الماكس.
   - **ProOptimizer:** لتخفيف عدد الـ Polygons على نسخة مؤقتة أثناء التصدير دون المساس بالمجسم الأصلي في الماكس.
4. **Convert & Save to Revit:**
   - اختر مكان واسم الحفظ من `Browse...` ثم اضغط **Convert & Save to Revit**.
   - إذا كان **Revit 2026** مفتوحاً، سيقوم الجسر (Bridge) تلقائياً في الخلفية بإنشاء وحفظ ملف الـ `.rfa` أو `.rvt` في المسار الذي اخترته خلال ثوانٍ! وإذا كان مغلقاً، سيسألك لتشغيله وتوليد الملف أوتوماتيكياً.
   - *(ملاحظة عند فتح Revit 2026 لأول مرة: إذا ظهرت رسالة أمان `Security - Unsigned Add-In` الخاصة بـ `MaxToRevit Bridge Pro`، اضغط **Always Load**).*
