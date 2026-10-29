import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { API_URL } from './config';
import { Tokens, User } from './models';
@Injectable({providedIn:'root'}) export class Auth {
 private http=inject(HttpClient); private router=inject(Router); private base=inject(API_URL); private tokens:Tokens|null=null; private refreshTask:Promise<Tokens>|null=null;
 user=signal<User|null>(null); ready=signal(false); private epoch=0;
 get accessToken(){return this.tokens?.accessToken??null;}
 private apply(tokens:Tokens){this.tokens=tokens;this.user.set(tokens.user);sessionStorage.setItem('stockflow.session',JSON.stringify(tokens));}
 clear(){this.epoch++;this.tokens=null;this.user.set(null);sessionStorage.removeItem('stockflow.session');}
 async initialize(){try{const saved=sessionStorage.getItem('stockflow.session');if(saved){const parsed=JSON.parse(saved) as Tokens;if(!parsed.refreshToken||!parsed.user)throw new Error('Invalid session');this.tokens=parsed;await this.refresh();this.user.set(await firstValueFrom(this.http.get<User>(this.base+'/auth/me')));}}catch{this.clear();}finally{this.ready.set(true);}}
 async login(email:string,password:string){const t=await firstValueFrom(this.http.post<Tokens>(this.base+'/auth/login',{email,password}));this.epoch++;this.apply(t);}
 refresh():Promise<Tokens>{
  if(this.refreshTask)return this.refreshTask;
  if(!this.tokens)return Promise.reject(new Error('No session'));
  const generation=this.epoch;const token=this.tokens.refreshToken;
  this.refreshTask=firstValueFrom(this.http.post<Tokens>(this.base+'/auth/refresh',{refreshToken:token})).then(t=>{if(generation!==this.epoch)throw new Error('Session changed');this.apply(t);return t;}).catch(e=>{if(generation===this.epoch)this.clear();throw e;}).finally(()=>this.refreshTask=null);
  return this.refreshTask;
 }
 async logout(){try{await firstValueFrom(this.http.post(this.base+'/auth/logout',{}));}finally{this.clear();await this.router.navigateByUrl('/login');}}
 async changePassword(currentPassword:string,newPassword:string){await firstValueFrom(this.http.post(this.base+'/auth/change-password',{currentPassword,newPassword}));this.clear();await this.router.navigateByUrl('/login');}
 expired(){this.clear();void this.router.navigateByUrl('/login');}
}
