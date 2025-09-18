import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { PowService } from '../services/pow.service';

function needsPow(url: string, method: string): boolean {
  return method.toUpperCase() === 'GET' && url.includes('/api/news/top');
}

export const powInterceptor: HttpInterceptorFn = (req, next) => {
  if (!needsPow(req.url, req.method)) {
    return next(req);
  }

  const powService = inject(PowService);

  const work = async () => {
    const token = await powService.mintPow(12);
    return req.clone({ setHeaders: { 'X-PoW': token } });
  };

  return from(work()).pipe(switchMap(clone => next(clone)));
};
