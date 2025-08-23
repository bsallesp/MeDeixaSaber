import { ApplicationConfig } from '@angular/core';
import { providePrimeNG } from 'primeng/config';
import Lara from '@primeuix/themes/lara';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    provideTranslateService({ fallbackLang: 'pt' }),
    ...provideTranslateHttpLoader({ prefix: 'assets/i18n/', suffix: '.json' }),
    providePrimeNG({ theme: { preset: Lara, options: { darkModeSelector: false } } })
  ]
};
