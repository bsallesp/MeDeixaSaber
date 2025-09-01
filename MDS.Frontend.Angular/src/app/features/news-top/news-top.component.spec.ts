import { TestBed } from '@angular/core/testing';
import NewsTopComponent from './news-top.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

describe('NewsTopComponent', () => {
  let ctrl: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewsTopComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    ctrl = TestBed.inject(HttpTestingController);
  });

  afterEach(() => ctrl.verify());

  it('deve criar e carregar lista', () => {
    const fixture = TestBed.createComponent(NewsTopComponent);
    fixture.detectChanges();
    const req = ctrl.expectOne('/api/news/top?pageSize=20');
    req.flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
