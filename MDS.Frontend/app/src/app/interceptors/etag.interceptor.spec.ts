import { setupHttpTest } from '../../testing';
import { etagInterceptor } from './etag.interceptor';

describe('etagInterceptor', () => {
  it('guarda ETag e envia If-None-Match', (done) => {
    const { http, ctrl } = setupHttpTest([etagInterceptor]);

    http.get('/api/news/top').subscribe(() => {
      http.get('/api/news/top').subscribe(() => done());
      const req2 = ctrl.expectOne('/api/news/top');
      expect(req2.request.headers.get('If-None-Match')).toBe('"etag-1"');
      req2.flush({}, { status: 304, statusText: 'Not Modified' });
    });

    const req1 = ctrl.expectOne('/api/news/top');
    expect(req1.request.headers.has('If-None-Match')).toBeFalse();
    req1.flush({ items: [] }, { headers: { ETag: '"etag-1"' } });
    ctrl.verify();
  });
});
