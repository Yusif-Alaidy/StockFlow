export type Role = 'Admin' | 'PurchasingOfficer' | 'WarehouseKeeper' | 'Viewer';
export interface Page<T> { items:T[]; pageNumber:number; pageSize:number; totalCount:number; }
export interface User { id:string; email:string; role:Role; isActive:boolean; warehouseIds:string[]; version:string; }
export interface Tokens { accessToken:string; accessExpiresAt:string; refreshToken:string; refreshExpiresAt:string; user:User; }
export interface Master { id:string; name:string; code:string|null; isActive:boolean; branchId:string|null; categoryId:string|null; unitId:string|null; email:string|null; phone:string|null; address:string|null; version:string; }
export interface MasterInput { name:string; code?:string|null; branchId?:string|null; categoryId?:string|null; unitId?:string|null; email?:string|null; phone?:string|null; address?:string|null; }
export type MasterKind='products'|'categories'|'units'|'suppliers'|'branches'|'warehouses';
export type DocKind='purchase-orders'|'openings'|'receipts'|'issues'|'transfers';
export interface Line { productId:string; quantity:number; unitPrice:number; id?:string; }
export interface StockDocument { id:string; number:string; kind:DocKind; status:string; warehouseId:string; destinationWarehouseId:string|null; supplierId:string|null; purchaseOrderId:string|null; reason:string|null; notes:string|null; createdAt:string; lines:Line[]; version:string; }
export interface DocumentInput { warehouseId:string; supplierId:string|null; destinationWarehouseId:string|null; reason:string|null; notes:string|null; lines:Line[]; }
export interface Dashboard { products:number; warehouses:number; draftDocuments:number; stockedPairs:number; }
export interface ReportRow { warehouseId?:string; productId?:string; quantity?:number; documentType?:string; documentId?:string; occurredAt?:string; balance?:number; ledger?:number; difference?:number; id?:string; actorId?:string; action?:string; resource?:string; resourceId?:string; at?:string; }
export interface Problem { title?:string; detail?:string; code?:string; traceId?:string; errors?:Record<string,string[]>; }
