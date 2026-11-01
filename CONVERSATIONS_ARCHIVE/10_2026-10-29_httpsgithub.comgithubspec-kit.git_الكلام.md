# محادثة رقم 10: https://github.com/github/spec-kit.git  الكلام على ايه   https://www.youtube.com/watch?v=GwqBMP4qs4U  <ADDITIONAL_METADA
- **Conversation ID:** `036efc8c-f573-4110-aae3-8f3d292095dc`
- **Start Date:** `2026-10-29T20:31:55Z`
- **Total Turns:** 6

---

### 👤 User (2026-10-29T20:31:55Z)
https://github.com/github/spec-kit.git

الكلام على ايه 

https://www.youtube.com/watch?v=GwqBMP4qs4U

<ADDITIONAL_METADATA>
The current local time is: 2026-10-29T23:31:55+03:00.
</ADDITIONAL_METADATA>
<USER_SETTINGS_CHANGE>
The user changed setting `Model Selection` from None to Gemini 3.8 Flash (High). No need to comment on this change if the user doesn't ask about it. If reporting what model you are, please use a human readable name instead of the exact string.
</USER_SETTINGS_CHANGE>

---

### 👤 Antigravity (Gemini) (2026-10-29T20:32:30Z)
**الكلام باختصار:** 

الموضوع كله بيدور حول الانتقال من **البرمجة العشوائية (Vibe Coding)** — اللي هي إنك تفضل ترزع برومبتات للذكاء الاصطناعي على عماها وتفضل ترقّع وراه لما يخرّف — إلى أسلوب هندسي منظم اسمه **التطوير المبني على المواصفات (Spec-Driven Development أو SDD)**.

الفيديو ده شرح عربي تفصيلي لمستودع مفتوح المصدر من GitHub اسمه [**Spec-Kit**](https://github.com/github/spec-kit).

---

### المشكلة اللي الأداة بتحلها:
لو جربت تبني مشروع متوسط أو كبير بـ Claude Code أو Cursor أو Gemini أو غيرهم، غالباً بتلاحظ الآتي:
1. **فقدان السياق (Context Drift):** بعد شوية كلام، الـ AI بينسى الاتفاقات المعمارية القديمة ويبدأ يخترع حلول متناقضة مع الكود القديم.
2. **الهلوسة وافتراض متطلبات من عنده:** بيقعد يفترض حاجات توديك في داهية تقنية (Technical Debt).
3. **حرق التوكنز والوقت:** محادثات طويلة رايحة جاية عشان تعدل غلطات هو سببها.

---

### إيه فكرة `Spec-Kit`؟
بدل ما الـ AI يبدأ يكتب كود علطول، **Spec-Kit** بتفرض workflow هندسي صارم بيخلي "ملفات المواصفات المكتوبة" هي المرجع الوحيد (Single Source of Truth) مش شات الـ AI المتطاير:

```
الدستور (Constitution) ➔ المواصفات (Spec) ➔ الخطة التقنية (Plan) ➔ قائمة المهام (Tasks) ➔ التنفيذ الفعلي (Implement)
```

1. **الدستور (Constitution):** بتحدد فيه القواعد الصارمة للمشروع (مثل: المكتبات المسموحة، معايير الأمان، أسلوب كتابة الكود) اللي الـ AI ممنوع يخالفها إطلاقاً.
2. **المواصفات (Specify - What & Why):** بتكتب بلغة بسيطة الميزة اللي أنت عايز تبنيها وليه محتاجها، بدون ما تشغل بالك بالتفاصيل البرمجية الدقيقة. الـ AI بيحولها لوثيقة مواصفات رسمية (`.spec`).
3. **الخطة الهندسية (Plan):** الـ AI بيبني Blueprint تقني معماري للميزة دي.
4. **تفكيك المهام (Tasks):** الخطة بتتحول لمهام خطوة بخطوة، وهنا في **Human Checkpoint**؛ أنت بتراجع المهام وتعدل فيها قبل ما يتكتب سطر كود واحد.
5. **التنفيذ (Implement):** دلوقتي بس الـ AI بيمسك المهام تاسك ورا تاسك وينفذها بدقة لأنه ملتزم بالوثائق مش بالتخمين.

---

### طريقة البدء السريعة:
الأداة بتنزل كـ CLI اسمها `specify-cli` (غالباً باستخدام `uv`):

```bash
# تثبيت الأداة
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git

# تهيئة المشروع واختيار أداة الـ AI المفضلة (مثل Claude أو Copilot أو Cursor)
specify init my-project --ai claude
```

**الخلاصة:** الفيديو والمستودع بيقدموا "كتالوج" يخلي الذكاء الاصطناعي يشتغل معاك كـ **Junior Developer بيسمع الكلام وبيتبع مواصفات معمارية صحيحة**، بدل ما تسيبه يسوق المشروع بعشوائية.

---

### 👤 User (2026-10-29T20:38:29Z)
ثبت الأداه 

<ADDITIONAL_METADATA>
The current local time is: 2026-10-29T23:38:29+03:00.
</ADDITIONAL_METADATA>

---

### 👤 Antigravity (Gemini) (2026-10-29T20:39:00Z)
بدأت بتثبيت المتطلبات وأداة `uv` أولاً للبدء في تثبيت `specify-cli`. جاري التنزيل والتثبيت حالياً... سأوافيك بالنتيجة فور الانتهاء.

---

### 👤 Antigravity (Gemini) (2026-10-29T20:41:55Z)
تم تثبيت `uv` بنجاح، والآن جاري تثبيت أداة `specify-cli` مباشرة من مستودع GitHub (`github/spec-kit`). سأبلغك بمجرد اكتمال التثبيت.

---

### 👤 Antigravity (Gemini) (2026-10-29T20:42:53Z)
تم تثبيت الأداة بنجاح وتجهيزها للاستخدام! 🎉

---

### ما تم تنفيذه:
1. **تثبيت مدير الحزم السريع `uv`** لبيئة بايثون لديك.
2. **تثبيت `specify-cli` (Spec Kit)** مباشرة من مستودع GitHub الرسمي (`github/spec-kit`).
3. **إضافة المسار للـ PATH** (`C:\Users\faqih\.local\bin`) ليصبح أمر `specify` متاحاً من أي مكان في النظام.
4. **التحقق من التثبيت:**
   - الإصدار المثبت: `specify 1.2.6.dev0`
   - حالة الفحص: `Specify CLI is ready to use!`

---

### طريقة الاستخدام والبدء:

إذا أردت إنشاء مشروع جديد مجهز بـ Spec Kit، افتح الـ Terminal (PowerShell أو CMD) واكتب:

```bash
# لإنشاء مشروع جديد (مثلاً باسم my-app)
specify init my-app
```

أو إذا كان لديك مشروع قائم بالفعل وتريد تفعيل Spec Kit داخله:

```bash
# داخل مجلد مشروعك الحالي
specify init .
```

سيطلب منك تحديد أداة الـ AI التي تعمل بها (مثل **Cursor**، **GitHub Copilot**، **Claude Code**، أو **VS Code**)، وسيقوم تلقائياً بإنشاء قوالب المواصفات والدستور (`.specify/`) للبدء في الـ Spec-Driven Development.

---

