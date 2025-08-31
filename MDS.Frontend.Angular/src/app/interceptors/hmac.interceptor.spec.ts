import { TestBed, fakeAsync, flushMicrotasks, tick, flush } from "@angular/core/testing";
import { HttpClient } from "@angular/common/http";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { hmacInterceptor } from "./hmac.interceptor";

function installCryptoHmacMock() {
  const mock = {
    getRandomValues: (arr: Uint8Array) => { for (let i = 0; i < arr.length; i++) arr[i] = i & 0xff; return arr; },
    subtle: {
      importKey: async () => ({}),
      sign: async () => { const buf = new ArrayBuffer(32); new Uint8Array(buf).fill(2); return buf; },
      digest: async (_algo: string, data: ArrayBuffer) => {
        const buf = new ArrayBuffer(32);
        const out = new Uint8Array(buf);
        const inp = new Uint8Array(data);
        for (let i = 0; i < out.length; i++) out[i] = (inp[i % inp.length] + i) & 0xff;
        return buf;
      }
    }
  } as any;
  spyOnProperty(window as any, "crypto", "get").and.returnValue(mock);
}

describe("hmacInterceptor", () => {
  let http: HttpClient;
  let ctrl: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    installCryptoHmacMock();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([hmacInterceptor] as any)),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    ctrl = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    ctrl.verify();
  });

  it("adiciona headers HMAC em /api/classifieds/top", fakeAsync(() => {
    http.get("/api/classifieds/top").subscribe();
    flushMicrotasks(); tick(1); flush();

    const req = ctrl.expectOne(r => r.url.endsWith("/api/classifieds/top"));
    expect(req.request.headers.has("X-Api-Key")).toBeTrue();
    expect(req.request.headers.has("X-Timestamp")).toBeTrue();
    expect(req.request.headers.has("X-Nonce")).toBeTrue();
    expect(req.request.headers.has("X-Signature")).toBeTrue();
    req.flush({});
  }));

  it("nao toca requests fora de /api/*", fakeAsync(() => {
    http.get("/ping").subscribe();
    flushMicrotasks(); tick(1); flush();

    const req = ctrl.expectOne(r => r.url.endsWith("/ping"));
    expect(req.request.headers.has("X-Api-Key")).toBeFalse();
    expect(req.request.headers.has("X-Signature")).toBeFalse();
    req.flush({});
  }));
});
