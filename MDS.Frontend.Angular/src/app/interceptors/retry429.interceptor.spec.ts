import { setupHttpTest } from '../../testing';
import { retry429Interceptor } from './retry429.interceptor';
import { fakeAsync, tick } from '@angular/core/testing';

describe('retry429Interceptor', () => {
  it('faz retry exponencial em 429 e depois sucesso', fakeAsync(() => {
    const { http, ctrl } = setupHttpTest([retry429Interceptor]);
    let completed = false;
    http.get('/api/ping').subscribe(() => {
      completed = true;
    });
    const req1 = ctrl.expectOne('/api/ping');
    req1.flush({}, { status: 429, statusText: 'Too Many Requests' });
    tick(500);
    const req2 = ctrl.expectOne('/api/ping');
    req2.flush({}, { status: 429, statusText: 'Too Many Requests' });
    tick(1000);
    const req3 = ctrl.expectOne('/api/ping');
    req3.flush({}, { status: 429, statusText: 'Too Many Requests' });
    tick(2000);
    const req4 = ctrl.expectOne('/api/ping');
    req4.flush({});
    expect(completed).toBeTrue();
    ctrl.verify();
  }));
});
