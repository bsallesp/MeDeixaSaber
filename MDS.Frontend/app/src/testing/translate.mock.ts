import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<any> {
    return of({});
  }
}

export const I18nTestingModule = TranslateModule.forRoot({
  defaultLanguage: 'pt',
  loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
});
