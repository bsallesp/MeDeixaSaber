import { TestBed } from "@angular/core/testing";
import { HttpClient } from "@angular/common/http";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";

export function setupHttpTest(interceptors: any[] = []) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors(interceptors as any)),
      provideHttpClientTesting()
    ]
  });
  const http = TestBed.inject(HttpClient);
  const ctrl = TestBed.inject(HttpTestingController);
  return { http, ctrl };
}
