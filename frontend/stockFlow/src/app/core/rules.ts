import { DocKind, Role } from './models';
export const roleLabels:Record<Role,string>={Admin:'مدير النظام',PurchasingOfficer:'مسؤول المشتريات',WarehouseKeeper:'أمين المخزن',Viewer:'عرض فقط'};
export const titles:Record<string,string>={dashboard:'نظرة عامة',products:'الأصناف والكتالوج',categories:'التصنيفات',units:'وحدات القياس',suppliers:'الموردون',branches:'الفروع',warehouses:'المخازن','purchase-orders':'أوامر الشراء',openings:'الأرصدة الافتتاحية',receipts:'استلام المشتريات',issues:'صرف المخزون',transfers:'التحويلات',balances:'أرصدة المخزون',movements:'حركة المخزون',reconciliation:'مطابقة الأرصدة',audit:'سجل النشاط',users:'المستخدمون',account:'حسابي'};
export const statusLabels:Record<string,string>={Draft:'مسودة',Submitted:'بانتظار الاستلام',Received:'مستلم',Posted:'مرحّل',Cancelled:'ملغي'};
export function canWriteDocument(role:Role|undefined,kind:DocKind):boolean { return role==='Admin'||(role==='PurchasingOfficer'&&(kind==='purchase-orders'||kind==='receipts'))||(role==='WarehouseKeeper'&&kind!=='purchase-orders'&&kind!=='receipts'); }
export function actionsFor(kind:DocKind,status:string):string[] {
 if(kind==='purchase-orders') return status==='Draft'?['submit','cancel']:status==='Submitted'?['cancel']:[];
 if(kind==='receipts'&&status==='Cancelled') return ['reopen'];
 return status==='Draft'?['post','cancel']:[];
}
export function ifMatch(version:string):string { if(!version) throw new Error('Missing version'); return '"'+version+'"'; }
export function decimalValid(value:number,places:number,positive=true):boolean { return Number.isFinite(value)&&Number.isSafeInteger(Math.round(value*10**places))&&(positive?value>0:value>=0)&&Math.abs(value*10**places-Math.round(value*10**places))<0.00001; }
