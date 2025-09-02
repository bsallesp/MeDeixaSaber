import {ApplicationConfig} from '@angular/core'
import {provideRouter} from '@angular/router'
import {routes} from './app.routes'
import {provideHttpClient, withInterceptors} from '@angular/common/http'
import {powInterceptor} from './interceptors/pow.interceptor'
import {hmacInterceptor} from './interceptors/hmac.interceptor'
import {etagInterceptor} from './interceptors/etag.interceptor'
import {retry429Interceptor} from './interceptors/retry429.interceptor'
import {providePrimeNG} from 'primeng/config'
import MdsPreset from '../theme/mds-preset'

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([
      powInterceptor,
      hmacInterceptor,
      etagInterceptor,
      retry429Interceptor
    ])),
    providePrimeNG({
      theme: {
        preset: MdsPreset,
        options: {
          prefix: 'p',
          cssLayer: true
        }
      }
    })
  ]
}
