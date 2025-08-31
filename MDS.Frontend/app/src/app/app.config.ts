import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { providePrimeNG } from 'primeng/config';
import Lara from '@primeng/themes/lara';
import { hmacInterceptor } from './interceptors/hmac.interceptor';
import { powInterceptor } from './interceptors/pow.interceptor';
import { etagInterceptor } from './interceptors/etag.interceptor';
import { retry429Interceptor } from './interceptors/retry429.interceptor';

export function httpTranslateLoader(http: HttpClient) {
  return new TranslateHttpLoader(http, 'assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withHashLocation()),
    provideHttpClient(withInterceptors([retry429Interceptor, etagInterceptor, powInterceptor, hmacInterceptor])),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'pt',
        loader: { provide: TranslateLoader, useFactory: httpTranslateLoader, deps: [HttpClient] }
      })
    ),
    providePrimeNG({ theme: { preset: Lara, options: { darkModeSelector: false } } })
  ]
};
