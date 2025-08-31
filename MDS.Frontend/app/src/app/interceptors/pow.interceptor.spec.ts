import { setupHttpTest } from '../../testing';
import { powInterceptor } from './pow.interceptor';

describe('powInterceptor', () => {
  it('adiciona X-PoW em GET /api/news/top', (done) => {
    const { http, ctrl } = setupHttpTest([powInterceptor]);
    http.get('/api/news/top').subscribe(() => done());
    const req = ctrl.expectOne('/api/news/top');
    expect(req.request.headers.has('X-PoW')).toBeTrue();
    req.flush({});
    ctrl.verify();
  });

  it('nao altera POST /api/news/top', (done) => {
    const { http, ctrl } = setupHttpTest([powInterceptor]);
    http.post('/api/news/top', {}).subscribe(() => done());
    const req = ctrl.expectOne('/api/news/top');
    expect(req.request.headers.has('X-PoW')).toBeFalse();
    req.flush({});
    ctrl.verify();
  });
});
