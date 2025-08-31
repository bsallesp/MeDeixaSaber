import { TestBed, fakeAsync, flushMicrotasks, tick, flush } from "@angular/core/testing";
import { HttpClient } from "@angular/common/http";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { powInterceptor } from "./pow.interceptor";

function installCryptoPowMock() {
  const mock = {
    getRandomValues: (arr: Uint8Array) => { for (let i = 0; i < arr.length; i++) arr[i] = 1; return arr; },
    subtle: {
      digest: async () => { const buf = new ArrayBuffer(32); new Uint8Array(buf).fill(0); return buf; }
    }
  } as any;
  spyOnProperty(window as any, "crypto", "get").and.returnValue(mock);
}

describe("powInterceptor", () => {
  let http: HttpClient;
  let ctrl: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    installCryptoPowMock();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([powInterceptor] as any)),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    ctrl = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    ctrl.verify();
  });

  it("adiciona X-PoW em GET /api/news/top", fakeAsync(() => {
    http.get("/api/news/top").subscribe();
    flushMicrotasks(); tick(1); flush();

    const req = ctrl.expectOne(r => r.url.endsWith("/api/news/top"));
    expect(req.request.headers.has("X-PoW")).toBeTrue();
    req.flush({});
  }));

  it("nao altera POST /api/news/top", fakeAsync(() => {
    http.post("/api/news/top", {}).subscribe();
    flushMicrotasks(); tick(1); flush();

    const req = ctrl.expectOne(r => r.url.endsWith("/api/news/top"));
    expect(req.request.headers.has("X-PoW")).toBeFalse();
    req.flush({});
  }));
});
