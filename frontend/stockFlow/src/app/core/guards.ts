import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from './auth.service';
export const authGuard:CanActivateFn=(_,state)=>inject(Auth).user()?true:inject(Router).createUrlTree(['/login'],{queryParams:{returnUrl:state.url}});
export const adminGuard:CanActivateFn=()=>inject(Auth).user()?.role==='Admin'?true:inject(Router).createUrlTree(['/dashboard']);
