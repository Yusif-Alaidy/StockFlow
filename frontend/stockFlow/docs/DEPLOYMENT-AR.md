# نشر الواجهة

## الطريق المباشر: استضافة static منفصلة

1. شغّل `npm ci` ثم `npm test` ثم `npm run build` داخل frontend. يمكن استخدام build المرفق مباشرة لو المصدر لم يتغير.
2. افتح `dist/stockflow/browser/app-config.json` وتأكد من رابط الباك.
3. ارفع محتويات `dist/stockflow/browser` إلى موقع static، مثل manual deploy في Netlify. ارفع الفولدر الذي يحتوي `index.html` مباشرة.
4. ملف `_redirects` المرفق يعيد مسارات Angular إلى `index.html` على الاستضافات التي تدعم هذا التنسيق.
5. بعد الحصول على رابط HTTPS، أضفه إلى `Cors__AllowedOrigins__1` في الباك، ثم أعد تشغيل الباك.
6. افتح رابط الفرونت، سجل الدخول، ثم افتح `/products` مباشرة واعمل Refresh للتأكد من SPA fallback.
7. جرّب مستندًا ببيانات مخصصة للاختبار، وتحقق من الرصيد والحركة في التقارير.

لم يتم نشر الواجهة على حساب استضافة ضمن هذا التسليم.

## النشر من Git

لو الريبو يحتوي `backend` و`frontend`:

- Base/Root directory: `frontend`.
- Install: `npm ci`.
- Build: `npm run build`.
- Publish/Output directory: `dist/stockflow/browser` بالنسبة إلى frontend.
- Node.js: 24 LTS.

Vercel: ملف `vercel.json` المرفق يضبط إعادة مسارات الواجهة. اختر frontend كـ Root Directory واضبط Output Directory كما سبق.

## IIS / MonsterASP

ملف `web.config` موجود في public وينسخ إلى build، ويحتوي قاعدة Angular fallback. يحتاج URL Rewrite وstatic files support في الاستضافة.

استخدم موقع frontend منفصلًا. **لا ترفع web.config الخاص بالفرونت فوق web.config الخاص بموقع API الحالي**: الملفان لهما وظيفتان مختلفتان وقد توقف الباك بذلك.

إذا أردت الاثنين في نفس الموقع لاحقًا، فده محتاج إعداد منفصل في ASP.NET لاستضافة static files مع استثناء `/api` و`/health` من fallback؛ ليس ضمن هذه الحزمة.

## تغيير الرابط بعد البناء

عدّل `app-config.json` في ملفات الموقع المنشور. لا تحتاج إعادة build لتغيير apiBaseUrl. في حالة وجود CDN تأكد من عدم تخزين هذا الملف طويلًا؛ `_headers` المرفق يضبط no-store على الاستضافات الداعمة.

المشروع معد للنشر على جذر الدومين `/`. النشر تحت مسار مثل `/stockflow/` يحتاج build مع `--base-href /stockflow/` وتعديل fallback وموضع ملفات الإعداد طبقًا للاستضافة.

## تشخيص سريع

- صفحة 404 بعد Refresh: SPA fallback غير مفعل.
- Http status 0 في Angular مع نجاح الطلب في Postman: راجع CORS وHTTPS ورابط الباك من Network في المتصفح.
- 401: راجع حساب الدخول أو انتهاء الجلسة.
- 403: الحساب ليس له صلاحية للعملية.
- 404 لسجل بعينه: قد يكون خارج مخازن المستخدم.
- 409: حالة مستند غير مناسبة أو رصيد غير كافٍ أو بيانات متعارضة؛ اقرأ code/detail.
- 412: حدّث القائمة ثم افتح السجل من جديد؛ لا تعد إرسال تعديل قديم تلقائيًا.
- 500: راجع traceId في رسالة الخطأ مع logs الباك.

لا تضع أسرارًا أو متغيرات داتا بيز في frontend. كل ما داخل frontend/build قابل للقراءة في المتصفح.
