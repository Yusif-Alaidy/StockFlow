import { Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Problem } from './models';
export function describeError(error:unknown):string {
 if(!(error instanceof HttpErrorResponse)) return 'تعذر إكمال العملية. حاول مرة أخرى.';
 const p=(error.error??{}) as Problem;
 const messages:Record<number,string>={0:'تعذر الاتصال بالخادم. تحقق من الإنترنت وإعدادات CORS.',400:'راجع البيانات المدخلة.',401:'انتهت الجلسة أو بيانات الدخول غير صحيحة.',403:'ليس لديك صلاحية لهذه العملية.',404:'العنصر غير موجود أو خارج المخازن المسموح بها.',409:'العملية تتعارض مع حالة البيانات أو الرصيد الحالي.',412:'البيانات اتغيرت بواسطة مستخدم آخر. حدّث القائمة وافتح السجل من جديد.',428:'نسخة السجل مطلوبة. حدّث البيانات وأعد فتحها.',429:'طلبات كثيرة. انتظر قليلًا ثم حاول.',500:'حدث خطأ في الخادم. تواصل مع المسؤول باستخدام رقم التتبع.'};
 const validation=p.errors?Object.values(p.errors).flat().join(' • '):'';
 return [messages[error.status]??'تعذر إكمال الطلب.',p.detail,validation,p.code?'الكود: '+p.code:'',p.traceId?'رقم التتبع: '+p.traceId:''].filter(Boolean).join(' — ');
}
@Injectable({providedIn:'root'}) export class Feedback {
 message=signal(''); kind=signal<'success'|'error'>('success'); private timer?:ReturnType<typeof setTimeout>;
 show(message:string,kind:'success'|'error'='success'){clearTimeout(this.timer);this.message.set(message);this.kind.set(kind);this.timer=setTimeout(()=>this.message.set(''),kind==='error'?14000:5000);}
 error(e:unknown){this.show(describeError(e),'error');}
}
