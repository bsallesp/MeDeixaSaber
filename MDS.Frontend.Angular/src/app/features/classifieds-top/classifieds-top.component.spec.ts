import { TestBed } from '@angular/core/testing';
import ClassifiedsTopComponent from './classifieds-top.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

describe('ClassifiedsTopComponent', () => {
  let ctrl: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClassifiedsTopComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    ctrl = TestBed.inject(HttpTestingController);
  });

  afterEach(() => ctrl.verify());

  it('deve criar e carregar lista', () => {
    const fixture = TestBed.createComponent(ClassifiedsTopComponent);
    fixture.detectChanges();
    const req = ctrl.expectOne('/api/classifieds/top?take=20&skip=0');
    req.flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
