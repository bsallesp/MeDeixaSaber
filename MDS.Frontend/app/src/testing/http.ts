import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpInterceptorFn } from '@angular/common/http';

export function setupHttpTest(interceptors: HttpInterceptorFn[]) {
  TestBed.configureTestingModule({
    imports: [HttpClientTestingModule],
    providers: [provideHttpClient(withInterceptors(interceptors))]
  });
  const http = TestBed.inject(HttpClient);
  const ctrl = TestBed.inject(HttpTestingController);
  return { http, ctrl };
}
