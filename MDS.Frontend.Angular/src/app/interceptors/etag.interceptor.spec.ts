import { TestBed } from "@angular/core/testing";
import { HttpClient } from "@angular/common/http";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { etagInterceptor } from "./etag.interceptor";

describe("etagInterceptor", () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([etagInterceptor])),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("guarda ETag da resposta e envia If-None-Match no request seguinte", () => {
    const url = "/api/news/top";

    http.get(url).subscribe();

    const req1 = httpMock.expectOne(url);
    expect(req1.request.headers.has("If-None-Match")).toBeFalse();
    req1.flush([{ ok: true }], { headers: { ETag: "\"abc123\"" } as any });

    http.get(url).subscribe();
    const req2 = httpMock.expectOne(url);
    expect(req2.request.headers.get("If-None-Match")).toBe("\"abc123\"");
    req2.flush([]);
  });
});
