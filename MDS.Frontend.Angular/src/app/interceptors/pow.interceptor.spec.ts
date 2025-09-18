import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { powInterceptor } from './pow.interceptor';
import { PowService } from '../services/pow.service';

describe('powInterceptor', () => {
  let http: HttpClient;
  let httpTestingController: HttpTestingController;
  let powServiceSpy: jasmine.SpyObj<PowService>;

  beforeEach(() => {
    powServiceSpy = jasmine.createSpyObj('PowService', ['mintPow']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([powInterceptor])),
        provideHttpClientTesting(),
        { provide: PowService, useValue: powServiceSpy }
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should add X-PoW header to /api/news/top requests', fakeAsync(() => {
    const fakeToken = 'v1:fake-timestamp:fake-nonce';
    powServiceSpy.mintPow.and.returnValue(Promise.resolve(fakeToken));

    const testUrl = '/api/news/top?pageSize=10';

    http.get(testUrl).subscribe();

    tick();

    const req = httpTestingController.expectOne(testUrl);
    req.flush([]);

    expect(req.request.headers.has('X-PoW')).toBe(true);
    expect(req.request.headers.get('X-PoW')).toBe(fakeToken);
  }));

  it('should NOT add X-PoW header to other requests', () => {
    const testUrl = '/api/classifieds/top';

    http.get(testUrl).subscribe();

    const req = httpTestingController.expectOne(testUrl);
    req.flush([]);

    expect(req.request.headers.has('X-PoW')).toBe(false);
  });
});
