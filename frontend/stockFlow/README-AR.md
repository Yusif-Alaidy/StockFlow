# StockFlow Frontend

واجهة Angular عربية RTL لإدارة مخزون شركة واحدة بعدة مخازن. المصدر كامل ونسخة production الجاهزة للنشر موجودة في `dist/stockflow/browser`.

## أحط الملفات فين؟

فك الضغط وانقل فولدر `frontend` إلى:

`D:\Programing\Projects\StockFlow\frontend`

بحيث يظل `backend` و`frontend` بجانب بعض. لو عندك frontend قديم احفظ نسخة منه الأول. لا تنقل هذه الملفات داخل مشروع الـ API ولا تستبدل ملفات الباك.

## التشغيل

ثبّت Node.js 24 LTS، ثم افتح Terminal داخل `frontend`:

```powershell
npm ci
npm start
```

افتح `http://localhost:4200`. لا تحتاج تثبيت Angular CLI عالميًا.

الواجهة متصلة افتراضيًا بـ:

`https://stock-flow.runasp.net/api/v1`

سجل الدخول بحساب أنشأته بالفعل في الباك. لا يوجد حساب أو باسورد افتراضي داخل الفرونت، ولا signup عام. إذا لم تنشئ الأدمن بعد فأنشئه بأمر bootstrap الخاص بالباك أولًا.

## الاختبار والبناء

```powershell
npm test
npm run build
```

ارفع **محتويات** `dist/stockflow/browser` إلى استضافة frontend منفصلة. لا ترفع node_modules أو ملفات TypeScript.

## الوظائف

- Login، تجديد جلسة تلقائي، Logout وتغيير الباسورد.
- لوحة تحكم ببيانات فعلية من الباك.
- الفروع والمخازن والتصنيفات والوحدات والأصناف والموردون: قائمة وبحث وإضافة وتعديل وتفعيل وتعطيل حسب الصلاحيات.
- الأرصدة الافتتاحية، أوامر الشراء، الاستلام الكامل، الصرف والتحويل: إنشاء وتفاصيل وتعديل مسودة والإجراءات المتاحة حسب نوع المستند وحالته.
- أرصدة المخزون والحركات ومطابقة الأرصدة وسجل النشاط.
- إدارة المستخدمين، الأدوار والمخازن المسموحة للأدمن.
- Pagination، رسائل الأخطاء ورقم traceId، تأكيد قبل الترحيل والإلغاء، وIf-Match لمنع الكتابة فوق تعديل مستخدم آخر.
- واجهة responsive مع قائمة جانبية للموبايل وخط عربي محلي بدون طلب Google Fonts أثناء التشغيل.

## رابط الباك

للتطوير أو قبل build: عدّل `public/app-config.json`.

لنسخة build موجودة بالفعل: عدّل `dist/stockflow/browser/app-config.json`.

```json
{
  "apiBaseUrl": "https://stock-flow.runasp.net/api/v1"
}
```

هذا الملف يحتوي رابطًا عامًا فقط؛ لا تضف connection string أو باسورد أو secret فيه.

## CORS — خطوة مطلوبة بعد النشر

في إعدادات موقع الباك، أضف رابط الفرونت الفعلي كـ origin مثل:

```text
Cors__AllowedOrigins__0 = http://localhost:4200
Cors__AllowedOrigins__1 = https://YOUR-FRONTEND-DOMAIN
```

اكتب scheme + domain + port إن وُجد، بدون مسار وبدون `/` أخيرة. أعد تشغيل موقع الباك بعد الحفظ. لا تستخدم `*`.

## اقرأ كمان

- `docs/DEPLOYMENT-AR.md`: النشر خطوة بخطوة وSPA fallback.
- `docs/STRUCTURE-AR.md`: تقسيم الملفات وكيف تعدّلها.
- `docs/API-MAP.md`: الشاشات والـ endpoints وقواعد التوافق.
- `docs/MANUAL-TESTS.md`: تجربة دورة عمل حقيقية قبل عرض المشروع.
- `docs/VERIFICATION.md`: ما تم اختباره وحدود الاختبار.

الواجهة جاهزة للبناء والنشر، لكن اختبار دورة حقيقية على الاستضافة بحسابك يظل مطلوبًا؛ لم يتم استخدام حساباتك أو تعديل قاعدة بياناتك أثناء تجهيزها.
