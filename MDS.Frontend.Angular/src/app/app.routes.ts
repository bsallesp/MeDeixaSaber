// src/app/app.routes.ts
import { Routes } from '@angular/router'

export const routes: Routes = [
  { path: '', redirectTo: 'news', pathMatch: 'full' },
  { path: 'news', loadComponent: () => import('./features/news-top/news-top.component') },
  { path: 'classifieds', loadComponent: () => import('./features/classifieds-top/classifieds-top.component') }
]
