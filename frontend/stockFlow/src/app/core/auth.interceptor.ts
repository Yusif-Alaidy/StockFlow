import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { Auth } from './auth.service';
import { API_URL } from './config';
export const authInterceptor:HttpInterceptorFn=(req,next)=>{
 const auth=inject(Auth);const base=inject(API_URL);
 if(!req.url.startsWith(base+'/'))return next(req);
 const anonymous=req.url===base+'/auth/login'||req.url===base+'/auth/refresh';
 const sentToken=auth.accessToken;
 const authorized=!anonymous&&sentToken?req.clone({setHeaders:{Authorization:'Bearer '+sentToken}}):req;
 return next(authorized).pipe(catchError((error:unknown)=>{
  if(!(error instanceof HttpErrorResponse)||error.status!==401||anonymous||!sentToken)return throwError(()=>error);
  // A parallel request may already have refreshed this session.
  const refresh=auth.accessToken&&auth.accessToken!==sentToken?Promise.resolve():auth.refresh();
  return from(refresh).pipe(switchMap(()=>next(req.clone({setHeaders:{Authorization:'Bearer '+auth.accessToken}}))),catchError(e=>{if(e instanceof HttpErrorResponse&&e.status===401)auth.expired();return throwError(()=>e);}));
 }));
};
