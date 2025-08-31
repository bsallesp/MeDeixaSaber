import { setupHttpTest } from '../../testing';
import { hmacInterceptor } from './hmac.interceptor';

describe('hmacInterceptor', () => {
  it('adiciona headers HMAC em /api/classifieds', (done) => {
    const { http, ctrl } = setupHttpTest([hmacInterceptor]);
    http.get('/api/classifieds/top').subscribe(() => done());
    const req = ctrl.expectOne('/api/classifieds/top');
    expect(req.request.headers.has('X-Api-Key')).toBeTrue();
    expect(req.request.headers.has('X-Signature')).toBeTrue();
    expect(req.request.headers.has('X-Timestamp')).toBeTrue();
    expect(req.request.headers.has('X-Nonce')).toBeTrue();
    req.flush({});
    ctrl.verify();
  });

  it('nao toca requests fora de /api/classifieds', (done) => {
    const { http, ctrl } = setupHttpTest([hmacInterceptor]);
    http.get('/api/ping').subscribe(() => done());
    const req = ctrl.expectOne('/api/ping');
    expect(req.request.headers.has('X-Api-Key')).toBeFalse();
    req.flush({});
    ctrl.verify();
  });
});
