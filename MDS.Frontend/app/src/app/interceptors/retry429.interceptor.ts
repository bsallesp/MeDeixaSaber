import { HttpInterceptorFn } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { retryWhen, mergeMap, delay } from 'rxjs/operators';

export const retry429Interceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    retryWhen(errors =>
      errors.pipe(
        mergeMap((err: any, i) => {
          const shouldRetry = err?.status === 429 && i < 3;
          if (shouldRetry) return of(err).pipe(delay(Math.pow(2, i) * 500));
          return throwError(() => err);
        })
      )
    )
  );
}
