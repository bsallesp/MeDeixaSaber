import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs/operators';

const etagStore = new Map<string, { etag: string; body: any }>();

export const etagInterceptor: HttpInterceptorFn = (req, next) => {
  const key = req.method.toUpperCase() + ' ' + req.urlWithParams;
  const cached = etagStore.get(key);
  const headers: Record<string, string> = {};
  if (cached?.etag) headers['If-None-Match'] = cached.etag;
  const clone = Object.keys(headers).length ? req.clone({ setHeaders: headers }) : req;
  return next(clone).pipe(
    map(event => {
      if (event instanceof HttpResponse) {
        const etag = event.headers.get('ETag');
        if (etag && event.status === 200) etagStore.set(key, { etag, body: event.body });
        if (event.status === 304 && cached) return new HttpResponse({ status: 200, body: cached.body });
      }
      return event;
    })
  );
}
