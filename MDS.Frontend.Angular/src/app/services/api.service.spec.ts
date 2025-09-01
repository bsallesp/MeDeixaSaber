import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ApiService } from './api.service';

describe('ApiService', () => {
  let svc: ApiService;
  let httpCtrl: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ApiService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    svc = TestBed.inject(ApiService);
    httpCtrl = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpCtrl.verify());

  it('getNewsTop deve chamar /api/news/top', () => {
    svc.getNewsTop(10, 0).subscribe(x => expect(x).toEqual([]));
    const req = httpCtrl.expectOne('/api/news/top?pageSize=10');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getClassifiedsTop deve chamar /api/classifieds/top', () => {
    svc.getClassifiedsTop(20, 3).subscribe(x => expect(x).toEqual([]));
    const req = httpCtrl.expectOne('/api/classifieds/top?take=20&skip=3');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
