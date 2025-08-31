import { TestBed } from "@angular/core/testing";
import { HttpClient } from "@angular/common/http";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { HttpInterceptorFn } from "@angular/common/http";

export function setupHttpTest(interceptors: HttpInterceptorFn[]) {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors(interceptors)),
      provideHttpClientTesting()
    ]
  });

  const http = TestBed.inject(HttpClient);
  const ctrl = TestBed.inject(HttpTestingController);
  return { http, ctrl };
}
