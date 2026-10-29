import { AfterViewInit, Directive, ElementRef, HostListener, OnDestroy, inject } from '@angular/core';
// Keep keyboard navigation inside the topmost open dialog; restore focus on close.
@Directive({selector:'[sfFocusTrap]',standalone:true})
export class FocusTrap implements AfterViewInit,OnDestroy {
 private static stack:FocusTrap[]=[];private host=inject<ElementRef<HTMLElement>>(ElementRef);private previous=document.activeElement as HTMLElement|null;
 private controls(){return Array.from(this.host.nativeElement.querySelectorAll<HTMLElement>('button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),a[href],[tabindex="0"]')).filter(x=>x.getClientRects().length>0);}
 ngAfterViewInit(){FocusTrap.stack.push(this);queueMicrotask(()=>{if(FocusTrap.stack.at(-1)===this)this.controls()[0]?.focus();});}
 @HostListener('document:keydown',['$event']) key(event:KeyboardEvent){if(event.key!=='Tab'||FocusTrap.stack.at(-1)!==this)return;const items=this.controls(),first=items[0],last=items.at(-1);if(!first)return;if(event.shiftKey&&(document.activeElement===first||!this.host.nativeElement.contains(document.activeElement))){event.preventDefault();last?.focus();}else if(!event.shiftKey&&(document.activeElement===last||!this.host.nativeElement.contains(document.activeElement))){event.preventDefault();first.focus();}}
 ngOnDestroy(){FocusTrap.stack=FocusTrap.stack.filter(x=>x!==this);if(this.previous?.isConnected)this.previous.focus();}
}
