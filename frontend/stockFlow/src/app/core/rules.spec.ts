import { describe,it,expect } from 'vitest';import { actionsFor,canWriteDocument,decimalValid,ifMatch } from './rules';
describe('document permissions and workflow',()=>{
 it('allows Admin on all document families',()=>{for(const kind of ['purchase-orders','receipts','openings','issues','transfers'] as const)expect(canWriteDocument('Admin',kind)).toBe(true);});
 it('limits purchasing and warehouse roles to their workflows',()=>{expect(canWriteDocument('PurchasingOfficer','receipts')).toBe(true);expect(canWriteDocument('PurchasingOfficer','issues')).toBe(false);expect(canWriteDocument('WarehouseKeeper','transfers')).toBe(true);expect(canWriteDocument('WarehouseKeeper','purchase-orders')).toBe(false);expect(canWriteDocument('Viewer','openings')).toBe(false);});
 it('never exposes actions on final documents',()=>{for(const status of ['Posted','Received'])for(const kind of ['purchase-orders','receipts','issues'] as const)expect(actionsFor(kind,status)).toEqual([]);});
 it('only receipts can reopen cancelled documents',()=>{expect(actionsFor('receipts','Cancelled')).toEqual(['reopen']);expect(actionsFor('issues','Cancelled')).toEqual([]);});
 it('requires submit for PO drafts and post for stock drafts',()=>{expect(actionsFor('purchase-orders','Draft')).toEqual(['submit','cancel']);expect(actionsFor('issues','Draft')).toEqual(['post','cancel']);});
 it('quotes versions for If-Match and refuses missing versions',()=>{expect(ifMatch('AAAAAAAAB9E=')).toBe('"AAAAAAAAB9E="');expect(()=>ifMatch('')).toThrow();});
 it('accepts decimal precision without accepting fractional overflow',()=>{expect(decimalValid(1.123,3)).toBe(true);expect(decimalValid(1.1234,3)).toBe(false);expect(decimalValid(0,3)).toBe(false);expect(decimalValid(-1,3)).toBe(false);expect(decimalValid(1.23,2,false)).toBe(true);expect(decimalValid(1.234,2,false)).toBe(false);expect(decimalValid(0,2,false)).toBe(true);expect(decimalValid(NaN,3)).toBe(false);expect(decimalValid(1e15,3)).toBe(false);});
});
