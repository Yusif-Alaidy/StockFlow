import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { API_URL } from './config';
import { Master, MasterInput, MasterKind, Page, StockDocument, DocKind, DocumentInput, Dashboard, ReportRow, User } from './models';
import { ifMatch } from './rules';
@Injectable({providedIn:'root'}) export class Api {
 private http=inject(HttpClient); private base=inject(API_URL);
 get<T>(path:string,query:Record<string,string|number|boolean|null|undefined>={}){let params=new HttpParams();for(const [k,v] of Object.entries(query))if(v!==''&&v!==null&&v!==undefined)params=params.set(k,v);return firstValueFrom(this.http.get<T>(this.base+'/'+path,{params}));}
 post<T>(path:string,body:unknown={},version?:string){return firstValueFrom(this.http.post<T>(this.base+'/'+path,body,{headers:version?{'If-Match':ifMatch(version)}:{}}));}
 put<T>(path:string,body:unknown,version:string){return firstValueFrom(this.http.put<T>(this.base+'/'+path,body,{headers:{'If-Match':ifMatch(version)}}));}
 masters(kind:MasterKind,pageNumber=1,search='',active?:boolean){return this.get<Page<Master>>(kind,{pageNumber,pageSize:20,search,active});}
 async allMasters(kind:MasterKind):Promise<Master[]>{const rows:Master[]=[];let n=1;for(;;){const p=await this.get<Page<Master>>(kind,{pageNumber:n++,pageSize:100});rows.push(...p.items);if(rows.length>=p.totalCount||p.items.length===0)return rows;}}
 saveMaster(kind:MasterKind,data:MasterInput,old?:Master){return old?this.put<Master>(kind+'/'+old.id,data,old.version):this.post<Master>(kind,data);}
 documents(kind:DocKind,pageNumber=1,warehouseId='',status=''){return this.get<Page<StockDocument>>(kind,{pageNumber,pageSize:20,warehouseId,status});}
 document(kind:DocKind,id:string){return this.get<StockDocument>(kind+'/'+id);}
 saveDocument(kind:DocKind,input:DocumentInput,old?:StockDocument){return old?this.put<StockDocument>(kind+'/'+old.id,{notes:input.notes,lines:input.lines},old.version):this.post<StockDocument>(kind,input);}
 action(doc:StockDocument,action:string){return this.post<StockDocument>(doc.kind+'/'+doc.id+'/'+action,{},doc.version);}
 dashboard(){return this.get<Dashboard>('reports/dashboard');}
 report(kind:string,query:Record<string,string|number>){return this.get<Page<ReportRow>>('reports/'+kind,query);}
 users(pageNumber=1){return this.get<Page<User>>('users',{pageNumber,pageSize:20});}
}
